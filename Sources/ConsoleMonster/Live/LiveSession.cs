using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.Live {
    /// <summary>
    /// Single-use live renderer. Producers only enqueue; one background thread owns widget updates.
    /// Stop closes admission, joins that thread, drains accepted events and restores the terminal.
    /// </summary>
    public sealed partial class LiveSession : ILiveDispatcher, IDisposable {
        private static readonly object CurrentGate = new object();
        private static readonly List<LiveSession> Running = new List<LiveSession>();
        private static LiveSession? _captureOwner;
        private readonly IWidget _root;
        private readonly LiveSessionOptions _options;
        private readonly ConcurrentDictionary<string, ILiveTarget> _targets = new ConcurrentDictionary<string, ILiveTarget>(StringComparer.Ordinal);
        private readonly List<ILiveTarget> _logTargets = new List<ILiveTarget>();
        private readonly ConcurrentQueue<LiveEvent> _queue = new ConcurrentQueue<LiveEvent>();
        private readonly ConcurrentQueue<string> _unroutedLines = new ConcurrentQueue<string>();
        private readonly object _lifecycle = new object();
        private readonly object _finishGate = new object();
        private readonly object _outputGate = new object();
        private readonly ManualResetEvent _wake = new ManualResetEvent(false);
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly Stopwatch _watch = new Stopwatch();
        private Thread? _thread;
        private TerminalGuard? _guard;
        private CancellationTokenRegistration _guardRegistration;
        private ConsoleCapture? _capture;
        private ITerminalOutput? _output;
        private RenderContext? _context;
        private LiveRecorder? _recorder;
        private ILiveTarget? _logTarget;
        private CellBuffer? _frame;
        private TimeSpan _clockStart;
        private TimeSpan _stoppedElapsed;
        private double _timelineScale = 1.0; // Replay runs the displayed clock on the recorded timeline.
        private bool _streamTimestamps;
        private readonly ResizeCoalescer _resizeCoalescer = new ResizeCoalescer(TimeSpan.FromMilliseconds(100));
        private int _windowLeft;
        private int _windowTop;
        private long _resizes;
        private long _renderErrors;
        private Output.ConsoleFontWatcher? _fontWatcher;
        private bool _began; // BeginLive succeeded: only then may stopping draw a final frame and summary.
        private Exception? _failure; // The render-thread exception that ended the session early.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _unknownTargets = new System.Collections.Concurrent.ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private int _state; // 0: new, 1: running, 2: stopping, 3: stopped
        private int _admission; // Sign bit closes admission; the remaining bits count admitted publications.
        private int _invalidate;
        private bool _callerStopping;
        private bool _renderExited;
        private bool _restored;
        private bool _finishing;
        private long _eventsPosted;
        private long _eventsApplied;
        private long _eventsDropped;
        private long _framesRendered;
        private long _applyErrors;
        private Exception? _lastError;

        /// <summary>Discovers targets in the widget tree. Definitions must remain stable after construction.</summary>
        /// <param name="root">Dashboard root.</param>
        /// <param name="options">Optional configuration, fixed when Start is called.</param>
        public LiveSession(IWidget root, LiveSessionOptions? options = null) {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _options = options ?? new LiveSessionOptions();
            Discover(root);
        }

        /// <summary>The root widget the session renders.</summary>
        public IWidget Root => _root;

        /// <summary>The most recently started session that is still running, across all runspaces.</summary>
        public static LiveSession? Current {
            get {
                lock (CurrentGate) {
                    for (int i = Running.Count - 1; i >= 0; i--) {
                        if (Running[i].IsRunning) { return Running[i]; }
                    }
                    return null;
                }
            }
        }

        /// <summary>Whether the session is accepting live work.</summary>
        public bool IsRunning => Volatile.Read(ref _state) == 1;

        /// <summary>Cancelled by Stop or the first intercepted Ctrl+C.</summary>
        public CancellationToken Cancellation => _cancellation.Token;

        /// <summary>The number of size changes the session redrew for (coalesced: rapid drags count once per redraw).</summary>
        public long Resizes => Interlocked.Read(ref _resizes);

        /// <summary>Frames where the layout threw and a one-line note was shown instead (the session keeps running).</summary>
        public long RenderErrors => Interlocked.Read(ref _renderErrors);

        /// <summary>
        /// The terminal width probe result for this session ("width probe (cursor report, 12 ms): ..."), the reason it was
        /// skipped, or null when probing was not applicable (Stream/Plain output, a borrowed output, or explicit width settings).
        /// </summary>
        public string? WidthDecision { get; private set; }

        /// <summary>
        /// Time since Start, frozen after shutdown. During <see cref="Replay"/> it follows the recorded timeline
        /// (real time multiplied by the replay speed), so the header clock agrees with recorded timestamps.
        /// </summary>
        public TimeSpan Elapsed {
            get {
                int state = Volatile.Read(ref _state);
                if (state == 0) {
                    return TimeSpan.Zero;
                }
                if (state == 3) {
                    return _stoppedElapsed;
                }
                return Scale(ReadElapsed());
            }
        }

        private TimeSpan RealElapsed {
            get {
                int state = Volatile.Read(ref _state);
                return state == 1 || state == 2 ? ReadElapsed() : TimeSpan.Zero;
            }
        }

        private TimeSpan Scale(TimeSpan real) {
            return _timelineScale == 1.0 ? real : TimeSpan.FromTicks((long)(real.Ticks * _timelineScale));
        }

        /// <summary>Number of events accepted before shutdown closed admission.</summary>
        public long EventsPosted => Interlocked.Read(ref _eventsPosted);

        /// <summary>Number of events successfully applied.</summary>
        public long EventsApplied => Interlocked.Read(ref _eventsApplied);

        /// <summary>Number of events ignored because the target was unknown, or admission had closed.</summary>
        public long EventsDropped => Interlocked.Read(ref _eventsDropped);

        /// <summary>Number of frames rendered, including the final frame.</summary>
        public long FramesRendered => Interlocked.Read(ref _framesRendered);

        /// <summary>Number of target Apply calls that threw.</summary>
        public long ApplyErrors => Interlocked.Read(ref _applyErrors);

        /// <summary>Most recent target, rendering, recording or cleanup exception.</summary>
        public Exception? LastError => Volatile.Read(ref _lastError);

        /// <summary>The exception that ended the session early (the render thread failed, for example on a broken output), or null.</summary>
        public Exception? Failure => Volatile.Read(ref _failure);

        /// <summary>
        /// A snapshot of the session's counters, the target names that received updates but do not exist, and the errors,
        /// so hosts can report problems that would otherwise disappear behind the live screen.
        /// </summary>
        /// <returns>The snapshot.</returns>
        public LiveSessionCounters GetCounters() {
            return new LiveSessionCounters(EventsPosted, EventsApplied, EventsDropped, ApplyErrors, RenderErrors, FramesRendered, Resizes,
                new Dictionary<string, long>(_unknownTargets, StringComparer.Ordinal), LastError, Failure);
        }

        /// <summary>A snapshot of registered target names.</summary>
        public IReadOnlyCollection<string> TargetNames => new List<string>(_targets.Keys);

        /// <summary>Registers an additional target, rejecting duplicate names and the reserved log alias.</summary>
        /// <param name="target">Target to register.</param>
        public void Register(ILiveTarget target) {
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
            lock (_lifecycle) {
                if (_state >= 2) { throw new InvalidOperationException("The session has stopped."); }
                if (string.IsNullOrEmpty(target.Name) || target.Name == "$log" || !_targets.TryAdd(target.Name, target)) {
                    throw new ArgumentException("Target names must be unique, nonempty, and must not be '$log'.", nameof(target));
                }
                if (target is ILogTarget) { _logTargets.Add(target); }
                if (_logTarget == null && target is ILogTarget && (_options.LogTarget == null || _options.LogTarget == target.Name)) {
                    Volatile.Write(ref _logTarget, target);
                }
            }
        }

        /// <summary>Looks up a registered target, including the reserved log alias.</summary>
        /// <param name="name">Target name.</param>
        /// <param name="target">Matching target, or null.</param>
        /// <returns>Whether the target exists.</returns>
        public bool TryGetTarget(string name, out ILiveTarget? target) {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }
            if (name == "$log") { target = Volatile.Read(ref _logTarget); return target != null; }
            return _targets.TryGetValue(name, out target);
        }

        /// <summary>Enqueues without locks or per-event signaling. Events may be queued before Start.</summary>
        /// <param name="liveEvent">Event; zero timestamps are stamped relative to Start.</param>
        public void Post(LiveEvent liveEvent) {
            if (liveEvent == null) { throw new ArgumentNullException(nameof(liveEvent)); }
            while (true) {
                int admission = Volatile.Read(ref _admission);
                if (admission < 0) { Interlocked.Increment(ref _eventsDropped); return; }
                if (Interlocked.CompareExchange(ref _admission, admission + 1, admission) == admission) { break; }
            }
            try {
                Enqueue(liveEvent);
            } finally { Interlocked.Decrement(ref _admission); }
        }

        /// <summary>Enqueues a line for the session log target.</summary>
        /// <param name="line">Text.</param>
        /// <param name="level">Stream or severity, default out.</param>
        public void Write(string line, string level = "out") => Post(new LiveEvent("$log", "write", text: line, state: level));

        /// <summary>Begins an ambient progress scope for this session.</summary>
        /// <param name="target">Target name.</param>
        /// <param name="key">Optional item key.</param>
        /// <returns>The scope lifetime.</returns>
        public IDisposable Scope(string target, string? key = null) => TerminalScope.Begin(this, target, key);

        /// <summary>Forces the next live tick to redraw fully.</summary>
        public void Invalidate() => Interlocked.Exchange(ref _invalidate, 1);

        /// <summary>Runs work inside a session, restoring the terminal even when work throws.</summary>
        /// <param name="work">Work to execute.</param>
        public void Run(Action<LiveSession> work) {
            if (work == null) { throw new ArgumentNullException(nameof(work)); }
            Start();
            try { work(this); }
            finally { Stop(); }
        }

        /// <summary>Runs asynchronous work with linked caller and session cancellation.</summary>
        /// <param name="work">Work to execute.</param>
        /// <param name="ct">Caller cancellation.</param>
        /// <returns>Completion, preserving work exceptions after restoration.</returns>
        public async Task RunAsync(Func<LiveSession, CancellationToken, Task> work, CancellationToken ct = default) {
            if (work == null) { throw new ArgumentNullException(nameof(work)); }
            Start();
            using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, Cancellation))
            using (ct.Register(RequestCancellation)) {
                try { await work(this, linked.Token).ConfigureAwait(false); }
                finally { Stop(); }
            }
        }

        /// <inheritdoc />
        public void Dispose() => Stop();

        private void Discover(IWidget widget) {
            if (widget is ILiveTarget target) { Register(target); }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) { Discover(child); }
            }
        }

        private TimeSpan ReadElapsed() {
            if (_context == null) { return TimeSpan.Zero; }
            TimeSpan elapsed = _options.Clock == null ? _watch.Elapsed : _options.Clock() - _clockStart;
            return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        }

        private void Enqueue(LiveEvent liveEvent) {
            _queue.Enqueue(liveEvent.Time == 0 ? liveEvent.WithTime((long)Elapsed.TotalMilliseconds) : liveEvent);
            Interlocked.Increment(ref _eventsPosted);
        }

        private void Error(Exception error) => Volatile.Write(ref _lastError, error);

        private void CloseAdmission() {
            while (true) {
                int admission = Volatile.Read(ref _admission);
                if (admission < 0 || Interlocked.CompareExchange(ref _admission, admission | int.MinValue, admission) == admission) { return; }
            }
        }

        private void ResolveLogTarget() {
            ILiveTarget? selected = null;
            foreach (ILiveTarget candidate in _logTargets) {
                if (_options.LogTarget == null || candidate.Name == _options.LogTarget) { selected = candidate; break; }
            }
            Volatile.Write(ref _logTarget, selected);
        }
    }
}
