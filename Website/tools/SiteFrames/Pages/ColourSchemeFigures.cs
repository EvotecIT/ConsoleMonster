using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Frames;

namespace SiteFrames.Pages {
    /// <summary>
    /// The colour scheme gallery (docs/colour-schemes): one sample program drawn with every curated scheme, faithful and
    /// accessible, each on the scheme's own background, with what the theme checker says about that theme.
    /// </summary>
    internal static class ColourSchemeFigures {
        // Two frames side by side fit the docs article (972 px) at a readable 12 px cell up to about 61 columns.
        internal const int Width = 60;

        /// <summary>A scheme of the gallery, with the family it is listed under.</summary>
        internal sealed record Entry(TerminalScheme Scheme, string Family, string Variant, string Source, string Licence);

        /// <summary>
        /// The schemes the gallery shows, in catalogue order. They come from the proposal's catalogue until the library
        /// ships the named schemes; then this is the one place that reads <c>Theme.Schemes</c> instead.
        /// </summary>
        internal static IReadOnlyList<Entry> Schemes(Site site) {
            string path = Path.Combine(site.Repository, "Docs", "Proposals", "Colour-Schemes.catalogue.json");
            JsonObject catalogue = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            List<Entry> entries = new List<Entry>();
            foreach (KeyValuePair<string, JsonNode?> item in catalogue) {
                JsonObject data = item.Value!.AsObject().DeepClone().AsObject();
                data["name"] = item.Key;
                TerminalScheme scheme = TerminalScheme.ParseWindowsTerminal(data.ToJsonString());
                entries.Add(new Entry(scheme, (string)data["family"]!, (string)data["variant"]!, (string)data["source"]!, (string)data["licence"]!));
            }
            // Families in the order they first appear, dark before light within a family.
            List<string> families = entries.Select(e => e.Family).Distinct().ToList();
            return entries.OrderBy(e => families.IndexOf(e.Family)).ThenBy(e => e.Variant == "Light" ? 1 : 0).ToList();
        }

        /// <summary>The figure id of a scheme: <c>scheme-tokyo-night</c>, and <c>-accessible</c> for its variant.</summary>
        internal static string Id(TerminalScheme scheme, bool accessible) =>
            "scheme-" + scheme.Name.ToLowerInvariant().Replace(' ', '-') + (accessible ? "-accessible" : string.Empty);

        public static void Render(Site site) {
            string program = Path.Combine(site.Website, "examples", "docs", "colour-schemes.cs");
            foreach (Entry entry in Schemes(site)) {
                Screen screen = Screen.Of(entry.Variant == "Light" ? "light" : "dark", entry.Scheme.Foreground, entry.Scheme.Background);
                foreach (bool accessible in new[] { false, true }) {
                    Theme theme = Theme.FromScheme(entry.Scheme, accessible);
                    CellBuffer frame = CSharpRunner.Run(program, Width, ColorDepth.TrueColor, GlyphTier.Full, theme);
                    string id = Id(entry.Scheme, accessible);
                    DocCards.Offer(id, frame, screen);
                    string[] rows = FrameHtml.Rows(frame, ColorDepth.TrueColor, screen, site.Styles);
                    site.Note(frame);
                    JsonObject data = new JsonObject {
                        ["w"] = frame.Width,
                        ["h"] = Site.Lines(rows),
                        [entry.Variant == "Light" ? "lightOnly" : "dark"] = FrameHtml.Body(rows),
                        // The scheme's own terminal colours, so the window is the scheme's background, not the site's.
                        ["fg"] = screen.Foreground,
                        ["bg"] = screen.Background
                    };
                    Figures.Add(id, Figures.WithProgram(site, new JsonObject {
                        // Both windows carry the scheme's name; the first chip says which form it is.
                        ["title"] = entry.Scheme.Name,
                        ["label"] = "Status table, status list, progress bar, bar chart and an error line in " + theme.Name,
                        ["meta"] = new JsonArray(accessible ? "Accessible" : "Faithful", Width + " columns"),
                        ["frame"] = data,
                        ["verdict"] = new JsonArray(Verdict(ThemeCheck.Run(theme)).Select(line => (JsonNode)line).ToArray())
                    }, program));
                }
            }
        }

        /// <summary>
        /// The checker's result in the words <c>Test-ConsoleTheme</c> prints by default: the verdict, the tokens the
        /// accessible variant adjusted, each problem (a pair that fails under several vision conditions once, with its
        /// lowest distance) and the number of other checks that pass.
        /// </summary>
        internal static List<string> Verdict(ThemeCheckReport report) {
            List<ThemeCheckResult[]> problems = Groups(report, ThemeCheckOutcome.Fail);
            List<ThemeCheckResult[]> unknowns = Groups(report, ThemeCheckOutcome.Unknown);
            string verdict = report.Passed ? "passes every check"
                : string.Join(", ", new[] {
                    problems.Count == 0 ? null : problems.Count + (problems.Count == 1 ? " problem" : " problems"),
                    unknowns.Count == 0 ? null : unknowns.Count + (unknowns.Count == 1 ? " unknown" : " unknowns")
                }.Where(text => text != null));
            List<string> lines = new List<string> { report.ThemeName + ": " + verdict };
            if (report.AdjustedTokens.Count > 0) { lines.Add("adjusted for contrast: " + string.Join(", ", report.AdjustedTokens)); }
            foreach (ThemeCheckResult[] group in problems.Concat(unknowns)) { lines.Add(Describe(group)); }
            int passed = report.Results.Count(result => result.Outcome == ThemeCheckOutcome.Pass);
            if (!report.Passed && passed > 0) { lines.Add(passed + (passed == 1 ? " other check passes" : " other checks pass")); }
            return lines;
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
