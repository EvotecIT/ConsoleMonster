using System;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class ReplayRecordingTests {
        [Fact]
        public void ReplayOnASkippedRecordingTargetUsesLogicalTimeAndCapturesEachTimeGroup() {
            var target = ConsoleMonster.Testing.TestTerminal.Target(40, 8);
            target.SkipDelays = true;
            var times = new System.Collections.Generic.List<TimeSpan>();
            var root = new FakeTarget { Rendering = context => times.Add(context.Elapsed) };
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            LiveSession.Replay(root, new[] {
                new LiveEvent("work", "detail", text: "one", time: 60_000),
                new LiveEvent("work", "detail", text: "same instant", time: 60_000),
                new LiveEvent("work", "detail", text: "two", time: 120_000)
            }, new LiveSessionOptions {
                Target = target, CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false,
                TaskbarProgress = false, StreamHeartbeat = TimeSpan.Zero
            }, speed: 2, ct: cancellation.Token);
            Assert.Equal(3, target.Screens.Frames.Count);
            Assert.Equal(new[] { TimeSpan.Zero, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2) }, times.Take(3));
            Assert.Equal(3, root.Events.Count);
        }

        [Fact]
        public void AppendOnlyReplayKeepsLogicalHeartbeatSummariesWithoutAccumulatingViewportFrames() {
            var profile = ConsoleMonster.Capabilities.TerminalProfile.Fixed(40, 8).WithMode(ConsoleMonster.Capabilities.OutputMode.Stream);
            var target = new ConsoleMonster.Output.RecordingTarget(profile) { SkipDelays = true, CaptureViewport = true };
            var root = new FakeTarget { Summary = "state summary" };
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            LiveSession.Replay(root, new[] {
                new LiveEvent("work", "detail", text: "one", time: 60_000),
                new LiveEvent("work", "detail", text: "two", time: 120_000)
            }, new LiveSessionOptions {
                Target = target, CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false,
                TaskbarProgress = false, StreamHeartbeat = TimeSpan.FromMinutes(1)
            }, speed: 2, ct: cancellation.Token);
            Assert.Empty(target.Screens.Frames);
            Assert.Contains("one", target.Text);
            Assert.Contains("two", target.Text);
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(target.Text, "state summary").Count);
            Assert.DoesNotContain("\u001b[?1049h", target.Output);
        }

        [Fact]
        public void Recording_RegionSwap_RecordsAPictureAndLaterLiveChanges() {
            RegionWidget slot = new RegionWidget("main", new TextBlock("starting"));
            StringWriter recording = new StringWriter();
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            options.RecordWriter = recording;
            using (LiveSession session = new LiveSession(slot, options)) {
                session.Start();
                StatusList steps = new StatusList("steps");
                steps.Set("deploy", "Deploy", StatusKind.Running);
                session.Region("main").Set(steps);
                session.Post(new LiveEvent("steps", "set", key: "deploy", status: "fail"));
                session.Mark("Deploy failed");
                session.Stop();
            }
            LiveEvent[] events = LiveRecordingReader.Read(new StringReader(recording.ToString())).ToArray();
            LiveEvent[] pictures = events.Where(e => e.WidgetId == "main" && e.Operation == "set").ToArray();
            Assert.True(pictures.Length >= 2, recording.ToString());
            Assert.All(pictures, e => Assert.True(e.IsMarkup));
            Assert.Contains("Deploy", pictures[0].Text);
            Assert.Equal("snapshot", pictures[pictures.Length - 1].Status);
            Assert.Contains(events, e => e.WidgetId == LiveSession.MarkerTarget && e.Text == "Deploy failed");

            // Played back, the region shows the last picture.
            RegionWidget replayed = new RegionWidget("main", new TextBlock("starting"));
            foreach (LiveEvent e in events.Where(e => e.WidgetId == "main")) {
                replayed.Apply(e);
            }
            string text = ConsoleMonster.Export.ConsoleExporter.ToText(ConsoleMonster.Rendering.FrameComposer.Compose(replayed, 40, 3, Terminal.CreateContext(ConsoleMonster.Capabilities.TerminalProfile.Fixed(40, 3), ConsoleMonster.Styling.Theme.Dark)));
            Assert.Contains("Deploy", text);
            Assert.DoesNotContain("starting", text);
        }

        [Fact]
        public void AutoRecord_WritesToTheFolderAndKeepsTheNewestRuns() {
            string folder = Path.Combine(Path.GetTempPath(), "cm-autorecord-" + Guid.NewGuid().ToString("N"));
            try {
                Directory.CreateDirectory(folder);
                for (int i = 0; i < 4; i++) {
                    string old = Path.Combine(folder, "nightly-build-2026010" + i + "-120000.jsonl");
                    File.WriteAllText(old, "{}\n");
                    File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddHours(-10 + i));
                }
                File.WriteAllText(Path.Combine(folder, "keep-me.jsonl"), "{}\n");
                File.WriteAllText(Path.Combine(folder, "nightly-build-notes.jsonl"), "{}\n");
                LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
                options.AutoRecord = 3;
                options.AutoRecordFolder = folder;
                options.RecordingName = "nightly build";
                FakeTarget target = new FakeTarget();
                string? path;
                using (LiveSession session = new LiveSession(target, options)) {
                    session.Start();
                    session.Post(new LiveEvent("work", "advance", value: 1));
                    session.Stop();
                    path = session.RecordingPath;
                }
                Assert.NotNull(path);
                Assert.StartsWith("nightly-build-", Path.GetFileName(path));
                Assert.Single(LiveRecordingReader.Read(new StringReader(File.ReadAllText(path!))));
                string[] kept = Directory.GetFiles(folder, "*.jsonl").Select(Path.GetFileName).OrderBy(n => n).ToArray()!;
                Assert.Equal(new[] { "keep-me.jsonl", "nightly-build-20260102-120000.jsonl", "nightly-build-20260103-120000.jsonl", "nightly-build-notes.jsonl", Path.GetFileName(path)! }.OrderBy(n => n, StringComparer.Ordinal).ToArray(), kept.OrderBy(n => n, StringComparer.Ordinal).ToArray());
            } finally {
                Directory.Delete(folder, true);
            }
        }

        [Fact]
        public void AutoRecord_StopsGrowingAtTheSizeLimit() {
            StringWriter writer = new StringWriter();
            using (LiveRecorder recorder = new LiveRecorder(writer) { Limit = 200 }) {
                for (int i = 0; i < 50; i++) {
                    recorder.Write(new LiveEvent("work", "advance", value: i + 1, time: i + 1));
                }
                Assert.True(recorder.Truncated);
            }
            Assert.True(writer.ToString().Length < 300);
            Assert.NotEmpty(LiveRecordingReader.Read(new StringReader(writer.ToString())));
        }

        [Fact]
        public void AutoRecord_OffByDefaultAndIgnoredWithRecordPath() {
            LiveSessionOptions options = new LiveSessionOptions();
            Assert.Equal(0, options.AutoRecord);
            options.AutoRecord = -3;
            Assert.Equal(0, options.AutoRecord);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.AutoRecordMaxBytes = 10);
            Assert.EndsWith(Path.Combine("ConsoleMonster", "Recordings"), LiveSessionOptions.DefaultRecordingFolder);
        }
    }
}
