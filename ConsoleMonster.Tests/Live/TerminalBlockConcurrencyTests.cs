using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class TerminalBlockConcurrencyTests {
        private static TerminalProfile Profile => TerminalProfile.Fixed(60, 12, ColorDepth.None, mode: OutputMode.Stream);

        [Fact]
        public void TerminalWrite_ManyProducersKeepEveryMultilineBlockWhole() {
            StringTerminalWriter writer = new StringTerminalWriter();
            Parallel.For(0, 16, producer => {
                for (int sequence = 0; sequence < 32; sequence++) {
                    string key = producer + ":" + sequence;
                    Terminal.Write(new TextBlock("begin-" + key + "\nbody-" + key + "\nend-" + key), Profile, writer);
                }
            });
            string[] lines = writer.ToString().Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimEnd()).ToArray();
            Assert.Equal(16 * 32 * 3, lines.Length);
            Assert.Equal(16 * 32, lines.Where((s, i) => i % 3 == 0).Distinct().Count());
            for (int i = 0; i < lines.Length; i += 3) {
                Assert.StartsWith("begin-", lines[i]);
                string key = lines[i].Substring(6);
                Assert.Equal("body-" + key, lines[i + 1]);
                Assert.Equal("end-" + key, lines[i + 2]);
            }
        }

        [Fact]
        public async Task SessionWrites_AdmitWholeMultilineBlocksBeforeAnotherProducer() {
            FakeOutput output = new FakeOutput();
            using (Terminal.Use(ConsoleTarget.Instance))
            using (LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(output)))
            using (Barrier enumerating = new Barrier(2)) {
                session.Start();
                Terminal.ClaimConsole(session);
                Task[] tasks = Enumerable.Range(0, 2).Select(producer => Task.Factory.StartNew(() => {
                    Terminal.WriteLines(new InterruptedLines("p" + producer, () => {
                        if (!enumerating.SignalAndWait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("Producer did not enumerate its write."); }
                    }), profile: Profile);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
                Task all = Task.WhenAll(tasks);
                Assert.Same(all, await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(30))));
                await all;
                session.Stop();
                string[] lines = output.Lines.ToArray();
                Assert.Equal(4, lines.Length);
                Assert.Equal(new[] { "p0-first", "p0-last", "p1-first", "p1-last" }.OrderBy(s => s), lines.OrderBy(s => s));
                Assert.EndsWith("-first", lines[0]);
                Assert.Equal(lines[0].Replace("-first", "-last"), lines[1]);
                Assert.Equal(lines[2].Replace("-first", "-last"), lines[3]);
            }
        }

        [Fact]
        public void StopDuringWritePreparation_DoesNotQueueAPrefixBeforeFallback() {
            FakeOutput output = new FakeOutput();
            using (Terminal.Use(ConsoleTarget.Instance))
            using (LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(output))) {
                session.Start();
                Terminal.ClaimConsole(session);
                Terminal.WriteLines(new InterruptedLines("value", session.Stop), profile: Profile);
                Assert.Empty(output.Lines);
            }
        }

        [Fact]
        public async Task ShutdownFallback_KeepsItsBlockWholeWhileAnAcceptedBlockIsStillDraining() {
            FakeOutput output = new FakeOutput(false);
            TaskCompletionSource<bool> firstLine = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> afterBlock = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> fallbackReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ManualResetEventSlim releaseLine = new ManualResetEventSlim())
            using (ManualResetEventSlim releaseStop = new ManualResetEventSlim())
            using (Terminal.Use(ConsoleTarget.Instance)) {
                output.WritingLine = line => {
                    if (line.PlainText == "accepted-first") {
                        firstLine.TrySetResult(true);
                        if (!releaseLine.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("Accepted line was not released."); }
                    }
                };
                FakeTarget fence = new FakeTarget { Applying = _ => {
                    afterBlock.TrySetResult(true);
                    if (!releaseStop.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("Final drain was not released."); }
                } };
                LiveSessionOptions options = LiveSessionTests.Options(output);
                options.StopTimeout = TimeSpan.FromSeconds(30);
                using (LiveSession session = new LiveSession(fence, options)) {
                    session.Start();
                    Terminal.ClaimConsole(session);
                    Terminal.WriteLines(new[] { StyledLine.From("accepted-first"), StyledLine.From("accepted-last") }, profile: Profile);
                    session.Post(new LiveEvent("work", "hold-stop"));
                    Task? stop = null;
                    Task? fallback = null;
                    try {
                        await Observed(firstLine.Task);
                        stop = Task.Run(() => session.Stop());
                        Assert.True(session.Cancellation.WaitHandle.WaitOne(TimeSpan.FromSeconds(30)));
                        fallback = Task.Run(() => {
                            fallbackReady.TrySetResult(true);
                            Terminal.WriteLines(new[] { StyledLine.From("fallback-first"), StyledLine.From("fallback-last") }, profile: Profile);
                        });
                        await Observed(fallbackReady.Task);
                        releaseLine.Set();
                        await Observed(afterBlock.Task);
                        await Observed(fallback);
                        Assert.False(stop.IsCompleted);
                        Assert.Equal(new[] { "accepted-first", "accepted-last", "fallback-first", "fallback-last" }, output.Lines);
                    } finally {
                        releaseLine.Set();
                        releaseStop.Set();
                        if (fallback != null) { await Observed(fallback); }
                        if (stop != null) { await Observed(stop); }
                    }
                    Assert.Equal(0, session.EventsDropped);
                    Assert.Equal(session.EventsPosted, session.EventsApplied);
                }
            }
        }

        private static async Task Observed(Task task) {
            Assert.Same(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30))));
            await task;
        }

        [Fact]
        public void SessionWrite_PreservesTheFinalInlineLine() {
            FakeOutput output = new FakeOutput();
            using (Terminal.Use(ConsoleTarget.Instance))
            using (LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(output))) {
                session.Start();
                Terminal.ClaimConsole(session);
                Terminal.WriteLines(new[] { StyledLine.From("first"), StyledLine.From("open") }, false, Profile);
                session.Stop();
                Assert.Equal(new[] { "first", "open" }, output.Lines);
                Assert.Contains("inline", output.Calls);
            }
        }

        private sealed class InterruptedLines : IReadOnlyList<StyledLine> {
            private readonly string _key;
            private readonly Action _between;
            internal InterruptedLines(string key, Action between) { _key = key; _between = between; }
            public int Count => 2;
            public StyledLine this[int index] => StyledLine.From(_key + (index == 0 ? "-first" : "-last"));
            public IEnumerator<StyledLine> GetEnumerator() {
                yield return this[0];
                _between();
                yield return this[1];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
