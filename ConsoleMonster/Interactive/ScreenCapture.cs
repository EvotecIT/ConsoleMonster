using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Draws prompts, menus and forms into memory instead of the terminal and keeps every frame: for scripted demos,
    /// documentation screenshots and exports (<c>ConsoleExporter.ToHtml(capture.LastFrame)</c>). Set it on
    /// <see cref="InteractiveOptions.Capture"/> together with a scripted <see cref="InteractiveOptions.Input"/>.
    /// </summary>
    public sealed class ScreenCapture {
        private readonly RecordingSurface _surface;
        private readonly Dictionary<int, TimeSpan> _frameHolds = new Dictionary<int, TimeSpan>();

        /// <summary>Creates a capture of a fixed size.</summary>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells.</param>
        /// <param name="inline">Whether prompts draw as they would in the scrollback (true) or on a full screen (false).</param>
        public ScreenCapture(int width, int height, bool inline = false) {
            if (width < 10 || height < 3) {
                throw new ArgumentOutOfRangeException(width < 10 ? nameof(width) : nameof(height), "A capture needs at least 10 columns and 3 rows.");
            }
            _surface = new RecordingSurface(width, height, inline);
        }

        /// <summary>Width in cells.</summary>
        public int Width => _surface.Width;

        /// <summary>Height in cells.</summary>
        public int Height => _surface.Height;

        /// <summary>Every frame drawn, in order.</summary>
        public IReadOnlyList<CellBuffer> Frames => _surface.Frames;

        /// <summary>Additional playback pauses after each zero-based frame index. Capturing a pause never sleeps.</summary>
        public IReadOnlyDictionary<int, TimeSpan> FrameHolds => _frameHolds;

        /// <summary>Records an additional playback pause after the last captured frame.</summary>
        /// <param name="duration">A nonnegative pause; zero does nothing.</param>
        public void Hold(TimeSpan duration) {
            if (duration < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(duration)); }
            if (duration == TimeSpan.Zero) { return; }
            int index = Frames.Count - 1;
            if (index < 0) { throw new InvalidOperationException("Capture a frame before adding its playback pause."); }
            _frameHolds.TryGetValue(index, out TimeSpan previous);
            _frameHolds[index] = previous + duration;
        }

        /// <summary>The last frame drawn, or null.</summary>
        public CellBuffer? LastFrame => _surface.LastFrame;

        /// <summary>The first frame whose text contains a phrase (for picking a screen to export), or null.</summary>
        /// <param name="text">The phrase.</param>
        /// <returns>The frame, or null.</returns>
        public CellBuffer? FindFrame(string text) {
            foreach (CellBuffer frame in _surface.Frames) {
                if (frame.ToPlainText().IndexOf(text, StringComparison.Ordinal) >= 0) {
                    return frame;
                }
            }
            return null;
        }

        internal IPromptSurface Surface => _surface;
    }
}
