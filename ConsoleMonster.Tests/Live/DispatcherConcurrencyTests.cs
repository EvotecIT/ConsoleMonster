using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class DispatcherConcurrencyTests {
        private readonly ITestOutputHelper _output;
        public DispatcherConcurrencyTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public async Task Stream_ManyProducersWriteWholeLinesInExactApplyOrder() {
            const int producers = 16;
            const int updates = 256;
            StringTerminalWriter writer = new StringTerminalWriter();
            TerminalProfile profile = TerminalProfile.Fixed(80, 20, ColorDepth.None, mode: OutputMode.Stream);
            FakeTarget target = new FakeTarget { Description = e => "event-" + e.Key + ":" + e.Value + " payload end" };
            using (LineOutput output = new LineOutput(writer, profile, false))
            using (LiveSession session = new LiveSession(target, new LiveSessionOptions {
                Output = output, Profile = profile, CaptureConsole = false, HandleCancelKey = false,
                StreamTimestamps = false, PrintFinalState = false
            })) {
                session.Start();
                await Observed(Task.WhenAll(Enumerable.Range(0, producers).Select(producer => Task.Run(() => {
                    string key = producer.ToString();
                    for (int i = 0; i < updates; i++) { session.Post(new LiveEvent("work", "set", key, value: i)); }
                }))));
                session.Stop();
                string[] lines = writer.ToString().Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string[] expected = target.Events.Select(e => "event-" + e.Key + ":" + e.Value + " payload end").ToArray();
                Assert.Equal(producers * updates, lines.Length);
                Assert.Equal(expected, lines);
                Assert.Equal(producers * updates, session.EventsApplied);
                Assert.Equal(session.EventsPosted, session.EventsApplied);
                Assert.Equal(0, session.EventsDropped);
                Assert.Equal(0, session.ApplyErrors);
                foreach (var producer in target.Events.GroupBy(e => e.Key)) {
                    Assert.Equal(Enumerable.Range(0, updates).Select(i => (double)i), producer.Select(e => e.Value));
                }
            }
        }

        [Fact]
        public async Task Flood_PostingFinishesWhileApplyIsBlocked_ThenEveryAcceptedUpdateDrains() {
            const int producers = 32;
            const int updates = 4096;
            TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ManualResetEventSlim release = new ManualResetEventSlim()) {
                int active = 0;
                int overlapping = 0;
                FakeTarget target = new FakeTarget();
                target.Applying = e => {
                    if (Interlocked.Increment(ref active) != 1) { Interlocked.Increment(ref overlapping); }
                    try {
                        if (e.Key == "block") {
                            entered.TrySetResult(true);
                            if (!release.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("Flood producer watchdog expired."); }
                        }
                    } finally { Interlocked.Decrement(ref active); }
                };
                using (LiveSession session = new LiveSession(target, LiveSessionTests.Options(new FakeOutput()))) {
                    session.Start();
                    session.Post(new LiveEvent("work", "set", "block"));
                    try {
                        await Observed(entered.Task);
                        long memoryBefore = GC.GetTotalMemory(true);
                        await Observed(Task.WhenAll(Enumerable.Range(0, producers).Select(producer => Task.Run(() => {
                            string key = producer.ToString();
                            for (int i = 0; i < updates; i++) { session.Post(new LiveEvent("work", "set", key, value: i)); }
                        }))));
                        long memoryAfter = GC.GetTotalMemory(true);
                        _output.WriteLine("FLOOD events={0} blockedBacklog={1} retainedManagedBytesDelta={2} managedBytesBefore={3} managedBytesAfter={4}; full-GC observations, not a fixed budget.",
                            producers * updates, session.EventsPosted - session.EventsApplied, memoryAfter - memoryBefore, memoryBefore, memoryAfter);
                        Assert.Equal(1 + producers * updates, session.EventsPosted);
                        Assert.Equal(0, session.EventsApplied);
                        Assert.Equal(0, session.EventsDropped);
                    } finally { release.Set(); }
                    session.Stop();
                    Assert.Equal(1 + producers * updates, target.Events.Count);
                    Assert.Equal(session.EventsPosted, session.EventsApplied);
                    Assert.Equal(0, session.EventsDropped);
                    Assert.Equal(0, session.ApplyErrors);
                    Assert.Equal(0, overlapping);
                    foreach (var producer in target.Events.Where(e => e.Key != "block").GroupBy(e => e.Key)) {
                        Assert.Equal(Enumerable.Range(0, updates).Select(i => (double)i), producer.Select(e => e.Value));
                    }
                }
            }
        }

        private static async Task Observed(Task task) {
            Assert.Same(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30))));
            await task;
        }
    }
}
