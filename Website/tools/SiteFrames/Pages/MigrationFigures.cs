using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Inventory report rendered from the complete program linked by the migration guides.</summary>
    internal static class MigrationFigures {
        internal static void Render(Site site) {
            string folder = Path.Combine(site.Website, "examples", "migration");
            string program = Path.Combine(folder, "report.cs");
            (string script, string full) = SourceSnippets.Program(Site.ReadText(Path.Combine(folder, "report.ps1")));
            foreach (int width in new[] { 100, 48 }) {
                var dark = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                var light = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                DocCards.Offer("migration-report-" + width, dark, null);
                Figures.Add("migration-report-" + width, Figures.WithProgram(site, new JsonObject {
                    ["title"] = "Patch inventory",
                    ["label"] = "Illustrative server inventory rendered by the C# program at " + width + " columns",
                    ["meta"] = new JsonArray(width + " columns", "24-bit", "Full"),
                    ["file"] = "report.ps1",
                    ["script"] = script,
                    ["scriptFull"] = full,
                    ["powershellRepo"] = "Website/examples/migration/report.ps1",
                    ["csharpRepo"] = "Website/examples/migration/report.cs",
                    ["frame"] = site.FrameAt(width, dark, light)
                }, program));
            }
            string formProgram = Path.Combine(folder, "form.cs");
            (string formScript, string formFull) = SourceSnippets.Program(Site.ReadText(Path.Combine(folder, "form.ps1")));
            var formDark = FormFrame(formProgram, Theme.Dark);
            DocCards.Offer("migration-form", formDark, null);
            Figures.Add("migration-form", Figures.WithProgram(site, new JsonObject {
                ["title"] = "Service inventory form",
                ["label"] = "An initialized form with an illustrative service name; no command is executed",
                ["meta"] = new JsonArray("80 columns", "24-bit", "Full"),
                ["file"] = "form.ps1",
                ["script"] = formScript,
                ["scriptFull"] = formFull,
                ["powershellRepo"] = "Website/examples/migration/form.ps1",
                ["csharpRepo"] = "Website/examples/migration/form.cs",
                ["frame"] = site.Frame(formDark, FormFrame(formProgram, Theme.Light))
            }, formProgram));
        }

        /// <summary>Drives the public form program through ordinary input and retains its initialized screen.</summary>
        private static ConsoleMonster.Canvas.CellBuffer FormFrame(string program, ITheme theme) {
            ScreenCapture capture = new ScreenCapture(80, 20);
            CSharpRunner.Execute(program, TerminalProfile.Fixed(80, 20, ColorDepth.TrueColor, GlyphTier.Full), theme,
                new InteractiveOptions {
                    Mode = InteractionMode.Rich,
                    Input = new ScriptedInput("Escape y"),
                    Capture = capture,
                    Theme = theme
                });
            return capture.Frames[0];
        }

    }
}
