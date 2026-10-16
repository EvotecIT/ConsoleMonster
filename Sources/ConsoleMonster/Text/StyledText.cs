using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>Multi-line styled text: the result of parsing markup or splitting plain text into lines.</summary>
    public sealed class StyledText {
        /// <summary>Creates styled text from lines.</summary>
        /// <param name="lines">The lines.</param>
        public StyledText(IReadOnlyList<StyledLine> lines) {
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        }

        /// <summary>The lines.</summary>
        public IReadOnlyList<StyledLine> Lines { get; }

        /// <summary>The widest line in cells.</summary>
        public int Width {
            get {
                int width = 0;
                foreach (StyledLine line in Lines) {
                    width = Math.Max(width, line.Width);
                }
                return width;
            }
        }

        /// <summary>Splits plain text (no markup) into lines with one style.</summary>
        /// <param name="text">The text; <c>\r\n</c>, <c>\n</c> and <c>\r</c> separate lines.</param>
        /// <param name="style">The style.</param>
        /// <returns>Styled text.</returns>
        public static StyledText FromPlain(string? text, Style style = default) {
            List<StyledLine> lines = new List<StyledLine>();
            string value = text ?? string.Empty;
            string[] parts = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string part in parts) {
                lines.Add(StyledLine.From(part.Replace("\t", "    "), style));
            }
            return new StyledText(lines);
        }

        /// <summary>Creates styled text from spans that may contain line breaks.</summary>
        /// <param name="spans">The spans.</param>
        /// <returns>Styled text.</returns>
        public static StyledText FromSpans(IEnumerable<TextSpan> spans) {
            if (spans == null) {
                throw new ArgumentNullException(nameof(spans));
            }
            List<StyledLine> lines = new List<StyledLine>();
            List<TextSpan> current = new List<TextSpan>();
            foreach (TextSpan span in spans) {
                string text = span.Text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
                int start = 0;
                while (true) {
                    int newline = text.IndexOf('\n', start);
                    if (newline < 0) {
                        if (start < text.Length) {
                            current.Add(new TextSpan(text.Substring(start), span.Style));
                        }
                        break;
                    }
                    if (newline > start) {
                        current.Add(new TextSpan(text.Substring(start, newline - start), span.Style));
                    }
                    lines.Add(new StyledLine(current));
                    current.Clear();
                    start = newline + 1;
                }
            }
            lines.Add(new StyledLine(current));
            return new StyledText(lines);
        }
    }
}
