using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Everyday error figures from the complete, compiled public example.</summary>
    internal static class ErrorFigures {
        internal static CellBuffer Run(Site site, int width, ITheme theme) => CSharpRunner.Run(
            Path.Combine(site.Website, "examples", "errors", "compact.cs"), width, ColorDepth.TrueColor, GlyphTier.Full, theme);

        internal static void Render(Site site) {
            foreach (int width in new[] { 120, 60 }) {
                JsonObject figure = new JsonObject {
                    ["title"] = "Enable-ConsoleFormatting -Feature Errors -CompactErrors",
                    ["label"] = "A compact error with highlighted invocation and folded causes.",
                    ["frame"] = site.FrameAt(width, Run(site, width, Theme.Dark), Run(site, width, Theme.Light))
                };
                Figures.Add("errors-" + width, Figures.WithProgram(site, figure, Path.Combine(site.Website, "examples", "errors", "compact.cs")));
            }
        }

        internal static void ExportReview(Site site, string directory) {
            Directory.CreateDirectory(directory);
            foreach (int width in new[] { 120, 60 }) {
                foreach ((string name, ITheme theme, ExportTheme export) in new[] {
                    ("dark", (ITheme)Theme.Dark, ExportTheme.Dark), ("light", (ITheme)Theme.Light, ExportTheme.Light)
                }) {
                    CellBuffer frame = Run(site, width, theme);
                    string path = Path.Combine(directory, "errors-" + width + "-" + name);
                    ExportOptions options = new ExportOptions { Width = width, Theme = export, Title = "Compact PowerShell errors", WindowChrome = true };
                    File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
                    File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
                    File.WriteAllText(path + ".txt", frame.ToPlainText());
                }
            }
        }
    }
}
