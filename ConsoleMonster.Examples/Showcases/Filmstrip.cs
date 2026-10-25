using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Records the Server toolbox flow (menu, choices, review, run, results, back) and the installer (steps, review, the
    /// install run, the summary) into screen captures driven by scripted keys, and writes each flow's key frames side by
    /// side as one page of SVG images, to compare frame against frame for any change of style.
    /// </summary>
    public static class Filmstrip {
        /// <summary>Writes the two filmstrip pages for a glyph tier and width.</summary>
        /// <param name="folder">The output folder.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <param name="width">The screen width.</param>
        /// <param name="height">The screen height.</param>
        /// <returns>The pages written.</returns>
        public static List<string> Run(string folder, GlyphTier tier, int width, int height) {
            Directory.CreateDirectory(folder);
            string suffix = tier.ToString().ToLowerInvariant() + "-" + width.ToString(CultureInfo.InvariantCulture);
            List<string> pages = new List<string>();

            ScreenCapture toolbox = new ScreenCapture(width, height);
            Drive(() => ServerToolbox.Build().Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(toolbox, tier,
                "wait:1600 Enter Space Down Space Escape k wait:300 Escape r wait:500 Enter wait:2500 wait:4500 Escape wait:500 Ctrl+Q y"),
                StorageRoot = Path.Combine(Path.GetTempPath(), "cm-film-" + Guid.NewGuid().ToString("N").Substring(0, 8)) }));
            pages.Add(Write(Path.Combine(folder, "filmstrip-toolbox-" + suffix + ".html"), "Server toolbox: choose, review, run, results", new[] {
                Frame("1 menu", First(toolbox, "Targets", "Tasks", "Presets")),
                Frame("2 targets", Last(toolbox, "Targets", "2 of 9 selected")),
                Frame("3 tasks", First(toolbox, "Tasks", "Disk cleanup")),
                Frame("4 review", First(toolbox, "Equivalent command", "Run")),
                Frame("5 run", First(toolbox, "Run", "stop")),
                Frame("6 run later", Last(toolbox, "Run", "stop")),
                Frame("7 results", Last(toolbox, "Results", "Next steps")),
                Frame("8 back", Last(toolbox, "Review & run", "Equivalent command"))
            }, width));

            ScreenCapture installer = new ScreenCapture(width, height);
            Drive(() => SetupWizard.Show(new FormOptions { Interactive = Options(installer, tier,
                "PageDown wait:200 PageDown wait:200 PageDown Right wait:1800 Down Enter wait:200 Enter PageDown wait:400 Enter wait:5000 Enter") }));
            pages.Add(Write(Path.Combine(folder, "filmstrip-installer-" + suffix + ".html"), "Installer: steps, review, install, summary", new[] {
                Frame("1 location", First(installer, "Install folder", "1/5")),
                Frame("2 components", First(installer, "Agent service", "2/5")),
                Frame("3 connection", Last(installer, "Region", "4/5")),
                Frame("4 review", First(installer, "Equivalent command", "5/5")),
                Frame("5 installing", First(installer, "Run", "stop")),
                Frame("6 installing later", Last(installer, "Run", "stop")),
                Frame("7 summary", Last(installer, "Toolbox Agent installed", "Next steps"))
            }, width));
            return pages;
        }

        private static InteractiveOptions Options(ScreenCapture capture, GlyphTier tier, string keys) {
            return new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = new ScriptedInput(keys) { RealTime = true, Paced = true },
                Capture = capture,
                Theme = Theme.Dark,
                Profile = TerminalProfile.Fixed(capture.Width, capture.Height, ColorDepth.TrueColor, tier)
            };
        }

        private static void Drive(Action run) {
            try {
                run();
            } catch (Exception ex) when (ex is InputClosedException || ex is PromptCanceledException || ex is OperationCanceledException) {
            }
        }

        private static KeyValuePair<string, CellBuffer?> Frame(string name, CellBuffer? frame) => new KeyValuePair<string, CellBuffer?>(name, frame);

        private static CellBuffer? Last(ScreenCapture capture, params string[] phrases) => Find(capture, phrases, false);

        private static CellBuffer? First(ScreenCapture capture, params string[] phrases) => Find(capture, phrases, true);

        private static CellBuffer? Find(ScreenCapture capture, string[] phrases, bool first) {
            CellBuffer? found = null;
            foreach (CellBuffer frame in capture.Frames) {
                string text = frame.ToPlainText();
                bool all = true;
                foreach (string phrase in phrases) {
                    if (text.IndexOf(phrase, StringComparison.Ordinal) < 0) {
                        all = false;
                        break;
                    }
                }
                if (all) {
                    found = frame;
                    if (first) {
                        break;
                    }
                }
            }
            return found;
        }

        private static string Write(string path, string title, KeyValuePair<string, CellBuffer?>[] frames, int width) {
            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>").Append(title).Append("</title><style>")
                .Append("body{margin:16px;background:#2b2b30;font:13px system-ui,'Segoe UI',sans-serif;color:#ddd}h1{font-size:15px;margin:0 0 12px}")
                .Append(".strip{display:grid;grid-template-columns:repeat(").Append(width > 100 ? 3 : 4).Append(",1fr);gap:14px}figure{margin:0}figure img{width:100%;display:block;border-radius:6px}figcaption{margin:4px 0 0;color:#aaa}.missing{padding:40px;border:1px dashed #666;color:#f88}")
                .Append("</style></head><body><h1>").Append(title).Append("</h1><div class=\"strip\">");
            foreach (KeyValuePair<string, CellBuffer?> frame in frames) {
                html.Append("<figure>");
                if (frame.Value == null) {
                    html.Append("<div class=\"missing\">no frame</div>");
                } else {
                    string svg = ConsoleExporter.ToSvg(frame.Value, new ExportOptions { Theme = ExportTheme.Dark, Padding = 6 });
                    html.Append("<img src=\"data:image/svg+xml;base64,").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))).Append("\">");
                }
                html.Append("<figcaption>").Append(frame.Key).Append("</figcaption></figure>");
            }
            html.Append("</div></body></html>");
            File.WriteAllText(path, html.ToString(), new UTF8Encoding(false));
            return path;
        }
    }
}
