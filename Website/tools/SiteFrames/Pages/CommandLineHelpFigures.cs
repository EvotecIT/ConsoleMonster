using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Compiles and renders the public CLI-help example in both themes.</summary>
    internal static class CommandLineHelpFigures {
        public static void Render(Site site) {
            string program = Path.Combine(site.Website, "examples", "docs", "cli-help.cs");
            foreach (int width in new[] { 120, 60 }) {
                var dark = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                var light = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                Figures.Add("cli-help-" + width, Figures.WithProgram(new JsonObject {
                    ["title"] = "dotnet run",
                    ["label"] = "Parser-independent command-line help",
                    ["meta"] = new JsonArray(width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.Frame(dark, light)
                }, program));
            }
        }
    }
}
