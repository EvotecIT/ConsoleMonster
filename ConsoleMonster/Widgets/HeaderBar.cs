using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The one header bar of every full-screen surface (dashboards, the Shell app, full-screen forms, runs started from a
    /// menu): a row on the Surface band with the title in SurfaceAccent, then the breadcrumb or a subtitle, and on the
    /// right a status (with its marker), free text and the elapsed clock. Without colour the band is left out and the same
    /// text is drawn plain. See Docs/Design-System.md.
    /// </summary>
    public sealed class HeaderBar : IWidget {
        /// <summary>Creates a header bar.</summary>
        /// <param name="title">The title (markup).</param>
        public HeaderBar(string title) {
            Title = title ?? string.Empty;
        }

        /// <summary>The title (markup).</summary>
        public string Title { get; set; }

        /// <summary>Breadcrumb steps after the title, plain text ("Tasks", "Review &amp; run").</summary>
        public IList<string> Crumbs { get; } = new List<string>();

        /// <summary>A subtitle after the title, separated by a dot (markup).</summary>
        public string? Subtitle { get; set; }

        /// <summary>Status text on the right (markup).</summary>
        public string? Status { get; set; }

        /// <summary>The status kind; its marker goes before the status text.</summary>
        public StatusKind? StatusLevel { get; set; }

        /// <summary>Plain text on the right, before the clock (an app's subtitle, a step count).</summary>
        public string? Right { get; set; }

        /// <summary>Shows the elapsed clock on the right.</summary>
        public bool ShowClock { get; set; }

        /// <summary>The clock value; null uses the render context's elapsed time.</summary>
        public TimeSpan? Clock { get; set; }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            Region bar = region.Sub(0, 0, region.Width, 1);
            Style surface = context.Style(ThemeToken.Surface);
            bool hasSurface = context.HasColor && !surface.Background.IsDefault;
            if (hasSurface) {
                bar.Fill(surface);
            }
            Style accent = hasSurface ? context.Style(ThemeToken.SurfaceAccent) : context.Style(ThemeToken.Title);
            Style text = hasSurface ? surface : context.Style(ThemeToken.Text);
            Style quiet = hasSurface ? surface.Without(TextDecoration.Bold) : context.Style(ThemeToken.Muted);
            string separator = " " + InteractiveGlyphs.For(context).Separator + " ";

            List<TextSpan> right = new List<TextSpan>();
            void Gap() {
                if (right.Count > 0) {
                    right.Add(new TextSpan(" " + separator + " ", quiet));
                }
            }
            if (!string.IsNullOrEmpty(Status)) {
                if (StatusLevel.HasValue) {
                    Style marker = context.Style(StatusLevel.Value);
                    right.Add(new TextSpan(context.Glyphs.StatusMarker(StatusLevel.Value) + " ", hasSurface ? marker.WithBackground(surface.Background) : marker));
                }
                foreach (TextSpan span in context.Markup(Status!).Lines[0]) {
                    right.Add(new TextSpan(span.Text, text.Combine(span.Style)));
                }
            }
            if (!string.IsNullOrEmpty(Right)) {
                Gap();
                right.Add(new TextSpan(Right!, quiet));
            }
            if (ShowClock) {
                Gap();
                right.Add(new TextSpan(NumberFormat.Clock(Clock ?? context.Elapsed), accent));
            }
            StyledLine rightLine = new StyledLine(right);
            int rightX = Math.Max(0, bar.Width - rightLine.Width - 1);

            List<TextSpan> left = new List<TextSpan>();
            foreach (TextSpan span in context.Markup(Title).Lines[0]) {
                left.Add(new TextSpan(span.Text, accent.Combine(span.Style)));
            }
            string breadcrumb = " " + InteractiveGlyphs.For(context).Breadcrumb + " ";
            foreach (string crumb in Crumbs) {
                left.Add(new TextSpan(breadcrumb, quiet));
                left.Add(new TextSpan(crumb, text));
            }
            if (!string.IsNullOrEmpty(Subtitle)) {
                left.Add(new TextSpan(separator, quiet));
                foreach (TextSpan span in context.Markup(Subtitle!).Lines[0]) {
                    left.Add(new TextSpan(span.Text, text.Combine(span.Style)));
                }
            }
            StyledLine leftLine = TextWrapper.Truncate(new StyledLine(left), Math.Max(0, (rightLine.Width > 0 ? rightX - 2 : bar.Width - 1) - 1), context.Ellipsis);
            bar.Write(1, 0, leftLine);
            if (rightLine.Width > 0 && rightX > leftLine.Width + 2) {
                bar.Write(rightX, 0, rightLine);
            }
        }
    }
}
