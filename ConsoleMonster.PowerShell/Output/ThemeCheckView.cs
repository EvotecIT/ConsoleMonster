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
            Rows rows = new Rows();
            foreach (ThemeCheckLine line in ThemeCheckVerdict.Lines(report, detailed)) {
                switch (line.Kind) {
                    case ThemeCheckLineKind.Verdict: rows.Add(TextBlock.Plain(line.Text)); break;
                    case ThemeCheckLineKind.Result: rows.Add(Line(line.Outcome, line.Text, context)); break;
                    default: rows.Add(TextBlock.Plain(line.Text, ThemeToken.Muted)); break;
                }
            }
            return rows;
        }

        private static IWidget Line(ThemeCheckOutcome outcome, string detail, RenderContext context) {
            string symbol = outcome == ThemeCheckOutcome.Unknown ? (context.Profile.Glyphs == GlyphTier.Ascii ? "?" : "○")
                : context.Glyphs.StatusSymbol(outcome == ThemeCheckOutcome.Fail ? StatusKind.Fail : StatusKind.Pass);
            string role = outcome == ThemeCheckOutcome.Unknown ? "muted" : outcome == ThemeCheckOutcome.Fail ? "fail" : "pass";
            return new TextBlock("[" + role + "]" + Markup.Escape(symbol) + "[/] " + Markup.Escape(detail));
        }
    }
}
