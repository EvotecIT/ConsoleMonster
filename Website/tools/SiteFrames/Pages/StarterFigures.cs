using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// Printed reports for the getting-started docs and the task guides: each is a C# program
    /// (<c>Website/examples/&lt;area&gt;/&lt;id&gt;.cs</c>) with its PowerShell twin beside it. The figure is what the program
    /// prints, in both themes; the PowerShell 5.1 and 7 captures and the generated fluent program must draw the same cells.
    /// </summary>
    internal static class StarterFigures {
        private sealed record Entry(string Area, string Id, int Width, string Title, string Label) {
            public string FigureId => Id + "-" + Width;

            public string Variant => "dark-" + Width;
        }

        private static readonly Entry[] Entries = {
            new("docs", "how-it-works", 80, "Show-Console",
                "A rule, a facts line and a panel holding a table of three invented services"),
            new("docs", "layout", 80, "Show-Console",
                "Two panels side by side, the first twice as wide, above a three-column grid of six hosts"),
            new("guides", "first-report", 80, "Show-Console",
                "An invented nightly server report: title rule, facts, a table with status and disk bars")
        };

        private static string ProgramPath(Site site, Entry entry) => Path.Combine(site.Website, "examples", entry.Area, entry.Id + ".cs");

        private static Dictionary<string, string?> Environment() {
            Dictionary<string, string?> environment = PowerShellRunner.Environment("dark");
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
            return environment;
        }

        /// <summary>Records each program's PowerShell twin on every available host.</summary>
        public static void Capture(Site site) {
            foreach (Entry entry in Entries) {
                Twins.Capture(site, entry.Area, entry.Id, entry.Width, entry.Variant, Environment());
            }
        }

        // The generated fluent program must print the same cells as the initializer program.
        private static bool SameFluent(string path, Entry entry, CellBuffer expected) {
            CellBuffer fluent = CSharpRunner.Run(path, entry.Width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark, fluent: true);
            ParityResult result = Parity.Compare(expected, fluent);
            if (!result.Identical) { WidgetsPage.Report(new List<string> { entry.Id + ": C# fluent " + result.Detail }, entry.Id + "/fluent"); }
            return result.Identical;
        }

        public static void Render(Site site) {
            JsonObject hosts = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "hosts.json")))!;
            foreach (Entry entry in Entries) {
                string program = ProgramPath(site, entry);
                CellBuffer dark = CSharpRunner.Run(program, entry.Width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                CellBuffer light = CSharpRunner.Run(program, entry.Width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                List<string> problems = new List<string>();
                JsonObject twins = Twins.Check(site, entry.Area, entry.Id, entry.Width, dark, hosts, problems, entry.Variant, Environment());
                WidgetsPage.Report(problems, entry.Id);
                // WithProgram adds the PowerShell twin beside the program as the figure's script.
                JsonObject figure = Figures.WithProgram(site, new JsonObject {
                    ["title"] = entry.Title,
                    ["label"] = entry.Label,
                    ["meta"] = new JsonArray(entry.Width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.FrameAt(entry.Width, dark, light)
                }, program);
                figure["parity"] = new JsonObject { ["powershell"] = twins, ["fluent"] = SameFluent(program, entry, dark) };
                DocCards.Offer(entry.FigureId, dark, null);
                Figures.Add(entry.FigureId, figure);
            }
        }
    }
}
