using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using SiteFrames.Catalog;

namespace SiteFrames.Pages {
    /// <summary>Site figures and review exports from the complete published explorer program.</summary>
    internal static class ExplorerFigures {
        public static void Render(Site site) {
            foreach (int width in new[] { 120, 60 }) {
                CellBuffer dark = ExplorerExamples.Frame(site, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                CellBuffer light = ExplorerExamples.Frame(site, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                JsonObject figure = Figures.WithProgram(new JsonObject {
                    ["title"] = "Show-ConsoleObject",
                    ["label"] = "Browse members without expanding the entire object",
                    ["meta"] = new JsonArray(width + " columns", "24-bit", "Full"),
                    ["frame"] = site.Frame(dark, light)
                }, ExplorerExamples.ProgramPath(site));
                figure["script"] = File.ReadAllText(Path.Combine(site.Website, "examples", "docs", "object-explorer.ps1"));
                DocCards.Offer("object-explorer-" + width, dark, null);
                Figures.Add("object-explorer-" + width, figure);
            }
        }

        public static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (int width in new[] { 120, 60 }) {
                foreach ((string name, ITheme theme, ExportTheme exportTheme) in new[] { ("dark", (ITheme)Theme.Dark, ExportTheme.Dark), ("light", Theme.Light, ExportTheme.Light) }) {
                    Save(site, output, width, name, theme, exportTheme, ColorDepth.TrueColor, GlyphTier.Full);
                    Save(site, output, width, "expanded-" + name, theme, exportTheme, ColorDepth.TrueColor, GlyphTier.Full, "Down Right F10");
                    Save(site, output, width, "drilled-" + name, theme, exportTheme, ColorDepth.TrueColor, GlyphTier.Full, "Down Down Enter F10");
                }
            }
            Save(site, output, 60, "16-colour", Theme.Dark, ExportTheme.Dark, ColorDepth.Ansi16, GlyphTier.Basic);
            Save(site, output, 60, "no-colour", Theme.Dark, ExportTheme.Dark, ColorDepth.None, GlyphTier.Ascii);
            return 0;
        }

        private static void Save(Site site, string output, int width, string name, ITheme theme, ExportTheme exportTheme, ColorDepth depth, GlyphTier glyphs, string keys = "F10") {
            CellBuffer frame = ExplorerExamples.Frame(site, width, depth, glyphs, theme, keys);
            string path = Path.Combine(output, "object-" + name + "-" + width);
            ExportOptions options = new ExportOptions { Theme = exportTheme, ColorDepth = depth, Glyphs = glyphs, Title = "Object explorer — " + name + " / " + width, WindowChrome = true };
            File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + Environment.NewLine);
            File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
            File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
        }
    }
}
