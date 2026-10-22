using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using ConsoleMonster.Canvas;
using SiteFrames.Catalog;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Documentation figures and terminal-profile review exports for text comparison.</summary>
    internal static class TextDiffFigures {
        private static string Variant(string name, int width) => name + "-" + width;

        private static Dictionary<string, string?> CaptureEnvironment(string name) {
            Dictionary<string, string?> environment = PowerShellRunner.Environment(
                name == "light" ? "light" : "dark", name == "16-colour" ? "16" : name == "no-colour" ? "none" : "truecolor",
                name == "16-colour" ? "basic" : name == "no-colour" ? "ascii" : "full");
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
            return environment;
        }

        /// <summary>Records the shown script on both hosts for every text comparison review profile.</summary>
        /// <param name="site">The website containing the exact PowerShell example shown with the figures.</param>
        public static void Capture(Site site) {
            foreach (int width in new[] { 120, 60 }) {
                foreach (string name in new[] { "dark", "light" }) {
                    Twins.Capture(site, "docs", "text-diff", width, Variant(name, width), CaptureEnvironment(name));
                }
            }
            foreach (string name in new[] { "16-colour", "no-colour" }) {
                Twins.Capture(site, "docs", "text-diff", 60, Variant(name, 60), CaptureEnvironment(name));
            }
        }

        private static CellBuffer CheckedFrame(Site site, int width, string name, ITheme theme, ColorDepth depth, GlyphTier glyphs) {
            CellBuffer frame = TextDiffExamples.Run(site, width, depth, glyphs, theme);
            JsonObject hosts = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "hosts.json")))!;
            List<string> problems = new List<string>();
            Twins.Check(site, "docs", "text-diff", width, frame, hosts, problems, Variant(name, width), CaptureEnvironment(name));
            WidgetsPage.Report(problems, "text-diff/" + Variant(name, width));
            return frame;
        }

        /// <summary>Adds both responsive widths to the site's generated figure collection.</summary>
        /// <param name="site">The site paths, shared renderer and style registry.</param>
        public static void Render(Site site) {
            foreach (int width in new[] { 120, 60 }) {
                CellBuffer dark = CheckedFrame(site, width, "dark", Theme.Dark, ColorDepth.TrueColor, GlyphTier.Full);
                CellBuffer light = CheckedFrame(site, width, "light", Theme.Light, ColorDepth.TrueColor, GlyphTier.Full);
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
            CheckedFrame(site, 60, "16-colour", Theme.Dark, ColorDepth.Ansi16, GlyphTier.Basic);
            CheckedFrame(site, 60, "no-colour", Theme.Dark, ColorDepth.None, GlyphTier.Ascii);
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
            return Program.Failures == 0 ? 0 : 1;
        }

        private static void Save(Site site, string output, int width, string name, ITheme theme, ExportTheme exportTheme, ColorDepth depth, GlyphTier glyphs) {
            CellBuffer frame = CheckedFrame(site, width, name, theme, depth, glyphs);
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
