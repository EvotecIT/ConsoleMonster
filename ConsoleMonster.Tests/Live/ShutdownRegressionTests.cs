using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Shutdown and encoding contracts exposed by independent lifecycle review.</summary>
    [Collection("Live sessions")]
    public sealed class ShutdownRegressionTests {
        /// <summary>Rejected publications cannot keep extending the shutdown barrier.</summary>
        [Fact]
        public void ContinuingProducersCannotDelayStopAndEveryAcceptedEventIsApplied() {
            FakeTarget target = new FakeTarget();
            using (LiveSession session = new LiveSession(target, LiveSessionTests.Options(new FakeOutput(false))))
            using (CountdownEvent ready = new CountdownEvent(32))
            using (ManualResetEventSlim flood = new ManualResetEventSlim())
            using (ManualResetEventSlim releaseApply = new ManualResetEventSlim())
            using (ManualResetEventSlim finished = new ManualResetEventSlim()) {
                int producing = 1;
                Thread[] producers = new Thread[32];
                target.Applying = e => {
                    if (!releaseApply.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("Apply release watchdog expired."); }
                };
                session.Start();
                for (int i = 0; i < producers.Length; i++) {
                    producers[i] = new Thread(() => {
                        LiveEvent liveEvent = new LiveEvent("work", "advance");
                        session.Post(liveEvent);
                        ready.Signal();
                        flood.Wait();
                        while (Volatile.Read(ref producing) != 0) { session.Post(liveEvent); }
                    }) { IsBackground = true };
                    producers[i].Start();
                }
                Thread stopper = new Thread(() => { session.Stop(); finished.Set(); }) { IsBackground = true };
                bool completed;
                try {
                    Assert.True(ready.Wait(TimeSpan.FromSeconds(30)));
                    stopper.Start();
                    Assert.True(SpinWait.SpinUntil(() => !session.IsRunning, TimeSpan.FromSeconds(30)));
                    flood.Set();
                    Assert.True(SpinWait.SpinUntil(() => session.EventsDropped >= 32, TimeSpan.FromSeconds(30)));
                    releaseApply.Set();
                    completed = finished.Wait(TimeSpan.FromSeconds(30));
                } finally {
                    Volatile.Write(ref producing, 0);
                    flood.Set();
                    releaseApply.Set();
                    foreach (Thread producer in producers) { Assert.True(producer.Join(TimeSpan.FromSeconds(30))); }
                }
                Assert.True(stopper.Join(1000));
                Assert.True(completed);
                Assert.Equal(session.EventsPosted, session.EventsApplied);
                Assert.Equal(session.EventsApplied, target.Events.Count);
                Assert.True(session.EventsDropped > 0);
            }
        }

        /// <summary>A blocked Apply returning after the join timeout still completes terminal and recorder cleanup.</summary>
        [Fact]
        public void TimedOutStopHandsCleanupToRenderThreadAfterBlockedApplyReturns() {
            FakeLogTarget target = new FakeLogTarget();
            FakeOutput output = new FakeOutput(false);
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.StopTimeout = TimeSpan.FromMilliseconds(10);
            options.CaptureConsole = true;
            StringWriter recording = new StringWriter();
            options.RecordWriter = recording;
            TextWriter original = Console.Out;
            using (ManualResetEventSlim entered = new ManualResetEventSlim())
            using (ManualResetEventSlim release = new ManualResetEventSlim())
            using (ManualResetEventSlim ended = new ManualResetEventSlim())
            using (LiveSession session = new LiveSession(target, options)) {
                target.Applying = e => { if (e.Operation == "block") { entered.Set(); release.Wait(TimeSpan.FromSeconds(1)); } };
                output.Ending = ended.Set;
                session.Start();
                session.Post(new LiveEvent("log", "block"));
                try {
                    Assert.True(entered.Wait(TimeSpan.FromSeconds(1)));
                    Console.Write("partial");
                    session.Stop();
                    Assert.Same(original, Console.Out);
                    Assert.IsType<TimeoutException>(session.LastError);
                } finally { release.Set(); }
                Assert.True(ended.Wait(TimeSpan.FromSeconds(1)));
                session.Stop();
                Assert.Equal(2, session.EventsApplied);
                Assert.Equal(2, LiveRecordingReader.Read(new StringReader(recording.ToString())).Count());
                Assert.Equal("partial", target.Events[1].Text);
                Assert.Equal(1, output.Calls.Count(c => c == "end"));
                Assert.Null(LiveSession.Current);
            }
        }

        /// <summary>The final drain happens on the stopping caller only after the render thread relinquishes ownership.</summary>
        [Fact]
        public void FinalDrainAllowsReentrantStopAndUsesStoppingCallerAsRequested() {
            FakeTarget target = new FakeTarget();
            FakeOutput output = new FakeOutput();
            using (ManualResetEventSlim frameEntered = new ManualResetEventSlim())
            using (ManualResetEventSlim releaseFrame = new ManualResetEventSlim())
            using (LiveSession session = new LiveSession(target, LiveSessionTests.Options(output))) {
                output.Frame = () => { frameEntered.Set(); releaseFrame.Wait(TimeSpan.FromSeconds(1)); };
                session.Start();
                Assert.True(frameEntered.Wait(TimeSpan.FromSeconds(1)));
                session.Post(new LiveEvent("work", "detail"));
                int stoppingThread = 0;
                int applyingThread = 0;
                target.Applying = e => { applyingThread = Thread.CurrentThread.ManagedThreadId; session.Stop(); };
                Thread stopper = new Thread(() => { stoppingThread = Thread.CurrentThread.ManagedThreadId; session.Stop(); }) { IsBackground = true };
                stopper.Start();
                try { Assert.True(SpinWait.SpinUntil(() => !session.IsRunning, 1000)); }
                finally { releaseFrame.Set(); }
                Assert.True(stopper.Join(1000));
                Assert.Equal(stoppingThread, applyingThread);
                Assert.Equal(1, session.EventsApplied);
                Assert.Equal(1, output.Calls.Count(c => c == "end"));
            }
        }

        /// <summary>Options finalized between construction and Start select the configured log destination.</summary>
        [Fact]
        public void LogTargetChangedBeforeStartIsUsedForCaptureAndAlias() {
            FakeLogTarget first = new FakeLogTarget("first");
            FakeLogTarget chosen = new FakeLogTarget("chosen");
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            options.CaptureConsole = true;
            using (LiveSession session = new LiveSession(new FakeContainer(first, chosen), options)) {
                options.LogTarget = "chosen";
                session.Run(s => { s.Write("alias"); Console.WriteLine("captured"); });
                Assert.Empty(first.Events);
                Assert.Equal(new[] { "alias", "captured" }, chosen.Events.Select(e => e.Text));
            }
        }

        /// <summary>UTF-8 recording output preserves isolated UTF-16 surrogate code units without replacement fallback.</summary>
        [Fact]
        public void Utf8RecordingPreservesIsolatedSurrogates() {
            string text = "high \ud800 low \udc00 pair 😀";
            using (MemoryStream bytes = new MemoryStream()) {
                using (StreamWriter writer = new StreamWriter(bytes, new UTF8Encoding(false, true), 1024, true))
                using (LiveRecorder recorder = new LiveRecorder(writer)) {
                    recorder.Write(new LiveEvent("work", "detail", text: text));
                }
                bytes.Position = 0;
                using (StreamReader reader = new StreamReader(bytes, Encoding.UTF8)) {
                    Assert.Equal(text, Assert.Single(LiveRecordingReader.Read(reader)).Text);
                }
            }
        }
    }
}
