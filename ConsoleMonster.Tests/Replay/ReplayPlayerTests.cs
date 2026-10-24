using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Replay;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Replay {
    public sealed class ReplayPlayerTests {
        private static IWidget Dashboard() {
            StatusList steps = new StatusList("steps");
            steps.Set("compile", "Compile", StatusKind.Pending);
            steps.Set("test", "Test", StatusKind.Pending);
            Rows rows = new Rows();
            rows.Add(steps, LayoutSize.Fixed(2));
            rows.Add(new LogRegion("log"), LayoutSize.Fraction());
            return rows;
        }

        private static List<LiveEvent> Events() {
            return new List<LiveEvent> {
                new LiveEvent("steps", "set", key: "compile", status: "running", time: 1),
                new LiveEvent("$log", "write", text: "restoring packages", status: "info", time: 200),
                new LiveEvent(LiveSession.MarkerTarget, "mark", text: "Build started", time: 300),
                new LiveEvent("steps", "set", key: "compile", status: "pass", time: 800),
                new LiveEvent("$log", "write", text: "disk almost full", status: "warn", time: 900),
                new LiveEvent("steps", "set", key: "test", status: "fail", time: 1600),
                new LiveEvent("steps", "set", key: "test", status: "fail", text: "still failing", time: 1700),
                new LiveEvent("$log", "write", text: "3 tests failed", status: "error", time: 1800)
            };
        }

        private static string Play(string keys, TerminalProfile? profile = null, int width = 60, int height = 14) {
            ScreenCapture capture = new ScreenCapture(width, height);
            InteractiveOptions interactive = new InteractiveOptions { Mode = InteractionMode.Rich, Input = new ScriptedInput(keys) { Paced = true }, Capture = capture, Theme = Theme.Dark, Profile = profile };
            ReplayPlayer.Play(Dashboard(), Events(), new ReplayOptions { Title = "nightly build", AutoPlay = false, Interactive = interactive });
            return ConsoleExporter.ToText(capture.LastFrame!);
        }

        [Fact]
        public void Markers_FromEvents_FindMarkersStateChangesAndLogLines() {
            IReadOnlyList<ReplayMarker> markers = ReplayMarker.From(Events());
            Assert.Equal(new[] { ReplayMarkerKind.Log, ReplayMarkerKind.Marker, ReplayMarkerKind.Warning, ReplayMarkerKind.Failure, ReplayMarkerKind.Failure }, markers.Select(m => m.Kind).ToArray());
            Assert.Equal("Build started", markers[1].Text);
            Assert.True(markers[2].IsLog);
            Assert.Equal("steps test: fail", markers[3].Text);
            Assert.Equal(TimeSpan.FromMilliseconds(1800), markers[4].Time);
        }

        [Fact]
        public void Markers_FromEvents_SkipRepeatedStatesAndSnapshots() {
            IReadOnlyList<ReplayMarker> markers = ReplayMarker.From(new[] {
                new LiveEvent("main", "set", text: "[red]x[/]", status: "snapshot", time: 5, isMarkup: true),
                new LiveEvent("lanes", "state", key: "web", status: "fail", time: 10),
                new LiveEvent("lanes", "state", key: "web", status: "fail", time: 20),
                new LiveEvent("lanes", "state", key: "web", status: "running", time: 30),
                new LiveEvent("lanes", "state", key: "web", status: "fail", time: 40)
            });
            Assert.Equal(new[] { 10.0, 40.0 }, markers.Select(m => m.Time.TotalMilliseconds).ToArray());
        }

        [Fact]
        public void Player_JumpToFailure_ShowsThatFramePausedWithTheMarker() {
            string screen = Play("f q");
            Assert.Contains("nightly build", screen);
            Assert.Contains("paused", screen);
            Assert.Contains("0:01.6 / 0:01.8", screen);
            Assert.Contains("steps test: fail", screen);
        }

        [Fact]
        public void Player_StepsFramesAndChangesSpeed() {
            string screen = Play("Right Right + + q");
            Assert.Contains("4x", screen);
            Assert.DoesNotContain("0:00.0 /", screen);
        }

        [Fact]
        public void Player_SearchFindsLogEntry() {
            string screen = Play("/ \"full\" Enter Escape q");
            Assert.Contains("0:00.9 /", screen);
            Assert.Contains("disk almost full", screen);
        }

        [Fact]
        public void Player_SearchWithoutMatch_StaysAndSaysSo() {
            string screen = Play("/ \"nothing\" Enter Escape q");
            Assert.Contains("No log entry contains nothing", screen);
        }

        [Fact]
        public void Player_JumpsBackToPreviousWarningAndNamedMarker() {
            Assert.Contains("disk almost full", Play("End W q"));
            Assert.Contains("Build started", Play("n q"));
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Player_MidScrub_MatchesGolden(GlyphTier glyphs) {
            string screen = Play("w q", TerminalProfile.Fixed(60, 14, ColorDepth.None, glyphs));
            Golden.Match("interactive/replay.midscrub." + glyphs.ToString().ToLowerInvariant() + ".txt", screen);
        }

        [Fact]
        public void Player_RecordedDashboard_ShowsOneHeaderWithItsTitleAndStatusAndNoRecordedFooter() {
            Dashboard dashboard = new Dashboard("Nightly patch run", Dashboard());
            List<LiveEvent> events = Events();
            events.Add(new LiveEvent("dashboard", "status", text: "2 failed", status: "fail", time: 1700));
            ScreenCapture capture = new ScreenCapture(100, 14);
            InteractiveOptions interactive = new InteractiveOptions { Mode = InteractionMode.Rich, Input = new ScriptedInput("End q") { Paced = true }, Capture = capture, Theme = Theme.Dark, Profile = TerminalProfile.Fixed(100, 14, ColorDepth.None, GlyphTier.Full) };
            ReplayPlayer.Play(dashboard, events, new ReplayOptions { Title = "Nightly patch run - 17 Oct", AutoPlay = false, Interactive = interactive });
            string[] lines = ConsoleExporter.ToText(capture.LastFrame!).Split('\n');

            Assert.Contains("Nightly patch run - 17 Oct", lines[0]);
            Assert.Contains("2 failed", lines[0]);
            Assert.Contains("paused", lines[0]);
            Assert.Single(lines, l => l.Contains("Nightly patch run"));
            Assert.DoesNotContain(lines, l => l.Contains("ctrl+c"));
            Assert.Contains(lines, l => l.Contains("Compile"));
            Assert.Contains("0:01.8 / 0:01.8", lines[0]);
        }

        [Fact]
        public void Player_RecordedDashboardWithoutTitleOption_UsesTheRecordedTitle() {
            Dashboard dashboard = new Dashboard("Nightly patch run", Dashboard()) { Subtitle = "ring 1" };
            ScreenCapture capture = new ScreenCapture(70, 14);
            InteractiveOptions interactive = new InteractiveOptions { Mode = InteractionMode.Rich, Input = new ScriptedInput("q") { Paced = true }, Capture = capture, Theme = Theme.Dark, Profile = TerminalProfile.Fixed(70, 14, ColorDepth.None, GlyphTier.Full) };
            ReplayPlayer.Play(dashboard, Events(), new ReplayOptions { AutoPlay = false, Interactive = interactive });
            string[] lines = ConsoleExporter.ToText(capture.LastFrame!).Split('\n');

            Assert.Contains("Nightly patch run", lines[0]);
            Assert.Contains("ring 1", lines[0]);
            Assert.DoesNotContain("Replay", lines[0]);
            Assert.Single(lines, l => l.Contains("Nightly patch run"));
        }

        [Fact]
        public void Player_WithoutInteractiveConsole_PrintsTheMarkers() {
            LineCapture lines = new LineCapture();
            ReplayPlayer.Play(Dashboard(), Events(), new ReplayOptions { Title = "nightly", Interactive = new InteractiveOptions { Mode = InteractionMode.None, LineConsole = lines } });
            string text = string.Join("\n", lines.Lines);
            Assert.StartsWith("nightly (0:01.8, 8 events)", text);
            Assert.Contains("0:01.6  failure  steps test: fail", text);
            Assert.Contains("0:00.3  marker  Build started", text);
        }

        [Fact]
        public void FormatTime_UsesMinutesAndTenthsOrHours() {
            Assert.Equal("1:05.3", ReplayPlayer.FormatTime(TimeSpan.FromMilliseconds(65_300)));
            Assert.Equal("1:02:03", ReplayPlayer.FormatTime(new TimeSpan(1, 2, 3)));
        }

        private sealed class LineCapture : ILineConsole {
            public List<string> Lines { get; } = new List<string>();

            public void Write(string text) => Lines.Add(text);

            public void WriteLine(string text) => Lines.Add(text);

            public string? ReadLine() => null;

            public string? ReadSecret() => null;

            public int? Choose(string caption, string message, IReadOnlyList<string> choices, int defaultIndex) => null;
        }
    }
}
