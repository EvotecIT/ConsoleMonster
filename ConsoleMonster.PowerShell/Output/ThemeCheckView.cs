using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    // Display selection stays on the PowerShell object; the library report always retains every measurement.
    internal static class ThemeCheckView {
        internal const string DetailedTypeName = "ConsoleMonster.DetailedThemeCheckReport";

        // The PSObject parameter preserves the view's type name across PowerShell's method binding.
        public static string Format(PSObject value, PSHost? host, string? outputRendering) =>
            WidgetFormatter.Render(Build((ThemeCheckReport)value.BaseObject, value.TypeNames.Contains(DetailedTypeName)), host, outputRendering, 0, null);

        internal static IWidget Build(ThemeCheckReport report, bool detailed = false) => new ContextWidget(context => BuildRows(report, detailed, context));

        private static IWidget BuildRows(ThemeCheckReport report, bool detailed, RenderContext context) {
            List<ThemeCheckResult[]> problems = Groups(report, ThemeCheckOutcome.Fail);
            List<ThemeCheckResult[]> unknowns = Groups(report, ThemeCheckOutcome.Unknown);
            string verdict = report.Passed ? "passes every check"
                : string.Join(", ", new[] {
                    problems.Count == 0 ? null : problems.Count + (problems.Count == 1 ? " problem" : " problems"),
                    unknowns.Count == 0 ? null : unknowns.Count + (unknowns.Count == 1 ? " unknown" : " unknowns")
                }.Where(text => text != null).Select(text => text!));
            Rows rows = new Rows(TextBlock.Plain(report.ThemeName + ": " + verdict));
            if (report.AdjustedTokens.Count > 0) { rows.Add(TextBlock.Plain("adjusted for contrast: " + string.Join(", ", report.AdjustedTokens), ThemeToken.Muted)); }
            if (detailed) {
                foreach (ThemeCheckResult result in report.Results) { rows.Add(Line(result.Outcome, Describe(new[] { result }), context)); }
            } else {
                foreach (ThemeCheckResult[] group in problems.Concat(unknowns)) { rows.Add(Line(group[0].Outcome, Describe(group), context)); }
                int passed = report.Results.Count(result => result.Outcome == ThemeCheckOutcome.Pass);
                if (!report.Passed && passed > 0) { rows.Add(TextBlock.Plain(passed + (passed == 1 ? " other check passes" : " other checks pass"), ThemeToken.Muted)); }
            }
            return rows;
        }

        private static List<ThemeCheckResult[]> Groups(ThemeCheckReport report, ThemeCheckOutcome outcome) => report.Results
            .Select((result, index) => new { Result = result, Index = index })
            .Where(item => item.Result.Outcome == outcome)
            .GroupBy(item => item.Result.Check == ThemeCheckKind.ColourVision ? string.Join("/", item.Result.Tokens) : item.Index.ToString(CultureInfo.InvariantCulture))
            .Select(group => group.Select(item => item.Result).OrderBy(result => result.Value).ToArray()).ToList();

        private static string Describe(ThemeCheckResult[] group) {
            ThemeCheckResult result = group[0];
            string detail = result.Check == ThemeCheckKind.ColourVision && group.Length > 1
                ? ThemeCheckText.Pair(result.Tokens) + (result.Outcome == ThemeCheckOutcome.Unknown ? " colours can't be compared " : " look alike ")
                    + ThemeCheckText.Conditions(group.Select(item => item.Vision))
                : result.Detail.TrimEnd('.');
            if (!result.Value.HasValue || (result.Check != ThemeCheckKind.Contrast && result.Check != ThemeCheckKind.ColourVision)) { return detail; }
            string units = result.Check == ThemeCheckKind.Contrast ? ":1" : string.Empty;
            string value = result.Value.Value.ToString("0.0", CultureInfo.InvariantCulture) + units;
            string required = result.Required?.ToString("0.#", CultureInfo.InvariantCulture) + units;
            return detail + (result.Check == ThemeCheckKind.Contrast ? ": " + value + ", needs " + required : " (" + value + ", needs " + required + ")");
        }

        private static IWidget Line(ThemeCheckOutcome outcome, string detail, RenderContext context) {
            string symbol = outcome == ThemeCheckOutcome.Unknown ? (context.Profile.Glyphs == GlyphTier.Ascii ? "?" : "○")
                : context.Glyphs.StatusSymbol(outcome == ThemeCheckOutcome.Fail ? StatusKind.Fail : StatusKind.Pass);
            string role = outcome == ThemeCheckOutcome.Unknown ? "muted" : outcome == ThemeCheckOutcome.Fail ? "fail" : "pass";
            return new TextBlock("[" + role + "]" + Markup.Escape(symbol) + "[/] " + Markup.Escape(detail));
        }
    }
}
