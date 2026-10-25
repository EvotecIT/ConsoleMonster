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
    public sealed class Panel : IContainer, IContentOwner {
        /// <summary>Creates a panel with a multicolour title.</summary>
        /// <param name="content">The content.</param>
        /// <param name="title">The title.</param>
        public Panel(IWidget content, Text.RichText title) : this(content, (title ?? throw new ArgumentNullException(nameof(title))).ToMarkup()) {
        }

        /// <summary>Creates a panel around a content with an optional title (markup).</summary>
        /// <param name="content">The content.</param>
        /// <param name="title">The title, or null.</param>
        public Panel(IWidget? content = null, string? title = null) {
            Content = content ?? new Rows();
            Title = title;
        }

        /// <summary>The content.</summary>
        public IWidget Content { get; set; }

        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);

        /// <summary>The title shown in the top edge (markup).</summary>
        public string? Title { get; set; }

        /// <summary>Text on the right of the top edge (markup), for example a count or a clock.</summary>
        public string? HeaderRight { get; set; }

        /// <summary>Text on the right of the bottom edge (markup), for example "+12 more".</summary>
        public string? Caption { get; set; }

        /// <summary>The border style (default rounded).</summary>
        public BoxStyle Border { get; set; } = BoxStyle.Rounded;

        /// <summary>The theme token for the border (default <see cref="ThemeToken.Border"/>).</summary>
        public ThemeToken BorderRole { get; set; } = ThemeToken.Border;

        /// <summary>Uses a double border and a centred title for a verdict.</summary>
        public bool Verdict { get; set; }

        /// <summary>Optional status colour and title glyph for a verdict.</summary>
        public StatusKind? Status { get; set; }

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
        public IReadOnlyList<IWidget> Children => new[] { Content };

        private bool HasBorder => Border != BoxStyle.None;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int horizontal = (HasBorder ? 2 : 0) + PaddingLeft + PaddingRight;
            int vertical = (HasBorder ? 2 : (string.IsNullOrEmpty(Title) ? 0 : 1)) + PaddingTop + PaddingBottom;
            Size inner = Content.Measure(constraints.Shrink(horizontal, vertical), context);
            int titleWidth = string.IsNullOrEmpty(Title) ? 0 : context.Markup(Title!).Width + 4 + (Verdict && Status.HasValue ? DisplayWidth.Measure(context.Glyphs.StatusSymbol(Status.Value)) + 1 : 0);
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
                Content.Render(region.Sub(PaddingLeft, top + PaddingTop, region.Width - PaddingLeft - PaddingRight, region.Height - top - PaddingTop - PaddingBottom), context);
                return;
            }
            if (region.Width < 4 || region.Height < 2) {
                Content.Render(region, context);
                return;
            }
            DrawBorder(region, context);
            Content.Render(region.Sub(1 + PaddingLeft, 1 + PaddingTop, region.Width - 2 - PaddingLeft - PaddingRight, region.Height - 2 - PaddingTop - PaddingBottom), context);
        }

        private void DrawBorder(Region region, RenderContext context) {
            BoxGlyphs box = context.Glyphs.GetBox(Verdict ? BoxStyle.Double : Border);
            Style border = context.Style(Verdict && Status.HasValue ? Status.Value.ToToken() : BorderRole);
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
            if (!string.IsNullOrEmpty(HeaderRight)) {
                StyledLine caption = TextWrapper.Truncate(context.Markup(HeaderRight!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), Math.Max(0, w - 8), context.Ellipsis);
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
                    int start = Verdict ? Math.Max(2, (w - captionWidth - title.Width) / 2) : 3;
                    region.Put(start - 1, 0, " ", Style.Plain);
                    int end = region.Write(start, 0, title);
                    region.Put(end, 0, " ", Style.Plain);
                }
            }
            if (!string.IsNullOrEmpty(Caption)) {
                StyledLine footer = TextWrapper.Truncate(context.Markup(Caption!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), Math.Max(0, w - 8), context.Ellipsis);
                int x = w - 2 - footer.Width - 1;
                if (x > 2) {
                    region.Put(x - 1, h - 1, " ", Style.Plain);
                    region.Write(x, h - 1, footer);
                    region.Put(x + footer.Width, h - 1, " ", Style.Plain);
                }
            }
        }

        private StyledLine TitleLine(RenderContext context, int width) {
            Style style = context.Style(Verdict && Status.HasValue ? Status.Value.ToToken() : ThemeToken.Title);
            var spans = new List<TextSpan>();
            if (Verdict && Status.HasValue) spans.Add(new TextSpan(context.Glyphs.StatusSymbol(Status.Value) + " ", style));
            spans.AddRange(context.Markup(Title!).Lines[0].WithBaseStyle(style));
            var line = new StyledLine(spans);
            return TextWrapper.Truncate(line, width, context.Ellipsis);
        }
    }
}
