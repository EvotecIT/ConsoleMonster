using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Localization;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Changes {
    /// <summary>A property matrix with differences highlighted against a reference column. Uses standard table wrapping and sizing.</summary>
    public sealed class BaselineView : IWidget {
        /// <summary>Creates an embeddable baseline matrix.</summary>
        /// <param name="comparison">The structured comparison.</param>
        public BaselineView(BaselineComparison comparison) {
            Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
        }

        /// <summary>The comparison shown by this widget.</summary>
        public BaselineComparison Comparison { get; }
        /// <summary>An optional markup title.</summary>
        public string? Title { get; set; }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Build(context).Measure(constraints, context);
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Build(context).Render(region, context);

        private IWidget Build(RenderContext context) {
            WidgetLabels labels = context.Labels;
            var table = new Table { Title = Title, Border = TableBorder.Rounded };
            table.AddColumn(Markup.Escape(labels.ChangesProperty));
            for (int i = 0; i < Comparison.Names.Count; i++) {
                string name = Comparison.Names[i] + (i == Comparison.BaselineIndex ? " (" + labels.ChangesBaseline + ")" : "");
                table.AddColumn(Markup.Escape(name));
            }
            table.AddColumn(Markup.Escape(labels.ChangesStatus));
            foreach (BaselineRow row in Comparison.Rows) {
                var cells = new List<string> { Markup.Escape(row.Path) };
                BaselineCell reference = row.Cells[Comparison.BaselineIndex];
                foreach (BaselineCell cell in row.Cells) {
                    string text = cell.IsPresent ? ObjectDiff.PairText(cell.Value, reference.Value) : labels.ChangesMissing;
                    string tone = !cell.IsPresent || cell.Kind == ChangeKind.Removed ? "fail" : cell.Kind == ChangeKind.Added ? "pass" :
                        cell.Kind == ChangeKind.Changed || cell.IsCaseOnly ? "warn" : "text";
                    cells.Add("[" + tone + "]" + Markup.Escape(text) + "[/]");
                }
                string status = row.MissingCount > 0 ? WidgetLabels.Format(labels.ChangesMissingOn, row.MissingCount.ToString(CultureInfo.InvariantCulture)) :
                    row.HasDifferences ? labels.ChangesDiffers : row.HasCaseDifferences ? labels.ChangesCaseOnly : labels.ChangesSame;
                cells.Add(Markup.Escape(status));
                table.AddRow(cells.Select(cell => new TableCell(cell)).ToArray());
            }
            if (Comparison.NotCompared.Count == 0) { return table; }
            return new Rows(table, new TextBlock("[warn]" + Markup.Escape(WidgetLabels.Format(labels.ChangesNotCompared,
                Comparison.NotCompared.Count.ToString(CultureInfo.InvariantCulture))) + "[/]"));
        }
    }
}
