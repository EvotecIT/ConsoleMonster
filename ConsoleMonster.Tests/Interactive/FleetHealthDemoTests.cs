using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// The fleet health check showcase driven by its own demo script, headless: a fixed 140 x 40 profile, a screen capture
    /// instead of the console, and no waiting. The flow must reach every step and end on the results with next actions.
    /// In the live-session collection because the run starts a live session.
    /// </summary>
    [Collection("Live sessions")]
    public sealed class FleetHealthDemoTests {
        private static ScreenCapture RunDemo() {
            ScreenCapture capture = new ScreenCapture(140, 40);
            ScriptedInput input = new ScriptedInput(FleetHealth.DemoKeys) { Paced = true };
            InteractiveOptions options = new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = input,
                Capture = capture,
                Theme = Theme.Dark,
                Labels = new PromptLabels(),
                Profile = TerminalProfile.Fixed(140, 40, ColorDepth.TrueColor, GlyphTier.Full)
            };
            string storage = Path.Combine(Path.GetTempPath(), "cm-fleet-none-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            try {
                FleetHealth.Run(FleetHealthOptions.Instant, options, storage);
            } catch (InputClosedException) {
                CellBuffer? last = capture.LastFrame;
                throw new Xunit.Sdk.XunitException("The demo script ended at this frame:\n" + (last == null ? "(none)" : last.ToPlainText()));
            }
            // The app quit on the script's last key, not earlier with keys left over.
            Assert.Equal(0, input.Remaining);
            return capture;
        }

        private static string? Last(ScreenCapture capture, params string[] phrases) {
            return capture.Frames.Select(f => f.ToPlainText()).LastOrDefault(text => phrases.All(p => text.IndexOf(p, StringComparison.Ordinal) >= 0));
        }

        [Fact]
        public void Demo_ScriptedHeadless_EndsOnTheResultsWithNextActions() {
            ScreenCapture capture = RunDemo();

            // Every step of the story was on screen.
            Assert.NotNull(Last(capture, "Nightly · all sites", "built in", "every online server", "every day 02:00"));
            Assert.NotNull(Last(capture, "Nightly · all sites", "Preset 'Nightly · all sites' applied"));
            Assert.NotNull(Last(capture, "Servers", "DC01", "FS22"));
            Assert.NotNull(Last(capture, "Cert chain", "trusted root"));
            string? review = Last(capture, "Invoke-FleetCheck");
            Assert.NotNull(review);
            Assert.Contains("-Domain corp.contoso.com", review);
            Assert.Contains("CertChain", review);
            // The preset's other values reached the form: eight workers and the emailed report.
            Assert.Contains("-ThrottleLimit 8", review);
            Assert.Contains("-Notify", review);
            Assert.NotNull(Last(capture, "Check map", "Categories", "Running", "Failures", "Slowest checks", "Durations", "Log"));
            Assert.NotNull(Last(capture, "DC01  ✓", "WEB21 ✓", "110 passed"));

            // The run ends on the results summary with its next actions.
            string? results = Last(capture, "Fleet check finished", "Next steps");
            Assert.True(results != null, "No results frame; the last frame was:\n" + capture.LastFrame?.ToPlainText());
            Assert.Contains("121 checks on 11 servers in corp.contoso.com. 4 need action now; 7 warnings to review.", results);
            Assert.Contains("110 passed", results);
            Assert.Contains("Open the report", results);
            Assert.Contains("Rerun the failed checks", results);
            Assert.Contains("Export findings to CSV", results);
            Assert.Contains("6% on D:", results);
            Assert.NotNull(Last(capture, "8 workers", "Report emailed to ops@contoso.com"));
            Assert.NotNull(Last(capture, "Report saved: " + Path.Combine("Reports", "Fleet", "fleet-20261020-0930.html")));
            Assert.DoesNotContain(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), string.Join("\n", capture.Frames.Select(frame => frame.ToPlainText())));

            // Then back in the menu: the history lists the new run, and the app quit at the end of the script.
            Assert.NotNull(Last(capture, "History", "110 passed · 7 warn · 4 failed"));
        }

        // The frames outside the live run, each once and without the footer line (a toast there follows real time).
        private static string[] StillFrames(ScreenCapture capture) {
            List<string> frames = new List<string>();
            foreach (CellBuffer frame in capture.Frames) {
                string text = frame.ToPlainText();
                string header = text.Split('\n')[0];
                if (header.Contains("New check › Running")) {
                    continue;
                }
                string body = text.Substring(0, Math.Max(0, text.TrimEnd('\n').LastIndexOf('\n')));
                if (frames.Count == 0 || frames[frames.Count - 1] != body) {
                    frames.Add(body);
                }
            }
            return frames.ToArray();
        }

        [Fact]
        public void Demo_TwoRuns_ShowTheSameFrames() {
            ScreenCapture first = RunDemo();
            ScreenCapture second = RunDemo();
            Assert.NotNull(Last(first, "Fleet check finished", "Next steps"));
            Assert.Equal(Last(first, "Fleet check finished", "Next steps"), Last(second, "Fleet check finished", "Next steps"));
            Assert.Equal(StillFrames(first), StillFrames(second));
        }
    }
}
