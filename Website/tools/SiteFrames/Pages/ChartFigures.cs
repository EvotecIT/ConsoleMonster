using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Documentation figures and compiled chart-review exports from the real chart renderer.</summary>
    internal static class ChartFigures {
        private static string ProgramPath(Site site) => Path.Combine(site.Website, "examples", "docs", "pipeline-charts.cs");

        private static string Variant(string name, int width) => name + "-" + width;

        private static Dictionary<string, string?> CaptureEnvironment(string name) {
            Dictionary<string, string?> environment = PowerShellRunner.Environment(
                name == "light" ? "light" : "dark", name == "16-colour" ? "16" : name == "no-colour" ? "none" : "truecolor",
                name == "16-colour" ? "basic" : name == "no-colour" ? "ascii" : "full");
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
            return environment;
        }

        /// <summary>Records the shown script on both hosts for every chart review profile.</summary>
        public static void Capture(Site site) {
            foreach (int width in new[] { 120, 60 }) {
                foreach (string name in new[] { "dark", "light" }) {
                    Twins.Capture(site, "docs", "pipeline-charts", width, Variant(name, width), CaptureEnvironment(name));
                }
            }
            foreach (string name in new[] { "16-colour", "no-colour" }) {
                Twins.Capture(site, "docs", "pipeline-charts", 60, Variant(name, 60), CaptureEnvironment(name));
            }
        }

        private static CellBuffer CheckedFrame(Site site, int width, string name, ITheme theme, ColorDepth depth, GlyphTier glyphs) {
            CellBuffer frame = CSharpRunner.Run(ProgramPath(site), width, depth, glyphs, theme);
            JsonObject hosts = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "hosts.json")))!;
            List<string> problems = new List<string>();
            Twins.Check(site, "docs", "pipeline-charts", width, frame, hosts, problems, Variant(name, width), CaptureEnvironment(name));
            WidgetsPage.Report(problems, "charts/" + Variant(name, width));
            return frame;
        }

        public static void Render(Site site) {
            string program = ProgramPath(site);
            (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Path.Combine(site.Website, "examples", "docs", "pipeline-charts.ps1")));
            foreach (int width in new[] { 120, 60 }) {
                CellBuffer dark = CheckedFrame(site, width, "dark", Theme.Dark, ColorDepth.TrueColor, GlyphTier.Full);
                CellBuffer light = CheckedFrame(site, width, "light", Theme.Light, ColorDepth.TrueColor, GlyphTier.Full);
                string id = "pipeline-charts-" + width;
                DocCards.Offer(id, dark, null);
                Figures.Add(id, Figures.WithProgram(site, new JsonObject {
                    ["title"] = "Show-Console -View Chart",
                    ["file"] = "pipeline-charts.ps1",
                    ["label"] = "Bars, a distribution and grouped trends from numeric samples",
                    ["meta"] = new JsonArray(width + " columns", "24-bit", "Full"),
                    ["frame"] = site.FrameAt(width, dark, light),
                    ["script"] = script,
                    ["scriptFull"] = scriptFull
                }, program));
            }
            CheckedFrame(site, 60, "16-colour", Theme.Dark, ColorDepth.Ansi16, GlyphTier.Basic);
            CheckedFrame(site, 60, "no-colour", Theme.Dark, ColorDepth.None, GlyphTier.Ascii);
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
            ReviewChrome(output);
            ReviewMinimumWindow(output);
            return Program.Failures == 0 ? 0 : 1;
        }

        /// <summary>Exports terminal backgrounds at the chrome minimum, including a two-frame recording.</summary>
        private static void ReviewMinimumWindow(string output) {
            foreach (ExportTheme theme in new[] { ExportTheme.Light, ExportTheme.Dark }) {
                foreach (int padding in new[] { 0, 4 }) {
                    LogRegion log = new LogRegion("minimum") { ShowElapsed = false };
                    log.Apply(new LiveEvent("minimum", "write", text: "x"));
                    string path = Path.Combine(output, "minimum-window-" + theme + "-padding-" + padding);
                    ExportOptions options = new ExportOptions {
                        Width = 1, Height = 1, Padding = padding, Theme = theme, Title = "Minimum window", WindowChrome = true
                    };
                    File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(log, options));
                    File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(log, options));
                    RecordingExportOptions recording = new RecordingExportOptions {
                        Width = 1, Height = 1, Padding = padding, Theme = theme, Title = options.Title, WindowChrome = true
                    };
                    LiveEvent[] events = { new LiveEvent("minimum", "write", text: "y", time: 200) };
                    File.WriteAllText(path + "-animated.svg", Recording.Export(log, events, ExportFormat.Svg, recording));
                }
            }
        }

        /// <summary>Reproduces narrow exporter chrome with real chart content, including the reported 20-column frame.</summary>
        private static void ReviewChrome(string output) {
            foreach (ExportTheme theme in new[] { ExportTheme.Light, ExportTheme.Dark }) {
                foreach (int width in new[] { 12, 20, 36, 60 }) {
                    var chart = new Rows();
                    string[] labels = { "long-host-name.example.org", "日本語の長いホスト名" };
                    int labelWidth = Math.Min(ConsoleMonster.Text.DisplayWidth.Measure(labels[0]), width - 6);
                    for (int i = 0; i < labels.Length; i++) {
                        IWidget trend = Chart.Create(new ChartOptions {
                            Type = ChartType.Sparkline, Role = i == 0 ? ThemeToken.Series1 : ThemeToken.Series2,
                            Values = new[] { 1.0 + i, 2.0 + i, 3.0 + i }
                        });
                        chart.Add(new Columns { CollapseBelow = 0 }
                            .Add(new TextBlock(labels[i]) { IsMarkup = false, Role = ThemeToken.Muted, Overflow = TextOverflow.Ellipsis }, LayoutSize.Fixed(labelWidth))
                            .Add(trend, LayoutSize.Fixed(5)));
                    }
                    foreach (bool longTitle in new[] { false, true }) {
                        string title = longTitle ? "Long labels / " + width + " columns" : "Chart";
                        string name = longTitle ? "long-labels-" : "short-title-";
                        ExportOptions options = new ExportOptions { Width = width, Theme = theme, Title = title, WindowChrome = true };
                        string path = Path.Combine(output, name + theme + "-" + width);
                        File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(chart, options));
                        File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(chart, options));
                    }
                }
            }
        }

        private static void Save(Site site, string output, int width, string name, ITheme theme, ExportTheme exportTheme, ColorDepth depth, GlyphTier glyphs) {
            CellBuffer frame = CheckedFrame(site, width, name, theme, depth, glyphs);
            string path = Path.Combine(output, "chart-" + name + "-" + width);
            ExportOptions options = new ExportOptions { Theme = exportTheme, ColorDepth = depth, Glyphs = glyphs, Title = "Pipeline charts - " + name + " / " + width, WindowChrome = true };
            File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame) + Environment.NewLine);
            File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options));
            File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options));
        }
    }
}
