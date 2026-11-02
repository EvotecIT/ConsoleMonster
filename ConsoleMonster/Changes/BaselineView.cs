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
            var table = new Table { Title = Title, Border = TableBorder.Rounded, ShowCardCount = false };
            table.AddColumn(Markup.Escape(labels.ChangesProperty));
            for (int i = 0; i < Comparison.Names.Count; i++) {
                string name = Comparison.Names[i] + (i == Comparison.BaselineIndex ? " (" + labels.ChangesBaseline + ")" : "");
                table.AddColumn(i == Comparison.BaselineIndex ? "[accent bold]" + Markup.Escape(name) + "[/]" : Markup.Escape(name));
            }
            table.AddColumn(Markup.Escape(labels.ChangesStatus));
            foreach (BaselineRow row in Comparison.Rows) {
                var cells = new List<string> { Markup.Escape(row.Path) };
                BaselineCell reference = row.Cells[Comparison.BaselineIndex];
                for (int i = 0; i < row.Cells.Count; i++) {
                    BaselineCell cell = row.Cells[i];
                    string text = cell.IsPresent ? ObjectDiff.PairText(cell.Value, reference.Value) : labels.ChangesMissing;
                    string tone = i == Comparison.BaselineIndex ? "accent bold" :
                        !cell.IsPresent || cell.Kind == ChangeKind.Removed ? "fail" : cell.Kind == ChangeKind.Added ? "pass" :
                        cell.Kind == ChangeKind.Changed || cell.IsCaseOnly ? "warn" : "text";
                    cells.Add("[" + tone + "]" + Markup.Escape(text) + "[/]");
                }
                var status = new List<string>();
                if (row.HasDifferences || row.HasCaseDifferences) {
                    status.Add("[warn]" + Markup.Escape(row.HasDifferences ? labels.ChangesDiffers : labels.ChangesCaseOnly) + "[/]");
                } else if (row.MissingCount == 0) {
                    status.Add("[muted]" + Markup.Escape(labels.ChangesSame) + "[/]");
                }
                if (row.MissingCount > 0) {
                    status.Add("[fail]" + Markup.Escape(WidgetLabels.Format(labels.ChangesMissingOn, row.MissingCount.ToString(CultureInfo.InvariantCulture))) + "[/]");
                }
                cells.Add(string.Join(", ", status));
                table.AddRow(cells.Select(cell => new TableCell(cell)).ToArray());
            }
            string footer = WidgetLabels.Format(Comparison.Rows.Count == 1 ? labels.ChangesOnePropertyShown : labels.ChangesPropertiesShown,
                Comparison.Rows.Count.ToString(CultureInfo.InvariantCulture));
            if (Comparison.HiddenMatchingCount > 0) {
                footer += ", " + WidgetLabels.Format(labels.ChangesMatchingHidden, Comparison.HiddenMatchingCount.ToString(CultureInfo.InvariantCulture));
            }
            var parts = new List<IWidget> { table, new TextBlock("[muted]" + Markup.Escape(footer) + "[/]") };
            if (Comparison.NotCompared.Count > 0) {
                parts.Add(new TextBlock("[warn]" + Markup.Escape(WidgetLabels.Format(labels.ChangesNotCompared,
                    Comparison.NotCompared.Count.ToString(CultureInfo.InvariantCulture))) + "[/]"));
            }
            return new Rows(parts.ToArray());
        }
    }
}
