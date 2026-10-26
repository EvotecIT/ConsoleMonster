using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster.Examples.Showcases;
using SiteFrames.Capture;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>Figures for the docs and guides: detection decisions, themes, the too-small fallback and PowerShell output.</summary>
    internal static class DocsPage {
        private static string ProfileScript(Site site) => Path.Combine(site.Website, "examples", "docs", "profile.ps1");


        private const int ThreeWidth = 88;

        private static string ProfilePath(Site site, string id) => CapturePlatforms.PathFor(site, Path.Combine(site.Captures, "docs", id + ".ansi"));

        private static IEnumerable<(string Id, Dictionary<string, string?> Environment)> Profiles() {
            yield return ("profile", PowerShellRunner.Environment("dark"));
            Dictionary<string, string?> detected = PowerShellRunner.Environment("auto");
            detected["CONSOLEMONSTER_COLOR"] = null;
            detected["CONSOLEMONSTER_GLYPHS"] = null;
            detected["CONSOLEMONSTER_THEME"] = null;
            yield return ("profile-detected", detected);
            Dictionary<string, string?> ci = PowerShellRunner.Environment("dark");
            ci["CONSOLEMONSTER_COLOR"] = null;
            ci["CONSOLEMONSTER_GLYPHS"] = null;
            ci["CI"] = "true";
            ci["NO_COLOR"] = "1";
            yield return ("profile-ci", ci);
        }

        public static void Capture(Site site) {
            foreach ((string id, Dictionary<string, string?> environment) in Profiles()) {
                List<OutputChunk> chunks = PowerShellRunner.Run(site, PowerShellHost.Core, ProfileScript(site), 100, 40, environment);
                Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath(site, id))!);
                File.WriteAllBytes(ProfilePath(site, id), PowerShellRunner.Bytes(chunks));
                Stamps.Write(site, ProfilePath(site, id), Stamps.Of(ProfileScript(site), environment, "100x40"));
                CaptureRun.Record(ProfilePath(site, id), 100, 40);
                Console.WriteLine("captured " + id);
            }
        }

        public static void Render(Site site, JsonObject powershell) {
            foreach ((string id, Dictionary<string, string?> environment) in Profiles()) {
                string logical = Path.Combine(site.Captures, "docs", id + ".ansi");
                bool found = false;
                foreach ((string platform, string path) in CapturePlatforms.Variants(logical)) {
                    found = true;
                    string? stale = Stamps.Check(site, path, Stamps.Of(ProfileScript(site), environment, "100x40"), "docs on " + platform);
                    if (stale != null) { WidgetsPage.Report(new List<string> { stale }, "docs"); }
                    CellBuffer frame = Site.Crop(Parity.Transcript(File.ReadAllBytes(path), 100, 40));
                    (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(ProfileScript(site)));
                    var figure = new JsonObject {
                        ["title"] = id == "profile-ci" ? "CI=true, NO_COLOR=1" : id == "profile" ? "pinned with CONSOLEMONSTER_*" : "detected terminal profile",
                        ["file"] = "profile.ps1", ["platform"] = platform,
                        ["label"] = "The terminal profile detected on " + CapturePlatforms.Label(platform) + ", with the decisions behind it",
                        ["meta"] = new JsonArray("PowerShell 7", "100 columns", CapturePlatforms.Label(platform)),
                        ["frame"] = site.Frame(frame, null), ["script"] = script, ["scriptFull"] = scriptFull
                    };
                    Figures.Add(id + "-" + platform, figure);
                    if (path == ProfilePath(site, id)) {
                        DocCards.Offer(id, frame, Frames.Screen.Dark);
                        Figures.Add(id, figure.DeepClone().AsObject());
                    }
                }
                if (!found) { WidgetsPage.Report(new List<string> { "missing capture " + id + "; run the capture command." }, "docs"); }
            }

            // One composition in the three built-in themes.
            foreach ((string id, ITheme theme, Frames.Screen screen) in new[] { ("theme-dark", (ITheme)Theme.Dark, Frames.Screen.Dark), ("theme-light", Theme.Light, Frames.Screen.Light), ("theme-contrast", Theme.HighContrast, Frames.Screen.Dark) }) {
                CellBuffer frame = Figures.Summary(site, 46, ColorDepth.TrueColor, GlyphTier.Full, theme);
                DocCards.Offer(id, frame, screen);
                JsonObject data = screen == Frames.Screen.Light ? LightOnly(site, frame) : site.Frame(frame, null);
                Figures.Add(id, Figures.WithProgram(site, new JsonObject { ["title"] = ((Theme)theme).Name, ["label"] = "The assessment summary in the " + ((Theme)theme).Name + " theme", ["meta"] = new JsonArray("Theme." + ((Theme)theme).Name), ["frame"] = data }, Figures.SummaryProgram(site)));
            }

            // One maintained initializer program, its PowerShell counterpart and its compiled fluent translation.
            string stylesProgram = Path.Combine(site.Website, "examples", "docs", "three", "objects.cs");
            CellBuffer stylesDark = Site.Crop(CSharpRunner.Run(stylesProgram, ThreeWidth, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark));
            CellBuffer stylesLight = Site.Crop(CSharpRunner.Run(stylesProgram, ThreeWidth, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light));
            DocCards.Offer("three-fluent", stylesDark, Frames.Screen.Dark);
            Figures.Add("three-fluent", Figures.WithProgram(site, new JsonObject {
                ["title"] = "Servers",
                ["label"] = "A servers table, a summary and a CPU sparkline in two panels",
                ["meta"] = new JsonArray(ThreeWidth + " columns", "24-bit", "Full"),
                ["frame"] = site.Frame(stylesDark, stylesLight)
            }, stylesProgram));

            // For the environment overrides card: what pinning NO_COLOR and the ASCII glyph tier gives, a table in plain text.
            DocCards.Offer("overrides-plain", TablesPage.Run(site, "dns", 80, Theme.Dark, ColorDepth.None, GlyphTier.Ascii), Frames.Screen.Dark);

            // The too-small fallback: the assessment program replaying its recording in three window sizes, halfway through.
            (string replayShort, string replayFull) = SourceSnippets.Program(Site.ReadText(LivePage.ProgramPath(site, "replay")));
            foreach ((int w, int h) in new[] { (100, 30), (64, 20), (50, 12) }) {
                string id = "size-" + w + "x" + h;
                CellBuffer? midRun = LivePage.FrameAt(site, id, 0.5);
                if (midRun == null) {
                    WidgetsPage.Report(new List<string> { "missing capture " + id + "; run the capture command for the live area." }, "docs");
                    continue;
                }
                DocCards.Offer(id, midRun, Frames.Screen.Dark);
                Figures.Add(id, SourceStyles.Add(site, new JsonObject {
                    ["title"] = w + "×" + h,
                    ["label"] = "The assessment dashboard in a " + w + " by " + h + " window",
                    ["meta"] = new JsonArray(w + "×" + h),
                    ["frame"] = site.Frame(midRun, null),
                    ["csharp"] = replayShort,
                    ["csharpFull"] = replayFull
                }, LivePage.ProgramPath(site, "replay")));
            }

            // PowerShell output used in the guides.
            Copy(site, powershell, "write-consoletext", "rich", "ps5", "write-consoletext");
            Copy(site, powershell, "markup", "rich", "ps7", "markup");
            Copy(site, powershell, "format-consoletable", "plain", "ps7", "plain", "CONSOLEMONSTER_MODE=plain");
            Copy(site, powershell, "format-consoletable", "nocolor", "ps7", "nocolor", "NO_COLOR=1");
            Copy(site, powershell, "format-consoletable", "narrow", "ps7", "narrow", "56 columns");
            Copy(site, powershell, "format-consoletable", "rich", "ps7", "format-consoletable");
            Copy(site, powershell, "format-table", "rich", "ps7", "format-table");
            Copy(site, powershell, "export-markdown", "rich", "ps7", "export-markdown", "Export-ConsoleFrame -Format Markdown");
            Copy(site, powershell, "files", "rich", "ps7", "files", "Get-ChildItem | Show-ConsoleTable");
            Copy(site, powershell, "relative-dates", "rich", "ps7", "relative-dates", "CONSOLEMONSTER_NOW=2026-10-21T12:00:00Z");
            // Stream colours: the dark capture on the dark page and the light-theme capture on the light page, per host.
            foreach (PowerShellHost host in PowerShellHost.All) {
                CellBuffer? dark = PowerShellPage.CaptureFrame(site, "streams", "rich", host);
                CellBuffer? light = PowerShellPage.CaptureFrame(site, "streams", "light", host);
                JsonNode? example = powershell["examples"]?["streams"];
                string? version = example?["rich"]?[host.Id]?["host"]?.GetValue<string>();
                if (dark == null || light == null || example == null || version == null) {
                    WidgetsPage.Report(new List<string> { "missing capture streams on " + host.Label + "; run the capture command for the powershell area." }, "docs");
                    continue;
                }
                Figures.Add("streams-" + host.Id, new JsonObject {
                    ["title"] = version,
                    ["file"] = "streams.ps1",
                    ["label"] = "Warning, verbose and debug messages before and after Enable-ConsoleFormatting -Feature Streams in " + version,
                    ["meta"] = new JsonArray(),
                    ["frame"] = site.Frame(dark, light),
                    ["script"] = example["script"]!.DeepClone(),
                    ["scriptFull"] = example["scriptFull"]?.DeepClone()
                });
            }
            Copy(site, powershell, "job", "rich", "ps5", "job-ps5", "PowerShell 5.1");
            Copy(site, powershell, "job", "rich", "ps7", "job-ps7", "PowerShell 7");
        }

        private static JsonObject LightOnly(Site site, CellBuffer frame) {
            string[] rows = Frames.FrameHtml.Rows(frame, ColorDepth.TrueColor, Frames.Screen.Light, site.Styles);
            site.Note(frame);
            return new JsonObject { ["w"] = frame.Width, ["h"] = Site.Lines(rows), ["lightOnly"] = Frames.FrameHtml.Body(rows) };
        }

        private static void Copy(Site site, JsonObject powershell, string example, string variant, string host, string id, string? title = null) {
            JsonNode? node = powershell["examples"]?[example]?[variant]?[host];
            if (node == null) {
                return;
            }
            // The capture itself, so a page can use the figure as its social card artwork (front matter card: <id>).
            PowerShellHost runHost = host == PowerShellHost.Desktop.Id ? PowerShellHost.Desktop : PowerShellHost.Core;
            CellBuffer? captured = PowerShellPage.CaptureFrame(site, example, variant, runHost, PowerShellPage.RunWidth(variant));
            if (captured != null) {
                DocCards.Offer(id, captured, Frames.Screen.Dark);
            }
            Figures.Add(id, new JsonObject {
                ["title"] = title ?? example + ".ps1",
                ["file"] = example + ".ps1",
                ["label"] = example + " in " + node["host"]!.GetValue<string>(),
                // A title that already names the host (Windows PowerShell 5.1) needs no host chip beside it.
                ["meta"] = title != null && title.Contains("PowerShell", StringComparison.Ordinal) ? new JsonArray() : new JsonArray(node["host"]!.GetValue<string>()),
                ["frame"] = node["frame"]!.DeepClone(),
                ["script"] = powershell["examples"]![example]!["script"]!.DeepClone(),
                ["scriptFull"] = powershell["examples"]![example]!["scriptFull"]?.DeepClone()
            });
        }
    }
}
