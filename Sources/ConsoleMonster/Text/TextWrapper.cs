using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>Word wrapping and end/middle truncation in terminal cells, preserving graphemes and styles.</summary>
    public static class TextWrapper {
        /// <summary>The ellipsis used by Full and Basic glyph tiers.</summary>
        public const string Ellipsis = "…";

        /// <summary>Word-wraps a line, keeping inner spaces and trimming spaces at breaks. An indivisible grapheme wider than the budget becomes a space.</summary>
        /// <param name="line">Styled line.</param>
        /// <param name="width">Maximum cells per line; values below one use one.</param>
        /// <returns>At least one line, each within the budget.</returns>
        public static IReadOnlyList<StyledLine> Wrap(StyledLine line, int width) {
            if (line == null) { throw new ArgumentNullException(nameof(line)); }
            width = Math.Max(1, width);
            if (line.Width <= width) { return new[] { line }; }
            StyledLineMap map = new StyledLineMap(line);
            List<StyledLine> result = new List<StyledLine>();
            int start = 0;
            while (start < map.Clusters.Count) {
                int end = map.PrefixEnd(start, width);
                if (end == start) {
                    result.Add(StyledLine.From(" ", map.StyleAt(start)));
                    start++;
                    continue;
                }
                if (end == map.Clusters.Count) {
                    result.Add(map.Slice(start, end));
                    break;
                }
                // A space immediately after a full line is itself the break. Otherwise
                // backtrack to the last word boundary, leaving earlier inner spaces intact.
                int split = end;
                if (!map.IsSpace(end)) {
                    bool found = false;
                    for (int i = end - 1; i >= start; i--) {
                        if (map.IsSpace(i)) {
                            split = i;
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        // No space: break after separators so DNs, paths and lists wrap at natural points.
                        for (int i = end - 1; i > start; i--) {
                            if (IsBreakAfter(map, i - 1)) {
                                split = i;
                                break;
                            }
                        }
                    }
                }
                int trimmed = split;
                while (trimmed > start && map.IsSpace(trimmed - 1)) { trimmed--; }
                if (trimmed > start) { result.Add(map.Slice(start, trimmed)); }
                start = split;
                while (start < map.Clusters.Count && map.IsSpace(start)) { start++; }
            }
            if (result.Count == 0) { result.Add(StyledLine.Empty); }
            return result;
        }

        private static bool IsBreakAfter(StyledLineMap map, int index) {
            TextCluster cluster = map.Clusters[index];
            if (cluster.Length != 1) {
                return false;
            }
            char c = map.Text[cluster.Start];
            return c == ',' || c == ';' || c == '/' || c == '\\' || c == '|';
        }

        /// <summary>Truncates a styled line at the end, appending an ellipsis when text was cut.</summary>
        /// <param name="line">Styled line.</param>
        /// <param name="width">Maximum cells; non-positive values produce an empty line.</param>
        /// <param name="ellipsis">Marker, clipped to the budget on grapheme boundaries.</param>
        /// <returns>A line no wider than the budget, with retained text styles.</returns>
        public static StyledLine Truncate(StyledLine line, int width, string ellipsis = Ellipsis) {
            if (line == null) { throw new ArgumentNullException(nameof(line)); }
            if (width <= 0) { return StyledLine.Empty; }
            if (line.Width <= width) { return line; }
            string marker = ClipMarker(ellipsis, width);
            StyledLineMap map = new StyledLineMap(line);
            int end = map.PrefixEnd(0, width - DisplayWidth.Measure(marker));
            List<TextSpan> spans = new List<TextSpan>(map.Slice(0, end));
            Style markerStyle = map.Clusters.Count == 0 ? Style.Plain : map.StyleAt(end > 0 ? end - 1 : 0);
            if (marker.Length > 0) { spans.Add(new TextSpan(marker, markerStyle)); }
            return new StyledLine(spans);
        }

        /// <summary>Truncates plain text at the end without splitting a grapheme.</summary>
        /// <param name="text">Plain text.</param>
        /// <param name="width">Maximum cells.</param>
        /// <param name="ellipsis">Marker appended when text is cut.</param>
        /// <returns>Text within the budget.</returns>
        public static string Truncate(string text, int width, string ellipsis = Ellipsis) {
            return Truncate(StyledLine.From(text ?? string.Empty), width, ellipsis).PlainText;
        }

        /// <summary>Truncates the middle of text, retaining both the beginning and the end (useful for paths).</summary>
        /// <param name="text">Plain text.</param>
        /// <param name="width">Maximum cells.</param>
        /// <param name="ellipsis">Marker inserted between the retained ends.</param>
        /// <returns>Text within the budget, on grapheme boundaries.</returns>
        public static string TruncateMiddle(string text, int width, string ellipsis = Ellipsis) {
            if (width <= 0) { return string.Empty; }
            text = text ?? string.Empty;
            if (DisplayWidth.Measure(text) <= width) { return text; }
            string marker = ClipMarker(ellipsis, width);
            int budget = width - DisplayWidth.Measure(marker);
            StyledLineMap map = new StyledLineMap(StyledLine.From(text));
            int leftEnd = map.PrefixEnd(0, (budget + 1) / 2);
            int used = 0;
            for (int i = 0; i < leftEnd; i++) { used += map.Clusters[i].Width; }
            int rightStart = map.Clusters.Count;
            while (rightStart > leftEnd && map.Clusters[rightStart - 1].Width <= budget - used) {
                used += map.Clusters[--rightStart].Width;
            }
            // Use any cells left by an uneven wide-character split on the left.
            while (leftEnd < rightStart && map.Clusters[leftEnd].Width <= budget - used) {
                used += map.Clusters[leftEnd++].Width;
            }
            return map.Slice(0, leftEnd).PlainText + marker + map.Slice(rightStart, map.Clusters.Count).PlainText;
        }

        private static string ClipMarker(string? marker, int width) {
            marker = marker ?? string.Empty;
            int length = 0;
            foreach (TextCluster cluster in DisplayWidth.Clusters(marker)) {
                if (cluster.Width > width) { break; }
                width -= cluster.Width;
                length = cluster.Start + cluster.Length;
            }
            return marker.Substring(0, length);
        }
    }
}
