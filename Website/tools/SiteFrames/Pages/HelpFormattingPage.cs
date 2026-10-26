using VtScreen = ConsoleMonster.Sessions.VtScreen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using System.Text.RegularExpressions;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>Native host-help captures and review frames, produced by the site's ConPTY and VT renderer.</summary>
    internal static class HelpFormattingPage {
        private static int Height(string example) => example == "help" ? 18 : 32;
        private static readonly string[] Examples = { "native", "get-help", "command-help", "help", "show-consolehelp" };
        private static readonly int[] Widths = { 120, 60 };
        private static readonly string[] Themes = { "dark", "light" };

        private static string Script(Site site, string example) => Path.Combine(site.Website, "examples", "help-formatting", example + ".ps1");

        private static string Driver(Site site) => Path.Combine(site.Tool, "Capture", "Assets", "HelpFormatting.ps1");

        private static string Id(string example, PowerShellHost host, int width, string theme) => example + "-" + host.Id + "-" + width + "-" + theme;

        private static string CapturePath(Site site, string id) => Path.Combine(site.Captures, "help-formatting", id + ".ansi");

        /// <summary>Runs the native host routing, section, transcript and pager contracts on both PowerShell runtimes.</summary>
        /// <param name="site">The site and repository paths.</param>
        /// <param name="output">The directory receiving native captures and contract receipts.</param>
        /// <returns>Zero after both native hosts pass.</returns>
        public static int Verify(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (PowerShellHost host in PowerShellHost.All) {
                string result = Path.Combine(output, host.Id + ".json");
                string script = Path.Combine(site.Repository, "Module", "Tests", "Support", "Assert-HelpFormattingHost.ps1");
                byte[] bytes = PowerShellRunner.Bytes(PowerShellRunner.Run(site, host, script, 120, 100,
                    PowerShellRunner.Environment("dark"), arguments: "-ResultPath '" + result.Replace("'", "''") + "'",
                    setup: "trap { $_ | Out-String | Set-Content -LiteralPath '" + (result + ".failure.txt").Replace("'", "''") + "'; exit 1 }"));
                File.WriteAllBytes(Path.Combine(output, host.Id + ".ansi"), bytes);
                if (!File.Exists(result)) { throw new InvalidOperationException("Help host contracts failed on " + host.Id + "; inspect " + output); }
                Console.WriteLine("Help host contracts passed " + host.Id);
            }
            return 0;
        }

        private static Dictionary<string, string?> CaptureEnvironment(string theme) {
            Dictionary<string, string?> environment = PowerShellRunner.Environment(theme);
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Debug";
            return environment;
        }

        private static string Fingerprint(Site site, string example, int width, string theme) =>
            Stamps.Of(Script(site, example), CaptureEnvironment(theme), width + "x" + Height(example), Site.ReadText(Driver(site)));

        /// <summary>Executes the complete published scripts on both supported PowerShell hosts.</summary>
        public static void Capture(Site site, Dictionary<string, string>? failures = null) {
            foreach (PowerShellHost host in PowerShellHost.Available) {
                foreach (int width in Widths) {
                    foreach (string theme in Themes) {
                        foreach (string example in Examples) {
                            string id = Id(example, host, width, theme);
                            try {
                                Dictionary<string, string?> environment = CaptureEnvironment(theme);
                                string diagnostic = Path.Combine(Path.GetTempPath(), "cmsite-help-" + Guid.NewGuid().ToString("N") + ".txt");
                                string arguments = "-ExamplePath '" + Script(site, example).Replace("'", "''") + "' -DiagnosticPath '" + diagnostic.Replace("'", "''") + "'";
                                if (example != "native" && example != "show-consolehelp") { arguments += " -Formatted"; }
                                if (example == "help") { arguments += " -Paged"; }
                                List<OutputChunk> chunks;
                                try {
                                    chunks = PowerShellRunner.Run(site, host, Driver(site), width, Height(example), environment,
                                        timeout: TimeSpan.FromSeconds(30), arguments: arguments, interactive: true);
                                } catch (InvalidOperationException ex) when (File.Exists(diagnostic)) {
                                    throw new InvalidOperationException(id + ": " + File.ReadAllText(diagnostic), ex);
                                } finally {
                                    if (File.Exists(diagnostic)) { File.Delete(diagnostic); }
                                }
                                string path = CapturePath(site, id);
                                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                                File.WriteAllBytes(path, PowerShellRunner.Bytes(chunks));
                                Stamps.Write(site, path, Fingerprint(site, example, width, theme));
                                CaptureRun.Record(path, width, Height(example), theme);
                                Console.WriteLine("captured help-formatting/" + id);
                            } catch (Exception ex) when (failures != null && (ex is InvalidOperationException || ex is TimeoutException)) {
                                failures[id] = ex.Message;
                                Console.Error.WriteLine("capture failed: " + id + ": " + ex.Message.Split('\n')[0]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Offers sourced figures to the existing cm-frame shortcode and docs card catalogue.</summary>
        public static void Render(Site site) {
            foreach (PowerShellHost host in PowerShellHost.All) {
                foreach (int width in Widths) {
                    foreach (string example in Examples) {
                        CellBuffer dark;
                        CellBuffer light;
                        try {
                            dark = Frame(site, example, host, width, "dark");
                            light = Frame(site, example, host, width, "light");
                        } catch (Exception ex) when (ex is InvalidOperationException || ex is IOException) {
                            WidgetsPage.Report(new List<string> { ex.Message }, "help-formatting");
                            continue;
                        }
                        string id = "help-formatting-" + example + "-" + host.Id + "-" + width;
                        string script = Site.ReadText(Script(site, example));
                        Figures.Add(id, new JsonObject {
                            ["title"] = Title(example),
                            ["label"] = host.Label + ": " + Title(example),
                            ["file"] = example + ".ps1",
                            ["meta"] = new JsonArray(host.Label, width + " columns", example == "help" ? "First page" : "Native command"),
                            ["frame"] = site.FrameAt(width, dark, light),
                            ["script"] = script,
                            ["scriptFull"] = script
                        });
                        DocCards.Offer(id, dark, Screen.Dark);
                    }
                }
            }
        }

        /// <summary>Captures real help and writes standalone HTML, SVG and text review frames.</summary>
        /// <param name="site">The site's canonical source and capture paths.</param>
        /// <param name="scratch">The directory receiving the review exports.</param>
        /// <param name="reuseCaptures">Uses existing captures after verifying their source fingerprints.</param>
        /// <returns>Zero when every host and frame passes; otherwise one.</returns>
        public static int Review(Site site, string scratch, bool reuseCaptures = false) {
            Dictionary<string, string> failures = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!reuseCaptures) { Capture(site, failures); }
            Directory.CreateDirectory(scratch);
            JsonObject evidence = new JsonObject();
            StringBuilder index = new StringBuilder("<h1>Native host help</h1><p>Complete runnable scripts, captured through ConPTY. Before and after use default Get-Help Get-ConsoleCapability.</p><ul>");
            foreach (PowerShellHost host in PowerShellHost.All) {
                foreach (int width in Widths) {
                    foreach (string theme in Themes) {
                        ParityResult? parity = null;
                        if (!failures.ContainsKey(Id("show-consolehelp", host, width, theme)) && !failures.ContainsKey(Id("command-help", host, width, theme))) {
                            parity = Parity.Compare(Frame(site, "get-help", host, width, theme), Frame(site, "command-help", host, width, theme));
                        }
                        if (parity != null && !parity.Identical) {
                            throw new InvalidOperationException(Id("command-help", host, width, theme) + " differs from default Get-Help: " + parity.Detail);
                        }
                        foreach (string example in Examples) {
                            string id = Id(example, host, width, theme);
                            if (failures.TryGetValue(id, out string? failure)) {
                                evidence[id] = new JsonObject { ["error"] = failure };
                                index.Append("<li>").Append(WebUtility.HtmlEncode(id)).Append(": capture failed; see evidence.json</li>");
                                continue;
                            }
                            CellBuffer frame = Frame(site, example, host, width, theme);
                            Screen screen = theme == "dark" ? Screen.Dark : Screen.Light;
                            string body = FrameHtml.Body(FrameHtml.Rows(frame, ColorDepth.TrueColor, screen, site.Styles));
                            string html = "<h1>" + WebUtility.HtmlEncode(host.Label + " · " + width + " columns · " + theme + " · " + Title(example)) + "</h1>"
                                + "<pre class=\"cm-screen cm-screen--" + theme + "\" style=\"--cols:" + width + "\">" + body + "</pre>"
                                + "<details><summary>" + example + ".ps1</summary><pre><code>" + WebUtility.HtmlEncode(Site.ReadText(Script(site, example))) + "</code></pre></details>";
                            site.Write(Path.Combine(scratch, id + ".html"), Document(site, html, theme));
                            site.Write(Path.Combine(scratch, id + ".txt"), Parity.Describe(frame));
                            site.Write(Path.Combine(scratch, id + ".svg"), ConsoleExporter.ToSvg(frame, new ExportOptions {
                                Theme = theme == "dark" ? ExportTheme.Dark : ExportTheme.Light,
                                Title = host.Label + " · " + width + " columns · " + Title(example), WindowChrome = true
                            }));
                            index.Append("<li><a href=\"").Append(id).Append(".html\">").Append(WebUtility.HtmlEncode(id)).Append("</a></li>");
                            evidence[id] = new JsonObject {
                                ["capture"] = Path.GetRelativePath(site.Website, CapturePath(site, id)).Replace('\\', '/'),
                                ["source"] = Path.GetRelativePath(site.Website, Script(site, example)).Replace('\\', '/'),
                                ["width"] = frame.Width, ["height"] = frame.Height,
                                ["canonicalParity"] = example == "command-help" ? parity?.Detail : null,
                                ["firstPagePrefix"] = example == "help"
                            };
                        }
                    }
                }
            }
            index.Append("</ul>");
            site.Write(Path.Combine(scratch, "index.html"), Document(site, index.ToString(), "dark"));
            site.WriteJson(Path.Combine(scratch, "evidence.json"), evidence);
            // The shared frame styles include every class the converter used; no review-specific layout or CSS.
            site.Write(Path.Combine(scratch, "frames.css"), site.Styles.Css());
            Console.WriteLine("Help review frames: " + Path.GetFullPath(scratch));
            return failures.Count == 0 ? 0 : 1;
        }

        private static CellBuffer Frame(Site site, string example, PowerShellHost host, int width, string theme) {
            string id = Id(example, host, width, theme);
            string path = CapturePath(site, id);
            string? stale = Stamps.Check(site, path, Fingerprint(site, example, width, theme), "help-formatting");
            if (stale != null) { throw new InvalidOperationException(stale); }
            string vt = File.ReadAllText(path, Encoding.UTF8);
            CellBuffer frame;
            if (example == "help") {
                frame = FirstPage(vt, id, width);
            } else {
                VtScreen screen = new VtScreen(width, Height(example));
                screen.Feed(vt);
                frame = Parity.Trim(screen.Transcript());
            }
            string text = Parity.Describe(frame);
            if (!text.Contains("Get-ConsoleCapability", StringComparison.Ordinal) || text.Contains("HelpFormatting.ps1:", StringComparison.Ordinal)) {
                throw new InvalidOperationException(id + " did not capture clean installed-module help.");
            }
            return frame;
        }

        // ConPTY may update just the page number; replay the cells instead of matching repeated raw strings.
        private static CellBuffer FirstPage(string vt, string id, int width) {
            VtScreen replay = new VtScreen(width, Height("help"));
            List<string> pages = new List<string>();
            CellBuffer? first = null;
            foreach (char character in vt) {
                replay.Feed(character.ToString());
                if (first == null && character == 't') {
                    CellBuffer candidate = replay.Transcript();
                    if (Parity.Describe(candidate).Contains("q quit", StringComparison.Ordinal)) { first = Parity.Trim(candidate); }
                }
                if (!char.IsDigit(character)) { continue; }
                MatchCollection matches = Regex.Matches(Parity.Describe(replay.Transcript()), @"Page ([0-9]+) of ([0-9]+)");
                if (matches.Count == 0) { continue; }
                string page = matches[matches.Count - 1].Groups[1].Value;
                if (pages.Count == 0 || pages[pages.Count - 1] != page) { pages.Add(page); }
            }
            if (string.Join(",", pages) != "1,2,1") {
                throw new InvalidOperationException(id + " did not navigate page 1, page 2, then back to page 1: " + string.Join(",", pages));
            }
            return first ?? throw new InvalidOperationException(id + " did not draw its first-page status line.");
        }

        private static string Title(string example) => example switch {
            "native" => "Native Get-Help",
            "get-help" => "Get-Help with Help formatting",
            "command-help" => "Native -? with Help formatting",
            "help" => "help -Full · page position · Space next · b back · q quit",
            _ => "Show-ConsoleHelp"
        };

        private static string Document(Site site, string body, string theme) {
            string assets = Path.Combine(site.Website, "themes", "consolemonster", "assets");
            string fonts = new Uri(Path.Combine(site.Website, "static", "fonts") + Path.DirectorySeparatorChar).AbsoluteUri;
            string css = File.ReadAllText(Path.Combine(assets, "tokens.css")).Replace("url(/fonts/", "url(" + fonts)
                + File.ReadAllText(Path.Combine(assets, "base.css"));
            return "<!doctype html><html lang=\"en\" data-theme=\"" + theme + "\"><head><meta charset=\"utf-8\"><title>ConsoleMonster help review</title>"
                + "<style>" + css + "</style><link rel=\"stylesheet\" href=\"frames.css\"></head><body><main>" + body + "</main></body></html>\n";
        }
    }
}
