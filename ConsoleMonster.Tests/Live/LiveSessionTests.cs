using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class LiveSessionTests {
        internal static LiveSessionOptions Options(FakeOutput output) => new LiveSessionOptions {
            Output = output, Profile = output.Profile, CaptureConsole = false, HandleCancelKey = false, FramesPerSecond = 60
        };

        [Fact]
        public void ConcurrentProducersPreserveEveryEventAndProducerOrderOnTheRenderThread() {
            const int producers = 32;
            const int perProducer = 2000;
            FakeTarget target = new FakeTarget();
            FakeOutput output = new FakeOutput();
            using (LiveSession session = new LiveSession(target, Options(output)))
            using (ManualResetEventSlim start = new ManualResetEventSlim())
            using (CountdownEvent done = new CountdownEvent(producers))
            using (ManualResetEventSlim applied = new ManualResetEventSlim()) {
                int count = 0;
                target.Applying = e => { if (Interlocked.Increment(ref count) == producers * perProducer) { applied.Set(); } };
                Thread[] workers = new Thread[producers];
                for (int i = 0; i < producers; i++) {
                    string key = i.ToString();
                    workers[i] = new Thread(() => {
                        start.Wait();
                        for (int j = 0; j < perProducer; j++) { session.Post(new LiveEvent("work", "advance", key, value: j)); }
                        done.Signal();
                    }) { IsBackground = true };
                    workers[i].Start();
                }
                session.Start();
                start.Set();
                Assert.True(done.Wait(TimeSpan.FromSeconds(30)), "Producer deadlock watchdog expired.");
                Assert.True(applied.Wait(TimeSpan.FromSeconds(30)), "Dispatcher deadlock watchdog expired.");
                session.Stop();
                foreach (Thread worker in workers) { Assert.True(worker.Join(TimeSpan.FromSeconds(30))); }
                Assert.Equal(producers * perProducer, session.EventsPosted);
                Assert.Equal(session.EventsPosted, session.EventsApplied);
                Assert.Equal(0, session.EventsDropped);
                Assert.Equal(0, session.ApplyErrors);
                Assert.Single(target.ApplyThreads);
                Assert.Equal(producers * perProducer, target.Events.Count);
                foreach (IGrouping<string?, LiveEvent> group in target.Events.GroupBy(e => e.Key)) {
                    Assert.Equal(Enumerable.Range(0, perProducer).Select(i => (double)i), group.Select(e => e.Value));
                }
            }
        }

        [Fact]
        public void ApplyFailureAndUnknownTargetDoNotPreventLaterEvents() {
            FakeTarget target = new FakeTarget { Applying = e => { if (e.Operation == "bad") { throw new InvalidOperationException("apply"); } } };
            FakeOutput output = new FakeOutput(false);
            using (LiveSession session = new LiveSession(target, Options(output))) {
                session.Start();
                session.Post(new LiveEvent("work", "bad"));
                session.Post(new LiveEvent("missing", "advance"));
                session.Post(new LiveEvent("work", "detail", text: "later"));
                session.Stop();
                Assert.Equal(1, session.ApplyErrors);
                Assert.Equal(1, session.EventsDropped);
                Assert.Equal(1, session.EventsApplied);
                Assert.Equal("apply", session.LastError!.Message);
                Assert.Equal("later", Assert.Single(target.Events).Text);
                Assert.Contains("later", output.Lines);
            }
        }

        [Fact]
        public void TreeDiscoveryRejectsDuplicatesAndRegistrationSelectsExplicitLog() {
            FakeLogTarget first = new FakeLogTarget("first");
            FakeLogTarget chosen = new FakeLogTarget("chosen");
            FakeTarget external = new FakeTarget("external");
            FakeOutput output = new FakeOutput(false);
            LiveSessionOptions options = Options(output);
            options.LogTarget = "chosen";
            using (LiveSession session = new LiveSession(new FakeContainer(first, chosen), options)) {
                session.Register(external);
                Assert.True(session.TryGetTarget("$log", out ILiveTarget? actual));
                Assert.Same(chosen, actual);
                Assert.Contains("external", session.TargetNames);
                Assert.Throws<ArgumentException>(() => session.Register(new FakeTarget("external")));
                session.Run(s => s.Write("hello"));
                Assert.Empty(first.Events);
                Assert.Equal("hello", Assert.Single(chosen.Events).Text);
            }
            Assert.Throws<ArgumentException>(() => new LiveSession(new FakeContainer(new FakeTarget(), new FakeTarget()), options));
        }

        [Fact]
        public void CurrentReturnsMostRecentlyStartedSurvivingSessionAndStopIsIdempotent() {
            using (LiveSession first = new LiveSession(new FakeTarget(), Options(new FakeOutput(false))))
            using (LiveSession second = new LiveSession(new FakeTarget(), Options(new FakeOutput(false)))) {
                first.Start();
                Assert.Same(first, LiveSession.Current);
                second.Start();
                Assert.Same(second, LiveSession.Current);
                second.Stop();
                Assert.Same(first, LiveSession.Current);
                first.Stop();
                first.Stop();
                first.Dispose();
                Assert.Null(LiveSession.Current);
                Assert.False(first.IsRunning);
                Assert.True(first.Cancellation.IsCancellationRequested);
                Assert.Throws<InvalidOperationException>(() => first.Start());
            }
        }

        [Fact]
        public void ConcurrentStopDrainsAcceptedEventsWithoutConcurrentApply() {
            FakeTarget target = new FakeTarget();
            using (LiveSession session = new LiveSession(target, Options(new FakeOutput(false)))) {
                session.Start();
                for (int i = 0; i < 10000; i++) { session.Post(new LiveEvent("work", "advance", value: i)); }
                Parallel.Invoke(session.Stop, session.Stop, session.Dispose);
                Assert.Equal(10000, target.Events.Count);
                Assert.Equal(10000, session.EventsApplied);
                session.Post(new LiveEvent("work", "advance"));
                Assert.Equal(1, session.EventsDropped);
            }
        }

        [Fact]
        public void IdleTicksAdvanceClockAndHonorInvalidation() {
            FakeTarget target = new FakeTarget();
            FakeOutput output = new FakeOutput();
            using (ManualResetEventSlim frames = new ManualResetEventSlim())
            using (LiveSession session = new LiveSession(target, Options(output))) {
                long lastTick = -1;
                TimeSpan lastElapsed = TimeSpan.Zero;
                target.Rendering = context => { lastTick = context.Tick; lastElapsed = context.Elapsed; };
                output.Frame = () => { if (output.Frames >= 3) { frames.Set(); } };
                session.Invalidate();
                session.Start();
                Assert.True(frames.Wait(TimeSpan.FromSeconds(1)));
                session.Stop();
                Assert.True(lastTick >= 2);
                Assert.True(lastElapsed > TimeSpan.Zero);
                Assert.True(output.Invalidations >= 1);
                Assert.Equal(0, session.EventsApplied);
            }
        }

        [Fact]
        public void LineModeNarratesAndUsesRegisteredSummariesOnHeartbeat() {
            FakeTarget target = new FakeTarget { Summary = "three done" };
            FakeOutput output = new FakeOutput(false);
            long ticks = 0;
            LiveSessionOptions options = Options(output);
            options.Clock = () => TimeSpan.FromTicks(Interlocked.Read(ref ticks));
            options.StreamHeartbeat = TimeSpan.FromSeconds(10);
            using (ManualResetEventSlim applied = new ManualResetEventSlim())
            using (LiveSession session = new LiveSession(target, options)) {
                target.Applying = e => applied.Set();
                session.Start();
                session.Post(new LiveEvent("work", "detail", text: "working"));
                Assert.True(applied.Wait(TimeSpan.FromSeconds(1)));
                Interlocked.Exchange(ref ticks, TimeSpan.FromSeconds(10).Ticks);
                Assert.True(SpinWait.SpinUntil(() => output.Lines.Contains("three done"), 1000));
                session.Stop();
                Assert.Contains("working", output.Lines);
                Assert.Equal(0, output.Frames);
            }
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(100, 60)]
        [InlineData(15, 15)]
        public void FrameRateIsClamped(int requested, int expected) {
            Assert.Equal(expected, new LiveSessionOptions { FramesPerSecond = requested }.FramesPerSecond);
        }
    }
}
