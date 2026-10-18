using ConsoleMonster.Canvas;

namespace SiteFrames.Replay {
    /// <summary>A distinct screen and the moment it appeared, in milliseconds from the start.</summary>
    internal readonly struct TimedFrame {
        public TimedFrame(long time, CellBuffer frame) {
            Time = time;
            Frame = frame;
        }

        public long Time { get; }

        public CellBuffer Frame { get; }
    }
}
