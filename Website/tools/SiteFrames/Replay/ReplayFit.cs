using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;

namespace SiteFrames.Replay {
    /// <summary>
    /// Sizes a recording to what it shows and picks the moment a player starts from when it opens on its final screen.
    /// </summary>
    internal static class ReplayFit {
        /// <summary>
        /// Cuts every frame to the lowest row and the rightmost column any frame uses. Inline views draw at the top of the
        /// pseudo console and leave the rows below empty, and few runs fill a 100-column terminal, so the player is as
        /// large as the run, not the console. Every frame keeps one size, so playback never resizes the window.
        /// </summary>
        public static List<TimedFrame> Trim(List<TimedFrame> frames) {
            int rows = 1;
            int columns = 1;
            foreach (TimedFrame frame in frames) {
                for (int y = frame.Frame.Height - 1; y >= rows; y--) {
                    if (Frames.FrameHtml.ContentEnd(frame.Frame, y, ColorDepth.TrueColor) > 0) {
                        rows = y + 1;
                        break;
                    }
                }
                columns = Math.Max(columns, Site.ContentWidth(frame.Frame));
            }
            List<TimedFrame> trimmed = new List<TimedFrame>(frames.Count);
            foreach (TimedFrame frame in frames) {
                if (frame.Frame.Height <= rows && frame.Frame.Width <= columns) {
                    trimmed.Add(frame);
                    continue;
                }
                int height = Math.Min(rows, frame.Frame.Height);
                int width = Math.Min(columns, frame.Frame.Width);
                CellBuffer cut = new CellBuffer(width, height);
                cut.Blit(frame.Frame, 0, 0, width, height, 0, 0);
                trimmed.Add(new TimedFrame(frame.Time, cut));
            }
            return trimmed;
        }

        /// <summary>
        /// The moment a replay that opens on its final screen plays from: the first frame that shows at least a third of
        /// the rows the fullest frame shows. A session recording starts with one prompt line in an empty window; playing
        /// from there makes a window that just showed a result look broken. The chapter marks still reach the start.
        /// </summary>
        public static long Hero(List<TimedFrame> frames) {
            int fullest = 0;
            foreach (TimedFrame frame in frames) {
                fullest = Math.Max(fullest, UsedRows(frame.Frame));
            }
            int enough = Math.Max(1, (fullest + 2) / 3);
            foreach (TimedFrame frame in frames) {
                if (UsedRows(frame.Frame) >= enough) {
                    return frame.Time;
                }
            }
            return 0;
        }

        /// <summary>The rows of a frame that show anything.</summary>
        public static int UsedRows(CellBuffer frame) {
            int used = 0;
            for (int y = 0; y < frame.Height; y++) {
                if (Frames.FrameHtml.ContentEnd(frame, y, ColorDepth.TrueColor) > 0) {
                    used++;
                }
            }
            return used;
        }
    }
}
