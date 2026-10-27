using System;
using System.Threading;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        /// <summary>Stops screen writes and leaves the alternate screen until this scope is disposed. The caller separately owns input and child lifetime.</summary>
        internal IDisposable SuspendTerminal() {
            LivePause pause = Pause();
            try {
                lock (_outputGate) {
                    if (!CanOverlay || _output == null) { throw new InvalidOperationException("Terminal handoff requires an active full-screen output."); }
                    _output.EndLive();
                }
                return new TerminalSuspension(this, pause);
            } catch { pause.Dispose(); throw; }
        }

        private sealed class TerminalSuspension : IDisposable {
            private LiveSession? _owner;
            private readonly LivePause _pause;
            internal TerminalSuspension(LiveSession owner, LivePause pause) { _owner = owner; _pause = pause; }
            public void Dispose() {
                LiveSession? owner = Interlocked.Exchange(ref _owner, null);
                if (owner == null) { return; }
                try {
                    lock (owner._outputGate) {
                        if (!owner._restored && owner._output != null) { owner._output.BeginLive(); owner._output.Invalidate(); }
                    }
                } finally { _pause.Dispose(); }
            }
        }
    }
}
