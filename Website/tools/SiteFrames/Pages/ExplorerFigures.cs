using System;
using System.Collections.Generic;
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
        private static InteractivePage.Story Story(int width, string name, string keys = "F10") =>
            new InteractivePage.Story("object-explorer-" + name + "-" + width, "Object explorer", string.Empty, keys, width, 20, false, false) {
                Area = "docs", ExampleId = "object-explorer",
                Theme = name.EndsWith("light", StringComparison.Ordinal) ? Theme.Light : Theme.Dark,
                Depth = name == "no-colour" ? ColorDepth.None : name == "16-colour" ? ColorDepth.Ansi16 : ColorDepth.TrueColor,
                Glyphs = name == "no-colour" ? GlyphTier.Ascii : name == "16-colour" ? GlyphTier.Basic : GlyphTier.Full
            };

        /// <summary>Captures the published PowerShell example in each review profile and navigation state.</summary>
        public static void Capture(Site site) {
            foreach (int width in new[] { 120, 60 }) {
                foreach (string name in new[] { "dark", "light" }) {
                    InteractivePage.CaptureExample(site, Story(width, name));
                    InteractivePage.CaptureExample(site, Story(width, "expanded-" + name, "Down Right F10"));
                    InteractivePage.CaptureExample(site, Story(width, "drilled-" + name, "Down Down Enter F10"));
                }
            }
            InteractivePage.CaptureExample(site, Story(60, "16-colour"));
            InteractivePage.CaptureExample(site, Story(60, "no-colour"));
        }

        private static bool Check(Site site, int width, string name, string keys = "F10") {
            List<string> problems = new List<string>();
            bool same = InteractivePage.CheckExample(site, Story(width, name, keys), problems);
            WidgetsPage.Report(problems, "object-explorer/" + name + "-" + width);
            return same;
        }

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
                figure["parity"] = new JsonObject { ["dark"] = Check(site, width, "dark"), ["light"] = Check(site, width, "light") };
                DocCards.Offer("object-explorer-" + width, dark, null);
                Figures.Add("object-explorer-" + width, figure);
            }
            Check(site, 60, "16-colour");
            Check(site, 60, "no-colour");
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
            return Program.Failures == 0 ? 0 : 1;
        }

        private static void Save(Site site, string output, int width, string name, ITheme theme, ExportTheme exportTheme, ColorDepth depth, GlyphTier glyphs, string keys = "F10") {
            Check(site, width, name, keys);
            CellBuffer frame = ExplorerExamples.Frame(site, width, depth, glyphs, theme, keys);
            string path = Path.Combine(output, "object-" + name + "-" + width);
            ExportOptions options = new ExportOptions { Theme = exportTheme, ColorDepth = depth, Glyphs = glyphs, Title = "Object explorer — " + name + " / " + width, WindowChrome = true };
            File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + Environment.NewLine);
            File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
            File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
        }
    }
}
