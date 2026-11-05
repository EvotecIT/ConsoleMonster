using System;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Back and forward through places a reader went, as a browser keeps them: <see cref="Visit"/> before leaving for a new
    /// place, <see cref="TryBack"/> and <see cref="TryForward"/> to return. A place is whatever the caller needs to restore:
    /// a screen and scroll position, a document and section, a selected row.
    /// </summary>
    /// <typeparam name="T">A place.</typeparam>
    public sealed class NavigationHistory<T> {
        private readonly List<T> _back = new List<T>();
        private readonly List<T> _forward = new List<T>();

        /// <summary>Creates an empty history.</summary>
        /// <param name="capacity">The most places kept going back; the oldest are dropped first.</param>
        public NavigationHistory(int capacity = 100) {
            if (capacity < 1) { throw new ArgumentOutOfRangeException(nameof(capacity)); }
            Capacity = capacity;
        }

        /// <summary>The most places kept going back.</summary>
        public int Capacity { get; }

        /// <summary>Whether there is a place to go back to.</summary>
        public bool CanGoBack => _back.Count > 0;

        /// <summary>Whether there is a place to go forward to.</summary>
        public bool CanGoForward => _forward.Count > 0;

        /// <summary>Records the place being left for a new one; going forward is no longer possible.</summary>
        /// <param name="leaving">Where the reader is now.</param>
        public void Visit(T leaving) {
            _back.Add(leaving);
            if (_back.Count > Capacity) { _back.RemoveAt(0); }
            _forward.Clear();
        }

        /// <summary>Goes back, remembering the current place for going forward.</summary>
        /// <param name="current">Where the reader is now.</param>
        /// <param name="place">The place to return to.</param>
        /// <returns>Whether there was a place to go back to.</returns>
        public bool TryBack(T current, out T place) => Travel(_back, _forward, current, out place);

        /// <summary>Goes forward again, remembering the current place for going back.</summary>
        /// <param name="current">Where the reader is now.</param>
        /// <param name="place">The place to return to.</param>
        /// <returns>Whether there was a place to go forward to.</returns>
        public bool TryForward(T current, out T place) => Travel(_forward, _back, current, out place);

        /// <summary>Forgets every place.</summary>
        public void Clear() { _back.Clear(); _forward.Clear(); }

        private static bool Travel(List<T> from, List<T> to, T current, out T place) {
            if (from.Count == 0) { place = current; return false; }
            place = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            to.Add(current);
            return true;
        }
    }
}
