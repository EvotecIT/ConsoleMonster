using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Host overview site figures and portable exports for visual review.</summary>
    internal static class OverviewFigures {
        private static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "host-overview.cs");

        public static void Render(Site site) {
            string program = ProgramPath(site);
            foreach (int width in new[] { 120, 60 }) {
                var dark = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                var light = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                JsonObject figure = Figures.WithProgram(site, new JsonObject {
                    ["title"] = "dotnet run",
                    ["label"] = "Illustrative host data rendered by OverviewWidget",
                    ["meta"] = new JsonArray(width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.FrameAt(width, dark, light)
                }, program);
                DocCards.Offer("host-overview-" + width, dark, null);
                Figures.Add("host-overview-" + width, figure);
            }
        }

        public static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (int width in new[] { 120, 60 }) {
                foreach ((string name, ITheme theme, ExportTheme exportTheme) in new[] { ("dark", (ITheme)Theme.Dark, ExportTheme.Dark), ("light", Theme.Light, ExportTheme.Light) }) {
                    Save(site, output, width, name, theme, exportTheme, ColorDepth.TrueColor, GlyphTier.Full);
                }
            }
            Save(site, output, 60, "16-colour", Theme.Dark, ExportTheme.Dark, ColorDepth.Ansi16, GlyphTier.Basic);
            Save(site, output, 60, "no-colour", Theme.Dark, ExportTheme.Dark, ColorDepth.None, GlyphTier.Ascii);
            return 0;
        }

        private static void Save(Site site, string output, int width, string name, ITheme theme, ExportTheme exportTheme, ColorDepth depth, GlyphTier glyphs) {
            var frame = CSharpRunner.Run(ProgramPath(site), width, depth, glyphs, theme);
            string path = Path.Combine(output, "overview-" + name + "-" + width);
            ExportOptions options = new ExportOptions { Theme = exportTheme, ColorDepth = depth, Glyphs = glyphs, Title = "Host overview — " + name + " / " + width, WindowChrome = true };
            File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + Environment.NewLine);
            File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
            File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
        }
    }
}
