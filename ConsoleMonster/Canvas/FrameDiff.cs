using System;
using System.Collections.Generic;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// Computes the runs of cells that changed between two frames of the same size. Nearby runs are merged when the
    /// gap between them is small, because repositioning the cursor costs more bytes than rewriting a few cells. Runs never
    /// start or end inside a wide grapheme.
    /// </summary>
    public static class FrameDiff {
        /// <summary>Computes changed runs.</summary>
        /// <param name="previous">The frame currently on screen.</param>
        /// <param name="current">The new frame.</param>
        /// <param name="mergeGap">Unchanged cells allowed between two runs before they are split.</param>
        /// <returns>Runs in row-major order.</returns>
        public static List<DiffRun> Compute(CellBuffer previous, CellBuffer current, int mergeGap = 4) {
            if (previous == null) {
                throw new ArgumentNullException(nameof(previous));
            }
            if (current == null) {
                throw new ArgumentNullException(nameof(current));
            }
            if (previous.Width != current.Width || previous.Height != current.Height) {
                throw new ArgumentException("Frames must have the same size.", nameof(previous));
            }
            List<DiffRun> runs = new List<DiffRun>();
            int width = current.Width;
            for (int y = 0; y < current.Height; y++) {
                int runStart = -1;
                int lastChanged = -1;
                for (int x = 0; x < width; x++) {
                    if (current[x, y].Equals(previous[x, y])) {
                        continue;
                    }
                    if (runStart >= 0 && x - lastChanged - 1 > mergeGap) {
                        runs.Add(Normalize(current, y, runStart, lastChanged));
                        runStart = -1;
                    }
                    if (runStart < 0) {
                        runStart = x;
                    }
                    lastChanged = x;
                }
                if (runStart >= 0) {
                    runs.Add(Normalize(current, y, runStart, lastChanged));
                }
            }
            return runs;
        }

        private static DiffRun Normalize(CellBuffer frame, int y, int start, int end) {
            if (start > 0 && frame[start, y].IsContinuation) {
                start--;
            }
            if (end + 1 < frame.Width && frame[end + 1, y].IsContinuation) {
                end++;
            }
            if (frame[end, y].Width == 2 && end + 1 < frame.Width) {
                end++;
            }
            return new DiffRun(y, start, end - start + 1);
        }
    }
}
