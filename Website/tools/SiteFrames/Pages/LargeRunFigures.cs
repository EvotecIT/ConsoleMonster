using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples;
using ConsoleMonster.Export;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Deterministic large-run viewports and full summaries from the same scene as the golden tests.</summary>
    internal static class LargeRunFigures {
        public static void Render(Site site) {
            string program = Path.Combine(site.Website, "examples", "docs", "large-run.cs");
            string script = Path.Combine(site.Website, "examples", "docs", "large-run.ps1");
            (string shortScript, string fullScript) = SourceSnippets.Program(Site.ReadText(script));
            Figures.Add("large-run-example", Figures.WithProgram(site, new JsonObject {
                ["title"] = "Fleet checks", ["label"] = "The completed two-item example, including its warning detail",
                ["file"] = "large-run.ps1", ["script"] = shortScript, ["scriptFull"] = fullScript,
                ["powershellRepo"] = "Website/examples/docs/large-run.ps1", ["csharpRepo"] = "Website/examples/docs/large-run.cs",
                ["frame"] = site.FrameAt(120,
                    CSharpRunner.Run(program, 120, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark),
                    CSharpRunner.Run(program, 120, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light))
            }, program));
            foreach (int width in new[] { 120, 60 }) {
                var dark = LargeRunScene.Frame(width, 50, Theme.Dark);
                var light = LargeRunScene.Frame(width, 50, Theme.Light);
                JsonObject figure = new JsonObject {
                    ["title"] = "Large-run progress", ["label"] = "Finished checks stay above the bounded live dashboard",
                    ["meta"] = new JsonArray(width + " columns", "50 rows", "8 workers", "Illustrative data"),
                    ["frame"] = site.FrameAt(width, dark, light), ["csharpRepo"] = "ConsoleMonster.Examples/LargeRunScene.cs"
                };
                Figures.Add("large-run-" + width, figure);
                DocCards.Offer("large-run-" + width, dark, null);
            }
        }
        public static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (int width in new[] { 120, 60 }) foreach (bool light in new[] { false, true }) {
                var frame = CSharpRunner.Run(Path.Combine(site.Website, "examples", "docs", "large-run.cs"), width,
                    ColorDepth.TrueColor, GlyphTier.Full, light ? Theme.Light : Theme.Dark);
                string path = Path.Combine(output, "try-it-" + width + "-" + (light ? "light" : "dark"));
                var options = new ExportOptions { Title = "Fleet checks · Try it", Theme = light ? ExportTheme.Light : ExportTheme.Dark, WindowChrome = true };
                File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + "\n");
                File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
                File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
            }
            foreach (int width in new[] { 120, 60 }) foreach (int height in new[] { 24, 50 })
            foreach (bool light in new[] { false, true }) foreach (string state in new[] { "mid-run", "short-terminal", "ci", "final-summary" }) {
                ITheme theme = light ? Theme.Light : Theme.Dark;
                var frame = LargeRunScene.Frame(width, height, theme, state);
                string path = Path.Combine(output, state + "-" + width + "x" + height + "-" + (light ? "light" : "dark"));
                ExportOptions options = new ExportOptions { Title = "Fleet checks · " + state + " · " + width + "x" + height, Theme = light ? ExportTheme.Light : ExportTheme.Dark, WindowChrome = true };
                File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + "\n");
                File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
                File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
            }
            foreach (int width in new[] { 120, 60 }) foreach (bool light in new[] { false, true }) {
                var profile = TerminalProfile.Fixed(width, 50, ColorDepth.TrueColor, GlyphTier.Full);
                var frame = FrameComposer.RenderBlock(LargeRunScene.Summary(LargeRunScene.Tracker(true)), width, new ConsoleMonster.Canvas.RenderContext(profile, light ? Theme.Light : Theme.Dark));
                string path = Path.Combine(output, "full-summary-" + width + "-" + (light ? "light" : "dark"));
                ExportOptions options = new ExportOptions { Theme = light ? ExportTheme.Light : ExportTheme.Dark, Title = "Fleet checks · complete scrollback summary", WindowChrome = true };
                File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + "\n");
                File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
                File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
            }
            foreach (ColorDepth depth in new[] { ColorDepth.Ansi16, ColorDepth.None }) {
                var frame = LargeRunScene.Frame(60, 24, Theme.Dark, "mid-run", depth);
                string path = Path.Combine(output, "mid-run-60x24-dark-" + (depth == ColorDepth.None ? "no-colour" : "16-colour"));
                ExportOptions options = new ExportOptions { Theme = ExportTheme.Dark, Title = "Fleet checks · reduced colour", WindowChrome = true };
                File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + "\n");
                File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
                File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
            }
            return 0;
        }
    }
}
