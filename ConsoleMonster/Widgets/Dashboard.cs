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
    public sealed partial class Dashboard : LiveWidget, IContainer {
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
        public string? Footer { get; set; }

        /// <summary>The key hints in the footer when <see cref="Footer"/> is not set (default: ctrl+c stop).</summary>
        public IList<KeyHint> FooterHints { get; } = new List<KeyHint> { new KeyHint("ctrl+c", "stop") };

        private bool HasFooter => !string.IsNullOrEmpty(Footer) || FooterHints.Count > 0;

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
            int chrome = 1 + (HasFooter ? 1 : 0);
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
            bool footer = HasFooter && region.Height > 3;
            int bodyHeight = region.Height - 2 - (footer ? 1 : 0);
            if (bodyHeight > 0) {
                Body.Render(region.Sub(0, 2, region.Width, bodyHeight), context);
            }
            if (footer) {
                DrawFooter(region.Sub(0, region.Height - 1, region.Width, 1), context);
            }
        }

        // Header and footer are the shared bars, so a dashboard looks the same full screen, inline and inside a menu run.
        private void DrawHeader(Region bar, RenderContext context) {
            new HeaderBar(Title) { Subtitle = Subtitle, Status = Status, StatusLevel = StatusLevel, ShowClock = ShowClock }.Render(bar, context);
        }

        private void DrawFooter(Region bar, RenderContext context) {
            FooterBar footer = new FooterBar(FooterHints);
            if (!string.IsNullOrEmpty(Footer)) {
                footer.Message = context.Markup(Footer!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted));
            }
            footer.Render(bar, context);
        }
    }
}