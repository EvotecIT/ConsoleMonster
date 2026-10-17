using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestRuns {
    /// <summary>
    /// The live view of a <see cref="TestRun"/>, drawn with the shared dashboard chrome (see Docs/Design-System.md): the
    /// header bar with the run totals, the status and the clock; the test files as lanes (running, passed, failed and
    /// skipped counts); the running test; and, in the full layout, the failures (expected against actual, file and line as a
    /// link), the slowest tests and a histogram of test durations. The compact layout (for inline areas) keeps the files,
    /// the running test and the first failures. Each frame renders a fresh snapshot, so the run can be fed from any thread.
    /// </summary>
    public sealed class TestRunView : IWidget {
        private static readonly double[] DurationEdges = { 0, 10, 50, 100, 250, 500, 1000, 5000 };
        private readonly TestRun _run;

        /// <summary>Creates the view.</summary>
        /// <param name="run">The run.</param>
        public TestRunView(TestRun run) {
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        /// <summary>The header title, or null (default) for the built-in word ("Tests" in English).</summary>
        public string? Title { get; set; }

        /// <summary>Draws the compact layout (files, running test, first failures) at the height it needs.</summary>
        public bool Compact { get; set; }

        /// <summary>The most file lanes the compact layout shows (default 6).</summary>
        public int CompactFiles { get; set; } = 6;

        /// <summary>The most failures the compact layout shows (default 2).</summary>
        public int CompactFailures { get; set; } = 2;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int limit = constraints.IsHeightUnbounded ? Math.Max(8, context.Profile.Height - 1) : constraints.MaxHeight;
            if (!Compact) {
                return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? Math.Max(24, limit) : constraints.MaxHeight);
            }
            return new Size(constraints.MaxWidth, Math.Min(limit, CompactHeight(_run.Snapshot(), constraints.MaxWidth, context)));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            Build(_run.Snapshot(), region.Width, region.Height, context).Render(region, context);
        }

        /// <summary>Builds the widgets for one snapshot at a size (used for each frame and for exports of the final state).</summary>
        /// <param name="snapshot">The snapshot.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The dashboard.</returns>
        public IWidget Build(TestRunSnapshot snapshot, int width, int height, RenderContext context) {
            Rows body = new Rows();
            int fileRows = Math.Max(1, snapshot.Containers.Count);
            if (Compact) {
                fileRows = Math.Min(fileRows, CompactFiles);
                body.Add(FilesPanel(snapshot, context), LayoutSize.Fixed(fileRows + 3));
                body.Add(new Padding(new CurrentTestLine(snapshot), 1, 0, 1, 0), LayoutSize.Fixed(1));
                if (snapshot.Failures.Count > 0) {
                    FailureList failures = new FailureList(snapshot.Failures, CompactFailures);
                    body.Add(FailuresPanel(snapshot, failures), LayoutSize.Fraction());
                }
            } else {
                int bodyHeight = Math.Max(6, height - 3);
                fileRows = Math.Min(fileRows, Math.Max(3, bodyHeight / 3));
                body.Add(FilesPanel(snapshot, context), LayoutSize.Fixed(fileRows + 3));
                body.Add(new Padding(new CurrentTestLine(snapshot), 1, 0, 1, 0), LayoutSize.Fixed(1));
                Rows side = new Rows();
                side.Add(new Panel(new SlowestList(snapshot.Slowest), Text.Markup.Escape(context.Labels.TestsSlowest)) { PaddingTop = 1 }, LayoutSize.Fixed(Math.Max(1, _run.SlowestCount) + 3));
                Histogram histogram = new Histogram(DurationEdges, "durations") { EdgeFormat = Milliseconds, Token = ThemeToken.Series1 };
                foreach (double duration in snapshot.Durations) {
                    histogram.Add(duration);
                }
                side.Add(new Panel(histogram, Text.Markup.Escape(context.Labels.TestsDurations)) { PaddingTop = 1 }, LayoutSize.Fraction(1, 6));
                Columns bottom = new Columns();
                bottom.Add(FailuresPanel(snapshot, new FailureList(snapshot.Failures, int.MaxValue)), LayoutSize.Fraction(3, 40));
                bottom.Add(side, LayoutSize.Fraction(2, 34));
                body.Add(bottom, LayoutSize.Fraction());
            }
            StatusKind status = snapshot.Status;
            return new Dashboard(Title ?? Text.Markup.Escape(context.Labels.TestsTitle), body) {
                Subtitle = TestRunText.Totals(snapshot, context.Glyphs, false),
                Status = status == StatusKind.Running ? context.Labels.TestsStatusRunning : status == StatusKind.Fail ? context.Labels.TestsStatusFailed : status == StatusKind.Warn ? context.Labels.TestsStatusInconclusive : context.Labels.TestsStatusPassed,
                StatusLevel = status,
                ShowClock = true
            };
        }

        private static string Milliseconds(double value) => value < 1000 ? value.ToString("0", CultureInfo.InvariantCulture) + "ms" : (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "s";

        private int CompactHeight(TestRunSnapshot snapshot, int width, RenderContext context) {
            int height = 3 + Math.Min(Math.Max(1, snapshot.Containers.Count), CompactFiles) + 3 + 1;
            if (snapshot.Failures.Count > 0) {
                height += new FailureList(snapshot.Failures, CompactFailures).Measure(new Constraints(Math.Max(1, width - 4)), context).Height + 3;
            }
            return height;
        }

        private static Panel FilesPanel(TestRunSnapshot snapshot, RenderContext context) {
            Lanes lanes = new Lanes("files") { MaxLabelWidth = 36, CountSkipped = true };
            int done = 0;
            foreach (TestContainerState container in snapshot.Containers) {
                Lane lane = lanes.Add(container.Key, container.Name, container.Tests);
                lane.Done = container.Done;
                lane.Passed = container.Passed;
                lane.Failed = container.Failed;
                lane.Skipped = container.Skipped + container.NotRun;
                lane.Warned = container.Inconclusive;
                lane.Status = container.Status;
                lane.Started = container.Started;
                lane.Finished = container.Finished;
                lane.Detail = container.Current;
                if (lane.IsFinished) {
                    done++;
                }
            }
            string caption = Localization.WidgetLabels.Format(context.Labels.TestsFilesDone, done.ToString(CultureInfo.InvariantCulture), snapshot.Containers.Count.ToString(CultureInfo.InvariantCulture));
            return new Panel(lanes, Text.Markup.Escape(context.Labels.TestsFiles)) { PaddingTop = 1, Caption = caption };
        }

        private static Panel FailuresPanel(TestRunSnapshot snapshot, FailureList list) {
            int count = snapshot.Failures.Count;
            return new Panel(list, Text.Markup.Escape(Localization.WidgetLabels.Current.TestsFailures)) {
                PaddingTop = 1,
                Caption = count == 0 ? null : count.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
