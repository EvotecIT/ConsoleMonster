using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>A horizontal line across the width, optionally with a title: <c>── Results ──────</c>.</summary>
    public sealed class Rule : IWidget {
        /// <summary>Creates a rule.</summary>
        /// <param name="title">The title (markup), or <c>null</c> for a plain line.</param>
        public Rule(string? title = null) {
            Title = title;
        }

        /// <summary>The title (markup).</summary>
        public string? Title { get; set; }

        /// <summary>Title alignment (default left).</summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>The line's theme token (default <see cref="ThemeToken.Border"/>).</summary>
        public ThemeToken LineToken { get; set; } = ThemeToken.Border;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Height <= 0 || region.Width <= 0) {
                return;
            }
            string glyph = context.Glyphs.Get(Glyph.RuleLine);
            Style line = context.Style(LineToken);
            region.HorizontalLine(0, 0, region.Width, glyph, line);
            if (string.IsNullOrEmpty(Title) || region.Width < 8) {
                return;
            }
            StyledLine title = TextWrapper.Truncate(context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Title)), region.Width - 6, context.Ellipsis);
            int x;
            switch (Alignment) {
                case HorizontalAlignment.Center:
                    x = Math.Max(2, (region.Width - title.Width) / 2);
                    break;
                case HorizontalAlignment.Right:
                    x = Math.Max(2, region.Width - title.Width - 3);
                    break;
                default:
                    x = 3;
                    break;
            }
            region.Put(x - 1, 0, " ", Style.Plain);
            int end = region.Write(x, 0, title);
            region.Put(end, 0, " ", Style.Plain);
        }
    }
}
