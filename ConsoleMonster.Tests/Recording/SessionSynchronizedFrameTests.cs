using System;
using System.IO;
using System.Linq;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class SessionSynchronizedFrameTests {
        private const string Header = "{\"version\":2,\"width\":12,\"height\":4}\n";
        private static string Event(double time, string text) => "[" + SessionCastWriter.Seconds(time) + ",\"o\"," + SessionCastWriter.Json(text) + "]\n";

        [Fact]
        public void Playback_DoesNotExportTheMiddleOfASynchronizedRepaint() {
            string cast = Header + Event(0.01, "\u001b[?2026h\u001b[HPartial")
                + Event(0.08, "\u001b[HComplete\u001b[?20") + Event(0.09, "26l");
            var frames = SessionRecording.Read(new StringReader(cast)).CaptureFrames(new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = 60 });
            Assert.DoesNotContain(frames, frame => ConsoleExporter.ToText(frame.Frame).Contains("Partial"));
            Assert.Contains(frames, frame => ConsoleExporter.ToText(frame.Frame).Contains("Complete"));
        }

        [Fact]
        public void Playback_KeepsCompletedRepaintsThatShareAnArrivalAndPrecedeCleanup() {
            string output = string.Concat(new[] { "One", "Two", "Three" }.Select(text =>
                "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l")) + "\u001b[2J\u001b[HReady";
            var recording = SessionRecording.Read(new StringReader(Header + Event(0.1, output)));
            var frames = recording.CaptureFrames(new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = 60 });
            string[] states = frames.Select(frame => ConsoleExporter.ToText(frame.Frame).Trim()).Where(text => text.Length > 0).ToArray();
            Assert.Equal(new[] { "One", "Two", "Three", "Ready" }, states);
            for (int i = 1; i < frames.Count; i++) { Assert.True(frames[i].Start > frames[i - 1].Start); }
        }

        [Theory]
        [InlineData(60, 1, 17)]
        [InlineData(10, 0.5, 200)]
        [InlineData(60, 2, 17)]
        [InlineData(0.1, 0.5, 1000)]
        public void Playback_GivesRetainedRedrawsAPlayableDuration(double fps, double speed, long minimum) {
            string output = string.Concat(new[] { "One", "Two", "Three" }.Select(text =>
                "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l")) + "\u001b[2J\u001b[HReady";
            var frames = SessionRecording.Read(new StringReader(Header + Event(0.1, output)))
                .CaptureFrames(new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = fps, Speed = speed });
            foreach (var frame in frames.Where(frame => new[] { "One", "Two", "Three" }.Contains(ConsoleExporter.ToText(frame.Frame).Trim()))) {
                Assert.True(frame.Duration >= minimum, "Completed redraws must be playable at the requested frame rate.");
            }
        }

        [Fact]
        public void Playback_HoldsTheFinalScreenAfterManyRetainedRedraws() {
            string output = string.Concat(Enumerable.Range(0, 3000).Select(i =>
                "\u001b[?2026h\u001b[2J\u001b[HFrame " + i + "\u001b[?2026l")) + "\u001b[2J\u001b[HReady";
            var frames = SessionRecording.Read(new StringReader(Header + Event(0.1, output)))
                .CaptureFrames(new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = 60, MaxFrames = 4096, HoldLastFrame = TimeSpan.FromSeconds(2) });
            Assert.True(frames.Last().Duration >= 2000);
        }

        [Fact]
        public void Playback_ResumesOrdinarySamplingAfterTerminalReset() {
            string cast = Header + Event(0.01, "\u001b[?2026hHidden") + Event(0.02, "\u001bcReady")
                + Event(0.2, "\rWorking") + Event(0.4, "\rDone   ");
            var frames = SessionRecording.Read(new StringReader(cast)).CaptureFrames(new RecordingExportOptions { Pace = RecordingPace.Source });
            Assert.Contains(frames, frame => ConsoleExporter.ToText(frame.Frame).Contains("Ready"));
            Assert.Contains(frames, frame => ConsoleExporter.ToText(frame.Frame).Contains("Working"));
        }

        [Fact]
        public void Playback_KeepsChapterPositionsOnTheAdjustedTimeline() {
            string output = string.Concat(new[] { "One", "Two", "Three" }.Select(text =>
                "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l"));
            string cast = Header + Event(0.1, output) + "[0.2,\"m\",\"Ready\"]\n" + Event(0.2, "\u001b[2J\u001b[HReady");
            var recording = SessionRecording.Read(new StringReader(cast));
            var options = new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = 60 };
            options.Markers.Add(new RecordingMarker(TimeSpan.FromMilliseconds(150), "Custom"));
            var data = (System.Collections.Generic.Dictionary<string, object?>)ConsoleMonster.Serialization.MiniJson.Read(recording.ExportPlayerData(options))!;
            var markers = (System.Collections.Generic.List<object?>)data["markers"]!;
            var custom = (System.Collections.Generic.List<object?>)markers[0]!;
            var ready = (System.Collections.Generic.List<object?>)markers[1]!;
            Assert.Equal("184", custom[0]); Assert.Equal("234", ready[0]);
            Assert.Equal(TimeSpan.FromMilliseconds(150), options.Markers[0].Time);
        }

        [Theory]
        [InlineData(false, 1, "151")]
        [InlineData(true, 1, "151")]
        [InlineData(false, 3, "84")]
        [InlineData(true, 3, "84")]
        public void Playback_ChapterAtABurstTimestampSelectsItsCompletedState(bool recorded, double speed, string expected) {
            string output = string.Concat(new[] { "One", "Two", "Three" }.Select(text =>
                "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l")) + "\u001b[2J\u001b[HReady";
            var recording = SessionRecording.Read(new StringReader(Header + Event(0.1, output) + (recorded ? "[0.1,\"m\",\"Completed\"]\n" : string.Empty)));
            var options = new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = 60, Speed = speed };
            if (!recorded) { options.Markers.Add(new RecordingMarker(TimeSpan.FromMilliseconds(100), "Completed")); }
            var data = (System.Collections.Generic.Dictionary<string, object?>)ConsoleMonster.Serialization.MiniJson.Read(recording.ExportPlayerData(options))!;
            var markers = (System.Collections.Generic.List<object?>)data["markers"]!;
            Assert.Equal(expected, ((System.Collections.Generic.List<object?>)markers[0]!)[0]);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(6)]
        public void Playback_RespectsTheFrameBudgetAndKeepsTheFinalScreen(int maximum) {
            string output = string.Concat(Enumerable.Range(0, 20).Select(i =>
                "\u001b[?2026h\u001b[2J\u001b[HFrame " + i + "\u001b[?2026l")) + "\u001b[2J\u001b[HReady";
            var frames = SessionRecording.Read(new StringReader(Header + Event(0.1, output)))
                .CaptureFrames(new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = 60, MaxFrames = maximum });
            Assert.InRange(frames.Count, 1, maximum);
            Assert.Contains("Ready", ConsoleExporter.ToText(frames[frames.Count - 1].Frame));
        }

        [Fact]
        public void Svg_PreservesDistinctBurstKeyframesInALongRecording() {
            string output = string.Concat(new[] { "One", "Two", "Three" }.Select(text =>
                "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l")) + "\u001b[2J\u001b[HReady";
            var recording = SessionRecording.Read(new StringReader(Header + Event(3600, output)));
            string svg = recording.Export(ExportFormat.Svg, new RecordingExportOptions { Pace = RecordingPace.Source, FramesPerSecond = 60 });
            var matches = System.Text.RegularExpressions.Regex.Matches(svg, @"([0-9.]+)%\{transform:translateX");
            decimal[] positions = matches.Cast<System.Text.RegularExpressions.Match>().Select(match =>
                decimal.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            Assert.True(positions.Length >= 6);
            for (int i = 1; i < positions.Length; i++) { Assert.True(positions[i] > positions[i - 1], "Distinct frames must have distinct SVG keyframe offsets."); }
        }

        [Fact]
        public void Playback_FinalizesAnInterruptedSynchronizedRepaint() {
            var recording = SessionRecording.Read(new StringReader(Header + Event(0.1, "\u001b[?2026hVisible")));
            Assert.Contains("Visible", ConsoleExporter.ToText(recording.CaptureFrames().Last().Frame));
        }
    }
}
