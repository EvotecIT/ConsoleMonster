using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples {
    /// <summary>A deterministic large-run scene shared by golden tests and website frames. Data is illustrative.</summary>
    public static class LargeRunScene {
        /// <summary>Creates 214 synthetic checks with eight active workers and seven pinned problems.</summary>
        /// <param name="final">Whether to finish all remaining work.</param>
        /// <returns>The state used by the example.</returns>
        public static RunTracker Tracker(bool final = false) {
            TimeSpan clock = TimeSpan.Zero;
            string[] groups = { "Identity", "Network", "Storage", "Services" };
            string[] names = { "Directory replication", "DNS records", "Free disk space", "Service health", "Certificate expiry", "Route reachability", "Volume permissions", "Time synchronisation" };
            RunTracker tracker = new RunTracker(Enumerable.Range(0, 214).Select(i => new RunItem(names[i % names.Length] + " " + (i + 1).ToString("D3"), "node-" + (i % 16 + 1).ToString("D2"), groups[i % groups.Length])), () => clock);
            for (int i = 0; i < 137; i++) {
                clock = TimeSpan.FromSeconds(i * 0.7 + Math.Sin(i * 0.22) * 2.0);
                tracker.StartItem(i, "collecting");
                StatusKind outcome = i == 23 || i == 67 || i == 102 || i == 136 ? StatusKind.Fail : i == 41 || i == 95 || i == 133 ? StatusKind.Warn : i < 5 ? StatusKind.Skip : StatusKind.Pass;
                tracker.FinishItem(i, outcome, outcome == StatusKind.Fail ? "Endpoint did not answer within the configured deadline. Check the connection and retry this check." : outcome == StatusKind.Warn ? "A partial response was returned; review the detailed report." : null, TimeSpan.FromSeconds(1.2 + i % 4));
                tracker.Snapshot();
            }
            for (int i = 137; i < 145; i++) { clock = TimeSpan.FromSeconds(96 + (i - 137) * 0.2); tracker.StartItem(i, i % 2 == 0 ? "evaluating" : "collecting"); }
            clock = TimeSpan.FromSeconds(100);
            if (final) {
                for (int i = 137; i < 214; i++) { clock = TimeSpan.FromSeconds(100 + (i - 137) * 0.7); tracker.FinishItem(i, StatusKind.Pass, duration: TimeSpan.FromSeconds(2)); }
                tracker.Complete();
            }
            return tracker;
        }

        /// <summary>Renders a complete terminal viewport, showing scrollback above its bounded live area.</summary>
        /// <param name="width">Terminal columns.</param>
        /// <param name="height">Terminal rows.</param>
        /// <param name="theme">Theme.</param>
        /// <param name="state">mid-run, short-terminal, ci or final-summary.</param>
        /// <param name="depth">Colour tier.</param>
        /// <returns>The terminal viewport.</returns>
        public static CellBuffer Frame(int width, int height, ITheme theme, string state = "mid-run", ColorDepth depth = ColorDepth.TrueColor) {
            bool final = state == "final-summary";
            RunTracker tracker = Tracker(final);
            TerminalProfile profile = TerminalProfile.Fixed(width, height, depth, depth == ColorDepth.None ? GlyphTier.Ascii : GlyphTier.Full);
            RenderContext context = new RenderContext(profile, theme, tick: 4);
            CellBuffer viewport = new CellBuffer(width, height);
            if (final) {
                CellBuffer summary = Terminal.Render(Summary(tracker), profile, theme);
                int shown = Math.Min(height, summary.Height);
                viewport.Blit(summary, 0, summary.Height - shown, width, shown, 0, 0);
                return viewport;
            }
            RunItemSnapshot[] completed = tracker.Snapshot().Items.Where(i => i.Status != StatusKind.Running && i.Status != StatusKind.Pending).OrderBy(i => i.CompletionOrder).ToArray();
            void Log(int rows, bool progress = false) {
                int itemRows = Math.Max(0, rows - (progress ? 1 : 0));
                int y = 0;
                foreach (RunItemSnapshot item in completed.Skip(Math.Max(0, completed.Length - itemRows))) {
                    new RunCompletion(item).Render(new Region(viewport).Sub(0, y++, width, 1), context);
                }
                if (progress) {
                    new RunProgress(tracker.Snapshot()).Render(new Region(viewport).Sub(0, rows - 1, width, 1), context);
                }
            }
            if (state == "ci") { Log(height, true); return viewport; }
            int dashboardHeight = Math.Max(1, (int)(height * 0.4));
            if (state == "short-terminal") { dashboardHeight = Math.Min(5, dashboardHeight); }
            Log(height - dashboardHeight);
            RunDashboard dashboard = new RunDashboard(tracker) { Title = "Fleet checks", ShowTrend = true, ShowKeys = true };
            dashboard.Render(new Region(viewport).Sub(0, height - dashboardHeight, width, dashboardHeight), context);
            return viewport;
        }

        /// <summary>The full final summary, independent of terminal viewport height.</summary>
        /// <param name="tracker">The completed tracker.</param>
        /// <returns>The summary widget.</returns>
        public static RunSummary Summary(RunTracker tracker) => new RunSummary(tracker) { Title = "Fleet checks", ReportPath = "reports/fleet-checks.html" };
    }
}
