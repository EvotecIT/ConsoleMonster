using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Hosting {
    /// <summary>
    /// The dashboard's list of hosted services and reported entries: a status symbol, the name, the state, how long it has
    /// been in it, and the detail. Each frame reads a fresh snapshot, so the list can change from any thread.
    /// </summary>
    public sealed class ServicesView : IWidget {
        private readonly IConsoleDashboard _dashboard;
        private readonly Func<DateTimeOffset> _clock;

        /// <summary>Creates the view.</summary>
        /// <param name="dashboard">The statuses to show.</param>
        /// <param name="clock">The current time (for tests), or null for the system clock.</param>
        public ServicesView(IConsoleDashboard dashboard, Func<DateTimeOffset>? clock = null) {
            _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
            _clock = clock ?? (() => DateTimeOffset.Now);
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int count = _dashboard.Snapshot().Count;
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Math.Max(1, count)));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            IReadOnlyList<ServiceStatus> items = _dashboard.Snapshot();
            Style muted = context.Style(ThemeToken.Muted);
            if (items.Count == 0) {
                region.Write(0, 0, "No hosted services.", muted);
                return;
            }
            int nameWidth = 0;
            int stateWidth = 0;
            foreach (ServiceStatus item in items) {
                nameWidth = Math.Max(nameWidth, DisplayWidth.Measure(item.Name));
                stateWidth = Math.Max(stateWidth, StateWord(item.State).Length);
            }
            nameWidth = Math.Min(nameWidth, Math.Max(8, region.Width / 3));
            DateTimeOffset now = _clock();
            // More entries than rows: the last row counts the rest.
            int shown = items.Count > region.Height ? Math.Max(0, region.Height - 1) : items.Count;
            if (shown < items.Count) {
                region.Write(0, shown, Localization.WidgetLabels.Format(context.Labels.MoreCount, (items.Count - shown).ToString("N0", context.Culture)), muted);
            }
            for (int y = 0; y < shown; y++) {
                ServiceStatus item = items[y];
                Style kind = context.Style(item.Kind);
                string elapsed = NumberFormat.Duration(now - item.Since > TimeSpan.Zero ? now - item.Since : TimeSpan.Zero);
                List<TextSpan> spans = new List<TextSpan> {
                    new TextSpan(context.Glyphs.StatusSymbol(item.Kind) + " ", kind),
                    new TextSpan(Pad(TextWrapper.Truncate(item.Name, nameWidth, context.Ellipsis), nameWidth) + "  ", context.Style(ThemeToken.Text)),
                    new TextSpan(StateWord(item.State).PadRight(stateWidth) + "  ", kind),
                    new TextSpan(elapsed.PadLeft(6) + "  ", muted)
                };
                if (!string.IsNullOrEmpty(item.Detail)) {
                    spans.Add(new TextSpan(item.Detail!, item.State == ServiceState.Failed ? context.Style(ThemeToken.Fail) : muted));
                }
                region.Write(0, y, TextWrapper.Truncate(new StyledLine(spans), region.Width, context.Ellipsis));
            }
        }

        private static string Pad(string text, int width) => text + new string(' ', Math.Max(0, width - DisplayWidth.Measure(text)));

        private static string StateWord(ServiceState state) {
            switch (state) {
                case ServiceState.Waiting:
                    return "waiting";
                case ServiceState.Starting:
                    return "starting";
                case ServiceState.Running:
                    return "running";
                case ServiceState.Stopping:
                    return "stopping";
                case ServiceState.Stopped:
                    return "stopped";
                case ServiceState.Completed:
                    return "completed";
                default:
                    return "failed";
            }
        }
    }
}
