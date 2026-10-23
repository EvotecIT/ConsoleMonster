using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ConsoleMonster.Interactive {
    /// <summary>Thread-safe named values shared by an application and its dependent views. Enumeration takes a snapshot.</summary>
    public sealed class AppState : IReadOnlyDictionary<string, object?> {
        private readonly object _gate = new object();
        private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private bool _exit;

        /// <summary>Raised after a changed value is committed, outside the dictionary lock.</summary>
        public event EventHandler<AppValueChangedEventArgs>? Changed;

        /// <summary>Gets or sets a named value; assigning an equal value does not raise a notification.</summary>
        /// <param name="key">The state key.</param>
        /// <returns>The value.</returns>
        public object? this[string key] {
            get { lock (_gate) { return _values[key]; } }
            set {
                object? previous;
                lock (_gate) {
                    bool exists = _values.TryGetValue(key, out previous);
                    if (exists && InputControl.ValuesEqual(previous, value)) { return; }
                    _values[key] = value;
                }
                Changed?.Invoke(this, new AppValueChangedEventArgs(key, previous, value));
            }
        }

        /// <summary>The current key count.</summary>
        public int Count { get { lock (_gate) { return _values.Count; } } }

        /// <summary>A snapshot of the keys.</summary>
        public IEnumerable<string> Keys => Snapshot().Keys;

        /// <summary>A snapshot of the values.</summary>
        public IEnumerable<object?> Values => Snapshot().Values;

        /// <summary>Whether a callback requested that the app finish.</summary>
        public bool ExitRequested { get { lock (_gate) { return _exit; } } }

        /// <summary>Requests an orderly exit after pending input callbacks finish.</summary>
        public void RequestExit() { lock (_gate) { _exit = true; } }

        internal void BeginRun() { lock (_gate) { _exit = false; } }

        /// <summary>Reads a typed value, returning a fallback when the key is absent or has another type.</summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="key">The state key.</param>
        /// <param name="fallback">The value to use when the typed value is unavailable.</param>
        /// <returns>The typed value.</returns>
        public T Get<T>(string key, T fallback = default!) => TryGetValue(key, out object? value) && value is T typed ? typed : fallback;

        /// <inheritdoc />
        public bool ContainsKey(string key) { lock (_gate) { return _values.ContainsKey(key); } }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object? value) { lock (_gate) { return _values.TryGetValue(key, out value); } }

        /// <summary>Copies the current values into a read-only dictionary.</summary>
        /// <returns>A stable dictionary snapshot.</returns>
        public IReadOnlyDictionary<string, object?> Snapshot() {
            lock (_gate) { return new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase)); }
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Snapshot().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
