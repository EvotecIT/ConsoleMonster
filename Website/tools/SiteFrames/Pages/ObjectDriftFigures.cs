using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Documentation figures for the baseline matrix that compares several objects against one reference.</summary>
    internal static class ObjectDriftFigures {
        private static readonly int[] Widths = { 120, 60 };

        private static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "object-drift.cs");

        private static Dictionary<string, string?> CaptureEnvironment(string name) {
            Dictionary<string, string?> environment = PowerShellRunner.Environment(name);
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
            return environment;
        }

        /// <summary>Records the shown script on both hosts, dark and light, at both widths.</summary>
        /// <param name="site">The website containing the exact PowerShell example shown with the figures.</param>
        public static void Capture(Site site) {
            foreach (int width in Widths) {
                foreach (string name in new[] { "dark", "light" }) {
                    Twins.Capture(site, "docs", "object-drift", width, name + "-" + width, CaptureEnvironment(name));
                }
            }
        }

        private static CellBuffer CheckedFrame(Site site, int width, string name, ITheme theme, JsonObject hosts) {
            CellBuffer frame = CSharpRunner.Run(ProgramPath(site), width, ColorDepth.TrueColor, GlyphTier.Full, theme);
            List<string> problems = new List<string>();
            Twins.Check(site, "docs", "object-drift", width, frame, hosts, problems, name + "-" + width, CaptureEnvironment(name));
            WidgetsPage.Report(problems, "object-drift/" + name + "-" + width);
            return frame;
        }

        /// <summary>Adds the wide matrix and its narrow record layout to the site's generated figures.</summary>
        /// <param name="site">The site paths, shared renderer and style registry.</param>
        public static void Render(Site site) {
            JsonObject hosts = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "hosts.json")))!;
            foreach (int width in Widths) {
                CellBuffer dark = CheckedFrame(site, width, "dark", Theme.Dark, hosts);
                CellBuffer light = CheckedFrame(site, width, "light", Theme.Light, hosts);
                JsonObject figure = Figures.WithProgram(site, new JsonObject {
                    ["title"] = "Compare-ConsoleObject -View Baseline",
                    ["file"] = "object-drift.ps1",
                    ["label"] = "Four domain controllers compared with dc01, drift highlighted and a status for every property",
                    ["meta"] = new JsonArray(width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.FrameAt(width, dark, light)
                }, ProgramPath(site));
                (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Path.Combine(site.Website, "examples", "docs", "object-drift.ps1")));
                figure["script"] = script;
                figure["scriptFull"] = scriptFull;
                DocCards.Offer("object-drift-" + width, dark, Frames.Screen.Dark);
                Figures.Add("object-drift-" + width, figure);
            }
        }
    }
}
