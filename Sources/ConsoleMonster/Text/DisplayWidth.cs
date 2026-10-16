using System;
using System.Collections.Generic;

namespace ConsoleMonster.Text {

    /// <summary>Deterministic Unicode 17.0 cell widths and terminal grapheme segmentation across all supported runtimes.</summary>
    public static class DisplayWidth {

        /// <summary>Returns 0 for combining/format/control characters, 2 for wide or default emoji characters, otherwise 1.</summary>
        /// <param name="codePoint">Unicode scalar value. Invalid values occupy one replacement cell.</param>
        /// <returns>0, 1 or 2.</returns>
        public static int GetWidth(int codePoint) {
            if (codePoint < 0 || codePoint > 0x10FFFF || (codePoint >= 0xD800 && codePoint <= 0xDFFF)) {
                return 1;
            }
            if (codePoint == 0xAD) {
                return 1;
            }
            if ((codePoint >= 0x1160 && codePoint <= 0x11FF) || (codePoint >= 0xD7B0 && codePoint <= 0xD7FF)
                || In(UnicodeWidthTables.ZeroWidth, codePoint)) {
                return 0;
            }
            return In(UnicodeWidthTables.Wide, codePoint) || In(UnicodeWidthTables.EmojiPresentation, codePoint) ? 2 : 1;
        }

        /// <summary>Measures terminal cells, counting each emoji or ZWJ sequence once.</summary>
        /// <param name="text">Text, or null for zero width.</param>
        /// <returns>The display width.</returns>
        public static int Measure(string? text) {
            int width = 0;
            foreach (TextCluster cluster in Clusters(text)) {
                width += cluster.Width;
            }
            return width;
        }

        /// <summary>Groups base characters with marks, selectors, modifiers and joined sequences. Standalone zero-width clusters are omitted.</summary>
        /// <param name="text">Text, or null for no cells.</param>
        /// <returns>Visible graphemes and their cell widths. Control characters are omitted.</returns>
        public static IEnumerable<TextCell> Cells(string? text) {
            foreach (TextCluster cluster in Clusters(text)) {
                if (cluster.Width > 0) {
                    yield return new TextCell(text!.Substring(cluster.Start, cluster.Length), cluster.Width);
                }
            }
        }

        // Offsets let styled slicing preserve the original span boundaries inside a grapheme.
        internal static IEnumerable<TextCluster> Clusters(string? text) {
            if (string.IsNullOrEmpty(text)) {
                yield break;
            }
            int offset = 0;
            while (offset < text!.Length) {
                int start = offset;
                int previous = Scalar(text, offset, out int length);
                offset += length;
                int regionalCount = IsRegional(previous) ? 1 : 0;
                int indicState = In(UnicodeWidthTables.IndicConsonant, previous) ? 1 : 0;
                bool pictographic = In(UnicodeWidthTables.ExtendedPictographic, previous);
                bool emojiJoiner = false;
                while (offset < text.Length) {
                    int current = Scalar(text, offset, out length);
                    bool join = previous == '\r' && current == '\n';
                    if (!join && !IsControl(previous) && !IsControl(current)) {
                        join = In(UnicodeWidthTables.Extend, current) || In(UnicodeWidthTables.SpacingMark, current)
                            || current == 0x200D
                            || (previous == 0x200D && emojiJoiner && In(UnicodeWidthTables.ExtendedPictographic, current))
                            || In(UnicodeWidthTables.Prepend, previous)
                            || HangulJoins(previous, current)
                            || (IsRegional(previous) && IsRegional(current) && regionalCount % 2 == 1)
                            || (indicState == 2 && In(UnicodeWidthTables.IndicConsonant, current));
                    }
                    if (!join) {
                        break;
                    }
                    if (current == 0x200D) {
                        emojiJoiner = pictographic;
                        pictographic = false;
                    } else if (!In(UnicodeWidthTables.Extend, current)) {
                        pictographic = In(UnicodeWidthTables.ExtendedPictographic, current);
                        emojiJoiner = false;
                    }
                    if (IsRegional(current)) {
                        regionalCount++;
                    } else if (!In(UnicodeWidthTables.Extend, current)) {
                        regionalCount = 0;
                    }
                    if (In(UnicodeWidthTables.IndicConsonant, current)) {
                        indicState = 1;
                    } else if (indicState > 0 && In(UnicodeWidthTables.IndicLinker, current)) {
                        indicState = 2;
                    } else if (!In(UnicodeWidthTables.IndicExtend, current)) {
                        indicState = 0;
                    }
                    previous = current;
                    offset += length;
                }
                yield return new TextCluster(start, offset - start, ClusterWidth(text, start, offset));
            }
        }

        private static int ClusterWidth(string text, int start, int end) {
            int width = 0;
            bool emoji = false;
            bool emojiCandidate = false;
            int regional = 0;
            for (int i = start; i < end;) {
                int cp = Scalar(text, i, out int length);
                i += length;
                int scalarWidth = GetWidth(cp);
                width += scalarWidth;
                emoji |= In(UnicodeWidthTables.EmojiPresentation, cp);
                if (cp == 0xFE0F && emojiCandidate) {
                    emoji = true;
                }
                if (cp == 0x20E3 && emojiCandidate) {
                    emoji = true;
                }
                if (IsRegional(cp)) {
                    regional++;
                }
                emojiCandidate |= In(UnicodeWidthTables.Emoji, cp) || In(UnicodeWidthTables.ExtendedPictographic, cp);
            }
            if (emoji || regional == 2) {
                return 2;
            }
            return width;
        }

        private static int Scalar(string text, int offset, out int length) {
            char first = text[offset];
            if (char.IsHighSurrogate(first) && offset + 1 < text.Length && char.IsLowSurrogate(text[offset + 1])) {
                length = 2;
                return char.ConvertToUtf32(first, text[offset + 1]);
            }
            length = 1;
            return first;
        }

        private static bool IsControl(int cp) => In(UnicodeWidthTables.Control, cp) && cp != 0xAD;
        private static bool IsRegional(int cp) => cp >= 0x1F1E6 && cp <= 0x1F1FF;
        private static bool In(int[] ranges, int cp) => UnicodeWidthTables.Contains(ranges, cp);

        private static bool HangulJoins(int previous, int current) {
            int left = HangulClass(previous);
            int right = HangulClass(current);
            return (left == 1 && (right == 1 || right == 2 || right == 4 || right == 5))
                || ((left == 2 || left == 4) && (right == 2 || right == 3))
                || ((left == 3 || left == 5) && right == 3);
        }

        private static int HangulClass(int cp) {
            if ((cp >= 0x1100 && cp <= 0x115F) || (cp >= 0xA960 && cp <= 0xA97C)) {
                return 1;
            }
            if ((cp >= 0x1160 && cp <= 0x11A7) || (cp >= 0xD7B0 && cp <= 0xD7C6)) {
                return 2;
            }
            if ((cp >= 0x11A8 && cp <= 0x11FF) || (cp >= 0xD7CB && cp <= 0xD7FB)) {
                return 3;
            }
            if (cp >= 0xAC00 && cp <= 0xD7A3) {
                return (cp - 0xAC00) % 28 == 0 ? 4 : 5;
            }
            return 0;
        }
    }
}
