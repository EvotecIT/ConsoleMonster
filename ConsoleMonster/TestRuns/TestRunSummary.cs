using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestRuns {
    /// <summary>
    /// The end of a test run, for the scrollback: a table of results grouped by file with a totals row (the file's status is
    /// the row's colour signal), the failures as in the live view, and a closing line with the outcome and the totals.
    /// </summary>
    public sealed class TestRunSummary : IWidget {
        private readonly TestRunSnapshot _snapshot;

        /// <summary>Creates the summary of a snapshot.</summary>
        /// <param name="snapshot">The snapshot (usually taken after the run finished).</param>
        public TestRunSummary(TestRunSnapshot snapshot) {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        /// <summary>The most failures listed (default 20); the rest are counted.</summary>
        public int MaxFailures { get; set; } = 20;

        /// <summary>Whether the failures are listed (default true); off when the test framework already printed them.</summary>
        public bool ShowFailures { get; set; } = true;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Build(context).Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Build(context).Render(region, context);

        private IWidget Build(RenderContext context) {
            Rows rows = new Rows();
            if (_snapshot.Containers.Count > 0) {
                rows.Add(FileTable(), LayoutSize.Auto);
            }
            if (ShowFailures && _snapshot.Failures.Count > 0) {
                rows.Add(new FailureBlock(_snapshot.Failures, MaxFailures), LayoutSize.Auto);
            }
            rows.Add(new ClosingLine(_snapshot), LayoutSize.Auto);
            return rows;
        }

        private Table FileTable() {
            Table table = new Table { Title = "Results by file" };
            table.AddColumn("File");
            table.AddColumn("Tests", HorizontalAlignment.Right);
            table.AddColumn("Passed", HorizontalAlignment.Right);
            table.AddColumn("Failed", HorizontalAlignment.Right);
            table.AddColumn("Skipped", HorizontalAlignment.Right);
            table.AddColumn("Time", HorizontalAlignment.Right);
            TimeSpan time = TimeSpan.Zero;
            foreach (TestContainerState container in _snapshot.Containers) {
                time += container.Duration;
                table.AddRow(
                    TableCell.ForStatus(container.Status, container.Name),
                    Number(container.Done),
                    Number(container.Passed),
                    Number(container.Failed),
                    Number(container.Skipped + container.NotRun),
                    NumberFormat.Duration(container.Duration));
            }
            table.SetFooter("Total", Number(_snapshot.Done), Number(_snapshot.Passed), Number(_snapshot.Failed), Number(_snapshot.Skipped + _snapshot.NotRun), NumberFormat.Duration(time));
            return table;
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class FailureBlock : IWidget {
            private readonly IReadOnlyList<TestRecord> _failures;
            private readonly int _limit;

            public FailureBlock(IReadOnlyList<TestRecord> failures, int limit) {
                _failures = failures;
                _limit = Math.Max(1, limit);
            }

            private List<StyledLine> Lines(RenderContext context) {
                List<StyledLine> lines = new List<StyledLine> { StyledLine.Empty };
                Style title = context.Style(ThemeToken.Title);
                lines.Add(StyledLine.From(" Failures", title));
                int shown = Math.Min(_failures.Count, _limit);
                for (int i = 0; i < shown; i++) {
                    lines.Add(StyledLine.Empty);
                    foreach (StyledLine line in TestRunText.FailureLines(_failures[i], context, 6)) {
                        List<TextSpan> spans = new List<TextSpan> { new TextSpan(" ") };
                        spans.AddRange(line);
                        lines.Add(new StyledLine(spans));
                    }
                }
                if (shown < _failures.Count) {
                    int more = _failures.Count - shown;
                    lines.Add(StyledLine.From(" + " + more.ToString(CultureInfo.InvariantCulture) + (more == 1 ? " more failure" : " more failures"), context.Style(ThemeToken.Muted)));
                }
                return lines;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Lines(context).Count);

            public void Render(Region region, RenderContext context) {
                List<StyledLine> lines = Lines(context);
                for (int y = 0; y < lines.Count && y < region.Height; y++) {
                    region.Write(0, y, TextWrapper.Truncate(lines[y], region.Width, context.Ellipsis));
                }
            }
        }

        private sealed class ClosingLine : IWidget {
            private readonly TestRunSnapshot _snapshot;

            public ClosingLine(TestRunSnapshot snapshot) {
                _snapshot = snapshot;
            }

            // The outcome and the totals share a line when they fit; otherwise the totals go on the next line, indented.
            private bool Wraps(int width, RenderContext context, out string outcome, out string totals) {
                StatusKind status = Outcome();
                outcome = " " + context.Glyphs.StatusSymbol(status) + " " + (status == StatusKind.Fail ? "Tests failed" : status == StatusKind.Warn ? "Tests inconclusive" : "Tests passed");
                totals = TestRunText.Totals(_snapshot, context.Glyphs);
                return DisplayWidth.Measure(outcome) + 2 + DisplayWidth.Measure(totals) > width;
            }

            private StatusKind Outcome() => _snapshot.Status == StatusKind.Running ? StatusKind.Info : _snapshot.Status;

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Wraps(constraints.MaxWidth, context, out _, out _) ? 3 : 2);

            public void Render(Region region, RenderContext context) {
                if (region.Height < 2) {
                    return;
                }
                bool wraps = Wraps(region.Width, context, out string outcome, out string totals);
                Style style = context.Style(Outcome());
                Style muted = context.Style(ThemeToken.Muted);
                int x = region.Write(0, 1, outcome, style.With(TextDecoration.Bold));
                if (wraps) {
                    if (region.Height > 2) {
                        region.Write(3, 2, totals, muted);
                    }
                } else {
                    region.Write(x + 2, 1, totals, muted);
                }
            }
        }
    }
}
