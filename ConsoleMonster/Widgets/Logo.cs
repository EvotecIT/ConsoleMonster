using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Banner text drawn with a built-in 5x7 pixel font on a half-block canvas (two pixel rows per cell, four cell rows
    /// tall) with a horizontal colour gradient, and an optional tagline. On the Ascii tier the same font is drawn with
    /// <c>#</c>; when the banner does not fit, it falls back to bold text.
    /// </summary>
    public sealed class Logo : IWidget {
        /// <summary>Creates a logo.</summary>
        /// <param name="text">The banner text (A-Z, 0-9 and - . : ! / _).</param>
        /// <param name="tagline">An optional tagline under the banner (markup).</param>
        public Logo(string text, string? tagline = null) {
            Text = text ?? string.Empty;
            Tagline = tagline;
        }

        /// <summary>The banner text.</summary>
        public string Text { get; set; }

        /// <summary>The tagline (markup).</summary>
        public string? Tagline { get; set; }

        /// <summary>The gradient start colour (default sky blue).</summary>
        public Color From { get; set; } = Color.FromRgb(56, 189, 248);

        /// <summary>The gradient end colour (default violet).</summary>
        public Color To { get; set; } = Color.FromRgb(192, 132, 252);

        /// <summary>Alignment of the banner and tagline (default left).</summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        private int PixelWidth => Math.Max(0, (Text.Length * (PixelFont.GlyphWidth + 1)) - 1);

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            bool banner = PixelWidth <= constraints.MaxWidth;
            int height = banner ? BannerRows(context) : 1;
            if (!string.IsNullOrEmpty(Tagline)) {
                height++;
            }
            int width = Alignment == HorizontalAlignment.Left ? Math.Min(constraints.MaxWidth, Math.Max(PixelWidth, TaglineWidth(context))) : constraints.MaxWidth;
            return new Size(width, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            int width = PixelWidth;
            int x = Offset(region.Width, width);
            int y = 0;
            if (width > region.Width) {
                region.Write(Offset(region.Width, DisplayWidth.Measure(Text)), 0, Text, context.Style(ThemeToken.Accent).With(TextDecoration.Bold));
                y = 1;
            } else if (context.Glyphs.Tier == GlyphTier.Ascii) {
                for (int py = 0; py < PixelFont.GlyphHeight; py++) {
                    for (int px = 0; px < width; px++) {
                        if (IsSet(px, py)) {
                            region.Put(x + px, py, "#", new Style(Gradient(px, width)).Downsample(context.Depth));
                        }
                    }
                }
                y = PixelFont.GlyphHeight;
            } else {
                HalfBlockCanvas canvas = new HalfBlockCanvas(width, (PixelFont.GlyphHeight + 1) / 2);
                for (int py = 0; py < PixelFont.GlyphHeight; py++) {
                    for (int px = 0; px < width; px++) {
                        if (IsSet(px, py)) {
                            canvas.Set(px, py, Gradient(px, width));
                        }
                    }
                }
                canvas.Render(region, x, 0, context);
                y = canvas.Rows;
            }
            if (!string.IsNullOrEmpty(Tagline) && y < region.Height) {
                StyledLine tagline = TextWrapper.Truncate(context.Markup(Tagline!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), region.Width, context.Ellipsis);
                region.Write(Offset(region.Width, tagline.Width), y, tagline);
            }
        }

        private int BannerRows(RenderContext context) => context.Glyphs.Tier == GlyphTier.Ascii ? PixelFont.GlyphHeight : (PixelFont.GlyphHeight + 1) / 2;

        private int TaglineWidth(RenderContext context) => string.IsNullOrEmpty(Tagline) ? 0 : context.Markup(Tagline!).Width;

        private int Offset(int available, int width) {
            switch (Alignment) {
                case HorizontalAlignment.Center:
                    return Math.Max(0, (available - width) / 2);
                case HorizontalAlignment.Right:
                    return Math.Max(0, available - width);
                default:
                    return 0;
            }
        }

        private bool IsSet(int px, int py) {
            int index = px / (PixelFont.GlyphWidth + 1);
            int column = px % (PixelFont.GlyphWidth + 1);
            if (index >= Text.Length || column >= PixelFont.GlyphWidth) {
                return false;
            }
            return PixelFont.IsSet(Text[index], column, py);
        }

        private Color Gradient(int px, int width) => From.Blend(To, width <= 1 ? 0 : (double)px / (width - 1));
    }
}
