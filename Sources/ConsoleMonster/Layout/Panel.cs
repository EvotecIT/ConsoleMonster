using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// A bordered box with a title in the top edge, an optional caption on the right of the top edge (counts, clocks)
    /// and an optional footer in the bottom edge. Borders follow the glyph tier (rounded on Full, single on Basic,
    /// <c>+-|</c> on Ascii). Inside a <see cref="Columns"/> row, panels stretch to the row height.
    /// </summary>
    public sealed class Panel : IContainer {
        /// <summary>Creates a panel.</summary>
        /// <param name="child">The content.</param>
        /// <param name="title">The title (markup), or <c>null</c>.</param>
        public Panel(IWidget child, string? title = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Title = title;
        }

        /// <summary>The content.</summary>
        public IWidget Child { get; }

        /// <summary>The title shown in the top edge (markup).</summary>
        public string? Title { get; set; }

        /// <summary>Text on the right of the top edge (markup), for example a count or a clock.</summary>
        public string? Caption { get; set; }

        /// <summary>Text on the right of the bottom edge (markup), for example "+12 more".</summary>
        public string? Footer { get; set; }

        /// <summary>The border style (default rounded).</summary>
        public BoxStyle Border { get; set; } = BoxStyle.Rounded;

        /// <summary>The theme token for the border (default <see cref="ThemeToken.Border"/>).</summary>
        public ThemeToken BorderToken { get; set; } = ThemeToken.Border;

        /// <summary>Cells between the left border and the content (default 1).</summary>
        public int PaddingLeft { get; set; } = 1;

        /// <summary>Cells between the content and the right border (default 1).</summary>
        public int PaddingRight { get; set; } = 1;

        /// <summary>Rows between the top border and the content (default 0).</summary>
        public int PaddingTop { get; set; }

        /// <summary>Rows between the content and the bottom border (default 0).</summary>
        public int PaddingBottom { get; set; }

        /// <summary>Whether the panel takes the full available width (default true).</summary>
        public bool Expand { get; set; } = true;

        /// <summary>A fixed total height including borders, or <c>null</c> to fit the content.</summary>
        public int? Height { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Child };

        private bool HasBorder => Border != BoxStyle.None;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int horizontal = (HasBorder ? 2 : 0) + PaddingLeft + PaddingRight;
            int vertical = (HasBorder ? 2 : (string.IsNullOrEmpty(Title) ? 0 : 1)) + PaddingTop + PaddingBottom;
            Size inner = Child.Measure(constraints.Shrink(horizontal, vertical), context);
            int titleWidth = string.IsNullOrEmpty(Title) ? 0 : context.Markup(Title!).Width + 4;
            int width = Expand ? constraints.MaxWidth : Math.Min(constraints.MaxWidth, Math.Max(inner.Width + horizontal, titleWidth + 2));
            int height = Height ?? inner.Height + vertical;
            return new Size(width, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            if (!HasBorder) {
                int top = 0;
                if (!string.IsNullOrEmpty(Title)) {
                    StyledLine title = TitleLine(context, region.Width);
                    region.Write(PaddingLeft, 0, title);
                    top = 1;
                }
                Child.Render(region.Sub(PaddingLeft, top + PaddingTop, region.Width - PaddingLeft - PaddingRight, region.Height - top - PaddingTop - PaddingBottom), context);
                return;
            }
            if (region.Width < 4 || region.Height < 2) {
                Child.Render(region, context);
                return;
            }
            DrawBorder(region, context);
            Child.Render(region.Sub(1 + PaddingLeft, 1 + PaddingTop, region.Width - 2 - PaddingLeft - PaddingRight, region.Height - 2 - PaddingTop - PaddingBottom), context);
        }

        private void DrawBorder(Region region, RenderContext context) {
            BoxGlyphs box = context.Glyphs.GetBox(Border);
            Style border = context.Style(BorderToken);
            int w = region.Width;
            int h = region.Height;
            region.Put(0, 0, box.TopLeft, border);
            region.HorizontalLine(1, 0, w - 2, box.Horizontal, border);
            region.Put(w - 1, 0, box.TopRight, border);
            region.VerticalLine(0, 1, h - 2, box.Vertical, border);
            region.VerticalLine(w - 1, 1, h - 2, box.Vertical, border);
            region.Put(0, h - 1, box.BottomLeft, border);
            region.HorizontalLine(1, h - 1, w - 2, box.Horizontal, border);
            region.Put(w - 1, h - 1, box.BottomRight, border);

            int captionWidth = 0;
            if (!string.IsNullOrEmpty(Caption)) {
                StyledLine caption = TextWrapper.Truncate(context.Markup(Caption!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), Math.Max(0, w - 8), context.Ellipsis);
                captionWidth = caption.Width + 2;
                int x = w - 2 - caption.Width - 1;
                if (x > 2) {
                    region.Put(x - 1, 0, " ", Style.Plain);
                    region.Write(x, 0, caption);
                    region.Put(x + caption.Width, 0, " ", Style.Plain);
                } else {
                    captionWidth = 0;
                }
            }
            if (!string.IsNullOrEmpty(Title)) {
                int available = w - 6 - (captionWidth > 0 ? captionWidth + 1 : 0);
                if (available > 0) {
                    StyledLine title = TitleLine(context, available);
                    region.Put(2, 0, " ", Style.Plain);
                    int end = region.Write(3, 0, title);
                    region.Put(end, 0, " ", Style.Plain);
                }
            }
            if (!string.IsNullOrEmpty(Footer)) {
                StyledLine footer = TextWrapper.Truncate(context.Markup(Footer!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), Math.Max(0, w - 8), context.Ellipsis);
                int x = w - 2 - footer.Width - 1;
                if (x > 2) {
                    region.Put(x - 1, h - 1, " ", Style.Plain);
                    region.Write(x, h - 1, footer);
                    region.Put(x + footer.Width, h - 1, " ", Style.Plain);
                }
            }
        }

        private StyledLine TitleLine(RenderContext context, int width) {
            StyledLine line = context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Title));
            return TextWrapper.Truncate(line, width, context.Ellipsis);
        }
    }
}
