using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConsoleMonster.Styling {
    // What a line of the readable verdict is, so each reader can style it: the headline, the adjusted tokens, a check
    // result (with its outcome) or the count of other passing checks.
    internal enum ThemeCheckLineKind { Verdict, Adjusted, Result, Passing }

    internal sealed class ThemeCheckLine {
        internal ThemeCheckLine(ThemeCheckLineKind kind, string text, ThemeCheckOutcome outcome = ThemeCheckOutcome.Pass) { Kind = kind; Text = text; Outcome = outcome; }
        internal ThemeCheckLineKind Kind { get; }
        internal string Text { get; }
        internal ThemeCheckOutcome Outcome { get; }
    }

    // The checker's result in readable words: the lines Test-ConsoleTheme prints, the site's gallery verdicts and the
    // settings picker's one- or two-line checker note all come from here.
    internal static class ThemeCheckVerdict {
        // The verdict, the tokens the accessible variant adjusted, then each problem (a pair that fails under several
        // vision conditions once, with its lowest distance) and the number of other checks that pass; detailed lists
        // every result instead.
        internal static IReadOnlyList<ThemeCheckLine> Lines(ThemeCheckReport report, bool detailed = false) {
            List<ThemeCheckResult[]> problems = Groups(report, ThemeCheckOutcome.Fail);
            List<ThemeCheckResult[]> unknowns = Groups(report, ThemeCheckOutcome.Unknown);
            string verdict = report.Passed ? "passes every check"
                : string.Join(", ", new[] {
                    problems.Count == 0 ? null : problems.Count + (problems.Count == 1 ? " problem" : " problems"),
                    unknowns.Count == 0 ? null : unknowns.Count + (unknowns.Count == 1 ? " unknown" : " unknowns")
                }.Where(text => text != null).Select(text => text!));
            List<ThemeCheckLine> lines = new List<ThemeCheckLine> { new ThemeCheckLine(ThemeCheckLineKind.Verdict, report.ThemeName + ": " + verdict) };
            if (report.AdjustedTokens.Count > 0) { lines.Add(new ThemeCheckLine(ThemeCheckLineKind.Adjusted, "adjusted for contrast: " + string.Join(", ", report.AdjustedTokens))); }
            if (detailed) {
                foreach (ThemeCheckResult result in report.Results) { lines.Add(new ThemeCheckLine(ThemeCheckLineKind.Result, Describe(new[] { result }), result.Outcome)); }
            } else {
                foreach (ThemeCheckResult[] group in problems.Concat(unknowns)) { lines.Add(new ThemeCheckLine(ThemeCheckLineKind.Result, Describe(group), group[0].Outcome)); }
                int passed = report.Results.Count(result => result.Outcome == ThemeCheckOutcome.Pass);
                if (!report.Passed && passed > 0) { lines.Add(new ThemeCheckLine(ThemeCheckLineKind.Passing, passed + (passed == 1 ? " other check passes" : " other checks pass"))); }
            }
            return lines;
        }

        // The settings preview's note under the sample, fitted to width: whether the statuses stay apart for colour-blind
        // users, which pair looks alike under which conditions, and the faintest text. Detail is empty when one line says it.
        internal static (string Summary, string Detail) Preview(ThemeCheckReport report, ITheme theme, int width) {
            ThemeCheckResult? failed = report.Results.Where(result => result.Check == ThemeCheckKind.ColourVision && result.Outcome == ThemeCheckOutcome.Fail).OrderBy(result => result.Value).FirstOrDefault();
            bool unknown = report.Results.Any(result => result.Check == ThemeCheckKind.ColourVision && result.Outcome == ThemeCheckOutcome.Unknown);
            string verdict = failed == null ? (unknown ? "unknown" : "yes") : "no";
            string prefix = width >= 60 ? "Statuses apart for colour-blind users: " : "Colour-blind: ";
            string summary = prefix + verdict;
            string detail = string.Empty;
            if (unknown && theme is Theme following && following.FollowsTerminal && Terminal.Palette == null) {
                const string reason = "the terminal did not report its colours";
                if ((summary + ", " + reason).Length <= width) { summary += ", " + reason; }
                else { summary += ","; detail = reason; }
            }
            if (failed != null) {
                var pair = report.Results.Where(result => result.Check == ThemeCheckKind.ColourVision && result.Outcome == ThemeCheckOutcome.Fail
                    && result.Tokens.SequenceEqual(failed.Tokens)).OrderBy(result => result.Value);
                string words = ThemeCheckText.Pair(failed.Tokens, false);
                string reason = "look alike " + ThemeCheckText.Conditions(pair.Select(result => result.Vision));
                string full = summary + ": " + words + " " + reason;
                if (full.Length <= width) { summary = full; }
                else if ((summary + ": " + words + " look alike").Length <= width) { summary += ": " + words + " look alike"; detail = ThemeCheckText.Conditions(pair.Select(result => result.Vision)); }
                else if ((summary + ": " + words).Length <= width) { summary += ": " + words; detail = reason; }
                else { detail = words + " " + reason; }
            }
            ThemeCheckResult? contrast = report.Results.Where(result => result.Check == ThemeCheckKind.Contrast && result.Outcome == ThemeCheckOutcome.Fail).OrderBy(result => result.Value).FirstOrDefault();
            if (contrast != null) {
                string ratio = contrast.Tokens[0] + " text too faint: " + contrast.Value!.Value.ToString("0.0", CultureInfo.InvariantCulture) + ":1";
                detail += (detail.Length > 0 ? ". " : string.Empty) + ratio;
                if (failed != null && detail.Length > width) {
                    summary = ThemeCheckText.Pair(failed.Tokens, true) + " look alike " + ThemeCheckText.Conditions(report.Results
                        .Where(result => result.Check == ThemeCheckKind.ColourVision && result.Outcome == ThemeCheckOutcome.Fail && result.Tokens.SequenceEqual(failed.Tokens))
                        .Select(result => result.Vision));
                    detail = ratio;
                }
            }
            return (summary, detail);
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
    }
}
