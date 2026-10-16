using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {

    /// <summary>Word wrapping and end/middle truncation in terminal cells, preserving graphemes and styles.</summary>
    public static class TextWrapper {

        /// <summary>The ellipsis used by Full and Basic glyph tiers.</summary>
        public const string Ellipsis = "…";

        /// <summary>
        /// Word-wraps a line, keeping inner spaces and trimming spaces at breaks. Lines break after a space when one fits;
        /// otherwise after a separator (<c>\ / ; , | &amp; ?</c> before <c>. - : =</c>, keeping a path segment or list
        /// item whole when it fits), so the line ends with it; only then by character.
        /// An indivisible grapheme wider than the budget becomes a space.
        /// </summary>
        /// <param name="line">Styled line.</param>
        /// <param name="width">Maximum cells per line; values below one use one.</param>
        /// <returns>At least one line, each within the budget.</returns>
        public static IReadOnlyList<StyledLine> Wrap(StyledLine line, int width) {
            return Wrap(line, width, DisplayWidth.Profile);
        }

        /// <summary>Word-wraps a line using an explicit terminal width profile.</summary>
        public static IReadOnlyList<StyledLine> Wrap(StyledLine line, int width, TerminalWidthProfile profile) {
            if (line == null) {
                throw new ArgumentNullException(nameof(line));
            }
            width = Math.Max(1, width);
            StyledLineMap map = new StyledLineMap(line, profile);
            if (map.Width <= width) {
                return new[] { line };
            }
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
                        split = SeparatorBreak(map, start, end, width);
                    }
                }
                int trimmed = split;
                while (trimmed > start && map.IsSpace(trimmed - 1)) {
                    trimmed--;
                }
                if (trimmed > start) {
                    result.Add(map.Slice(start, trimmed));
                }
                start = split;
                while (start < map.Clusters.Count && map.IsSpace(start)) {
                    start++;
                }
            }
            if (result.Count == 0) {
                result.Add(StyledLine.Empty);
            }
            return result;
        }

        // No space fits: break after a separator, so hostnames, URLs, paths, DNs and lists wrap at natural points and each
        // line ends with its separator ("evotec-xyz.mail.protection." / "outlook.com"). Strong separators (\ / ; , | & ?)
        // end a path segment, list item or query part; weak ones (. - : =) sit inside a name. The last strong break wins
        // when the segment after it fits on one line ("Artefacts\Packed\" / "ConsoleMonster.2.0.0.nupkg"); otherwise the
        // line fills up to the last break of either kind. A run of separators ("://", "::", "..") stays together. Without
        // any separator the text breaks by character (the returned end).
        private static int SeparatorBreak(StyledLineMap map, int start, int end, int width) {
            int strong = -1;
            int any = -1;
            // A break at i ends the line after cluster i - 1; i == end uses the last cluster that fits.
            for (int i = end; i > start; i--) {
                if (BreakAfter(map, i - 1) == BreakKind.None || BreakAfter(map, i) != BreakKind.None) {
                    continue;
                }
                BreakKind kind = RunKind(map, i - 1, start);
                if (any < 0) {
                    any = i;
                }
                if (kind == BreakKind.Strong) {
                    strong = i;
                    break;
                }
            }
            if (any < 0) {
                return end;
            }
            if (strong < 0 || strong == any) {
                return any;
            }
            return SegmentWidth(map, strong) <= width ? strong : any;
        }

        // The strongest separator in the run that ends at last ("/." in ".xyz/.well-known" counts as strong).
        private static BreakKind RunKind(StyledLineMap map, int last, int start) {
            BreakKind kind = BreakKind.None;
            for (int i = last; i >= start; i--) {
                BreakKind current = BreakAfter(map, i);
                if (current == BreakKind.None) {
                    break;
                }
                if (current > kind) {
                    kind = current;
                }
            }
            return kind;
        }

        // Cells from index up to and including the next space or strong separator (or the end of the text).
        private static int SegmentWidth(StyledLineMap map, int index) {
            int cells = 0;
            for (int i = index; i < map.Clusters.Count; i++) {
                if (map.IsSpace(i)) {
                    break;
                }
                cells += map.Clusters[i].Width;
                if (BreakAfter(map, i) == BreakKind.Strong) {
                    break;
                }
            }
            return cells;
        }

        private enum BreakKind {
            None,
            Weak,
            Strong
        }

        private static BreakKind BreakAfter(StyledLineMap map, int index) {
            TextCluster cluster = map.Clusters[index];
            if (cluster.Length != 1) {
                return BreakKind.None;
            }
            switch (map.Text[cluster.Start]) {
                case '/':
                case '\\':
                case ';':
                case ',':
                case '|':
                case '&':
                case '?':
                    return BreakKind.Strong;
                case '.':
                case '-':
                case ':':
                case '=':
                    return BreakKind.Weak;
                default:
                    return BreakKind.None;
            }
        }

        /// <summary>Truncates a styled line at the end, appending an ellipsis when text was cut.</summary>
        /// <param name="line">Styled line.</param>
        /// <param name="width">Maximum cells; non-positive values produce an empty line.</param>
        /// <param name="ellipsis">Marker, clipped to the budget on grapheme boundaries.</param>
        /// <returns>A line no wider than the budget, with retained text styles.</returns>
        public static StyledLine Truncate(StyledLine line, int width, string ellipsis = Ellipsis) {
            return Truncate(line, width, DisplayWidth.Profile, ellipsis);
        }

        /// <summary>Truncates a styled line using an explicit width profile.</summary>
        public static StyledLine Truncate(StyledLine line, int width, TerminalWidthProfile profile, string ellipsis = Ellipsis) {
            if (line == null) {
                throw new ArgumentNullException(nameof(line));
            }
            if (width <= 0) {
                return StyledLine.Empty;
            }
            StyledLineMap map = new StyledLineMap(line, profile);
            if (map.Width <= width) {
                return line;
            }
            string marker = ClipMarker(ellipsis, width, profile);
            int end = map.PrefixEnd(0, width - DisplayWidth.Measure(marker));
            List<TextSpan> spans = new List<TextSpan>(map.Slice(0, end));
            Style markerStyle = map.Clusters.Count == 0 ? Style.Plain : map.StyleAt(end > 0 ? end - 1 : 0);
            if (marker.Length > 0) {
                spans.Add(new TextSpan(marker, markerStyle));
            }
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

        /// <summary>Truncates plain text with an explicit width profile.</summary>
        public static string Truncate(string text, int width, TerminalWidthProfile profile, string ellipsis = Ellipsis) => Truncate(StyledLine.From(text ?? string.Empty), width, profile, ellipsis).PlainText;

        /// <summary>Truncates the middle of text, retaining both the beginning and the end (useful for paths).</summary>
        /// <param name="text">Plain text.</param>
        /// <param name="width">Maximum cells.</param>
        /// <param name="ellipsis">Marker inserted between the retained ends.</param>
        /// <returns>Text within the budget, on grapheme boundaries.</returns>
        public static string TruncateMiddle(string text, int width, string ellipsis = Ellipsis) {
            return TruncateMiddle(text, width, DisplayWidth.Profile, ellipsis);
        }

        /// <summary>Truncates the middle using an explicit width profile.</summary>
        public static string TruncateMiddle(string text, int width, TerminalWidthProfile profile, string ellipsis = Ellipsis) {
            if (width <= 0) {
                return string.Empty;
            }
            text = text ?? string.Empty;
            if (DisplayWidth.Measure(text, profile) <= width) {
                return text;
            }
            string marker = ClipMarker(ellipsis, width, profile);
            int budget = width - DisplayWidth.Measure(marker, profile);
            StyledLineMap map = new StyledLineMap(StyledLine.From(text), profile);
            int leftEnd = map.PrefixEnd(0, (budget + 1) / 2);
            int used = 0;
            for (int i = 0; i < leftEnd; i++) {
                used += map.Clusters[i].Width;
            }
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

        private static string ClipMarker(string? marker, int width, TerminalWidthProfile? profile = null) {
            marker = marker ?? string.Empty;
            int length = 0;
            foreach (TextCluster cluster in DisplayWidth.Clusters(marker, profile ?? DisplayWidth.Profile)) {
                if (cluster.Width > width) {
                    break;
                }
                width -= cluster.Width;
                length = cluster.Start + cluster.Length;
            }
            return marker.Substring(0, length);
        }
    }
}
