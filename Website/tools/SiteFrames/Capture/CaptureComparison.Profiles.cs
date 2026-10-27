using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace SiteFrames.Capture {
    internal static partial class CaptureComparison {
        // Ordered schema of the three current 100x40 docs profiles; FontFace alone is optional.
        private static readonly string[] ProfilePrefixes = {
            "This console", "Mode             ", "ColorDepth       ", "Glyphs           ", "Width            ",
            "Height           ", "ThemePreference  ", "FontFace         ", "",
            "  CONSOLEMONSTER_EMOJI_WIDTH: ", "  CONSOLEMONSTER_AMBIGUOUS_WIDTH: ", "  hyperlinks: ",
            "  mode: ", "  colour: ", "  glyphs: ", "  VT: ", "  kind: ", "  width profile: ",
            "  probe widths: ", "  theme preference: ", "  culture: ", "  size: "
        };

        /// <summary>Compares every complete profile frame, preserving styles and reporting each permitted value change.</summary>
        private static bool ProfileDifferences(string file, Capture a, Capture b, out JsonArray differences) {
            differences = new JsonArray();
            if (file is not ("docs/profile.ansi" or "docs/profile-ci.ansi" or "docs/profile-detected.ansi") ||
                CapturePlatforms.Tag(a.Path) == null || CapturePlatforms.Tag(b.Path) == null ||
                a.Category != "terminal-profile" || b.Category != a.Category || a.Source == null || a.Source != b.Source ||
                a.Screen != b.Screen || a.Width != 100 || b.Width != 100 || a.Height != 40 || b.Height != 40 ||
                a.Frames.Count == 0 || a.Frames.Count != b.Frames.Count || a.ScreenHashes.Length != 0 || b.ScreenHashes.Length != 0) { return false; }
            bool detected = file == "docs/profile-detected.ansi";
            for (int frame = 0; frame < a.Frames.Count; frame++) {
                if (!ProfileRows(a.Frames[frame], out var x) || !ProfileRows(b.Frames[frame], out var y)) { return false; }
                // Native detected labels alone may differ by the built-in Dark theme's explicit TrueColor/Ansi256 token mapping.
                bool reduce = detected && new[] { x["ColorDepth"].Value, y["ColorDepth"].Value }.OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(new[] { "Ansi256", "TrueColor" });
                foreach (string prefix in ProfilePrefixes) {
                    string key = prefix.Trim().TrimEnd(':');
                    bool hasX = x.TryGetValue(key, out var u), hasY = y.TryGetValue(key, out var v);
                    bool mutable = key is "FontFace" or "kind" || detected && (key is "ColorDepth" or "Glyphs" or "colour" or "glyphs");
                    if (!hasX || !hasY) {
                        if (key != "FontFace") { return false; }
                        if (hasX != hasY) { differences.Add(new JsonObject { ["frame"] = frame, ["key"] = key, ["left"] = hasX ? u.Value : null, ["right"] = hasY ? v.Value : null }); }
                        continue;
                    }
                    if (!mutable && u.Value != v.Value) { return false; }
                    if (u.Value != v.Value) { differences.Add(new JsonObject { ["frame"] = frame, ["key"] = key, ["left"] = u.Value, ["right"] = v.Value }); }
                    int start = prefix.Length;
                    int count = mutable ? start : u.Cells.Length;
                    for (int i = 0; i < count; i++) {
                        Cell p = u.Cells[i], q = v.Cells[i];
                        if (reduce && prefix.Length == 17 && i < 17) {
                            p = ProfileLabel(p, x["ColorDepth"].Value); q = ProfileLabel(q, y["ColorDepth"].Value);
                        }
                        if (!SameCell(p, q)) { return false; }
                    }
                    if (mutable && (!SameCell(new Cell("x", u.Cells[start].Style), new Cell("x", v.Cells[start].Style)))) { return false; }
                }
            }
            return differences.Count > 0;
        }

        private static Cell ProfileLabel(Cell cell, string depth) {
            Style style = cell.Style;
            Color foreground = Rgb(style.Color);
            Color expected = Theme.Dark.Resolve(ThemeToken.Muted, depth == "Ansi256" ? ColorDepth.Ansi256 : ColorDepth.TrueColor).Color;
            if (foreground.Equals(Rgb(expected))) { foreground = Rgb(Theme.Dark.Resolve(ThemeToken.Muted, ColorDepth.TrueColor).Color); }
            return new Cell(cell.Glyph, new Style(foreground, Rgb(style.BackgroundColor), style.Decoration, style.Link), cell.Width);
        }

        /// <summary>Recognizes only the current ordered schema; joins a decision continuation only after a full physical row.</summary>
        private static bool ProfileRows(CellBuffer frame, out Dictionary<string, (string Value, Cell[] Cells)> values) {
            values = new(StringComparer.Ordinal);
            if (frame.Width != 100 || frame.Height != 40 || Quality(frame).Any()) { return false; }
            int row = 0;
            Cell[] Row(int r) => Enumerable.Range(0, frame.Width).Select(c => frame[c, r]).ToArray();
            // The separator is the one unpainted blank row after the fields; all blank padding must retain its style.
            int separator = frame.GetRowText(7).StartsWith("FontFace         ", StringComparison.Ordinal) ? 8 : 7;
            Cell[] blank = Row(separator);
            if (blank.Any(c => c.Glyph != " " || c.Width != 1) || blank.Any(c => !SameCell(c, blank[0]))) { return false; }
            foreach (string prefix in ProfilePrefixes) {
                if (row >= frame.Height) { return false; }
                string text = frame.GetRowText(row).TrimEnd(' ');
                if (prefix.StartsWith("FontFace", StringComparison.Ordinal) && !text.StartsWith(prefix, StringComparison.Ordinal)) { continue; }
                var cells = new List<Cell>(Row(row++));
                if (prefix == "  hyperlinks: ") {
                    while (text.Length == cells.Count && row < frame.Height && !frame.GetRowText(row).StartsWith("  ", StringComparison.Ordinal) && frame.GetRowText(row).TrimEnd(' ').Length > 0) {
                        string tail = frame.GetRowText(row).TrimEnd(' ');
                        cells.AddRange(Row(row++)); text += tail;
                    }
                }
                if (prefix == "  hyperlinks: " && text != "  hyperlinks: False (terminal/CONSOLEMONSTER_HYPERLINKS; requires interactive VT, never redirected or CI)") { return false; }
                if (!text.StartsWith(prefix, StringComparison.Ordinal) || prefix.Length > 0 && prefix != "This console" && text.Length <= prefix.Length ||
                    prefix == "This console" && text != prefix || prefix.Length == 0 && text.Length != 0 ||
                    cells.Any(c => c.Width != 1 || c.Glyph.Length != 1)) { return false; }
                for (int i = text.Length; i < cells.Count; i++) { if (!SameCell(cells[i], blank[0])) { return false; } }
                // Values are single-style text in the native table/decision writer; prevent hidden style/link changes.
                for (int i = prefix.Length; i < text.Length; i++) { if (!SameCell(new Cell("x", cells[i].Style), new Cell("x", cells[prefix.Length].Style))) { return false; } }
                if (!values.TryAdd(prefix.Trim().TrimEnd(':'), (text[prefix.Length..], cells.Take(prefix.Length == 0 ? cells.Count : text.Length).ToArray()))) { return false; }
            }
            if (values.TryGetValue("FontFace", out var font)) {
                var mode = values["Mode"];
                for (int i = 0; i < font.Cells.Length; i++) {
                    if (!SameCell(new Cell("x", font.Cells[i].Style), new Cell("x", mode.Cells[Math.Min(i, 17)].Style))) { return false; }
                }
            }
            return values["Width"].Value == "100" && values["Height"].Value == "40" && values["size"].Value == "100x40 (visible window or defaults)" &&
                Enumerable.Range(row, frame.Height - row).All(r => Row(r).All(c => SameCell(c, blank[0])));
        }
    }
}
