using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Dashboard : ICompactWidget {
        /// <summary>
        /// The smallest window the full layout needs (default 60 × 14). Below it a live session renders the compact form:
        /// a one-line status and the highest-priority <see cref="Prioritized"/> widgets that fit.
        /// </summary>
        public Size MinimumSize { get; set; } = new Size(60, 14);

        /// <summary>
        /// The progress text for the compact status line (for example <c>218/332 · ✗12 ▲25</c>), or null to use
        /// <see cref="Detail"/>. Called on the render thread.
        /// </summary>
        public Func<string>? CompactSummary { get; set; }

        /// <inheritdoc />
        public void RenderCompact(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            region.Write(0, 0, CompactLine(region.Width, region.Height, context));
            int y = 1;
            List<Prioritized> widgets = new List<Prioritized>();
            Collect(Body, widgets);
            widgets.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            foreach (Prioritized widget in widgets) {
                int room = region.Height - y;
                if (room < widget.MinimumSize.Height || region.Width < Math.Min(widget.MinimumSize.Width, region.Width)) {
                    continue;
                }
                Size size = widget.Measure(new Constraints(region.Width, room), context);
                int height = Math.Max(widget.MinimumSize.Height, Math.Min(room, size.Height));
                widget.Render(region.Sub(0, y, region.Width, height), context);
                y += height;
                if (y >= region.Height) {
                    break;
                }
            }
        }

        private StyledLine CompactLine(int width, int height, RenderContext context) {
            string dot = " " + context.Glyphs.Get(Glyph.Dot) + " ";
            Style muted = context.Style(ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in context.Markup(Title).Lines[0].WithBaseStyle(context.Style(ThemeToken.Title))) {
                spans.Add(span);
            }
            string? summary = CompactSummary != null ? CompactSummary() : (Detail != null ? Markup.Strip(Detail) : null);
            if (!string.IsNullOrEmpty(summary)) {
                spans.Add(new TextSpan(dot, muted));
                foreach (TextSpan span in context.Markup(summary!).Lines[0]) {
                    spans.Add(span);
                }
            }
            if (ShowElapsed) {
                spans.Add(new TextSpan(dot, muted));
                spans.Add(new TextSpan(NumberFormat.Clock(context.Elapsed), context.Style(ThemeToken.Accent)));
            }
            string times = context.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? "x" : "×";
            string needs = MinimumSize.Width.ToString(CultureInfo.InvariantCulture) + times + MinimumSize.Height.ToString(CultureInfo.InvariantCulture);
            string note = Localization.WidgetLabels.Format(context.Labels.WindowTooSmall, width.ToString(CultureInfo.InvariantCulture) + times + height.ToString(CultureInfo.InvariantCulture), needs);
            StyledLine noteLine = StyledLine.From(dot + note, muted);
            if (width - noteLine.Width < 24) {
                // Keep the progress readable: the short form still says what the window needs.
                note = Localization.WidgetLabels.Format(context.Labels.Needs, needs);
                noteLine = StyledLine.From(dot + note, muted);
            }
            if (noteLine.Width >= width - 8) {
                // Very narrow: the size note alone (it says what to do).
                return TextWrapper.Truncate(StyledLine.From(note, muted), width, context.Ellipsis);
            }
            List<TextSpan> line = new List<TextSpan>(TextWrapper.Truncate(new StyledLine(spans), width - noteLine.Width, context.Ellipsis));
            line.AddRange(noteLine);
            return new StyledLine(line);
        }

        private static void Collect(IWidget widget, List<Prioritized> found) {
            if (widget is Prioritized prioritized) {
                found.Add(prioritized);
                return;
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    Collect(child, found);
                }
            }
        }
    }
}
