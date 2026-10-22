using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Documentation figures and compiled chart-review exports from the real chart renderer.</summary>
    internal static class ChartFigures {
        private static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "pipeline-charts.cs");

        public static void Render(Site site) {
            string program = ProgramPath(site);
            (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Path.Combine(site.Website, "examples", "docs", "pipeline-charts.ps1")));
            foreach (int width in new[] { 120, 60 }) {
                CellBuffer dark = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                CellBuffer light = CSharpRunner.Run(program, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                string id = "pipeline-charts-" + width;
                DocCards.Offer(id, dark, null);
                Figures.Add(id, Figures.WithProgram(new JsonObject {
                    ["title"] = "Out-ConsoleChart",
                    ["file"] = "pipeline-charts.ps1",
                    ["label"] = "Bars, a distribution and grouped trends from numeric samples",
                    ["meta"] = new JsonArray(width + " columns", "24-bit", "Full"),
                    ["frame"] = site.Frame(dark, light),
                    ["script"] = script,
                    ["scriptFull"] = scriptFull
                }, program));
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
            CellBuffer frame = CSharpRunner.Run(ProgramPath(site), width, depth, glyphs, theme);
            string path = Path.Combine(output, "chart-" + name + "-" + width);
            ExportOptions options = new ExportOptions { Theme = exportTheme, ColorDepth = depth, Glyphs = glyphs, Title = "Pipeline charts - " + name + " / " + width, WindowChrome = true };
            File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + Environment.NewLine);
            File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
            File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
        }
    }
}
