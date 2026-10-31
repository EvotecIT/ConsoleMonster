using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.Headless {
    /// <summary>
    /// Masks secrets on a screen before it leaves the process (debug socket, browser host): the recording redactor's
    /// built-in patterns plus any configured ones, matched row by row, with matched cells replaced by <c>*</c>. Link
    /// targets that hold a secret are dropped. A secret split across two rows is not recognised.
    /// </summary>
    internal static class ScreenRedaction {
        internal static CellBuffer Apply(CellBuffer frame, RecordingRedactor redactor) {
            CellBuffer? copy = null;
            StringBuilder row = new StringBuilder();
            List<int> columns = new List<int>();
            Dictionary<string, bool> links = new Dictionary<string, bool>();
            for (int y = 0; y < frame.Height; y++) {
                row.Clear();
                columns.Clear();
                for (int x = 0; x < frame.Width; x++) {
                    Cell cell = frame[x, y];
                    string? link = cell.Style.Link;
                    if (link != null) {
                        if (!links.TryGetValue(link, out bool secret)) { secret = redactor.MatchRanges(link).Count > 0; links[link] = secret; }
                        if (secret) {
                            copy ??= Copy(frame);
                            copy.SetStyle(x, y, copy[x, y].Style.WithLink(null));
                        }
                    }
                    if (cell.IsContinuation) { continue; }
                    string glyph = string.IsNullOrEmpty(cell.Glyph) ? " " : cell.Glyph;
                    foreach (char c in glyph) { row.Append(c); columns.Add(x); }
                }
                // Trailing blanks are padding, not text: a match that runs to the end of the line stops at its last character.
                foreach (KeyValuePair<int, int> range in redactor.MatchRanges(row.ToString().TrimEnd())) {
                    copy ??= Copy(frame);
                    for (int i = range.Key; i < range.Key + range.Value && i < columns.Count; i++) {
                        int x = columns[i];
                        Cell cell = copy[x, y];
                        copy.Set(x, y, "*", cell.Style);
                        if (frame[x, y].Width == 2 && x + 1 < frame.Width) { copy.Set(x + 1, y, "*", cell.Style); }
                    }
                }
            }
            return copy ?? frame;
        }

        private static CellBuffer Copy(CellBuffer frame) {
            CellBuffer copy = new CellBuffer(frame.Width, frame.Height);
            copy.CopyFrom(frame);
            return copy;
        }
    }
}
