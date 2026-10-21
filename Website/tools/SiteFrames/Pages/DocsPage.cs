using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
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
            LivePage.Build(site);
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

            // The too-small fallback: the assessment dashboard mid-run at three window sizes.
            foreach ((int w, int h) in new[] { (100, 30), (64, 20), (50, 12) }) {
                CellBuffer midRun = MidRun(w, h);
                DocCards.Offer("size-" + w + "x" + h, midRun, Frames.Screen.Dark);
                Figures.Add("size-" + w + "x" + h, new JsonObject {
                    ["title"] = w + "×" + h,
                    ["label"] = "The assessment dashboard in a " + w + " by " + h + " window",
                    ["meta"] = new JsonArray(w + "×" + h),
                    ["frame"] = site.Frame(midRun, null)
                });
            }

            // PowerShell output used in the guides.
            Copy(site, powershell, "write-consoletext", "rich", "ps5", "write-consoletext");
            Copy(site, powershell, "markup", "rich", "ps7", "markup");
            Copy(site, powershell, "format-consoletable", "plain", "ps7", "plain");
            Copy(site, powershell, "format-consoletable", "nocolor", "ps7", "nocolor");
            Copy(site, powershell, "format-consoletable", "narrow", "ps7", "narrow");
            Copy(site, powershell, "format-consoletable", "rich", "ps7", "format-consoletable");
            Copy(site, powershell, "format-table", "rich", "ps7", "format-table");
            Copy(site, powershell, "job", "rich", "ps5", "job-ps5");
            Copy(site, powershell, "job", "rich", "ps7", "job-ps7");
        }

        private static JsonObject LightOnly(Site site, CellBuffer frame) {
            string[] rows = Frames.FrameHtml.Rows(frame, ColorDepth.TrueColor, Frames.Screen.Light, site.Styles);
            site.Note(frame);
            return new JsonObject { ["w"] = frame.Width, ["h"] = Site.Lines(rows), ["lightOnly"] = Frames.FrameHtml.Body(rows) };
        }

        private static void Copy(Site site, JsonObject powershell, string example, string variant, string host, string id) {
            JsonNode? node = powershell["examples"]?[example]?[variant]?[host];
            if (node == null) {
                return;
            }
            Figures.Add(id, new JsonObject {
                ["title"] = example + ".ps1",
                ["file"] = example + ".ps1",
                ["label"] = example + " in " + node["host"]!.GetValue<string>(),
                ["meta"] = new JsonArray(node["host"]!.GetValue<string>()),
                ["frame"] = node["frame"]!.DeepClone(),
                ["script"] = powershell["examples"]![example]!["script"]!.DeepClone(),
                ["scriptFull"] = powershell["examples"]![example]!["scriptFull"]?.DeepClone()
            });
        }

        // The assessment after half of its recorded events, composed at a given size.
        private static CellBuffer MidRun(int width, int height) {
            Dashboard dashboard = ParallelRun.Build(out _);
            Dictionary<string, ILiveTarget> targets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
            Collect(dashboard, targets);
            List<LiveEvent> events = ParallelRunRecording.Generate();
            long half = events[events.Count - 1].Time / 2;
            foreach (LiveEvent e in events) {
                if (e.Time > half) {
                    break;
                }
                if (targets.TryGetValue(e.Target, out ILiveTarget? target)) {
                    target.Apply(e);
                }
            }
            RenderContext context = ConsoleMonster.Terminal.CreateContext(TerminalProfile.Fixed(width, height, ColorDepth.TrueColor, GlyphTier.Full).WithCulture(Site.Culture), Theme.Dark);
            return FrameComposer.Compose(dashboard, width, height, context.WithClock(0, TimeSpan.FromMilliseconds(half)));
        }

        private static void Collect(IWidget widget, Dictionary<string, ILiveTarget> targets) {
            if (widget is ILiveTarget target && !targets.ContainsKey(target.Name)) {
                targets.Add(target.Name, target);
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    Collect(child, targets);
                }
            }
        }
    }
}
