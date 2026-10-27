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
        private readonly Func<TimeSpan>? _actionClock;
        private TimeSpan _clockStart;
        private readonly CancellationTokenSource _lifetime;
        private CancellationTokenSource? _operation;
        private Task? _work;
        private bool _closed;
        private long _revision;
        private long _completionRevision;
        private AppCommand? _completedCommand;
        private AppCommand? _operationCommand;
        private AppProgress? _progress;
        private AppOperationSummary? _summary;
        internal AppProgress? Progress { get { lock (_gate) { return _progress; } } }
        internal AppOperationSummary? Summary { get { lock (_gate) { return _summary; } } }
        internal AppCommand? OperationCommand { get { lock (_gate) { return _operationCommand; } } }
        internal AppDispatcher Dispatcher { get; }
        internal AppActionSession(CancellationToken token, AppDispatcher? dispatcher = null, Func<TimeSpan>? actionClock = null) { _actionClock = actionClock; Dispatcher = dispatcher ?? new AppDispatcher(); _lifetime = CancellationTokenSource.CreateLinkedTokenSource(token); }
        internal bool IsBusy { get { lock (_gate) { return _work != null && !_work.IsCompleted; } } }
        /// <summary>The current worker, including output and completion bookkeeping; completed when no action has started.</summary>
        internal Task Completion { get { lock (_gate) { return _work ?? Task.CompletedTask; } } }
        internal long CompletionRevision { get { lock (_gate) { return _completionRevision; } } }
        internal AppCommand? CompletedCommand { get { lock (_gate) { return _completedCommand; } } }
        internal long Revision { get { lock (_gate) { return _revision; } } }
        internal IReadOnlyList<LogEntry> Output { get { lock (_gate) { return _output.ToArray(); } } }

        internal bool Start(AppCommand command, IReadOnlyList<object?> rows) {
            lock (_gate) {
                if (_closed || (_work != null && !_work.IsCompleted)) { return false; }
                _operation?.Dispose();
                _operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                CancellationToken token = _operation.Token;
                _clockStart = _actionClock?.Invoke() ?? TimeSpan.Zero;
                _clock.Restart();
                var captured = Array.AsReadOnly(rows.ToArray());
                _operationCommand = command; _progress = null; _summary = null;
                _work = Task.Run(async () => {
                    bool succeeded = false;
                    try {
                        token.ThrowIfCancellationRequested();
                        await command.Action(new AppActionContext(captured, Write, progress => { lock (_gate) { if (!_closed) { _progress = progress; _revision++; } } }, summary => { lock (_gate) { if (!_closed) { _summary = summary; _revision++; } } }), token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        Write(command.Label + ": completed", "pass");
                        succeeded = true;
                    } catch (OperationCanceledException) when (token.IsCancellationRequested) {
                        Write(command.Label + ": cancelled", "warn");
                        lock (_gate) { _summary ??= new AppOperationSummary(command.Label + ": cancelled"); }
                    }
                    catch (Exception error) {
                        WriteFailure(command.Label, error);
                        lock (_gate) { _summary ??= new AppOperationSummary(command.Label + ": failed", new[] { error.Message }); }
                    }
                    finally { lock (_gate) { _summary ??= new AppOperationSummary(command.Label + ": completed"); _completedCommand = succeeded ? command : null; _completionRevision++; } }
                });
                return true;
            }
        }

        internal void Cancel() {
            CancellationTokenSource? operation;
            lock (_gate) { operation = _closed ? null : _operation; }
            try { operation?.Cancel(); } catch (ObjectDisposedException) { }
        }

        internal void WriteFailure(string label, Exception error) {
            // AggregateException.Message omits the individual failures on .NET Framework.
            if (error is AggregateException aggregate && aggregate.InnerExceptions.Count > 0) {
                foreach (Exception cause in aggregate.Flatten().InnerExceptions) { Write(label + ": " + cause.Message, "err"); }
            } else { Write(label + ": " + error.Message, "err"); }
        }

        internal void Write(string text, string level) {
            lock (_gate) {
                if (_closed) { return; }
                if (text.Length > 16384) { text = text.Substring(0, 16384) + " [truncated]"; }
                foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
                    if (_output.Count == 1000) { _output.Dequeue(); }
                    _output.Enqueue(new LogEntry(_actionClock == null ? _clock.Elapsed : _actionClock() - _clockStart, level, line.Length > 16384 ? line.Substring(0, 16384) : line));
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
