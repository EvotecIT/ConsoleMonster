using System;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Live;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    /// <summary>
    /// The summary bar's time left and duration follow the session's clock (a simulated clock included) from the first
    /// progress until progress reaches the total, instead of real time.
    /// </summary>
    [Collection("Live sessions")]
    public sealed class SummaryBarClockTests {
        private static readonly RenderContext Context = new RenderContext(TerminalProfile.Fixed(100, 3, ColorDepth.None, GlyphTier.Full), Theme.Dark);

        private static void Draw(SummaryBar bar, long tick, double seconds) {
            CellBuffer buffer = new CellBuffer(100, 1);
            bar.Render(new Region(buffer), Context.WithClock(tick, TimeSpan.FromSeconds(seconds)));
        }

        private static string Text(SummaryBar bar) => bar.Line(80, Context).PlainText;

        // A bar given the elapsed time directly: what the clocked bar must read like.
        private static string Expected(double done, double total, double seconds) => Text(new SummaryBar { Elapsed = TimeSpan.FromSeconds(seconds) }.SetProgress(done, total).SetCounter("passed", (int)done, StatusKind.Pass));

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void Recording_ProgressAtTimeZero_ExportsFullDuration(int framesPerSecond) {
            SummaryBar bar = new SummaryBar();
            LiveEvent[] events = {
                new LiveEvent("summary", "progress", value: 0, total: 10, time: 0),
                new LiveEvent("summary", "progress", value: 10, total: 10, time: 1000)
            };
            // Recording.Export uses RecordingFrames.Sample, whose first frame is tick 0 at time 0.
            string cast = Recording.Export(bar, events, ExportFormat.Asciicast, new RecordingExportOptions {
                Width = 100, Height = 1, FramesPerSecond = framesPerSecond
            });
            Assert.Contains("in 1.0s", cast);
        }

        [Fact]
        public void Render_FirstFrameAtTickZero_StartsDirectProgress() {
            SummaryBar bar = new SummaryBar().SetProgress(0, 10);
            Draw(bar, 0, 0);
            bar.Apply(new LiveEvent("summary", "progress", value: 5, total: 10, time: 1000));
            Draw(bar, 1, 1);
            Assert.Equal(TimeSpan.FromSeconds(1), bar.Remaining);
        }

        [Fact]
        public void Apply_ProgressAtZero_FollowedByCompletionBeforeRender_PreservesStart() {
            SummaryBar bar = new SummaryBar();
            bar.Apply(new LiveEvent("summary", "progress", value: 0, total: 10, time: 0));
            bar.Apply(new LiveEvent("summary", "progress", value: 10, total: 10, time: 1000));
            Draw(bar, 0, 2);
            Assert.Contains("in 1.0s", Text(bar));
        }

        [Fact]
        public void Render_ClockedFrames_EstimateFromTheSessionClockAndFreezeTheDurationAtTheEnd() {
            SummaryBar bar = new SummaryBar().SetProgress(0, 10).SetCounter("passed", 0, StatusKind.Pass);
            Draw(bar, 1, 0);
            bar.Apply(new LiveEvent("summary", "progress", value: 5, total: 10, time: 30000));
            bar.SetCounter("passed", 5, StatusKind.Pass);
            Draw(bar, 2, 30);
            Assert.Equal(TimeSpan.FromSeconds(30), bar.Remaining);
            Assert.Equal(Expected(5, 10, 30), Text(bar));

            bar.Apply(new LiveEvent("summary", "progress", value: 10, total: 10, time: 45000));
            bar.SetCounter("passed", 10, StatusKind.Pass);
            Draw(bar, 3, 45);
            // The clock keeps going while the finished screen stays; the duration stays at the moment the run ended.
            Draw(bar, 4, 90);
            Assert.Null(bar.Remaining);
            Assert.Equal(Expected(10, 10, 45), Text(bar));
        }

        [Fact]
        public void Render_ProgressSetDirectly_StartsAtTheNextClockedFrame() {
            SummaryBar bar = new SummaryBar();
            Draw(bar, 1, 4);
            bar.SetProgress(2, 8);
            Draw(bar, 2, 10);
            Draw(bar, 3, 16);
            // Started at 10 s (the first frame after the progress), 6 s for 2 of 8: 18 s left.
            Assert.Equal(TimeSpan.FromSeconds(18), bar.Remaining);
        }

        [Fact]
        public void Apply_UnstampedEvent_LeavesTheStartToTheNextClockedFrame() {
            SummaryBar bar = new SummaryBar();
            // An event applied outside a session carries no time (0); it must not count as "the run started at 0 s".
            bar.Apply(new LiveEvent("summary", "progress", value: 0, total: 10));
            Draw(bar, 1, 5);
            bar.Apply(new LiveEvent("summary", "progress", value: 5, total: 10, time: 15000));
            Draw(bar, 2, 15);
            Assert.Equal(TimeSpan.FromSeconds(10), bar.Remaining);
        }

        [Fact]
        public void Render_ExplicitElapsed_WinsOverTheSessionClock() {
            SummaryBar bar = new SummaryBar { Elapsed = TimeSpan.FromSeconds(8) }.SetProgress(4, 8);
            Draw(bar, 5, 100);
            Assert.Equal(TimeSpan.FromSeconds(8), bar.Remaining);
        }

        [Fact]
        public void LiveSession_SimulatedClock_DrivesTheTimeLeftAndDuration() {
            long ticks = 0;
            SummaryBar bar = new SummaryBar().SetProgress(0, 10).SetCounter("passed", 0, StatusKind.Pass);
            FakeOutput output = new FakeOutput();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.Clock = () => TimeSpan.FromTicks(Interlocked.Read(ref ticks));
            using (LiveSession session = new LiveSession(bar, options)) {
                session.Start();
                // Two frames at 0 s, so the bar knows the run started then.
                session.Post(new LiveEvent("summary", "counter", key: "passed", value: 0, status: "Pass"));
                Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref output.Frames) >= 2, 5000), "No frames were drawn.");
                // 4 of 10 after 20 s on the simulated clock: 30 s left (real time would say almost nothing has passed).
                Interlocked.Exchange(ref ticks, TimeSpan.FromSeconds(20).Ticks);
                session.Post(new LiveEvent("summary", "progress", value: 4, total: 10));
                session.Post(new LiveEvent("summary", "counter", key: "passed", value: 4, status: "Pass"));
                Assert.True(SpinWait.SpinUntil(() => bar.Remaining == TimeSpan.FromSeconds(30), 5000), "The time left did not follow the clock: " + bar.Remaining);

                // Done at 50 s; the clock moving on to 75 s while the screen stays does not change the duration.
                Interlocked.Exchange(ref ticks, TimeSpan.FromSeconds(50).Ticks);
                session.Post(new LiveEvent("summary", "progress", value: 10, total: 10));
                session.Post(new LiveEvent("summary", "counter", key: "passed", value: 10, status: "Pass"));
                Assert.True(SpinWait.SpinUntil(() => bar.Done >= 10 && bar.Remaining == null, 5000), "The end was not applied.");
                Interlocked.Exchange(ref ticks, TimeSpan.FromSeconds(75).Ticks);
                int frames = Volatile.Read(ref output.Frames);
                session.Post(new LiveEvent("summary", "counter", key: "passed", value: 10, status: "Pass"));
                SpinWait.SpinUntil(() => Volatile.Read(ref output.Frames) > frames + 1, 1000);
                session.Stop();
            }
            Assert.Equal(Expected(10, 10, 50), Text(bar));
        }
    }
}
