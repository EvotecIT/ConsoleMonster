using System;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>A bounded mailbox. Producers never execute UI callbacks; the app thread drains them with a per-tick budget.</summary>
    public sealed class AppDispatcher : IDisposable {
        private readonly object _gate = new object();
        private readonly Queue<Action> _pending = new Queue<Action>();
        private readonly int _capacity;
        private bool _closed;
        /// <summary>Creates a mailbox with an explicit finite capacity.</summary>
        public AppDispatcher(int capacity = 512) {
            if (capacity < 1) { throw new ArgumentOutOfRangeException(nameof(capacity)); }
            _capacity = capacity;
        }
        /// <summary>Attempts to queue a callback. Returns false when full or disposed.</summary>
        public bool TryPost(Action callback) {
            if (callback == null) { throw new ArgumentNullException(nameof(callback)); }
            lock (_gate) {
                if (_closed || _pending.Count >= _capacity) { return false; }
                _pending.Enqueue(callback);
                return true;
            }
        }
        /// <summary>Runs at most budget callbacks on the caller's app thread, outside the mailbox lock.</summary>
        public int Drain(int budget = 64) {
            if (budget < 1) { throw new ArgumentOutOfRangeException(nameof(budget)); }
            int count = 0;
            while (count < budget) {
                Action callback;
                lock (_gate) {
                    if (_closed || _pending.Count == 0) { break; }
                    callback = _pending.Dequeue();
                }
                callback();
                count++;
            }
            return count;
        }
        /// <summary>Rejects future work and drops callbacks whose app no longer exists.</summary>
        public void Dispose() { lock (_gate) { _closed = true; _pending.Clear(); } }
    }
}
