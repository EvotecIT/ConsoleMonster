using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class LiveSessionDelayTests {
        private static LiveSessionOptions Options(ITerminalTarget target) => new LiveSessionOptions {
            Target = target, CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false,
            TaskbarProgress = false, StreamHeartbeat = TimeSpan.Zero, FramesPerSecond = 60
        };

        [Theory]
        [InlineData(-1L)]
        [InlineData(21474836470001L)]
        [InlineData(long.MaxValue)]
        public void InvalidDelayIsRejectedEvenWhenSnapshotWaitsAreSkipped(long ticks) {
            string? previous = Environment.GetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT");
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", "1");
            try {
                using (var session = new LiveSession(TextBlock.Plain("work"), Options(NullTarget.Instance))) {
                    Assert.Throws<ArgumentOutOfRangeException>(() => session.Delay(TimeSpan.FromTicks(ticks)));
                }
            } finally { Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", previous); }
        }

        [Fact]
        public void NormalWaitIsInterruptedBySessionCancellation() {
            string? previous = Environment.GetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT");
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", null);
            var target = TestTerminal.Target();
            Assert.False(target.SkipDelays);
            Exception? error = null;
            try {
                using (var session = new LiveSession(TextBlock.Plain("work"), Options(target))) {
                    session.Start();
                    var worker = new Thread(() => {
                        try { session.Delay(TimeSpan.FromMilliseconds(int.MaxValue)); }
                        catch (Exception ex) { error = ex; }
                    }) { IsBackground = true };
                    try {
                        worker.Start();
                        Assert.True(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0, TimeSpan.FromSeconds(5)), "Delay did not enter its cancellable wait.");
                        Assert.Equal(1, target.CheckpointCount);
                    } finally {
                        session.Cancel();
                        Assert.True(worker.Join(TimeSpan.FromSeconds(5)), "Delay did not release its producer after cancellation.");
                    }
                    Assert.IsAssignableFrom<OperationCanceledException>(error);
                    Assert.Empty(target.Screens.Frames);
                }
            } finally { Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", previous); }
        }

        [Theory]
        [InlineData("1")]
        [InlineData("TrUe")]
        [InlineData("YES")]
        public async Task SnapshotEnvironmentSkipsWaitsWithoutAddingOutput(string value) {
            string? previous = Environment.GetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT");
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", value);
            var output = new FakeOutput(false);
            using (var session = new LiveSession(new FakeTarget(), new LiveSessionOptions {
                Output = output, Profile = output.Profile, Target = NullTarget.Instance,
                CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false
            })) {
                Task? wait = null;
                try {
                    session.Start();
                    session.Post(new LiveEvent("work", "detail", text: "updated"));
                    wait = Task.Run(() => session.Delay(TimeSpan.FromMilliseconds(int.MaxValue)));
                    Assert.Same(wait, await Task.WhenAny(wait, Task.Delay(TimeSpan.FromSeconds(5))));
                    await wait;
                    session.Stop();
                    Assert.Equal(new[] { "updated" }, output.Lines);
                    Assert.DoesNotContain("block", output.Calls);
                } finally {
                    session.Cancel();
                    if (wait != null) { try { await wait; } catch (OperationCanceledException) { } }
                    Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", previous);
                }
            }
        }

        [Fact]
        public async Task RecordingCheckpointsFollowQueuedSlotChangesOnTheDispatcherAndKeepTheCapturedTarget() {
            var target = TestTerminal.Target(32, 20, keys: "Enter");
            target.SkipDelays = true;
            ITerminalTarget previous = Terminal.Target;
            IInputSource? previousInput = ConsoleInput.Override;
            bool acquired = ConsoleInput.IsAcquired;
            var gate = new FakeTarget();
            var slot = new Slot("body", gate);
            var options = Options(target);
            options.Target = null; // Start captures the scope; producer calls below run after it has ended.
            using (var entered = new ManualResetEventSlim())
            using (var proceed = new ManualResetEventSlim())
            using (var session = new LiveSession(slot, options)) {
                gate.Applying = update => {
                    entered.Set();
                    if (!proceed.Wait(TimeSpan.FromSeconds(5))) { throw new TimeoutException("Checkpoint test did not release the dispatcher."); }
                };
                using (Terminal.Use(target)) { session.Start(); }
                Assert.Same(previous, Terminal.Target);
                Task? producer = null;
                try {
                    session.Post(new LiveEvent("work", "hold"));
                    Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
                    producer = Task.Run(() => {
                        int thread = Thread.CurrentThread.ManagedThreadId;
                        session.Region("body").Set(new CheckpointText("first", thread));
                        session.Delay(TimeSpan.FromHours(1));
                        session.Region("body").Set(new CheckpointText("second", thread));
                        session.Delay(TimeSpan.Zero);
                    });
                    Assert.True(SpinWait.SpinUntil(() => session.EventsPosted >= 3, TimeSpan.FromSeconds(5)), "Checkpoint was not queued behind the held update.");
                    Assert.Empty(target.Screens.Frames);
                    proceed.Set();
                    Assert.Same(producer, await Task.WhenAny(producer, Task.Delay(TimeSpan.FromSeconds(5))));
                    await producer;
                    session.Stop();
                    Assert.Null(session.Failure);
                    Assert.Equal(2, target.CheckpointCount);
                    Assert.Equal(new[] { "first|0|0", "second|0|36000000000" }, target.Screens.Frames.Select(frame => frame.ToPlainText()));
                    Assert.Equal(TimeSpan.FromHours(1), session.Elapsed);
                    Assert.All(target.Screens.Frames, frame => { Assert.Equal(32, frame.Width); Assert.Equal(1, frame.Height); });
                    Assert.Equal(1, Assert.IsType<ScriptedInput>(target.Input).Remaining);
                    Assert.Same(previous, Terminal.Target);
                    Assert.Same(previousInput, ConsoleInput.Override);
                    Assert.Equal(acquired, ConsoleInput.IsAcquired);
                } finally {
                    proceed.Set();
                    session.Cancel();
                    if (producer != null) { try { await producer; } catch (OperationCanceledException) { } }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SelectedCheckpointsBoundFrameRetentionAndCountUnselectedCalls(bool captureSome) {
            var target = TestTerminal.Target(24, 12);
            target.SkipDelays = true;
            target.Checkpoints = captureSome ? new[] { 1, 3 } : Array.Empty<int>();
            var slot = new Slot("body");
            using (var session = new LiveSession(slot, Options(target))) {
                session.Start();
                for (int i = 0; i < 5; i++) {
                    session.Region("body").Set(TextBlock.Plain("state " + i));
                    session.Delay(TimeSpan.FromTicks(7));
                }
                session.Stop();
                Assert.Equal(5, target.CheckpointCount);
                Assert.Equal(TimeSpan.FromTicks(35), session.Elapsed);
                Assert.Equal(captureSome ? new[] { "state 1", "state 3" } : Array.Empty<string>(), target.Screens.Frames.Select(frame => frame.ToPlainText()));
                Assert.Equal("state 4", Assert.IsType<TextBlock>(slot.Content).Content);
                Assert.Equal(0, session.EventsDropped);
            }
        }

        [Fact]
        public void ExplicitClockTakesPrecedenceOverSkippedDelayDurations() {
            var target = TestTerminal.Target();
            target.SkipDelays = true;
            TimeSpan clock = TimeSpan.FromSeconds(10);
            var options = Options(target);
            options.Clock = () => clock;
            using (var session = new LiveSession(new CheckpointText("clock", Thread.CurrentThread.ManagedThreadId), options)) {
                session.Start();
                clock += TimeSpan.FromSeconds(2);
                session.Delay(TimeSpan.FromHours(1));
                Assert.Equal(TimeSpan.FromSeconds(2), session.Elapsed);
                Assert.Equal("clock|0|20000000", Assert.Single(target.Screens.Frames).ToPlainText());
            }
        }

        [Fact]
        public void SkippedDelaysStampUpdatesWithLogicalElapsedAndPreserveExplicitEventTimes() {
            var target = TestTerminal.Target();
            target.SkipDelays = true;
            target.Checkpoints = Array.Empty<int>();
            var work = new FakeTarget();
            using (var session = new LiveSession(work, Options(target))) {
                session.Start();
                session.Post(new LiveEvent("work", "first"));
                session.Delay(TimeSpan.FromSeconds(2));
                session.Post(new LiveEvent("work", "second"));
                session.Post(new LiveEvent("work", "source-time", time: 812));
                session.Delay(TimeSpan.FromSeconds(3));
                session.Stop();
                Assert.Equal(new long[] { 0, 2000, 812 }, work.Events.Select(update => update.Time));
                Assert.Equal(TimeSpan.FromSeconds(5), session.Elapsed);
                Assert.Equal(2, target.CheckpointCount);
                Assert.Empty(target.Screens.Frames);
            }
        }

        [Fact]
        public async Task CancellationReleasesAQueuedCheckpointBeforeTheDispatcherCanApplyIt() {
            var target = TestTerminal.Target();
            target.SkipDelays = true;
            var gate = new FakeTarget();
            using (var entered = new ManualResetEventSlim())
            using (var proceed = new ManualResetEventSlim())
            using (var session = new LiveSession(gate, Options(target))) {
                gate.Applying = update => {
                    entered.Set();
                    if (!proceed.Wait(TimeSpan.FromSeconds(5))) { throw new TimeoutException("Checkpoint cancellation test did not release the dispatcher."); }
                };
                session.Start();
                Task? wait = null;
                try {
                    session.Post(new LiveEvent("work", "hold"));
                    Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
                    wait = Task.Run(() => session.Delay(TimeSpan.FromSeconds(1)));
                    Assert.True(SpinWait.SpinUntil(() => session.EventsPosted >= 2, TimeSpan.FromSeconds(5)));
                    session.Cancel();
                    Assert.Same(wait, await Task.WhenAny(wait, Task.Delay(TimeSpan.FromSeconds(5))));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
                    Assert.Empty(target.Screens.Frames);
                } finally {
                    proceed.Set();
                    session.Cancel();
                    if (wait != null) { try { await wait; } catch (OperationCanceledException) { } }
                }
                session.Stop();
                Assert.Empty(target.Screens.Frames);
            }
        }

        // Rendering this widget on its producer is a race even if the resulting text happens to look right.
        private sealed class CheckpointText : IWidget {
            private readonly string _text;
            private readonly int _producer;
            internal CheckpointText(string text, int producer) { _text = text; _producer = producer; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, 1);
            public void Render(Region region, RenderContext context) {
                Assert.NotEqual(_producer, Thread.CurrentThread.ManagedThreadId);
                region.Write(0, 0, _text + "|" + context.Tick + "|" + context.Elapsed.Ticks, Style.Plain);
            }
        }
    }
}
