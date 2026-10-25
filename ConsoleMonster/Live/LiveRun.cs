using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    /// <summary>
    /// Monitors an existing runner without scheduling its work or changing its results. Completed items are appended
    /// once to scrollback; a bounded dashboard shows active work and problems. All update methods are safe from worker threads.
    /// </summary>
    public sealed partial class LiveRun : IDisposable {
        private readonly object _gate = new object();
        private readonly RunTracker _tracker;
        private readonly LiveSession _session;
        private readonly LiveRunOptions _options;
        private readonly Queue<RunItemSnapshot> _held = new Queue<RunItemSnapshot>();
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);
        private readonly Thread? _pulse;
        private readonly int _progressEvery;
        private readonly TimeSpan _progressInterval;
        private TimeSpan _lastProgress;
        private int _sinceProgress;
        private bool _complete;

        /// <summary>Starts monitoring the declared work. No item is executed by this constructor.</summary>
        /// <param name="items">Names, targets and optional categories in input order.</param>
        /// <param name="options">Display configuration, or null for defaults.</param>
        public LiveRun(IEnumerable<RunItem> items, LiveRunOptions? options = null) {
            _options = options ?? new LiveRunOptions();
            if (_options.ProgressEvery < 0 || _options.ProgressInterval < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(options)); }
            _progressEvery = _options.ProgressEvery; _progressInterval = _options.ProgressInterval;
            _tracker = new RunTracker(items, _options.Clock);
            Dashboard = new RunDashboard(_tracker) { Title = _options.Title, ShowGroups = _options.ShowGroups, ShowTrend = _options.ShowTrend };
            LiveSessionOptions session = _options.Session ?? new LiveSessionOptions();
            session.Mode = LiveMode.Inline; session.InlineHeightFraction = _options.HeightPercent / 100.0;
            session.StopOnCancellation = false;
            session.FramesPerSecond = 10; session.StreamHeartbeat = TimeSpan.Zero;
            session.Summary = new RunSummary(_tracker) { Title = _options.Title, ReportPath = _options.ReportPath };
            session.LogTarget = Dashboard.Log.WidgetId;
            if (!_options.Live && session.Output?.SupportsLiveFrames == true) { throw new ArgumentException("NoLive requires an append-only output.", nameof(options)); }
            session.AppendOnly = !_options.Live;
            _session = new LiveSession(Dashboard, session);
            _session.Start();
            IsLive = _session.SupportsLiveFrames;
            if (!IsLive) { _session.Write(_options.Title); }
            if (_progressInterval > TimeSpan.Zero || (_options.InteractiveKeys && IsLive)) {
                _pulse = new Thread(() => Pulse(session)) { IsBackground = true, Name = "ConsoleMonster run input" };
                _pulse.Start();
            }
        }

        /// <summary>The live presentation; its log retains only its configured capacity, while scrollback receives every completion.</summary>
        public RunDashboard Dashboard { get; }
        /// <summary>The serialized output session, including its cooperative cancellation token and diagnostics.</summary>
        internal LiveSession Session => _session;
        /// <summary>Cancelled by Ctrl+C or <see cref="Cancel"/>. Producers should observe it and report their last results before Complete.</summary>
        public CancellationToken Cancellation => _session.Cancellation;
        /// <summary>A fatal rendering or output failure, including final summary and flushing failures. Worker results remain in the structured snapshot.</summary>
        public Exception? OutputFailure => _session.Failure;
        /// <summary>Signals cooperative cancellation while leaving completion output open.</summary>
        public void Cancel() => _session.Cancel();
        /// <summary>Waits for previously posted output to reach the target. After Complete, waits for final output and restoration, including deferred cleanup. Hosts can then drain their output on their pipeline thread.</summary>
        /// <param name="timeout">Maximum wait.</param>
        /// <returns>Whether output was flushed.</returns>
        public bool Flush(TimeSpan timeout) {
            LiveSession.CheckFlushTimeout(timeout);
            System.Threading.Tasks.Task<bool> signal;
            // Queue behind the last published completion, then release producers before waiting on their output sink.
            lock (_gate) { signal = _complete ? _session.OutputStopped : _session.QueueFlush(); }
            return signal.Wait(timeout) && signal.Result;
        }
        /// <summary>Whether the target supports live redraw for this run.</summary>
        public bool IsLive { get; }
        /// <summary>A consistent immutable observation of the run.</summary>
        /// <returns>Items, totals, running phases and throughput.</returns>
        public RunSnapshot Snapshot() => _tracker.Snapshot();

        /// <summary>Starts one item or changes its running phase. Duplicate starts preserve its original start time.</summary>
        /// <param name="index">Zero-based input position.</param>
        /// <param name="phase">Literal phase text.</param>
        public void Started(int index, string? phase = null) {
            lock (_gate) { if (!_complete) { _tracker.StartItem(index, phase); } }
        }

        /// <summary>Completes an item once. Repeated completion notifications are ignored.</summary>
        /// <param name="index">Zero-based input position.</param>
        /// <param name="outcome">Pass, Fail, Warn or Skip.</param>
        /// <param name="message">Full literal result detail.</param>
        /// <param name="duration">Explicit elapsed time, or null to use the item's running clock.</param>
        public void Finished(int index, StatusKind outcome, string? message = null, TimeSpan? duration = null) {
            lock (_gate) {
                if (_complete) { return; }
                RunItemSnapshot? item = _tracker.FinishItem(index, outcome, message, duration);
                if (item == null) { return; }
                if (OutputFailure != null) { return; }
                if (Dashboard.LogPaused) { _held.Enqueue(item); }
                else { Append(item); }
                _sinceProgress++;
                if (!IsLive && _progressEvery > 0 && _sinceProgress >= _progressEvery) { Progress(); }
            }
        }

        private void Append(RunItemSnapshot item, bool force = false) {
            if (!force && Dashboard.ProblemsOnly && item.Status != StatusKind.Fail && item.Status != StatusKind.Warn) { return; }
            _session.Post(new LiveEvent(Dashboard.Log.WidgetId, "write", text: item.ToString(), status: item.Status == StatusKind.Fail ? "error" : item.Status.ToString().ToLowerInvariant()) { Payload = new RunCompletion(item) });
        }

        private void Progress() {
            RunSnapshot snapshot = _tracker.Snapshot();
            _lastProgress = snapshot.Elapsed; _sinceProgress = 0;
            _session.Post(new LiveEvent(Dashboard.Log.WidgetId, "write", text: "progress " + snapshot.ProgressLine()) {
                Payload = new RunProgress(snapshot)
            });
        }

        /// <summary>Applies an optional dashboard key. This changes display only; work continues.</summary>
        /// <param name="key">p, f, g or ?.</param>
        /// <returns>Whether the key was handled.</returns>
        public bool HandleKey(char key) {
            lock (_gate) {
                if (_complete || !IsLive) { return false; }
                switch (char.ToLowerInvariant(key)) {
                    case 'p':
                        Dashboard.LogPaused = !Dashboard.LogPaused;
                        if (!Dashboard.LogPaused) { while (_held.Count > 0) { Append(_held.Dequeue(), true); } }
                        return true;
                    case 'f': Dashboard.ProblemsOnly = !Dashboard.ProblemsOnly; return true;
                    case 'g': Dashboard.ShowGroups = !Dashboard.ShowGroups; return true;
                    case '?': Dashboard.ShowHelp = !Dashboard.ShowHelp; return true;
                    default: return false;
                }
            }
        }

        private void Pulse(LiveSessionOptions session) {
            InputLease? input = null;
            try {
                if (_options.InteractiveKeys && IsLive) {
                    try {
                        input = ConsoleInput.Acquire((session.Target ?? Terminal.Target).Input);
                        if (input.Source is WindowsConsoleInput windows) { windows.EnableCancelKeyProcessing(); }
                        else if (input.Source is ConsoleKeyInput console) { console.EnableCancelKeyProcessing(); }
                        Dashboard.ShowKeys = true;
                    } catch (Exception ex) when (ex is InputClosedException || ex is InvalidOperationException || ex is System.IO.IOException) {
                        input?.Dispose(); input = null; /* A host without usable input still gets the dashboard. */
                    }
                }
                while (!_stop.WaitOne(100)) {
                    if (input != null) {
                        try { InputEvent? key = input.Source.Read(TimeSpan.Zero, CancellationToken.None); if (key?.IsText == true) { HandleKey(key.Char); } }
                        catch (Exception ex) when (ex is InputClosedException || ex is System.IO.IOException || ex is InvalidOperationException) { input.Dispose(); input = null; Dashboard.ShowKeys = false; }
                    }
                    lock (_gate) {
                        if (_complete) { break; }
                        if (!IsLive && _progressInterval > TimeSpan.Zero && _tracker.Snapshot().Elapsed - _lastProgress >= _progressInterval) { Progress(); }
                    }
                }
            } finally { input?.Dispose(); }
        }

        /// <summary>Marks unfinished items skipped, flushes held completion lines and prints the full final summary. Idempotent.</summary>
        /// <returns>The final structured state. The runner's own results and exit code are unaffected.</returns>
        /// <exception cref="InvalidOperationException">Output failed. The complete structured state is retained by Snapshot.</exception>
        public RunSnapshot Complete() {
            lock (_gate) {
                if (_complete) { return _tracker.Snapshot(); }
                foreach (RunItemSnapshot item in _tracker.Snapshot().Items.Where(item => !RunTracker.IsTerminal(item.Status))) { Finished(item.Index, StatusKind.Skip, "Run ended before completion"); }
                while (_held.Count > 0) { var held = _held.Dequeue(); if (OutputFailure == null) { Append(held, true); } }
                _tracker.Complete(); _complete = true; _stop.Set();
            }
            _pulse?.Join();
            _session.Dispose();
            _stop.Dispose();
            ThrowIfFailed();
            return _tracker.Snapshot();
        }
        private void ThrowIfFailed() {
            if (_session.Failure != null) { throw new InvalidOperationException("Run output stopped before completion. The structured snapshot remains available.", _session.Failure); }
        }
        /// <inheritdoc />
        public void Dispose() { Complete(); }
    }
}
