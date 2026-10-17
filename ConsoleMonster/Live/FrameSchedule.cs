using System;

namespace ConsoleMonster.Live {
    /// <summary>
    /// Frame timing against absolute deadlines: frame n is due at n x period. Windows wait timeouts round up to the system
    /// timer tick (about 15.6 ms), so sleeping "period minus work" every frame drifted to about 12.5 fps at a 15 fps
    /// setting; with deadlines a late wake-up shortens the next wait instead, and the average meets the rate. When the loop
    /// falls more than one period behind (a stall or a slow terminal) it resynchronises instead of rendering a burst.
    /// </summary>
    internal sealed class FrameSchedule {
        private long _frame;

        /// <summary>Creates a schedule.</summary>
        /// <param name="framesPerSecond">The target rate.</param>
        public FrameSchedule(double framesPerSecond) {
            Period = 1000.0 / Math.Max(1, framesPerSecond);
        }

        /// <summary>Milliseconds per frame.</summary>
        public double Period { get; }

        /// <summary>Marks a frame as rendered and returns how long to wait for the next one.</summary>
        /// <param name="nowMilliseconds">Milliseconds since the schedule started.</param>
        /// <returns>The wait in whole milliseconds (0 when the next frame is already due).</returns>
        public int Next(double nowMilliseconds) {
            _frame++;
            double wait = (_frame * Period) - nowMilliseconds;
            if (wait < -Period) {
                _frame = (long)(nowMilliseconds / Period);
                wait = 0;
            }
            return wait >= 1 ? (int)wait : 0;
        }
    }

    /// <summary>
    /// Coalesces drag-resizes: at most one full redraw per settle period while the size keeps changing. A frame skipped
    /// here keeps the old size, so the next frame after the period sees the change again: the final size is never missed.
    /// </summary>
    internal sealed class ResizeCoalescer {
        private readonly TimeSpan _settle;
        private TimeSpan _last = TimeSpan.FromDays(-1);

        /// <summary>Creates a coalescer.</summary>
        /// <param name="settle">The minimum time between resize redraws.</param>
        public ResizeCoalescer(TimeSpan settle) {
            _settle = settle;
        }

        /// <summary>Whether a frame that sees a new size at <paramref name="now"/> may redraw (and records it when it may).</summary>
        /// <param name="now">The current time.</param>
        /// <returns>True to redraw now; false to keep the current frame until the period has passed.</returns>
        public bool TryRedraw(TimeSpan now) {
            if (now - _last < _settle) {
                return false;
            }
            _last = now;
            return true;
        }
    }
}
