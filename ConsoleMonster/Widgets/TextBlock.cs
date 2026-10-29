using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Text with Spectre-compatible markup (<c>[bold]</c>, <c>[pass]</c>, <c>[#ff8800]</c>), wrapped or truncated to its
    /// width and aligned left, centre or right.
    /// </summary>
    public sealed class TextBlock : IWidget {
        /// <summary>Creates a text block from markup.</summary>
        /// <param name="markup">The markup.</param>
        public TextBlock(string markup) {
            Content = markup ?? string.Empty;
        }

        /// <summary>The content (markup unless <see cref="IsMarkup"/> is false).</summary>
        public string Content { get; set; }

        /// <summary>Whether <see cref="Content"/> is markup (default true).</summary>
        public bool IsMarkup { get; set; } = true;

        /// <summary>A theme role layered under the text, or <c>null</c>.</summary>
        public ThemeToken? Role { get; set; }

        /// <summary>Alignment (default left).</summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>Overflow handling (default wrap).</summary>
        public TextOverflow Overflow { get; set; } = TextOverflow.Wrap;

        /// <summary>When set, wrap at this literal separator before breaking within an oversized entry.</summary>
        public string? WrapSeparator { get; set; }

        /// <summary>Creates a text block from plain text (brackets are shown literally).</summary>
        /// <param name="text">The text.</param>
        /// <param name="role">An optional theme role.</param>
        /// <returns>The text block.</returns>
        public static TextBlock Plain(string text, ThemeToken? role = null) => new TextBlock(text ?? string.Empty) { IsMarkup = false, Role = role };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            List<StyledLine> lines = BuildLines(context, constraints.MaxWidth);
            int width = 0;
            foreach (StyledLine line in lines) {
                width = Math.Max(width, line.Width);
            }
            if (Alignment != HorizontalAlignment.Left) {
                width = constraints.MaxWidth;
            }
            int height = constraints.IsHeightUnbounded ? lines.Count : Math.Min(lines.Count, constraints.MaxHeight);
            return new Size(Math.Min(width, constraints.MaxWidth), height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            List<StyledLine> lines = BuildLines(context, region.Width);
            for (int i = 0; i < lines.Count && i < region.Height; i++) {
                StyledLine line = lines[i];
                int x = 0;
                if (Alignment == HorizontalAlignment.Center) {
                    x = Math.Max(0, (region.Width - line.Width) / 2);
                } else if (Alignment == HorizontalAlignment.Right) {
                    x = Math.Max(0, region.Width - line.Width);
                }
                region.Write(x, i, line);
            }
        }

        private List<StyledLine> BuildLines(RenderContext context, int width) {
            string? separator = WrapSeparator;
            StyledText text = IsMarkup ? context.Markup(Content) : StyledText.FromPlain(Content);
            Style baseStyle = Role.HasValue ? context.Style(Role.Value) : Style.Plain;
            List<StyledLine> result = new List<StyledLine>();
            foreach (StyledLine raw in text.Lines) {
                StyledLine line = raw.WithBaseStyle(baseStyle);
                StyledLine convertedLine = WrapSeparator == " · " ? FactFlow.TierSeparators(line, context) : line;
                if (convertedLine.Width <= width || width <= 0) {
                    result.Add(convertedLine);
                } else if (Overflow == TextOverflow.Wrap) {
                    IEnumerable<StyledLine> wrapped = !string.IsNullOrEmpty(separator) && line.PlainText.Contains(separator!)
                        ? FactFlow.WrapSeparated(line, width, separator!, preserveSeparator: separator != " · ",
                            convert: separator == " · " ? part => FactFlow.TierSeparators(part, context) : null)
                        : TextWrapper.Wrap(convertedLine, width);
                    result.AddRange(wrapped);
                } else {
                    result.Add(TextWrapper.Truncate(convertedLine, width, Overflow == TextOverflow.Ellipsis ? context.Ellipsis : string.Empty));
                }
            }
            return result;
        }
    }
}
