using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Owns a single cancellable action and a bounded log for one app lifetime. Selection and UI are never read on a worker.</summary>
    internal sealed class AppActionSession : IDisposable {
        private readonly object _gate = new object();
        private readonly Queue<LogEntry> _output = new Queue<LogEntry>();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly CancellationTokenSource _lifetime;
        private CancellationTokenSource? _operation;
        private Task? _work;
        private bool _closed;
        private long _revision;
        internal AppDispatcher Dispatcher { get; }
        internal AppActionSession(CancellationToken token, AppDispatcher? dispatcher = null) { Dispatcher = dispatcher ?? new AppDispatcher(); _lifetime = CancellationTokenSource.CreateLinkedTokenSource(token); }
        internal bool IsBusy { get { lock (_gate) { return _work != null && !_work.IsCompleted; } } }
        internal long Revision { get { lock (_gate) { return _revision; } } }
        internal IReadOnlyList<LogEntry> Output { get { lock (_gate) { return _output.ToArray(); } } }

        internal bool Start(AppCommand command, IReadOnlyList<object?> rows) {
            lock (_gate) {
                if (_closed || (_work != null && !_work.IsCompleted)) { return false; }
                _operation?.Dispose();
                _operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                CancellationToken token = _operation.Token;
                var captured = Array.AsReadOnly(rows.ToArray());
                _work = Task.Run(async () => {
                    try {
                        token.ThrowIfCancellationRequested();
                        await command.Action(new AppActionContext(captured, Write), token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        Write(command.Label + ": completed", "pass");
                    } catch (OperationCanceledException) when (token.IsCancellationRequested) { Write(command.Label + ": cancelled", "warn"); }
                    catch (Exception error) { Write(command.Label + ": " + error.Message, "err"); }
                });
                return true;
            }
        }

        internal void Cancel() {
            CancellationTokenSource? operation;
            lock (_gate) { operation = _closed ? null : _operation; }
            try { operation?.Cancel(); } catch (ObjectDisposedException) { }
        }

        private void Write(string text, string level) {
            lock (_gate) {
                if (_closed) { return; }
                if (text.Length > 16384) { text = text.Substring(0, 16384) + " [truncated]"; }
                foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
                    if (_output.Count == 1000) { _output.Dequeue(); }
                    _output.Enqueue(new LogEntry(_clock.Elapsed, level, line.Length > 16384 ? line.Substring(0, 16384) : line));
                }
                _revision++;
            }
        }

        public void Dispose() {
            Task? work;
            lock (_gate) { if (_closed) { return; } _closed = true; work = _work; }
            Dispatcher.Dispose();
            _lifetime.Cancel();
            // A noncooperative backend cannot keep the terminal leased indefinitely.
            if (work != null && !work.IsCompleted) { try { work.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { } }
            if (work == null || work.IsCompleted) { _operation?.Dispose(); _lifetime.Dispose(); }
            else { _ = work.ContinueWith(_ => { _operation?.Dispose(); _lifetime.Dispose(); }, TaskScheduler.Default); }
        }
    }
}
