using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// Runs the complete live-filter program shown on the site and checks its output against the frozen showcase and
    /// native PowerShell captures. Published frames use its first and final states, independent of intermediate scheduling.
    /// </summary>
    internal static class AppPage {
        private const string Id = "live-filter";
        // The tallest state (all six hosts) uses 16 rows; the key hints sit on the last row, one empty row below it.
        private const int Height = 18;
        private static readonly int[] Widths = { 120, 60 };
        private static readonly string[] States = { "initial", "filtered" };
        private static readonly string[] Themes = { "dark", "light" };

        /// <summary>Runs the compiled factory with real character events and captures the final committed query.</summary>
        /// <param name="width">Terminal width in cells.</param>
        /// <param name="theme">Theme shared by the input and dependent widgets.</param>
        /// <returns>The initial and final frames, plus any intermediate frames the live engine presented.</returns>
        internal static ScreenCapture CaptureFrames(int width, ITheme theme) {
            ScreenCapture capture = new ScreenCapture(width, Height);
            using (ScriptedInput input = new ScriptedInput("\"sql\" Escape") { Paced = true }) {
                AppState state = LiveFilterApp.Run(new InteractiveOptions {
                    Input = input,
                    Capture = capture,
                    Mode = InteractionMode.Rich,
                    Theme = theme,
                    Labels = new PromptLabels(),
                    Profile = TerminalProfile.Fixed(width, Height, ColorDepth.TrueColor, GlyphTier.Full).WithCulture(Site.Culture)
                });
                if (!string.Equals(state.Get<string>("Query", string.Empty), "sql", StringComparison.Ordinal)) {
                    throw new InvalidOperationException("The live-filter input script did not commit its final query.");
                }
            }
            if (capture.Frames.Count < 2 || capture.LastFrame == null) {
                throw new InvalidOperationException("The live-filter application did not capture initial and final frames.");
            }
            return capture;
        }

        /// <summary>Executes the exact published program with site-only terminal settings and scripted input.</summary>
        /// <param name="site">The checkout supplying the complete example.</param>
        /// <param name="width">Terminal width in cells.</param>
        /// <param name="theme">Theme shared by the input and dependent widgets.</param>
        /// <returns>The initial and final frames produced by the shown program.</returns>
        private static ScreenCapture CaptureProgram(Site site, int width, ITheme theme) {
            ScreenCapture capture = new ScreenCapture(width, Height);
            TerminalProfile profile = CSharpRunner.Profile(width, Height, ColorDepth.TrueColor, GlyphTier.Full);
            using (ScriptedInput input = new ScriptedInput("\"sql\" Escape") { Paced = true }) {
                CSharpRunner.Execute(ExamplePath(site), profile, theme, new InteractiveOptions {
                    Input = input,
                    Capture = capture,
                    Mode = InteractionMode.Rich,
                    Theme = theme,
                    Labels = new PromptLabels(),
                    Profile = profile
                });
            }
            if (capture.Frames.Count < 2 || capture.LastFrame == null) {
                throw new InvalidOperationException("The published live-filter program did not capture initial and final frames.");
            }
            return capture;
        }

        /// <summary>Captures the native PowerShell twin through the site's existing host runner.</summary>
        /// <param name="site">The current website checkout.</param>
        public static void Capture(Site site) {
            foreach (int width in Widths) {
                foreach (string theme in Themes) {
                    foreach (string state in States) {
                        foreach (PowerShellHost host in PowerShellHost.Available) {
                            Dictionary<string, string?> environment = PowerShellRunner.Environment(theme);
                            environment["CM_SITE_APP_STATE"] = state;
                            environment["CM_SITE_APP_WIDTH"] = width.ToString(Site.Culture);
                            environment["CM_SITE_APP_HEIGHT"] = Height.ToString(Site.Culture);
                            byte[] vt = PowerShellRunner.Bytes(PowerShellRunner.Run(site, host,
                                Path.Combine(site.Website, "tools", "SiteFrames", "Capture", "AppCapture.ps1"), width, Height + 4, environment));
                            string path = CapturePath(site, width, theme, state, host);
                            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                            File.WriteAllBytes(path, vt);
                            Stamps.Write(site, path, Fingerprint(site, width, theme, state));
                            CaptureRun.Record(path, width, Height + 4, theme);
                            Console.WriteLine("captured " + Path.GetRelativePath(site.Website, path));
                        }
                    }
                }
            }
        }

        /// <summary>Registers sourced docs figures and checks the published program against both existing twins.</summary>
        /// <param name="site">Paths and shared frame style registry.</param>
        /// <param name="hosts">Recorded versions of the two PowerShell hosts.</param>
        public static void Render(Site site, JsonObject hosts) {
            JsonObject frames = new JsonObject();
            JsonArray variants = new JsonArray();
            List<string> problems = new List<string>();
            (string csharp, string csharpFull) = SourceSnippets.Program(Site.ReadText(ExamplePath(site)));
            (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Twins.Script(site, "app", Id)));
            foreach (int width in Widths) {
                ScreenCapture dark = CaptureProgram(site, width, Theme.Dark);
                ScreenCapture light = CaptureProgram(site, width, Theme.Light);
                ScreenCapture showcaseDark = CaptureFrames(width, Theme.Dark);
                ScreenCapture showcaseLight = CaptureFrames(width, Theme.Light);
                foreach (string state in States) {
                    CellBuffer darkFrame = Pick(dark, state);
                    CellBuffer lightFrame = Pick(light, state);
                    string key = state + "-" + width.ToString(Site.Culture);
                    frames[key] = site.FrameAt(width, darkFrame, lightFrame);
                    string figureId = "app-" + Id + "-" + key;
                    Figures.Add(figureId, SourceStyles.Add(site, new JsonObject {
                        ["title"] = state == "initial" ? "Host inventory · initial" : "Host inventory · sql",
                        ["label"] = "Synthetic host inventory " + (state == "initial" ? "before typing" : "filtered by sql"),
                        ["meta"] = new JsonArray(width.ToString(Site.Culture) + " columns", "24-bit", "Full"),
                        ["frame"] = frames[key]!.DeepClone(),
                        ["file"] = "live-filter.ps1",
                        ["script"] = script,
                        ["scriptFull"] = scriptFull,
                        ["csharp"] = csharp,
                        ["csharpFull"] = csharpFull
                    }, ExamplePath(site)));
                    DocCards.Offer(figureId, darkFrame, Frames.Screen.Dark);
                    JsonObject parity = new JsonObject();
                    foreach (string theme in Themes) {
                        CellBuffer expected = theme == "dark" ? darkFrame : lightFrame;
                        CellBuffer showcase = Pick(theme == "dark" ? showcaseDark : showcaseLight, state);
                        ParityResult sourceParity = Parity.Compare(expected, showcase);
                        if (!sourceParity.Identical) {
                            problems.Add(key + " differs from the compiled showcase in " + theme + ": " + sourceParity.Detail);
                        }
                        foreach (PowerShellHost host in PowerShellHost.All) {
                            string path = CapturePath(site, width, theme, state, host);
                            string parityKey = theme + "-" + host.Id;
                            if (!File.Exists(path)) {
                                problems.Add(key + " has no " + parityKey + " capture; run SiteFrames capture Website app.");
                                continue;
                            }
                            string? stale = Stamps.Check(site, path, Fingerprint(site, width, theme, state), "app");
                            if (stale != null) { problems.Add(stale); }
                            CellBuffer actual = Parity.Transcript(File.ReadAllBytes(path), width, Height + 4);
                            ParityResult result = Parity.Compare(expected, actual);
                            parity[parityKey] = new JsonObject {
                                ["version"] = hosts[host.Id]?.GetValue<string>(),
                                ["identical"] = result.Identical
                            };
                            if (!result.Identical) {
                                problems.Add(key + " on " + parityKey + ": " + result.Detail);
                            }
                        }
                    }
                    variants.Add(new JsonObject {
                        ["id"] = key,
                        ["width"] = width,
                        ["query"] = state == "initial" ? string.Empty : "sql",
                        ["matches"] = state == "initial" ? LiveFilterApp.Hosts.Count : LiveFilterApp.Filter("sql").Count,
                        ["parity"] = parity
                    });
                }
            }
            site.WriteJson(site.Static("frames/app/" + Id + ".json"), frames);
            site.WriteJson(site.Data("app"), SourceStyles.Add(site, new JsonObject {
                ["id"] = Id,
                ["title"] = "Filter hosts as you type",
                ["summary"] = "A Query input updates the matching count and table after every edit. All hosts are synthetic.",
                ["bundle"] = "/frames/app/" + Id + ".json",
                ["keys"] = "\"sql\" Escape",
                ["csharp"] = csharp,
                ["csharpFull"] = csharpFull,
                ["powershell"] = script,
                ["powershellFull"] = scriptFull,
                ["variants"] = variants
            }, ExamplePath(site)));
            WidgetsPage.Report(problems, "app");
        }

        private static string Fingerprint(Site site, int width, string theme, string state) => Stamps.Of(
            Twins.Script(site, "app", Id), PowerShellRunner.Environment(theme), width.ToString(Site.Culture),
            Height.ToString(Site.Culture), state, Site.ReadText(Path.Combine(site.Tool, "Capture", "AppCapture.ps1")));

        /// <summary>
        /// Writes eight standalone exporter HTML, SVG and text frames to a caller-selected review folder. This path
        /// does not regenerate website assets or require saved PowerShell captures.
        /// </summary>
        /// <param name="site">The checkout supplying the published program.</param>
        /// <param name="outputDirectory">A scratch directory chosen by the parent performing visual review.</param>
        /// <returns>Zero after all initial/final frames have been written.</returns>
        public static int Review(Site site, string outputDirectory) {
            string output = Path.GetFullPath(outputDirectory);
            JsonArray artifacts = new JsonArray();
            foreach (int width in Widths) {
                foreach (string theme in Themes) {
                    ScreenCapture capture = CaptureProgram(site, width, theme == "dark" ? Theme.Dark : Theme.Light);
                    foreach (string state in States) {
                        CellBuffer frame = Pick(capture, state);
                        string name = Id + "-" + state + "-" + width.ToString(Site.Culture) + "-" + theme;
                        site.Write(Path.Combine(output, name + ".txt"), ConsoleExporter.ToText(frame));
                        ExportOptions options = new ExportOptions {
                            Theme = theme == "dark" ? ExportTheme.Dark : ExportTheme.Light,
                            Title = "Host inventory · " + state + " · " + width.ToString(Site.Culture) + " columns",
                            WindowChrome = true
                        };
                        site.Write(Path.Combine(output, name + ".html"), ConsoleExporter.ToHtml(frame, options));
                        site.Write(Path.Combine(output, name + ".svg"), ConsoleExporter.ToSvg(frame, options));
                        artifacts.Add(new JsonObject {
                            ["name"] = name,
                            ["width"] = width,
                            ["height"] = frame.Height,
                            ["theme"] = theme,
                            ["query"] = state == "initial" ? string.Empty : "sql",
                            ["text"] = name + ".txt",
                            ["html"] = name + ".html",
                            ["svg"] = name + ".svg"
                        });
                    }
                }
            }
            site.WriteJson(Path.Combine(output, "app-review.json"), new JsonObject { ["artifacts"] = artifacts });
            Console.WriteLine("Wrote app review frames to " + output);
            return 0;
        }

        private static string ExamplePath(Site site) => Path.Combine(site.Website, "examples", "app", Id + ".cs");

        private static CellBuffer Pick(ScreenCapture capture, string state) =>
            state == "initial" ? capture.Frames[0] : capture.LastFrame!;

        private static string CapturePath(Site site, int width, string theme, string state, PowerShellHost host) =>
            Path.Combine(site.Captures, "app", Id + "-" + state + "-" + width.ToString(Site.Culture) + "-" + theme + "." + host.Id + ".ansi");
    }
}
