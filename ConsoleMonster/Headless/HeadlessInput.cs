using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Input;

namespace ConsoleMonster.Headless {
    /// <summary>
    /// The keyboard and mouse of a headless terminal: a thread-safe queue that scripts, tests, the browser host and the debug
    /// socket fill while the app reads it. Pauses in scripts (<c>wait:250</c>) delay later events in real time. A read that
    /// does not wait returns an event that is already due, as a terminal's input buffer would.
    /// </summary>
    internal sealed class HeadlessInput : IInputSource {
        private const int MaxPending = 100000;
        private readonly object _gate = new object();
        private readonly Queue<Pending> _queue = new Queue<Pending>();
        private long _readyAt;
        private bool _completed;

        public string Name => "headless input";

        internal void Enqueue(InputEvent input) => Enqueue(new[] { new Pending(input, 0, null) });

        // A resize step changes the screen when it is read, in order with the keys around it.
        internal void Enqueue(List<ScriptedInput.Step> steps, Action<int, int> resize) {
            List<Pending> pending = new List<Pending>(steps.Count);
            foreach (ScriptedInput.Step step in steps) {
                InputEvent? input = step.Event;
                if (input == null) { pending.Add(new Pending(null, step.WaitMilliseconds, null)); }
                else if (input.Kind == InputEventKind.Resize) { pending.Add(new Pending(input, 0, () => resize(input.Width, input.Height))); }
                else { pending.Add(new Pending(input, 0, null)); }
            }
            Enqueue(pending);
        }

        private void Enqueue(IReadOnlyCollection<Pending> items) {
            lock (_gate) {
                if (_completed) { throw new InvalidOperationException("The headless input was completed; no more input can be sent."); }
                if (_queue.Count + items.Count > MaxPending) { throw new InvalidOperationException("Too much input is waiting; at most " + MaxPending + " steps can be queued."); }
                foreach (Pending item in items) { _queue.Enqueue(item); }
                Monitor.PulseAll(_gate);
            }
        }

        // After the queued events are read, further reads report the end of input, so a scripted app run finishes.
        internal void Complete() {
            lock (_gate) {
                _completed = true;
                Monitor.PulseAll(_gate);
            }
        }

        public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            Stopwatch waited = Stopwatch.StartNew();
            Action? onRead = null;
            InputEvent? result = null;
            lock (_gate) {
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    long now = Stopwatch.GetTimestamp();
                    if (_queue.Count > 0 && now >= _readyAt) {
                        Pending next = _queue.Dequeue();
                        if (next.Event == null) {
                            _readyAt = now + (long)(next.WaitMilliseconds * (double)Stopwatch.Frequency / 1000);
                            continue;
                        }
                        result = next.Event;
                        onRead = next.OnRead;
                        break;
                    }
                    if (_queue.Count == 0 && _completed) { throw new InputClosedException("The headless input ended."); }
                    double left = timeout < TimeSpan.Zero ? 50 : timeout.TotalMilliseconds - waited.Elapsed.TotalMilliseconds;
                    if (left <= 0) { return null; }
                    if (_queue.Count > 0) { left = Math.Min(left, Math.Max(1, (_readyAt - now) * 1000.0 / Stopwatch.Frequency)); }
                    // Short slices keep cancellation responsive without a registration per read.
                    Monitor.Wait(_gate, TimeSpan.FromMilliseconds(Math.Min(50, Math.Max(1, left))));
                }
            }
            onRead?.Invoke();
            return result;
        }

        public void Dispose() => Complete();

        private readonly struct Pending {
            internal Pending(InputEvent? input, int waitMilliseconds, Action? onRead) { Event = input; WaitMilliseconds = waitMilliseconds; OnRead = onRead; }
            internal InputEvent? Event { get; }
            internal int WaitMilliseconds { get; }
            internal Action? OnRead { get; }
        }
    }
}
