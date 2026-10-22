using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using ConsoleMonster.Canvas;
using SiteFrames.Catalog;

namespace SiteFrames.Pages {
    /// <summary>Documentation figures and terminal-profile review exports for text comparison.</summary>
    internal static class TextDiffFigures {
        /// <summary>Adds both responsive widths to the site's generated figure collection.</summary>
        /// <param name="site">The site paths, shared renderer and style registry.</param>
        public static void Render(Site site) {
            foreach (int width in new[] { 120, 60 }) {
                CellBuffer dark = TextDiffExamples.Run(site, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark);
                CellBuffer light = TextDiffExamples.Run(site, width, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light);
                JsonObject figure = Figures.WithProgram(new JsonObject {
                    ["title"] = "Compare-ConsoleText",
                    ["file"] = "text-diff.ps1",
                    ["label"] = "Before and After with numbered changes, word highlights and collapsed context",
                    ["meta"] = new JsonArray(width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.Frame(dark, light)
                }, TextDiffExamples.ProgramPath(site));
                (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Path.Combine(site.Website, "examples", "docs", "text-diff.ps1")));
                figure["script"] = script;
                figure["scriptFull"] = scriptFull;
                DocCards.Offer("text-diff-" + width, dark, Frames.Screen.Dark);
                Figures.Add("text-diff-" + width, figure);
            }
        }

        /// <summary>Exports dark, light, 16-colour and no-colour review frames through the core exporter.</summary>
        /// <param name="site">The website containing the exact program used by the documentation figures.</param>
        /// <param name="output">The directory receiving HTML, SVG and plain text exports.</param>
        /// <returns>Zero after every requested profile has been written.</returns>
        public static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (int width in new[] { 120, 60 }) {
                foreach ((string name, ITheme theme, ExportTheme exportTheme) in new[] {
                    ("dark", (ITheme)Theme.Dark, ExportTheme.Dark), ("light", Theme.Light, ExportTheme.Light)
                }) {
                    Save(site, output, width, name, theme, exportTheme, ColorDepth.TrueColor, GlyphTier.Full);
                }
            }
            Save(site, output, 60, "16-colour", Theme.Dark, ExportTheme.Dark, ColorDepth.Ansi16, GlyphTier.Basic);
            Save(site, output, 60, "no-colour", Theme.Dark, ExportTheme.Dark, ColorDepth.None, GlyphTier.Ascii);
            return 0;
        }

        private static void Save(Site site, string output, int width, string name, ITheme theme, ExportTheme exportTheme, ColorDepth depth, GlyphTier glyphs) {
            CellBuffer frame = TextDiffExamples.Run(site, width, depth, glyphs, theme);
            string path = Path.Combine(output, "text-diff-" + name + "-" + width);
            ExportOptions options = new ExportOptions {
                Theme = exportTheme, ColorDepth = depth, Glyphs = glyphs,
                Title = "Text comparison — " + name + " / " + width, WindowChrome = true
            };
            File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + Environment.NewLine);
            File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
            File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
        }
    }
}
