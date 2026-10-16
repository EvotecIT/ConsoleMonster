using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A full-screen shell for live sessions: a header bar (title, subtitle, status and elapsed clock), the body and a
    /// footer bar (hints such as "Ctrl+C to stop"). In print-once output it renders the same parts top to bottom.
    /// Live operations: <c>title</c> (Text), <c>subtitle</c> (Text), <c>status</c> (Text, State), <c>footer</c> (Text).
    /// </summary>
    public sealed class Dashboard : LiveWidget, IContainer {
        /// <summary>Creates a dashboard.</summary>
        /// <param name="title">The title (markup).</param>
        /// <param name="body">The body.</param>
        /// <param name="name">The live name (default <c>dashboard</c>).</param>
        public Dashboard(string title, IWidget body, string name = "dashboard") : base(name) {
            Title = title ?? string.Empty;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>The title (markup).</summary>
        public string Title { get; set; }

        /// <summary>The subtitle (markup), shown after the title.</summary>
        public string? Subtitle { get; set; }

        /// <summary>A status shown on the right of the header (markup).</summary>
        public string? Status { get; set; }

        /// <summary>The status kind, shown as a marker before <see cref="Status"/>.</summary>
        public StatusKind? StatusLevel { get; set; }

        /// <summary>The footer text (markup), or <c>null</c> for no footer.</summary>
        public string? Footer { get; set; } = "[muted]Ctrl+C[/] stop";

        /// <summary>Whether the elapsed clock is shown in the header (default true).</summary>
        public bool ShowClock { get; set; } = true;

        /// <summary>The body.</summary>
        public IWidget Body { get; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Body };

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            switch (liveEvent.Operation) {
                case "title":
                    Title = liveEvent.Text ?? string.Empty;
                    break;
                case "subtitle":
                    Subtitle = liveEvent.Text;
                    break;
                case "status":
                    Status = liveEvent.Text;
                    StatusLevel = StatusKinds.TryParse(liveEvent.State, out StatusKind kind) ? kind : (StatusKind?)null;
                    break;
                case "footer":
                    Footer = liveEvent.Text;
                    break;
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            return liveEvent.Operation == "status" && !string.IsNullOrEmpty(liveEvent.Text) ? Markup.Strip(liveEvent.Text) : null;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int chrome = 1 + (string.IsNullOrEmpty(Footer) ? 0 : 1);
            Size body = Body.Measure(constraints.Shrink(0, chrome + 1), context);
            int height = body.Height + chrome + 1;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0) {
                return;
            }
            DrawHeader(region.Sub(0, 0, region.Width, 1), context);
            bool footer = !string.IsNullOrEmpty(Footer) && region.Height > 3;
            int bodyHeight = region.Height - 2 - (footer ? 1 : 0);
            if (bodyHeight > 0) {
                Body.Render(region.Sub(0, 2, region.Width, bodyHeight), context);
            }
            if (footer) {
                Region bar = region.Sub(0, region.Height - 1, region.Width, 1);
                StyledLine text = TextWrapper.Truncate(context.Markup(Footer!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), bar.Width - 2, context.Ellipsis);
                bar.Write(1, 0, text);
            }
        }

        private void DrawHeader(Region bar, RenderContext context) {
            Style surface = context.Style(ThemeToken.Surface);
            bool hasSurface = context.HasColor && !surface.Background.IsDefault;
            if (hasSurface) {
                bar.Fill(surface);
            }
            Style accent = hasSurface ? context.Style(ThemeToken.SurfaceAccent) : context.Style(ThemeToken.Title);
            Style text = hasSurface ? surface : context.Style(ThemeToken.Text);
            Style quiet = hasSurface ? surface.Without(TextDecoration.Bold) : context.Style(ThemeToken.Muted);

            List<TextSpan> right = new List<TextSpan>();
            if (!string.IsNullOrEmpty(Status)) {
                if (StatusLevel.HasValue) {
                    Style marker = context.Style(StatusLevel.Value);
                    right.Add(new TextSpan(context.Glyphs.StatusMarker(StatusLevel.Value) + " ", hasSurface ? marker.WithBackground(surface.Background) : marker));
                }
                foreach (TextSpan span in context.Markup(Status!).Lines[0]) {
                    right.Add(new TextSpan(span.Text, text.Combine(span.Style)));
                }
            }
            if (ShowClock) {
                if (right.Count > 0) {
                    right.Add(new TextSpan("  " + context.Glyphs.Get(Glyph.Separator) + "  ", quiet));
                }
                right.Add(new TextSpan(NumberFormat.Clock(context.Elapsed), accent));
            }
            StyledLine rightLine = new StyledLine(right);
            int rightX = Math.Max(0, bar.Width - rightLine.Width - 1);

            List<TextSpan> left = new List<TextSpan>();
            foreach (TextSpan span in context.Markup(Title).Lines[0]) {
                left.Add(new TextSpan(span.Text, accent.Combine(span.Style)));
            }
            if (!string.IsNullOrEmpty(Subtitle)) {
                left.Add(new TextSpan("  " + context.Glyphs.Get(Glyph.Dot) + "  ", quiet));
                foreach (TextSpan span in context.Markup(Subtitle!).Lines[0]) {
                    left.Add(new TextSpan(span.Text, text.Combine(span.Style)));
                }
            }
            StyledLine leftLine = TextWrapper.Truncate(new StyledLine(left), Math.Max(0, rightX - 3), context.Ellipsis);
            bar.Write(1, 0, leftLine);
            if (rightLine.Width > 0 && rightX > leftLine.Width + 2) {
                bar.Write(rightX, 0, rightLine);
            }
        }
    }
}
