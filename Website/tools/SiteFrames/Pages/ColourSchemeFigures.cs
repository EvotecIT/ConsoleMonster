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

        /// <summary>The figure id of a scheme: <c>scheme-tokyo-night-dark</c>, and <c>-accessible</c> for its variant.</summary>
        internal static string Id(ThemeScheme entry, bool accessible) =>
            "scheme-" + entry.Name.ToLowerInvariant().Replace(' ', '-') + (accessible ? "-accessible" : string.Empty);

        public static void Render(Site site) {
            string program = Path.Combine(site.Website, "examples", "docs", "colour-schemes.cs");
            foreach (ThemeScheme entry in Theme.Schemes) {
                Screen screen = Screen.Of(entry.Variant == ThemeBackground.Light ? "light" : "dark", entry.Scheme.Foreground, entry.Scheme.Background);
                foreach (bool accessible in new[] { false, true }) {
                    Theme theme = Theme.FromScheme(entry.Scheme, accessible);
                    CellBuffer frame = CSharpRunner.Run(program, Width, ColorDepth.TrueColor, GlyphTier.Full, theme);
                    string id = Id(entry, accessible);
                    DocCards.Offer(id, frame, screen);
                    string[] rows = FrameHtml.Rows(frame, ColorDepth.TrueColor, screen, site.Styles);
                    site.Note(frame);
                    JsonObject data = new JsonObject {
                        ["w"] = frame.Width,
                        ["h"] = Site.Lines(rows),
                        [entry.Variant == ThemeBackground.Light ? "lightOnly" : "dark"] = FrameHtml.Body(rows),
                        // The scheme's own terminal colours, so the window is the scheme's background, not the site's.
                        ["fg"] = screen.Foreground,
                        ["bg"] = screen.Background
                    };
                    Figures.Add(id, Figures.WithProgram(site, new JsonObject {
                        // Both windows carry the scheme's name; the first chip says which form it is.
                        ["title"] = entry.Name,
                        ["label"] = "Status table, status list, progress bar, bar chart and an error line in " + theme.Name,
                        ["meta"] = new JsonArray(accessible ? "Accessible" : "Faithful", Width + " columns"),
                        ["frame"] = data,
                        ["verdict"] = new JsonArray(ThemeCheckVerdict.Lines(ThemeCheck.Run(theme)).Select(line => (JsonNode)line.Text).ToArray())
                    }, program));
                }
            }
        }
    }
}
