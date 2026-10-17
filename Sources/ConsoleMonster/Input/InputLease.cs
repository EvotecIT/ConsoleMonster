using System;
using System.Threading;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Input {
    /// <summary>A hold on the keyboard from <see cref="ConsoleInput.Acquire"/>.</summary>
    public sealed class InputLease : IDisposable {
        private int _released;

        internal InputLease(IInputSource source, bool global) {
            Source = source;
            Global = global;
        }

        /// <summary>The source to read from.</summary>
        public IInputSource Source { get; }

        internal bool Global { get; }

        /// <summary>Releases the hold.</summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _released, 1) == 0) {
                ConsoleInput.Release(this);
            }
        }
    }
}
