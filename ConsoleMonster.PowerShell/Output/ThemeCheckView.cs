using System.Globalization;
using System.Linq;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    // Presentation only: measurements and verdicts come from the library report.
    internal static class ThemeCheckView {
        internal static Table Build(ThemeCheckReport report) {
            Table table = new Table { Border = TableBorder.None, CardsBelow = 0 };
            table.AddColumn(new TableColumn("Check / tokens") { Width = 17 });
            table.AddColumn(new TableColumn("Result") { MinWidth = 11 });
            table.AddColumn(new TableColumn("Value / needed") { MinWidth = 15 });
            table.AddColumn(new TableColumn("Verdict") { MinWidth = 28, Wrap = true });
            foreach (ThemeCheckResult result in report.Results) {
                string label = (result.Check == ThemeCheckKind.ColourVision ? "Vision" : result.Check.ToString()) + " " + string.Join("/", result.Tokens);
                string units = result.Check == ThemeCheckKind.Contrast ? ":1" : string.Empty;
                string value = result.Value.HasValue ? result.Value.Value.ToString(result.Check == ThemeCheckKind.Contrast ? "0.0" : "0.0#", CultureInfo.InvariantCulture) + units : "?";
                string required = result.Required?.ToString("0.#", CultureInfo.InvariantCulture) + units;
                StatusKind status = result.Outcome == ThemeCheckOutcome.Pass ? StatusKind.Pass : result.Outcome == ThemeCheckOutcome.Fail ? StatusKind.Fail : StatusKind.Unknown;
                table.AddRow(label, TableCell.ForStatus(status, result.Outcome.ToString()), value + " / " + required, result.Detail);
            }
            return table;
        }
    }
}
