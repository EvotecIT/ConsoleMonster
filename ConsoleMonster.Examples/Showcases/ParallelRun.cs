using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Building;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Showcase 1: a parallel assessment dashboard ("Nightly checks"), replayed from a recorded JSON-lines event stream.
    /// The layout knows nothing about the recording: every update arrives as a <see cref="LiveEvent"/> addressed by name.
    /// </summary>
    public static class ParallelRun {
        /// <summary>Writes the synthetic recording to a file.</summary>
        /// <param name="path">The JSON-lines path.</param>
        /// <returns>The number of events written.</returns>
        public static int Record(string path) {
            List<LiveEvent> events = ParallelRunRecording.Generate();
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory)) {
                Directory.CreateDirectory(directory);
            }
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            using (LiveRecorder recorder = new LiveRecorder(writer)) {
                foreach (LiveEvent liveEvent in events) {
                    recorder.Write(liveEvent);
                }
            }
            return events.Count;
        }

        /// <summary>Replays a recording into the dashboard.</summary>
        /// <param name="path">The recording, or <c>null</c> to generate one in memory.</param>
        /// <param name="speed">Replay speed (2 = twice as fast).</param>
        public static void Replay(string? path, double speed) {
            Dashboard dashboard = Build(out Lanes lanes);
            dashboard.Footer = "[muted]replaying recorded run · " + speed.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "x · Ctrl+C stop[/]";
            LiveSessionOptions options = new LiveSessionOptions {
                Summary = new RunSummary(lanes)
            };
            TextReader reader;
            if (path != null) {
                reader = new StreamReader(path, Encoding.UTF8);
            } else {
                StringWriter buffer = new StringWriter();
                using (LiveRecorder recorder = new LiveRecorder(buffer)) {
                    foreach (LiveEvent liveEvent in ParallelRunRecording.Generate()) {
                        recorder.Write(liveEvent);
                    }
                }
                reader = new StringReader(buffer.ToString());
            }
            using (reader) {
                LiveSession.Replay(dashboard, reader, options, speed);
            }
        }

        /// <summary>Builds the dashboard layout.</summary>
        /// <param name="lanes">The lanes widget (for the summary).</param>
        /// <returns>The dashboard.</returns>
        public static Dashboard Build(out Lanes lanes) {
            Phases phases = new Phases()
                .Set("Discover")
                .Set("Collect")
                .Set("Evaluate")
                .Set("Report");
            StackedBar results = new StackedBar("results")
                .Add("Passed", 0, ThemeToken.Pass)
                .Add("Warnings", 0, ThemeToken.Warn)
                .Add("Failed", 0, ThemeToken.Fail)
                .Add("Skipped", 0, ThemeToken.Skip);
            KeyValueList summary = new KeyValueList("summary")
                .Add("Evaluated", "0 / " + ParallelRunRecording.TotalRules)
                .Add("Average rate", "-")
                .Add("Remaining", "-")
                .Add("Targets done", "0 / 12")
                .Add("Targets failing", "0");
            LineChart throughput = new LineChart("throughput") { Height = 7 };
            throughput.AddSeries("completed", ThemeToken.Accent);
            throughput.AddSeries("failed", ThemeToken.Fail);
            Histogram durations = new Histogram(new double[] { 0, 0.25, 0.5, 1, 2, 4, 8 }, "durations") { Height = 6, Unit = "s" };
            BarChart findings = new BarChart("findings") { BarToken = ThemeToken.Fail };
            foreach (string category in new[] { "Domain controllers", "Replication", "DNS", "Kerberos", "Security", "Group Policy", "Accounts", "PKI" }) {
                findings.Add(category, 0);
            }
            Lanes targets = new Lanes { MaxLabelWidth = 28 };
            Dashboard dashboard = new DashboardBuilder("[bold]Nightly checks[/]")
                .Subtitle("ad.evotec.xyz forest assessment · 12 targets · " + ParallelRunRecording.TotalRules + " rules")
                .Add(new Prioritized(new Padding(phases, 1, 0, 1, 1), 10, 1, 30))
                .Columns(c => c
                    .Panel("Targets", new Prioritized(targets, 8, 4, 40), 2.3, ParallelRunRecording.Workers + " workers")
                    .Panel("Results", new Rows(results, new Rule(), summary), 1))
                .Columns(c => c
                    .Panel("Throughput", throughput, 1.2, "rules/s")
                    .Panel("Rule duration", durations, 1)
                    .Panel("Failures by category", findings, 1))
                .Panel("Log", new LogRegion { PreferredHeight = 6 }, fill: true)
                .BuildDashboard();
            // Below 60 x 20 the dashboard shows one status line plus the phases and the lanes that fit.
            dashboard.MinimumSize = new Size(60, 20);
            dashboard.CompactSummary = () => CompactSummary(results);
            lanes = targets;
            return dashboard;
        }

        private static string CompactSummary(StackedBar results) {
            double done = 0;
            double failed = 0;
            double warned = 0;
            foreach (ChartItem segment in results.Segments) {
                done += segment.Value;
                if (segment.Label == "Failed") {
                    failed = segment.Value;
                } else if (segment.Label == "Warnings") {
                    warned = segment.Value;
                }
            }
            return done.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "/" + ParallelRunRecording.TotalRules
                + " · [fail]✗" + failed.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "[/] [warn]▴"
                + warned.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "[/]";
        }

        /// <summary>
        /// A custom widget (implementing <see cref="IWidget"/> directly) that summarises the run from the final lane state
        /// when the session prints its summary to scrollback.
        /// </summary>
        private sealed class RunSummary : IWidget {
            private readonly Lanes _lanes;

            public RunSummary(Lanes lanes) {
                _lanes = lanes;
            }

            public Size Measure(Constraints constraints, Canvas.RenderContext context) => Content().Measure(constraints, context);

            public void Render(Canvas.Region region, Canvas.RenderContext context) => Content().Render(region, context);

            private IWidget Content() {
                Table table = new Table { Border = TableBorder.Minimal, Title = "[bold]Run summary[/]" };
                table.AddColumn(new TableColumn("Target") { Wrap = false });
                table.AddColumn("Status");
                table.AddColumn("Passed");
                table.AddColumn("Failed");
                table.AddColumn("Warnings");
                table.AddColumn("Skipped");
                table.AddColumn("Duration");
                int passed = 0;
                int failed = 0;
                int warned = 0;
                int skipped = 0;
                foreach (Lane lane in _lanes.Items) {
                    string duration = lane.Started.HasValue && lane.Finished.HasValue ? NumberFormat.Duration(lane.Finished.Value - lane.Started.Value) : "-";
                    table.AddRow(Text.Markup.Escape(lane.Label), TableCell.ForStatus(lane.Status), lane.Passed.ToString(), lane.Failed.ToString(), lane.Warned.ToString(), lane.Skipped.ToString(), duration);
                    passed += lane.Passed;
                    failed += lane.Failed;
                    warned += lane.Warned;
                    skipped += lane.Skipped;
                }
                table.SetFooter("[bold]Total[/]", string.Empty, passed.ToString(), failed.ToString(), warned.ToString(), skipped.ToString(), string.Empty);
                return new Padding(table, 1, 1, 1, 1);
            }
        }
    }
}
