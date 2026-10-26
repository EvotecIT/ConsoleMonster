using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>
    /// The simulated clock: work moves it forward, sleeping a share of each simulated second set by the speed (never at
    /// speed 0), and a cancelled token or a stopped session lets it move on without sleeping.
    /// </summary>
    [Collection("Live sessions")]
    public sealed class SimulatedClockTests {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FullViewportRecordingCapturesEverySimulatedStepAndFinalHold(bool viewport) {
            var target = new RecordingTarget(TerminalProfile.Fixed(40, 8).WithMode(OutputMode.Interactive)) {
                SkipDelays = true, CaptureViewport = viewport
            };
            var clock = new SimulatedClock();
            var text = new ConsoleMonster.Widgets.TextBlock("initial");
            var options = new LiveSessionOptions { Target = target, Clock = clock.Read, CaptureConsole = false,
                HandleCancelKey = false, PrintFinalState = false, TaskbarProgress = false };
            using (var session = new LiveSession(text, options)) {
                await session.RunAsync((running, token) => {
                    clock.Wait(0.6, token);
                    clock.Hold(1, token);
                    return Task.CompletedTask;
                });
            }
            Assert.Equal(TimeSpan.FromMilliseconds(600), clock.Elapsed);
            Assert.Equal(viewport ? 4 : 0, target.CheckpointCount);
            Assert.Equal(viewport ? 4 : 0, target.Screens.Frames.Count);
            if (viewport) { Assert.All(target.Screens.Frames, frame => Assert.Contains("initial", frame.ToPlainText())); }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ViewportRecordingRetainsCancellationAfterSkippedWaitOrHold(bool holdFirst) {
            var target = new RecordingTarget(TerminalProfile.Fixed(40, 8).WithMode(OutputMode.Interactive)) { SkipDelays = true, CaptureViewport = true };
            var clock = new SimulatedClock(1);
            var options = new LiveSessionOptions { Target = target, Clock = clock.Read, CaptureConsole = false,
                HandleCancelKey = false, PrintFinalState = false, TaskbarProgress = false };
            using (var stop = new CancellationTokenSource())
            using (var session = new LiveSession(ConsoleMonster.Widgets.TextBlock.Plain("work"), options)) {
                await session.RunAsync((running, token) => {
                    if (holdFirst) { clock.Hold(0.25); } else { clock.Wait(0.25); }
                    stop.Cancel();
                    var real = Stopwatch.StartNew();
                    clock.Wait(2);
                    clock.Hold(2);
                    Assert.True(real.Elapsed < TimeSpan.FromSeconds(1), "A cancelled recording clock slept " + real.Elapsed);
                    return Task.CompletedTask;
                }, stop.Token);
            }
            Assert.Equal(holdFirst ? 2 : 2.25, clock.Seconds);
        }

        [Fact]
        public void Wait_SpeedZero_MovesTheClockWithoutSleeping() {
            SimulatedClock clock = new SimulatedClock();
            Stopwatch real = Stopwatch.StartNew();
            clock.Wait(90);
            clock.WaitUntil(120.5);
            Assert.Equal(TimeSpan.FromSeconds(120.5), clock.Elapsed);
            Assert.True(real.Elapsed < TimeSpan.FromSeconds(1), "Waiting at speed 0 slept " + real.Elapsed);
        }

        [Fact]
        public void WaitUntil_TimeAlreadyPassedAndNegativeAdvance_LeaveTheClockAlone() {
            SimulatedClock clock = new SimulatedClock();
            clock.Advance(10);
            clock.WaitUntil(4);
            clock.Advance(-3);
            clock.Advance(double.NaN);
            Assert.Equal(10, clock.Seconds);
        }

        [Fact]
        public void Wait_WithSpeed_SleepsInProportion() {
            SimulatedClock clock = new SimulatedClock(20);
            Stopwatch real = Stopwatch.StartNew();
            clock.Wait(4);
            // Four simulated seconds at 20x are 200 ms of real time.
            Assert.Equal(TimeSpan.FromSeconds(4), clock.Elapsed);
            Assert.InRange(real.Elapsed.TotalMilliseconds, 150, 2000);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Wait_AfterInstantAdvance_DoesNotSleepForTheAdvancedTime(bool afterWait) {
            SimulatedClock clock = new SimulatedClock(1);
            if (afterWait) {
                clock.Wait(0.05);
                clock.Hold(0.12);
            }
            clock.Advance(2);
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(800))) {
                Stopwatch real = Stopwatch.StartNew();
                clock.Wait(0.08, timeout.Token);
                Assert.False(timeout.IsCancellationRequested, "Instantly advanced time was charged as sleep: " + real.Elapsed);
                // Pacing is cumulative: an earlier wait can oversleep, leaving no real time due here.
                // Wait_WithSpeed_SleepsInProportion and the repeated-interval test cover pacing itself.
                Assert.Equal(TimeSpan.FromMilliseconds(afterWait ? 2130 : 2080), clock.Elapsed);
            }
        }

        [Fact]
        public void Wait_RepeatedShortIntervals_UsesCumulativePacing() {
            SimulatedClock clock = new SimulatedClock(1);
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1))) {
                Stopwatch real = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++) {
                    clock.Wait(0.001, timeout.Token);
                }
                Assert.False(timeout.IsCancellationRequested, "Coarse sleeps accumulated: " + real.Elapsed);
                Assert.True(real.Elapsed >= TimeSpan.FromMilliseconds(70), "The waits skipped proportional pacing: " + real.Elapsed);
                Assert.Equal(TimeSpan.FromMilliseconds(100), clock.Elapsed);
            }
        }

        [Fact]
        public void Wait_CancelledToken_MovesOnWithoutSleeping() {
            SimulatedClock clock = new SimulatedClock(0.01);
            using (CancellationTokenSource cancelled = new CancellationTokenSource()) {
                cancelled.Cancel();
                Stopwatch real = Stopwatch.StartNew();
                clock.Wait(30, cancelled.Token);
                clock.Hold(30, cancelled.Token);
                Assert.Equal(TimeSpan.FromSeconds(30), clock.Elapsed);
                Assert.True(real.Elapsed < TimeSpan.FromSeconds(1), "A cancelled wait slept " + real.Elapsed);
            }
        }

        [Fact]
        public async Task Wait_AfterTheSessionIsStoppedWithCtrlC_MovesOnWithoutSleeping() {
            SimulatedClock clock = new SimulatedClock(1);
            LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(new FakeOutput(false)));
            TimeSpan took = TimeSpan.Zero;
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                await session.RunAsync((running, token) => {
                    clock.Wait(0.25);
                    // Cancelling the caller's token stops the session as Ctrl+C does; it no longer counts as current.
                    stop.Cancel();
                    Stopwatch real = Stopwatch.StartNew();
                    clock.Wait(30);
                    took = real.Elapsed;
                    return Task.CompletedTask;
                }, stop.Token);
            }
            Assert.Equal(30.25, clock.Seconds);
            Assert.True(took < TimeSpan.FromSeconds(2), "After Ctrl+C the clock still slept " + took);
        }

        [Fact]
        public void Hold_BeforeTheFirstWait_DoesNotDelayLaterWaits() {
            SimulatedClock clock = new SimulatedClock(10);
            clock.Hold(1);
            Stopwatch real = Stopwatch.StartNew();
            clock.Wait(1);
            // One simulated second at 10x is 100 ms; the hold is not waited again.
            Assert.True(real.Elapsed < TimeSpan.FromMilliseconds(700), "The wait after a hold took " + real.Elapsed);
        }

        [Fact]
        public void Hold_SpeedZero_ReturnsAtOnceWithTheClockStill() {
            SimulatedClock clock = new SimulatedClock();
            Stopwatch real = Stopwatch.StartNew();
            clock.Hold(5);
            Assert.Equal(TimeSpan.Zero, clock.Elapsed);
            Assert.True(real.Elapsed < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ToSessionOptions_SessionReadsTheClock() {
            SimulatedClock clock = new SimulatedClock();
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            options.Clock = clock.ToSessionOptions().Clock;
            LiveSession session = new LiveSession(new FakeTarget(), options);
            session.Start();
            try {
                clock.Advance(83);
                Assert.Equal(TimeSpan.FromSeconds(83), session.Elapsed);
            } finally {
                session.Stop();
            }
            // Stopped, the session keeps the time it ended at.
            clock.Advance(10);
            Assert.Equal(TimeSpan.FromSeconds(83), session.Elapsed);
            Assert.Equal("00:01:33", clock.ToString());
        }
    }
}
