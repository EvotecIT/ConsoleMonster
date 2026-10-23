using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>The shared shell prompt renderer with explicit, illustrative completed-command data.</summary>
    internal static class PromptFigures {
        private static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "shell-prompt.cs");

        public static void Render(Site site) {
            string program = ProgramPath(site);
            foreach (int width in new[] { 120, 60 }) {
                CellBuffer dark = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                CellBuffer light = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                Figures.Add("shell-prompt-" + width, Figures.WithProgram(new JsonObject {
                    ["title"] = "Shell prompt · " + width + " columns",
                    ["label"] = "Illustrative path, dirty branch, success and failure; only the slow command has a duration.",
                    ["frame"] = site.Frame(dark, light)
                }, program));
            }
        }

        public static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (int width in new[] { 120, 60 }) {
                foreach (bool light in new[] { false, true }) {
                    foreach (ColorDepth depth in new[] { ColorDepth.TrueColor, ColorDepth.Ansi16, ColorDepth.None }) {
                        ITheme theme = light ? Theme.Light : Theme.Dark;
                        GlyphTier glyphs = depth == ColorDepth.TrueColor ? GlyphTier.Full : GlyphTier.Ascii;
                        CellBuffer frame = CSharpRunner.Run(ProgramPath(site), width, depth, glyphs, theme);
                        ExportOptions options = new ExportOptions { Theme = light ? ExportTheme.Light : ExportTheme.Dark,
                            ColorDepth = depth, Glyphs = glyphs, Title = "ConsoleMonster shell prompt" };
                        string name = Path.Combine(output, "prompt-" + width + "-" + (light ? "light" : "dark") + "-" + depth);
                        File.WriteAllText(name + ".html", ConsoleExporter.ToHtml(frame, options));
                        File.WriteAllText(name + ".svg", ConsoleExporter.ToSvg(frame, options));
                        File.WriteAllText(name + ".txt", ConsoleExporter.ToText(frame));
                    }
                }
            }
            return 0;
        }
    }
}
