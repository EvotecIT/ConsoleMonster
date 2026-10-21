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
        private static string Examples(Site site) => Path.Combine(site.Repository, "ConsoleMonster.Examples", "bin", "Release", "net8.0", "ConsoleMonster.Examples.exe");

        private static string ProfilePath(Site site, string id) => Path.Combine(site.Captures, "docs", id + ".ansi");

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
            DemosPage.BuildExamples(site);
            foreach ((string id, Dictionary<string, string?> environment) in Profiles()) {
                List<OutputChunk> chunks = PseudoConsole.Run("\"" + Examples(site) + "\" profile", 100, 40, environment, site.Repository, TimeSpan.FromMinutes(1));
                Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath(site, id))!);
                File.WriteAllBytes(ProfilePath(site, id), PowerShellRunner.Bytes(chunks));
                Console.WriteLine("captured " + id);
            }
        }

        public static void Render(Site site, JsonObject powershell) {
            foreach ((string id, _) in Profiles()) {
                string path = ProfilePath(site, id);
                if (!File.Exists(path)) {
                    WidgetsPage.Report(new List<string> { "missing capture " + id + "; run the capture command." }, "docs");
                    continue;
                }
                CellBuffer frame = Site.Crop(Parity.Transcript(File.ReadAllBytes(path), 100, 40));
                DocCards.Offer(id, frame, Frames.Screen.Dark);
                Figures.Add(id, new JsonObject {
                    ["title"] = "ConsoleMonster.Examples profile",
                    ["label"] = "The terminal profile ConsoleMonster detected, with the decisions behind it",
                    ["meta"] = new JsonArray("100 columns"),
                    ["frame"] = site.Frame(frame, null)
                });
            }

            // One composition in the three built-in themes.
            foreach ((string id, ITheme theme, Frames.Screen screen) in new[] { ("theme-dark", (ITheme)Theme.Dark, Frames.Screen.Dark), ("theme-light", Theme.Light, Frames.Screen.Light), ("theme-contrast", Theme.HighContrast, Frames.Screen.Dark) }) {
                CellBuffer frame = Site.Render(Figures.Assessment(), 46, ColorDepth.TrueColor, GlyphTier.Full, theme);
                DocCards.Offer(id, frame, screen);
                JsonObject data = screen == Frames.Screen.Light ? LightOnly(site, frame) : site.Frame(frame, null);
                Figures.Add(id, new JsonObject { ["title"] = ((Theme)theme).Name, ["label"] = "The assessment summary in the " + ((Theme)theme).Name + " theme", ["meta"] = new JsonArray("Theme." + ((Theme)theme).Name), ["frame"] = data });
            }

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
                Figures.Add(id, new JsonObject {
                    ["title"] = w + "×" + h,
                    ["label"] = "The assessment dashboard in a " + w + " by " + h + " window",
                    ["meta"] = new JsonArray(w + "×" + h),
                    ["frame"] = site.Frame(midRun, null),
                    ["csharp"] = replayShort,
                    ["csharpFull"] = replayFull
                });
            }

            // PowerShell output used in the guides.
            Copy(site, powershell, "write-consoletext", "rich", "ps5", "write-consoletext");
            Copy(site, powershell, "markup", "rich", "ps7", "markup");
            Copy(site, powershell, "format-consoletable", "plain", "ps7", "plain", "CONSOLEMONSTER_MODE=plain");
            Copy(site, powershell, "format-consoletable", "nocolor", "ps7", "nocolor", "NO_COLOR=1");
            Copy(site, powershell, "format-consoletable", "narrow", "ps7", "narrow", "56 columns");
            Copy(site, powershell, "format-consoletable", "rich", "ps7", "format-consoletable");
            Copy(site, powershell, "format-table", "rich", "ps7", "format-table");
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
