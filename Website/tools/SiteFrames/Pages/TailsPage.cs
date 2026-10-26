using VtScreen = ConsoleMonster.Sessions.VtScreen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>Real file-tail examples, native PowerShell captures and navigation evidence for the live-tails guide.</summary>
    internal static partial class TailsPage {
        private const int Rows = 12;
        private const string Keys = "wait:300 PgUp wait:300 End wait:300 q";
        private static readonly int[] Widths = { 120, 60 };

        private static string Example(Site site, string name) => Path.Combine(site.Website, "examples", "tails", name);
        private static string Driver(Site site) => Path.Combine(site.Tool, "Assets", "tails-capture.ps1");
        private static string CapturePath(Site site, PowerShellHost host, int width, string theme) =>
            Path.Combine(site.Captures, "tails", host.Id + "-" + width + "-" + theme + ".ansi");

        private static string Fingerprint(Site site, int width, string theme) => Stamps.Of(
            Example(site, "file.ps1"), PowerShellRunner.Environment(theme), width + "x" + Rows,
            Site.ReadText(Example(site, "synthetic.log")), Site.ReadText(Driver(site)));

        /// <summary>Captures the exact native script on each PowerShell host; every run ends after one source poll.</summary>
        public static void Capture(Site site) {
            foreach (PowerShellHost host in PowerShellHost.Available) {
                foreach (int width in Widths) {
                    foreach (string theme in new[] { "dark", "light" }) {
                        string path = CapturePath(site, host, width, theme);
                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        string arguments = "-Example '" + Example(site, "file.ps1").Replace("'", "''")
                            + "' -Columns " + width + " -Appearance " + theme + " -Destination '" + path.Replace("'", "''") + "'";
                        PowerShellRunner.Run(site, host, Driver(site), width, Rows, PowerShellRunner.Environment(theme),
                            timeout: TimeSpan.FromSeconds(30), arguments: arguments);
                        // The wrapper saved the target's VT, excluding ConPTY's host startup/exit text.
                        FindFrames(File.ReadAllText(path), width);
                        CaptureRun.Observe(File.ReadAllBytes(path), width, Rows);
                        Stamps.Write(site, path, Fingerprint(site, width, theme));
                        CaptureRun.Record(path, width, Rows);
                        Console.WriteLine("captured tails " + host.Id + " " + width + " " + theme);
                    }
                }
            }
        }

        /// <summary>Renders both themes from exact C# programs and checks the finite script's parity on both PS hosts.</summary>
        public static void Render(Site site) {
            foreach (int width in Widths) {
                Dictionary<string, CellBuffer> dark = Run(site, width, Theme.Dark);
                Dictionary<string, CellBuffer> light = Run(site, width, Theme.Light);
                foreach (PowerShellHost host in PowerShellHost.All) {
                    foreach ((string theme, CellBuffer expected) in new[] { ("dark", dark["following"]), ("light", light["following"]) }) {
                        string path = CapturePath(site, host, width, theme);
                        if (!File.Exists(path)) {
                            WidgetsPage.Report(new List<string> { "missing " + path + "; run capture Website tails" }, "tails");
                            continue;
                        }
                        string? stale = Stamps.Check(site, path, Fingerprint(site, width, theme), "tails");
                        if (stale != null) { WidgetsPage.Report(new List<string> { stale }, "tails"); }
                        CellBuffer actual = FindFrames(File.ReadAllText(path), width)["following"];
                        ParityResult parity = Parity.Compare(expected, actual);
                        if (!parity.Identical) { WidgetsPage.Report(new List<string> { host.Id + " " + width + " " + theme + ": " + parity.Detail }, "tails"); }
                    }
                }
                foreach (string state in new[] { "following", "paused", "resumed" }) {
                    string id = "tails-" + state + "-" + width;
                    string program = Example(site, state == "following" ? "file.cs" : "navigate.cs");
                    JsonObject figure = Figures.WithProgram(site, new JsonObject {
                        ["title"] = state == "following" ? "Following" : state == "paused" ? "PgUp · paused" : "End · following",
                        ["label"] = "Synthetic file tail, " + state + ", " + width + " columns",
                        ["meta"] = new JsonArray(width + " columns", "24-bit", "Full"),
                        ["frame"] = site.FrameAt(width, dark[state], light[state])
                    }, program);
                    if (state == "following") {
                        (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(Example(site, "file.ps1")));
                        figure["file"] = "file.ps1";
                        figure["script"] = shortForm;
                        figure["scriptFull"] = full;
                    }
                    Figures.Add(id, figure);
                    if (width == 120 && state == "following") { DocCards.Offer(id, dark[state], Screen.Dark); }
                }
            }
            // Readers can download each shown source and the same synthetic data without a repository checkout.
            foreach (string name in new[] { "file.ps1", "file.cs", "navigate.cs", "synthetic.log" }) {
                site.Write(site.Static("examples/tails/" + name), Site.ReadText(Example(site, name)) + "\n");
            }
        }

        private static Dictionary<string, CellBuffer> Run(Site site, int width, ITheme theme) {
            string previousDirectory = Environment.CurrentDirectory;
            IInputSource? previousInput = ConsoleInput.Override;
            try {
                Environment.CurrentDirectory = Path.GetDirectoryName(Example(site, "file.cs"))!;
                TerminalProfile profile = CSharpRunner.Profile(width, Rows, ColorDepth.TrueColor, GlyphTier.Full);
                ConsoleInput.Override = new ScriptedInput(string.Empty) { EndsQuietly = true };
                RecordingTarget snapshot = CSharpRunner.Execute(Example(site, "file.cs"), profile, theme, null);
                CellBuffer following = FindFrames(snapshot.Output, width)["following"];
                ConsoleInput.Override = new ScriptedInput(Keys) { RealTime = true };
                RecordingTarget navigation = CSharpRunner.Execute(Example(site, "navigate.cs"), profile, theme, null);
                Dictionary<string, CellBuffer> frames = FindFrames(navigation.Output, width);
                if (!frames.ContainsKey("paused") || !frames.ContainsKey("resumed")) {
                    throw new InvalidOperationException("tails navigation did not draw PgUp and End states.");
                }
                if (Parity.Describe(following) == Parity.Describe(frames["paused"])) {
                    throw new InvalidOperationException("PgUp did not change the tail viewport.");
                }
                ParityResult resumed = Parity.Compare(following, frames["resumed"]);
                if (!resumed.Identical) { throw new InvalidOperationException("End did not restore following: " + resumed.Detail); }
                frames["following"] = following;
                return frames;
            } finally {
                ConsoleInput.Override = previousInput;
                Environment.CurrentDirectory = previousDirectory;
            }
        }

        // Snapshot only complete synchronized updates on the alternate screen. Stop restores the empty primary screen,
        // which is intentionally not evidence of the live view that was visible before restoration.
        private static Dictionary<string, CellBuffer> FindFrames(string vt, int width) {
            const string boundary = "\u001b[?2026l";
            VtScreen screen = new VtScreen(width, Rows);
            Dictionary<string, CellBuffer> result = new Dictionary<string, CellBuffer>(StringComparer.Ordinal);
            bool paused = false;
            foreach (string part in vt.Split(boundary, StringSplitOptions.None)) {
                screen.Feed(part + boundary);
                if (!screen.OnAlternate || screen.SynchronizedOutput) { continue; }
                CellBuffer frame = screen.Snapshot();
                string text = Parity.Describe(frame);
                if (!text.Contains("database", StringComparison.Ordinal)) { continue; }
                if (text.Contains("Paused", StringComparison.Ordinal)) { result["paused"] = frame; paused = true; }
                else if (text.Contains("Following", StringComparison.Ordinal)) { result[paused ? "resumed" : "following"] = frame; }
            }
            if (!result.ContainsKey("following")) { throw new InvalidOperationException("No complete following tail frame was captured."); }
            return result;
        }

    }
}
