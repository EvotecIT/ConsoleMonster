using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using ConsoleMonster.Serialization;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class RecordingPacingTests {
        private const string Header = "{\"version\":2,\"width\":80,\"height\":4}\n";
        private static string Event(double at, string kind, string value) => "[" + SessionCastWriter.Seconds(at) + "," + SessionCastWriter.Json(kind) + "," + SessionCastWriter.Json(value) + "]\n";
        private static SessionRecording Read(string events) => SessionRecording.Read(new StringReader(Header + events));
        private static long Total(List<RecordedFrame> frames) => frames.Last().Start + frames.Last().Duration;
        private static RecordingExportOptions Source() => new RecordingExportOptions { Pace = RecordingPace.Source, IdleLimit = TimeSpan.Zero, HoldLastFrame = TimeSpan.Zero };

        [Fact]
        public void HoldBetweenSameArrivalRedrawsKeepsItsArrivalOrder() {
            string Draw(string text) => "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l";
            var recording = Read(Event(.1, "o", Draw("One"))
                + Event(.1, "cm", SessionMarkerData.Write("Read One", TimeSpan.FromSeconds(3), null, true))
                + Event(.1, "o", Draw("Two") + "\u001b[2J\u001b[HReady"));
            var options = Source(); options.FramesPerSecond = 60;
            options.Markers.Add(new RecordingMarker(TimeSpan.FromMilliseconds(100), "Custom"));
            var frames = recording.CaptureFrames(options);
            Assert.Equal(new[] { "One", "Two", "Ready" }, frames.Select(f => ConsoleExporter.ToText(f.Frame).Trim()).Where(t => t.Length > 0));
            Assert.Equal(3000, frames.Single(f => ConsoleExporter.ToText(f.Frame).Trim() == "One").ExplicitHold.TotalMilliseconds);
            Assert.Contains("[100,\"Read One\"]", recording.ExportPlayerData(options));
        }

        [Fact]
        public void ChapterTrimAndSpeedDistinguishRedrawsWithTheSameSourceTime() {
            string Draw(string text) => "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l";
            var recording = Read(Event(.1, "o", Draw("One")) + Event(.1, "m", "Start")
                + Event(.1, "o", Draw("Two")) + Event(.1, "m", "End") + Event(.1, "o", Draw("Three")));
            var options = Source(); options.FramesPerSecond = 10; options.ChapterSpeeds.Add("Start", 2);
            var all = recording.CaptureFrames(options);
            Assert.Equal(50, all.Single(f => ConsoleExporter.ToText(f.Frame).Trim() == "One").Duration);
            Assert.Equal(100, all.Single(f => ConsoleExporter.ToText(f.Frame).Trim() == "Two").Duration);
            options.TrimStartChapter = "Start"; options.TrimEndChapter = "End";
            var trimmed = recording.CaptureFrames(options);
            Assert.Equal(new[] { "One", "Two" }, trimmed.Select(f => ConsoleExporter.ToText(f.Frame).Trim()));
            Assert.DoesNotContain("Three", recording.ExportPlayerData(options));
        }

        [Theory]
        [InlineData(0, 4, 2000)]
        [InlineData(.1, 2, 4050)]
        public void ChapterSpeedAppliesWhenItsMarkerDoesNotChangeTheScreen(double at, double speed, long expected) {
            var recording = Read(Event(0, "o", "A") + Event(at, "m", "Install") + Event(8, "m", "End"));
            var options = Source(); options.ChapterSpeeds.Add("Install", speed);
            Assert.Equal(expected, Total(recording.CaptureFrames(options)));
        }

        [Fact]
        public void ChapterTrimExcludesEarlierHoldsAtTheSameTimestamp() {
            string Draw(string text) => "\u001b[?2026h\u001b[2J\u001b[H" + text + "\u001b[?2026l";
            var recording = Read(Event(.1, "o", Draw("One"))
                + Event(.1, "cm", SessionMarkerData.Write("Before", TimeSpan.FromSeconds(3), null, true))
                + Event(.1, "m", "Start") + Event(.1, "o", Draw("Two"))
                + Event(.1, "m", "End") + Event(.1, "o", Draw("Three")));
            var options = Source(); options.TrimStartChapter = "Start"; options.TrimEndChapter = "End"; options.FramesPerSecond = 60;
            var frames = recording.CaptureFrames(options);
            Assert.Equal(34, Total(frames));
            Assert.All(frames, f => Assert.Equal(TimeSpan.Zero, f.ExplicitHold));
            Assert.DoesNotContain("\"Before\"", recording.ExportPlayerData(options));
        }

        [Fact]
        public void HoldsCapsTrimsAndChapterSpeedComposeOnTheSourceTimeline() {
            SessionRecording recording = Read(Event(0, "o", "A") + Event(0, "m", "Install")
                + Event(4, "cm", SessionMarkerData.Write("", TimeSpan.FromSeconds(3), null, false))
                + Event(6, "o", "\rB") + Event(6, "m", "Results") + Event(12, "o", "\rC") + Event(14, "m", "End"));
            RecordingExportOptions options = Source();
            options.Speed = 2;
            options.ChapterSpeeds.Add("Install", 2);
            options.IdleLimit = TimeSpan.FromSeconds(2);
            options.ChapterHold = TimeSpan.FromSeconds(1);
            options.HoldLastFrame = TimeSpan.FromSeconds(2);
            options.TrimStart = TimeSpan.FromSeconds(2);
            options.TrimEndChapter = "End";
            List<RecordedFrame> frames = recording.CaptureFrames(options);
            Assert.Equal(new[] { "A", "B", "C" }, frames.Select(frame => ConsoleExporter.ToText(frame.Frame).Trim()));
            Assert.Equal(new long[] { 4000, 3000, 3000 }, frames.Select(frame => frame.Duration));
            Assert.Equal(10000, Total(frames));
            var player = (Dictionary<string, object?>)MiniJson.Read(recording.ExportPlayerData(options))!;
            Assert.Equal("10000", player["duration"]);
            Assert.Contains("[4000,\"Results\"]", recording.ExportPlayerData(options));
            Assert.Contains("animation:cm-play 10s", recording.Export(ExportFormat.Svg, options));
            // Exporting must not replace source marker times or mutate the caller's settings.
            Assert.Empty(options.Markers);
            Assert.Equal(TimeSpan.FromSeconds(6), recording.Chapters[1].Time);
        }

        [Fact]
        public void TrimByChapterReplaysEarlierOutputButExcludesEndChapterAndItsHold() {
            SessionRecording recording = Read(Event(0, "o", "Install\r\n") + Event(0, "m", "Install")
                + Event(6, "m", "Results") + Event(6, "o", "Results\r\n")
                + Event(12, "m", "Cleanup") + Event(12, "o", "Cleanup"));
            RecordingExportOptions options = Source();
            options.TrimStartChapter = "Results"; options.TrimEndChapter = "Cleanup";
            options.ChapterHold = TimeSpan.FromSeconds(3);
            List<RecordedFrame> frames = recording.CaptureFrames(options);
            Assert.Equal(9000, Total(frames));
            Assert.All(frames, frame => Assert.DoesNotContain("Cleanup", ConsoleExporter.ToText(frame.Frame)));
            Assert.Contains("Install", ConsoleExporter.ToText(frames.Last().Frame));
            Assert.Contains("Results", ConsoleExporter.ToText(frames.Last().Frame));
            Assert.DoesNotContain("Cleanup", recording.ExportPlayerData(options));
            options.TrimStart = TimeSpan.Zero;
            Assert.Throws<ArgumentException>(() => recording.CaptureFrames(options));
        }

        [Theory]
        [InlineData(RecordingPace.Fast, 550)]
        [InlineData(RecordingPace.Normal, 1200)]
        [InlineData(RecordingPace.Slow, 2000)]
        public void ReadingMinimumIsNeverClippedByIdleCap(RecordingPace pace, long expected) {
            SessionRecording recording = Read(Event(0, "o", "A") + Event(0.1, "o", "\rB"));
            var options = Source(); options.Pace = pace; options.IdleLimit = TimeSpan.FromMilliseconds(10);
            List<RecordedFrame> frames = recording.CaptureFrames(options);
            Assert.Equal(expected, frames.Last().Duration);
        }

        [Fact]
        public void ReadingTimeScalesWithNewTextAndWaitsUntilTheBurstSettles() {
            var options = Source(); options.Pace = RecordingPace.Normal;
            SessionRecording burst = Read(Event(0, "o", "A") + Event(0.1, "o", "B") + Event(0.2, "o", "C") + Event(0.3, "o", "D"));
            Assert.Equal(1200, burst.CaptureFrames(options).Last().Duration);
            SessionRecording longer = Read(Event(0, "o", new string('A', 75)));
            Assert.InRange(Total(longer.CaptureFrames(options)), 1865, 1867);
            SessionRecording sameText = Read(Event(0, "o", "A") + Event(2, "o", "\r\u001b[31mA"));
            Assert.Equal(2001, Total(sameText.CaptureFrames(options))); // a colour change adds no reading time
            SessionRecording large = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":100,\"height\":20}\n" + Event(0, "o", new string('x', 1900))));
            Assert.Equal(8000, Total(large.CaptureFrames(options)));
        }

        [Fact]
        public void SourcePaceKeepsSourceTimingAndExplicitPaceOverridesRecordedDirectives() {
            SessionRecording recording = Read(Event(0, "cm", SessionMarkerData.Write("", TimeSpan.Zero, RecordingPace.Slow, false))
                + Event(0, "o", "A") + Event(0.1, "o", "\rB"));
            Assert.Equal(2100, Total(recording.CaptureFrames(new RecordingExportOptions { HoldLastFrame = TimeSpan.Zero })));
            Assert.Equal(1300, Total(recording.CaptureFrames(new RecordingExportOptions { Pace = RecordingPace.Normal, HoldLastFrame = TimeSpan.Zero })));
            Assert.Equal(101, Total(recording.CaptureFrames(Source())));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(75)]
        [InlineData(225)]
        [InlineData(320)]
        [InlineData(450)]
        [InlineData(1900)]
        public void SettledScreenPresetsKeepDistinctReadingBudgets(int characters) {
            var recording = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":100,\"height\":20}\n" + Event(0, "o", new string('x', characters))));
            long ReadAt(RecordingPace pace) {
                var options = Source(); options.Pace = pace;
                return Total(recording.CaptureFrames(options));
            }
            long normal = ReadAt(RecordingPace.Normal);
            // A presenter choosing Slow gets 1.5–2x the reading budget, including at saturation.
            Assert.InRange((double)ReadAt(RecordingPace.Slow) / normal, 1.49, 2.01);
            Assert.InRange((double)ReadAt(RecordingPace.Fast) / normal, 0.4, 0.65);
        }

        [Theory]
        [InlineData(RecordingPace.Slow, 16000)]
        [InlineData(RecordingPace.Normal, 8000)]
        [InlineData(RecordingPace.Fast, 4000)]
        public void DenseScreensKeepTheSelectedReadingMaximum(RecordingPace pace, long expected) {
            var recording = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":100,\"height\":20}\n" + Event(0, "o", new string('x', 1900))));
            var options = Source(); options.Pace = pace; options.IdleLimit = TimeSpan.FromMilliseconds(10);
            Assert.Equal(expected, Total(recording.CaptureFrames(options)));
        }

        [Fact]
        public void MarkerRoundTripPreservesHoldPaceAndTheScreenBeforeSameTimeOutput() {
            string folder = Path.Combine(Path.GetTempPath(), "cm-hold-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                string path = Path.Combine(folder, "hold.cast");
                using (SessionRecorder recorder = SessionRecorder.Start(new SessionRecordingOptions { Path = path, Width = 80, Height = 4, InProcess = true })) {
                    recorder.CapturePowerShellOutput("Before\n");
                    recorder.AddMarker(TimeSpan.FromSeconds(3));
                    recorder.SetPace(RecordingPace.Slow);
                }
                SessionRecording recording = SessionRecording.Read(path);
                SessionRecording roundTrip = SessionRecording.Read(new StringReader(recording.ToAsciicast()));
                Assert.Equal(recording.ToAsciicast(), roundTrip.ToAsciicast());
                Assert.Empty(roundTrip.Chapters);
                Assert.Equal(TimeSpan.FromSeconds(3), roundTrip.Markers[0].Hold);
                Assert.Equal(RecordingPace.Slow, roundTrip.Markers[1].Pace);
                // Same timestamp is common when metadata follows a synchronous burst of output.
                SessionRecording sameTime = Read(Event(0, "o", "Before") + Event(0, "cm", SessionMarkerData.Write("Read", TimeSpan.FromSeconds(3), null, true)) + Event(0, "o", "\rAfter "));
                List<RecordedFrame> frames = sameTime.CaptureFrames(Source());
                Assert.Equal("Before", ConsoleExporter.ToText(frames[0].Frame).Trim());
                Assert.Equal(3000, frames[0].Duration);
                Assert.Equal("After", ConsoleExporter.ToText(frames.Last().Frame).Trim());
            } finally { Directory.Delete(folder, true); }
        }

        [Fact]
        public void ChildHoldControlSurvivesEveryChunkBoundaryAndNeverLeaksIntoOutput() {
            string folder = Path.Combine(Path.GetTempPath(), "cm-control-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                string sequence = SessionRecorder.MarkerSequence(TimeSpan.FromSeconds(3), "Results");
                for (int split = 0; split <= sequence.Length; split++) {
                    string path = Path.Combine(folder, split + ".cast");
                    using (SessionCastWriter writer = new SessionCastWriter(new SessionRecordingOptions { Path = path }, "test")) {
                        string output = writer.Append("Before" + sequence.Substring(0, split));
                        output += writer.Append(sequence.Substring(split) + "After\n");
                        Assert.Equal("BeforeAfter\n", output);
                    }
                    SessionRecording recording = SessionRecording.Read(path);
                    Assert.Single(recording.Chapters);
                    Assert.Equal("Results", recording.Chapters[0].Label);
                    Assert.Equal(TimeSpan.FromSeconds(3), recording.Markers[0].Hold);
                }
            } finally { Directory.Delete(folder, true); }
        }

        [Fact]
        public void AChapterAtTheFractionalMillisecondSourceEndIsNotLost() {
            SessionRecording recording = Read(Event(0, "o", "Ready") + Event(0.012345, "m", "Results"));
            Assert.Contains("Results", recording.ExportPlayerData(Source()));
            Assert.Equal(12, Total(recording.CaptureFrames(Source())));
        }

        [Fact]
        public void MetadataDoesNotResetAnUnchangedScreensIdleBudget() {
            SessionRecording recording = Read(Event(0, "o", "Ready") + Event(4, "m", "Middle") + Event(8, "m", "End"));
            var options = Source(); options.IdleLimit = TimeSpan.FromSeconds(2);
            Assert.Equal(2000, Total(recording.CaptureFrames(options)));
            options.ChapterHold = TimeSpan.FromSeconds(3);
            Assert.Equal(8000, Total(recording.CaptureFrames(options)));
        }

        [Fact]
        public void HoldBetweenRegularSamplesDoesNotRestoreThePrecedingScreen() {
            SessionRecording recording = Read(Event(0, "o", "A") + Event(0.02, "o", "\rB")
                + Event(0.02, "cm", SessionMarkerData.Write("", TimeSpan.FromSeconds(3), null, false)) + Event(0.04, "o", "\rC"));
            var frames = recording.CaptureFrames(Source());
            Assert.Equal(new[] { "A", "B", "C" }, frames.Select(frame => ConsoleExporter.ToText(frame.Frame).Trim()));
            Assert.Equal(new long[] { 20, 3020, 1 }, frames.Select(frame => frame.Duration));
        }

        [Fact]
        public void FractionalSourceTimePreservesHoldArrivalOrderUntilFinalQuantization() {
            SessionRecording recording = Read(Event(0, "o", "A") + Event(0.0208, "o", "\rB")
                + Event(0.0208, "cm", SessionMarkerData.Write("", TimeSpan.FromSeconds(3), null, false))
                + Event(0.0208, "o", "\rC") + Event(0.04, "o", "\rD"));
            var frames = recording.CaptureFrames(Source());
            Assert.Equal(new[] { "A", "B", "C", "D" }, frames.Select(frame => ConsoleExporter.ToText(frame.Frame).Trim()));
            Assert.Equal(new long[] { 21, 3000, 19, 1 }, frames.Select(frame => frame.Duration));
        }

        [Theory]
        [InlineData(2.999785, 3.020957, "B")]
        [InlineData(2.8, 2.804153, "\rB")]
        public void FinalFractionalChapterRemainsInPlayerDataAfterAnUnchangedScreen(double outputTime, double markerTime, string output) {
            SessionRecording recording = Read(Event(0, "o", "A") + Event(outputTime, "o", output)
                + Event(markerTime, "m", "Interactive"));

            Assert.Equal("Interactive", Assert.Single(recording.Markers).Label);
            var player = Assert.IsType<Dictionary<string, object?>>(MiniJson.Read(recording.ExportPlayerData(Source())));
            Assert.True(player.TryGetValue("markers", out object? markerData), "The player must retain the final recorded chapter.");
            var marker = Assert.IsType<List<object?>>(Assert.Single(Assert.IsType<List<object?>>(markerData)));
            Assert.Equal("Interactive", marker[1]);
        }

        [Fact]
        public void MarkerMetadataRetainsSubMillisecondHoldPrecisionOnEveryFramework() {
            TimeSpan hold = TimeSpan.FromTicks(30008000);
            string data = SessionMarkerData.Write("Results", hold, null, true);
            RecordingMarker marker = SessionMarkerData.Read(TimeSpan.Zero, data);
            Assert.Equal(hold, marker.Hold);
            // TimeSpan.FromMilliseconds rounded to whole milliseconds on .NET Framework.
            Assert.Equal(TimeSpan.FromTicks(508000), RecordingTimeline.FromMilliseconds(50.8));
        }

        [Fact]
        public void FractionalExclusiveTrimEndNeverIncludesOutputAtTheBoundary() {
            SessionRecording recording = Read(Event(0, "o", "A") + Event(0.0508, "o", "\rB") + Event(1, "o", "\rC"));
            var options = Source(); options.TrimEnd = TimeSpan.FromTicks(508000);
            var frames = recording.CaptureFrames(options);
            Assert.Single(frames);
            Assert.Equal("A", ConsoleExporter.ToText(frames[0].Frame).Trim());
            Assert.Equal(51, Total(frames));
            options.TrimStart = options.TrimEnd; options.TrimEnd = TimeSpan.FromTicks(758000);
            frames = recording.CaptureFrames(options);
            Assert.Single(frames);
            Assert.Equal("B", ConsoleExporter.ToText(frames[0].Frame).Trim());
            Assert.Equal(25, Total(frames));
        }

        [Fact]
        public void FractionalDashboardTrimDoesNotRoundForwardIntoTheNextEvent() {
            var options = Source(); options.Width = 30; options.Height = 3;
            options.TrimEnd = TimeSpan.FromTicks(508000);
            var events = new[] { new LiveEvent("log", "write", text: "A", time: 1), new LiveEvent("log", "write", text: "B", time: 51), new LiveEvent("log", "write", text: "C", time: 1000) };
            var frames = Recording.CaptureFrames(new LogRegion("log"), events, options);
            Assert.DoesNotContain("B", ConsoleExporter.ToText(frames.Last().Frame));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TrimBetweenRegularSamplesBeginsWithTheExactSourceScreen(bool chapter) {
            SessionRecording recording = Read(Event(0, "o", "A") + Event(0.05, "o", "\rB") + Event(0.05, "m", "Results") + Event(1, "o", "\rC"));
            var options = Source(); options.TrimEnd = TimeSpan.FromMilliseconds(75);
            if (chapter) { options.TrimStartChapter = "Results"; }
            else { options.TrimStart = TimeSpan.FromMilliseconds(50); }
            var frames = recording.CaptureFrames(options);
            Assert.Single(frames);
            Assert.Equal("B", ConsoleExporter.ToText(frames[0].Frame).Trim());
            Assert.Equal(25, Total(frames));
        }

        [Fact]
        public void DashboardTrimmingAlsoReconstructsTheExactBoundaryBetweenSamples() {
            var options = Source(); options.Width = 30; options.Height = 3;
            options.TrimStart = TimeSpan.FromMilliseconds(50); options.TrimEnd = TimeSpan.FromMilliseconds(75);
            var events = new[] { new LiveEvent("log", "write", text: "A", time: 1), new LiveEvent("log", "write", text: "B", time: 50), new LiveEvent("log", "write", text: "C", time: 1000) };
            var frames = Recording.CaptureFrames(new LogRegion("log"), events, options);
            Assert.Single(frames);
            Assert.Contains("B", ConsoleExporter.ToText(frames[0].Frame));
            Assert.DoesNotContain("C", ConsoleExporter.ToText(frames[0].Frame));
            Assert.Equal(25, Total(frames));
        }

        [Fact]
        public void NamedHoldsRemainInTheCompanionChapterIndexAndUnnamedHoldsDoNot() {
            string folder = Path.Combine(Path.GetTempPath(), "cm-chapter-index-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                string path = Path.Combine(folder, "chapters.cast");
                using (SessionRecorder recorder = SessionRecorder.Start(new SessionRecordingOptions { Path = path, InProcess = true })) {
                    recorder.AddMarker(TimeSpan.FromSeconds(3), "Results");
                    recorder.AddMarker(TimeSpan.FromSeconds(3));
                }
                var sidecar = (Dictionary<string, object?>)MiniJson.Read(File.ReadAllText(path + ".json"))!;
                var chapters = (List<object?>)sidecar["markers"]!;
                Assert.Single(chapters);
                Assert.Equal("Results", ((List<object?>)chapters[0]!)[1]);
            } finally { Directory.Delete(folder, true); }
        }

        [Fact]
        public void DashboardFramesAndPlayerUseTheSameHoldsSpeedAndCaps() {
            var options = Source(); options.Width = 30; options.Height = 3;
            options.Markers.Add(new RecordingMarker(TimeSpan.FromSeconds(1), "Install", TimeSpan.FromSeconds(3)));
            options.Markers.Add(new RecordingMarker(TimeSpan.FromSeconds(4), "Results"));
            options.ChapterSpeeds.Add("Install", 2);
            options.ChapterHold = TimeSpan.FromSeconds(1);
            options.IdleLimit = TimeSpan.FromSeconds(1);
            var events = new[] { new LiveEvent("build", "advance", value: 1, time: 1000), new LiveEvent("build", "advance", value: 1, time: 4000) };
            Func<ProgressBar> root = () => new ProgressBar("Build", "build") { Total = 2 };
            var frames = Recording.CaptureFrames(root(), events, options);
            string data = Recording.ExportPlayerData(root(), events, options);
            Assert.Equal(7000, Total(frames));
            Assert.Contains("\"duration\":7000", data);
            Assert.Contains("[6000,\"Results\"]", data);
            Assert.Equal(new long[] { 1000, 5000, 1000 }, frames.Select(frame => frame.Duration));
        }
    }
}
