using System;
using System.Threading;
using ConsoleMonster.Interop;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        private readonly object _outsideGate = new object();

        /// <summary>
        /// Runs <paramref name="write"/> synchronously with the live area out of the way, for output the session does not
        /// draw: a PowerShell host writing a warning or verbose line, or a prompt such as <c>ShouldContinue</c> or
        /// <c>-WarningAction Inquire</c> that waits for an answer. Inline sessions write pending captured lines, clear their
        /// area and show the cursor; full-screen sessions leave the alternate screen. Then the action runs on the calling
        /// thread, for as long as it needs. Afterwards the area starts again below whatever the action wrote, so later
        /// frames never draw over it; a full-screen session returns to the alternate screen and redraws.
        /// <para>
        /// No frame is drawn while the action runs: updates are applied and the next frame after it shows the current
        /// state. Calls from several threads run one at a time; a call made inside the action, or from a callback on the
        /// render thread, runs directly. What the thread writes to <see cref="Console.Out"/> during the action goes to the
        /// console, not to the session log. Append-only (stream) sessions and stopped sessions just run the action.
        /// </para>
        /// </summary>
        /// <param name="write">The output to write.</param>
        public void WriteOutside(Action write) {
            if (write == null) { throw new ArgumentNullException(nameof(write)); }
            if (!IsRunning || Thread.CurrentThread == _thread || Monitor.IsEntered(_outsideGate)) {
                write();
                return;
            }
            lock (_outsideGate) {
                LivePause pause = Pause();
                bool parked = false;
                try {
                    lock (_outputGate) {
                        if (CanOverlay && _output != null) {
                            if (_output is InlineOutput inline) {
                                // Lines captured before this write come first.
                                FlushUnrouted();
                                inline.Park();
                            } else {
                                _output.EndLive();
                            }
                            parked = true;
                        }
                    }
                    using (SuspendCapture()) {
                        write();
                    }
                } finally {
                    try {
                        if (parked) { Unpark(); }
                    } finally {
                        pause.Dispose();
                    }
                }
            }
        }

        private void Unpark() {
            lock (_outputGate) {
                if (_restored || _output == null) { return; }
                if (_output is InlineOutput inline) {
                    inline.Unpark(_onConsole && CursorAwayFromLineStart());
                } else {
                    _output.BeginLive();
                    _output.Invalidate();
                }
            }
        }

        // Host writes normally end their line; a Write without a newline leaves the cursor after its text.
        private static bool CursorAwayFromLineStart() =>
            WindowsConsole.TryGetCursor(WindowsConsole.StandardOutput, out int x, out _, out _, out _) && x > 0;
    }
}
