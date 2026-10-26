using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Print-once run totals, group counts and complete wrapped problem details. It is never height-limited by the live dashboard.</summary>
    public sealed class RunSummary : IWidget {
        private readonly RunTracker _tracker;
        /// <summary>Creates a final summary over the shared tracker.</summary>
        /// <param name="tracker">The run state.</param>
        public RunSummary(RunTracker tracker) { _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker)); }
        /// <summary>The literal run name.</summary>
        public string Title { get; set; } = "Run";
        /// <summary>Optional report location printed after the problems.</summary>
        public string? ReportPath { get; set; }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Build(context).Measure(constraints, context);
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Build(context).Render(region, context);
        private IWidget Build(RenderContext context) {
            RunSnapshot snapshot = _tracker.Snapshot();
            List<IWidget> rows = new List<IWidget> {
                RunDashboard.Line(Title + " · " + snapshot.Done + " / " + snapshot.Total + " finished · " + NumberFormat.Duration(snapshot.Elapsed)),
                RunDashboard.Counts(snapshot, context)
            };
            var groups = snapshot.Items.Where(i => i.Item.Group.Length > 0).GroupBy(i => i.Item.Group).ToArray();
            if (groups.Length > 0) {
                Table table = new Table { CardsBelow = 80 };
                table.AddColumn("Group"); table.AddColumn("Done"); table.AddColumn("Passed"); table.AddColumn("Failed"); table.AddColumn("Warnings"); table.AddColumn("Skipped");
                foreach (var group in groups) {
                    table.AddRow(Markup.Escape(group.Key), group.Count(i => RunTracker.IsTerminal(i.Status)) + "/" + group.Count(), TableCell.FromValue(group.Count(i => i.Status == Styling.StatusKind.Pass)), TableCell.FromValue(group.Count(i => i.Status == Styling.StatusKind.Fail)), TableCell.FromValue(group.Count(i => i.Status == Styling.StatusKind.Warn)), TableCell.FromValue(group.Count(i => i.Status == Styling.StatusKind.Skip)));
                }
                rows.Add(RunDashboard.Label("Groups"));
                rows.Add(table);
            }
            var problems = snapshot.Items.Where(i => i.Status == Styling.StatusKind.Fail || i.Status == Styling.StatusKind.Warn)
                .OrderBy(i => i.Status == Styling.StatusKind.Fail ? 0 : 1).ThenBy(i => i.CompletionOrder).ToArray();
            if (problems.Length > 0) {
                // Each problem keeps its entire message. Table's narrow cards wrap rather than hiding detail columns.
                Table table = new Table { CardsBelow = 160, ShowKeyField = true };
                table.AddColumn(new TableColumn("Item") { IsKey = true, MaxLines = TableColumn.Unlimited });
                foreach (string column in new[] { "Target", "State", "Duration", "Message" }) { table.AddColumn(new TableColumn(column) { MaxLines = TableColumn.Unlimited }); }
                foreach (var item in problems) { table.AddRow(Markup.Escape(item.Item.Label), Markup.Escape(item.Item.Target), TableCell.ForStatus(item.Status), NumberFormat.Duration(item.Duration), Markup.Escape(item.Detail)); }
                rows.Add(RunDashboard.Label("Problems"));
                rows.Add(table);
            }
            if (!string.IsNullOrEmpty(ReportPath)) { rows.Add(new TextBlock(Markup.Escape("Report: " + ReportPath))); }
            return new Rows(rows.ToArray());
        }
    }
}
