using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Session clock contracts: replay timeline, stream timestamps and the frame schedule.</summary>
    [Collection("Live sessions")]
    public sealed class LiveTimingTests {
        [Fact]
        public void Replay_SpeedUp_ElapsedFollowsTheRecordedTimeline() {
            FakeTarget target = new FakeTarget();
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            long clock = 0;
            options.Clock = () => TimeSpan.FromTicks(Interlocked.Read(ref clock));
            LiveSession? replaying = null;
            options.ReplayDelay = (delay, token) => {
                replaying = LiveSession.Current;
                Interlocked.Add(ref clock, delay.Ticks);
            };
            string recording = "{\"t\":500,\"target\":\"work\",\"op\":\"advance\"}\n{\"t\":2000,\"target\":\"work\",\"op\":\"advance\"}\n";
            LiveSession.Replay(target, new StringReader(recording), options, 4);
            // 500 ms of real (fake) time passed at 4x, so the clock (frozen at shutdown) reads the recorded 2 s.
            Assert.NotNull(replaying);
            Assert.Equal(TimeSpan.FromMilliseconds(500), TimeSpan.FromTicks(clock));
            Assert.Equal(TimeSpan.FromMilliseconds(2000), replaying!.Elapsed);
        }

        [Fact]
        public void StreamTimestamps_Enabled_PrefixesNarrationAndHeartbeat() {
            FakeOutput output = new FakeOutput(false);
            FakeTarget target = new FakeTarget();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.StreamTimestamps = true;
            long clock = 0;
            options.Clock = () => TimeSpan.FromTicks(Interlocked.Read(ref clock));
            using (LiveSession session = new LiveSession(target, options)) {
                session.Start();
                Interlocked.Exchange(ref clock, TimeSpan.FromSeconds(65).Ticks);
                session.Post(new LiveEvent("work", "note", text: "checked forest"));
            }
            Assert.Contains("[01:05] checked forest", output.Lines);
        }

        [Fact]
        public void StreamTimestamps_DefaultOutsideCi_LeavesLinesUnchanged() {
            FakeOutput output = new FakeOutput(false);
            FakeTarget target = new FakeTarget();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            Assert.False(options.Profile!.IsContinuousIntegration);
            using (LiveSession session = new LiveSession(target, options)) {
                session.Start();
                session.Post(new LiveEvent("work", "note", text: "plain line"));
            }
            Assert.Contains("plain line", output.Lines);
        }

        [Fact]
        public void StreamTimestamps_DefaultInCi_PrefixesLines() {
            FakeOutput output = new FakeOutput(false);
            FakeTarget target = new FakeTarget();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.Profile = new TerminalProfile(ColorDepth.None, GlyphTier.Basic, OutputMode.Stream, 80, 24, isContinuousIntegration: true);
            using (LiveSession session = new LiveSession(target, options)) {
                session.Start();
                session.Post(new LiveEvent("work", "note", text: "ci line"));
            }
            Assert.Contains(output.Lines, line => line.StartsWith("[00:0", StringComparison.Ordinal) && line.EndsWith("] ci line", StringComparison.Ordinal));
        }

        [Fact]
        public void FrameSchedule_FifteenFps_WithTimerTickRounding_AveragesTheConfiguredRate() {
            // Fake clock: every wait rounds up to the 15.625 ms Windows timer tick and each frame costs 2 ms of work. The old
            // "sleep the remainder" loop measured about 12.5 fps under exactly these conditions.
            FrameSchedule schedule = new FrameSchedule(15);
            const double Tick = 15.625;
            double now = 0;
            int frames = 0;
            while (now < 3000) {
                now += 2;
                frames++;
                int wait = schedule.Next(now);
                now += wait == 0 ? 0 : Math.Ceiling(wait / Tick) * Tick;
            }
            Assert.InRange(frames, 44, 46);
        }

        [Fact]
        public void FrameSchedule_AfterAStall_ResynchronisesInsteadOfBursting() {
            FrameSchedule schedule = new FrameSchedule(15);
            Assert.True(schedule.Next(1) > 0);
            // A 2-second stall (about 30 missed frames): the next frame renders immediately, then the schedule waits
            // again instead of rendering the missed frames back to back (at most one more is due at once).
            Assert.Equal(0, schedule.Next(2000));
            int immediate = 0;
            for (int i = 1; i <= 5; i++) {
                if (schedule.Next(2000 + i) == 0) {
                    immediate++;
                }
            }
            Assert.InRange(immediate, 0, 1);
        }

        [Fact]
        public void RenderLoop_FifteenFramesPerSecond_KeepsRendering() {
            // Smoke test only; the rate itself is asserted deterministically above and measured in ConsoleMonster.Benchmarks.
            FakeOutput output = new FakeOutput(true);
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.FramesPerSecond = 15;
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Start();
                Assert.True(SpinWait.SpinUntil(() => output.Frames >= 5, TimeSpan.FromSeconds(10)));
            }
        }
    }
}
