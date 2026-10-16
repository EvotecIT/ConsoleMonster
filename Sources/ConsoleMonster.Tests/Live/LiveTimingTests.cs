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
        public void RenderLoop_FifteenFramesPerSecond_MeetsTheConfiguredRate() {
            FakeOutput output = new FakeOutput(true);
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.FramesPerSecond = 15;
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Start();
                Thread.Sleep(3000);
            }
            // The deadline schedule averages the configured rate; the old "sleep the remainder" loop measured ~12.5 fps
            // (37-38 frames in 3 s) because every wait rounded up to the 15.6 ms system timer tick.
            Assert.InRange(output.Frames, 42, 50);
        }
    }
}
