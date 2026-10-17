using System;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        private int _paused;

        /// <summary>Whether drawing is paused for a prompt.</summary>
        public bool IsPaused => Volatile.Read(ref _paused) > 0;

        /// <summary>
        /// Pauses drawing so a prompt can take the screen (for example "Continue on errors?" during a run). Updates keep being
        /// applied while paused, so the dashboard is current when it comes back. Dispose the pause to resume; the next frame
        /// redraws everything.
        /// </summary>
        /// <returns>The pause, with the last frame as the backdrop for an overlay.</returns>
        public LivePause Pause() {
            CellBuffer? backdrop = null;
            lock (_outputGate) {
                Interlocked.Increment(ref _paused);
                if (_frame != null) {
                    backdrop = new CellBuffer(_frame.Width, _frame.Height);
                    backdrop.CopyFrom(_frame);
                }
            }
            return new LivePause(this, backdrop);
        }

        /// <summary>
        /// A copy of the last frame the session drew (for exports and screenshots), or null before the first frame and for
        /// line-based outputs that draw no frames. Safe to call from any thread.
        /// </summary>
        /// <returns>The frame copy, or null.</returns>
        public CellBuffer? Snapshot() {
            lock (_outputGate) {
                if (_frame == null) {
                    return null;
                }
                CellBuffer copy = new CellBuffer(_frame.Width, _frame.Height);
                copy.CopyFrom(_frame);
                return copy;
            }
        }

        internal bool CanOverlay => IsRunning && _output != null && _output.SupportsLiveFrames && !_restored;

        internal void PresentOverlay(CellBuffer frame) {
            lock (_outputGate) {
                if (_restored || _output == null) {
                    return;
                }
                _output.RenderFrame(frame);
            }
        }

        internal void Resume() {
            if (Interlocked.Decrement(ref _paused) <= 0) {
                Volatile.Write(ref _paused, 0);
                Invalidate();
            }
        }
    }

    /// <summary>A pause of a live session's drawing; dispose it to resume.</summary>
    public sealed class LivePause : IDisposable {
        private readonly LiveSession _session;
        private int _disposed;

        internal LivePause(LiveSession session, CellBuffer? backdrop) {
            _session = session;
            Backdrop = backdrop;
        }

        /// <summary>The last frame drawn before the pause (the backdrop of an overlay), or null.</summary>
        public CellBuffer? Backdrop { get; }

        /// <summary>The session that is paused.</summary>
        public LiveSession Session => _session;

        /// <summary>Draws a frame through the session's output (diffed like any live frame).</summary>
        /// <param name="frame">The frame.</param>
        public void Present(CellBuffer frame) => _session.PresentOverlay(frame ?? throw new ArgumentNullException(nameof(frame)));

        /// <summary>Resumes drawing.</summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                _session.Resume();
            }
        }
    }
}
