using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Real frames of the existing grid used by the unavailable Out-GridView adapter.</summary>
    internal static class GridViewFigures {
        private static ScreenCapture Capture(Site site, int width, ITheme theme) {
            ScreenCapture capture = new ScreenCapture(width, 20);
            InteractiveOptions options = new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Profile = TerminalProfile.Fixed(width, 20, ColorDepth.TrueColor, GlyphTier.Full),
                Theme = theme,
                Capture = capture,
                Input = new ScriptedInput("Space Down Space F10") { Paced = true },
                Labels = new PromptLabels()
            };
            CSharpRunner.Execute(Path.Combine(site.Website, "examples", "interactive", "gridview-fallback.cs"), options.Profile, theme, options);
            return capture;
        }

        internal static void Render(Site site) {
            JsonNode copy = JsonNode.Parse(Site.ReadText(site.Data("copy")))!["gridViewFallback"]!;
            foreach (int width in new[] { 120, 60 }) {
                Figures.Add("gridview-fallback-" + width, new JsonObject {
                    ["title"] = copy["title"]!.GetValue<string>(),
                    ["label"] = copy["label"]!.GetValue<string>(),
                    ["script"] = Site.ReadText(Path.Combine(site.Website, "examples", "interactive", "gridview-fallback.ps1")),
                    ["csharp"] = Site.ReadText(Path.Combine(site.Website, "examples", "interactive", "gridview-fallback.cs")),
                    ["frame"] = site.FrameAt(width, Capture(site, width, Theme.Dark).Frames[3], Capture(site, width, Theme.Light).Frames[3])
                });
            }
        }

        internal static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "module-link-root.txt"), PowerShellRunner.ModuleLinkRoot(site));
            string caption = JsonNode.Parse(Site.ReadText(site.Data("copy")))!["gridViewFallback"]!["caption"]!.GetValue<string>();
            foreach (int width in new[] { 120, 60 }) {
                foreach (ITheme theme in new[] { Theme.Dark, Theme.Light }) {
                    ScreenCapture capture = Capture(site, width, theme);
                    foreach ((string name, int index) in new[] { ("initial", 0), ("selected", 3) }) {
                        CellBuffer frame = capture.Frames[index];
                        string stem = Path.Combine(output, "gridview-" + name + "-" + width + "-" + theme.Name.ToLowerInvariant());
                        File.WriteAllText(stem + ".txt", frame.ToPlainText());
                        ExportOptions export = new ExportOptions { Title = caption, Theme = theme == Theme.Light ? ExportTheme.Light : ExportTheme.Dark };
                        File.WriteAllText(stem + ".html", ConsoleExporter.ToHtml(frame, export));
                        File.WriteAllText(stem + ".svg", ConsoleExporter.ToSvg(frame, export));
                    }
                }
            }
            Console.WriteLine("GridView review: 8 real frames, HTML, SVG and text, from the compiled example.");
            return 0;
        }
    }
}
