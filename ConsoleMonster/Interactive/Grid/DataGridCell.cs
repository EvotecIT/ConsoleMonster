using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>A cell computed on demand when it first enters a grid viewport. The factory runs on a worker and never owns UI state.</summary>
    public sealed class DataGridCell {
        private readonly object _gate = new object();
        private readonly Func<CancellationToken, Task<object?>> _value;
        private Task<object?>? _work;

        /// <summary>Creates an asynchronous cell. Use a fresh cell for a new listing or a changed value.</summary>
        public DataGridCell(Func<CancellationToken, Task<object?>> value, string placeholder = "…") {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            Placeholder = placeholder ?? string.Empty;
        }

        /// <summary>Creates a lazy synchronous cell; computation is still moved off the renderer.</summary>
        public DataGridCell(Func<object?> value, string placeholder = "…") : this(_ => Task.FromResult(value()), placeholder) {
            if (value == null) { throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>Text displayed before completion.</summary>
        public string Placeholder { get; }
        /// <summary>The failure, or null. A failed cell displays its placeholder and does not close the app.</summary>
        public Exception? Error { get { lock (_gate) { return _work?.Exception?.GetBaseException(); } } }
        /// <summary>Whether a value has completed successfully.</summary>
        public bool Ready { get { lock (_gate) { return _work?.Status == TaskStatus.RanToCompletion; } } }

        internal object? Peek() {
            lock (_gate) { return _work?.Status == TaskStatus.RanToCompletion ? _work.Result : Placeholder; }
        }

        internal object? Resolve(CancellationToken cancellation, Action changed) {
            lock (_gate) {
                if (_work == null) {
                    _work = Task.Run(() => _value(cancellation), cancellation);
                    _ = _work.ContinueWith(task => { _ = task.Exception; changed(); }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                return _work.Status == TaskStatus.RanToCompletion ? _work.Result : Placeholder;
            }
        }
    }
}
