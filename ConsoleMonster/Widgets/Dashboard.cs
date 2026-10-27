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
    public sealed partial class Dashboard : LiveWidget, IContainer, IContentOwner, Output.ITaskbarProgressSource {
        /// <summary>Creates a dashboard.</summary>
        /// <param name="title">The title (markup).</param>
        /// <param name="content">The body.</param>
        /// <param name="widgetId">The live name (default <c>dashboard</c>).</param>
        public Dashboard(string title = "", IWidget? content = null, string widgetId = "dashboard") : base(widgetId) {
            Title = title ?? string.Empty;
            Content = content ?? new Rows();
        }

        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);

        /// <summary>The title (markup).</summary>
        public string Title { get; set; }

        /// <summary>The subtitle (markup), shown after the title.</summary>
        public string? Subtitle { get; set; }

        /// <summary>A status shown on the right of the header (markup).</summary>
        public string? Detail { get; set; }

        /// <summary>The status kind, shown as a marker before <see cref="Detail"/>.</summary>
        public StatusKind? Status { get; set; }

        /// <summary>The footer text (markup), or <c>null</c> for no footer.</summary>
        public string? Footer { get; set; }

        /// <summary>The key hints in the footer when <see cref="Footer"/> is not set (default: ctrl+c stop).</summary>
        public IList<KeyHint> FooterHints { get; } = new List<KeyHint> { new KeyHint("ctrl+c", Localization.WidgetLabels.Current.Stop) };

        private bool HasFooter => !string.IsNullOrEmpty(Footer) || FooterHints.Count > 0;

        /// <summary>Whether the elapsed clock is shown in the header (default true).</summary>
        public bool ShowElapsed { get; set; } = true;

        /// <summary>The body.</summary>
        public IWidget Content { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Content };

        /// <summary>The run state for the terminal's indicator: red after a failure status, yellow after a warning status.</summary>
        /// <param name="state">The state.</param>
        /// <param name="fraction">Always NaN (the body's widgets report the percentage).</param>
        /// <returns>Whether the status is a failure or a warning.</returns>
        public bool TryGetTaskbarProgress(out Output.TaskbarState state, out double fraction) {
            fraction = double.NaN;
            state = Status == StatusKind.Fail ? Output.TaskbarState.Error : Status == StatusKind.Warn ? Output.TaskbarState.Paused : Output.TaskbarState.Clear;
            return state != Output.TaskbarState.Clear;
        }

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
                    Detail = liveEvent.Text;
                    Status = StatusKinds.TryParse(liveEvent.Status, out StatusKind kind) ? kind : (StatusKind?)null;
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
            Size body = Content.Measure(constraints.Shrink(0, chrome + 1), context);
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
                Content.Render(region.Sub(0, 2, region.Width, bodyHeight), context);
            }
            if (footer) {
                DrawFooter(region.Sub(0, region.Height - 1, region.Width, 1), context);
            }
        }

        // Header and footer are the shared bars, so a dashboard looks the same full screen, inline and inside a menu run.
        private void DrawHeader(Region bar, RenderContext context) {
            new HeaderBar(Title) { Subtitle = Subtitle, Detail = Detail, Status = Status, ShowElapsed = ShowElapsed }.Render(bar, context);
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