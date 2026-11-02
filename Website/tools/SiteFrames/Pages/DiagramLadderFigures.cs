using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// The diagram docs ladder: eight levels from three boxes to a hierarchy, each a C# program
    /// (<c>Website/examples/docs/diagram-N.cs</c>) with its PowerShell twin, drawn at one width in both themes.
    /// </summary>
    internal static class DiagramLadderFigures {
        private const int Width = 120;

        private static readonly (int Level, string Title, string Label)[] Levels = {
            (1, "Three boxes, two arrows", "Laptop, router and internet joined by two arrows"),
            (2, "Kinds and labels", "A home network with subtitles and labelled links"),
            (3, "Groups", "A small office with LAN, DMZ and cloud groups"),
            (4, "Status", "The small office on Monday morning with warnings, failures and a needs-attention list"),
            (5, "From a list", "A web shop drawn from a list of links"),
            (6, "Live values", "A ping monitor with latency on every node"),
            (7, "A request walkthrough", "A checkout use case at its third step"),
            (8, "Hierarchy", "An IT team as a top-down hierarchy")
        };

        private static string Id(int level) => "diagram-" + level;

        private static string ProgramPath(Site site, int level) => Path.Combine(site.Website, "examples", "docs", Id(level) + ".cs");

        private static Dictionary<string, string?> CaptureEnvironment(string name) {
            Dictionary<string, string?> environment = PowerShellRunner.Environment(name);
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
            return environment;
        }

        /// <summary>Records each level's PowerShell twin on both hosts, dark and light.</summary>
        /// <param name="site">The website containing the shown scripts.</param>
        public static void Capture(Site site) {
            foreach ((int level, _, _) in Levels) {
                foreach (string name in new[] { "dark", "light" }) {
                    Twins.Capture(site, "docs", Id(level), Width, name + "-" + Width, CaptureEnvironment(name));
                }
            }
        }

        private static CellBuffer CheckedFrame(Site site, int level, string name, ITheme theme, JsonObject hosts) {
            CellBuffer frame = CSharpRunner.Run(ProgramPath(site, level), Width, ColorDepth.TrueColor, GlyphTier.Full, theme);
            List<string> problems = new List<string>();
            Twins.Check(site, "docs", Id(level), Width, frame, hosts, problems, name + "-" + Width, CaptureEnvironment(name));
            WidgetsPage.Report(problems, Id(level) + "/" + name + "-" + Width);
            return frame;
        }

        /// <summary>Adds every level to the site's generated figures.</summary>
        /// <param name="site">The site paths, shared renderer and style registry.</param>
        public static void Render(Site site) {
            JsonObject hosts = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "hosts.json")))!;
            foreach ((int level, string title, string label) in Levels) {
                CellBuffer dark = CheckedFrame(site, level, "dark", Theme.Dark, hosts);
                CellBuffer light = CheckedFrame(site, level, "light", Theme.Light, hosts);
                JsonObject figure = Figures.WithProgram(site, new JsonObject {
                    ["title"] = title,
                    ["file"] = Id(level) + ".ps1",
                    ["label"] = label,
                    ["meta"] = new JsonArray(Width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.FrameAt(Width, dark, light)
                }, ProgramPath(site, level));
                (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Path.Combine(site.Website, "examples", "docs", Id(level) + ".ps1")));
                figure["script"] = script;
                figure["scriptFull"] = scriptFull;
                DocCards.Offer(Id(level) + "-" + Width, dark, Frames.Screen.Dark);
                Figures.Add(Id(level) + "-" + Width, figure);
            }
        }
    }
}
