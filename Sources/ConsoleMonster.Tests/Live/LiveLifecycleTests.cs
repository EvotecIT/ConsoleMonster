using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class LiveLifecycleTests {
        [Fact]
        public void CaptureFlushesPartialLinesAndRestoresBothWritersWhenWorkThrows() {
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;
            FakeLogTarget target = new FakeLogTarget();
            FakeOutput output = new FakeOutput(false);
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.CaptureConsole = true;
            using (LiveSession session = new LiveSession(target, options)) {
                InvalidOperationException expected = new InvalidOperationException("work");
                Exception actual = Assert.Throws<InvalidOperationException>(() => session.Run(s => {
                    Console.Out.Write("one\r");
                    Console.Out.Write("\ntwo\npartial");
                    Console.Error.WriteLine("error");
                    throw expected;
                }));
                Assert.Same(expected, actual);
                Assert.Same(originalOut, Console.Out);
                Assert.Same(originalError, Console.Error);
                Assert.Equal(new[] { "one", "two", "partial" }, target.Events.Where(e => e.State == "out").Select(e => e.Text));
                Assert.Equal("error", Assert.Single(target.Events, e => e.State == "err").Text);
                Assert.All(target.Events, e => Assert.Equal("write", e.Operation));
                Assert.False(session.IsRunning);
                Assert.Equal(1, output.Calls.Count(c => c == "end"));
            }
        }

        [Fact]
        public void CaptureWithoutLogTargetRetainsOutputForScrollback() {
            FakeOutput output = new FakeOutput(false);
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.CaptureConsole = true;
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Run(s => {
                    Console.WriteLine("unrouted out");
                    Console.Error.Write("unrouted partial");
                });
                Assert.Equal(new[] { "unrouted out", "unrouted partial" }, output.Lines);
                // Unrouted lines come first, then the final state of the root (PrintFinalState defaults to true).
                Assert.Equal(new[] { "begin", "end", "line", "line", "block" }, output.Calls);
            }
        }

        [Fact]
        public void ConcurrentConsoleLinesAreCapturedExactlyOnce() {
            FakeLogTarget log = new FakeLogTarget();
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            options.CaptureConsole = true;
            using (LiveSession session = new LiveSession(log, options)) {
                session.Run(s => Parallel.For(0, 1000, i => Console.WriteLine("line-" + i)));
                Assert.Equal(1000, log.Events.Count);
                Assert.Equal(1000, log.Events.Select(e => e.Text).Distinct().Count());
            }
        }

        [Fact]
        public void BeginFailureRestoresConsoleAndRecorderAndRethrows() {
            TextWriter original = Console.Out;
            FakeOutput output = new FakeOutput { Beginning = () => throw new IOException("begin") };
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.CaptureConsole = true;
            StringWriter recording = new StringWriter();
            options.RecordWriter = recording;
            using (LiveSession session = new LiveSession(new FakeLogTarget(), options)) {
                Assert.Equal("begin", Assert.Throws<IOException>(session.Start).Message);
                Assert.Same(original, Console.Out);
                Assert.False(session.IsRunning);
                Assert.Null(LiveSession.Current);
                Assert.Contains("end", output.Calls);
                recording.Write("still open");
                Assert.Contains("still open", recording.ToString());
            }
        }

        [Fact]
        public void RenderFailureCancelsAndRestoresWithoutKillingProcess() {
            TextWriter original = Console.Out;
            FakeOutput output = new FakeOutput();
            using (ManualResetEventSlim ended = new ManualResetEventSlim()) {
                output.Frame = () => throw new IOException("render");
                output.Ending = ended.Set;
                LiveSessionOptions options = LiveSessionTests.Options(output);
                options.CaptureConsole = true;
                using (LiveSession session = new LiveSession(new FakeLogTarget(), options)) {
                    session.Start();
                    Assert.True(ended.Wait(TimeSpan.FromSeconds(1)));
                    session.Stop();
                    Assert.True(session.Cancellation.IsCancellationRequested);
                    Assert.Same(original, Console.Out);
                    Assert.Equal("render", session.LastError!.Message);
                }
            }
        }

        [Fact]
        public void StopFromApplyDefersCleanupWithoutJoiningItself() {
            FakeTarget target = new FakeTarget();
            FakeOutput output = new FakeOutput(false);
            using (ManualResetEventSlim ended = new ManualResetEventSlim())
            using (LiveSession session = new LiveSession(target, LiveSessionTests.Options(output))) {
                output.Ending = ended.Set;
                target.Applying = e => session.Stop();
                session.Start();
                session.Post(new LiveEvent("work", "detail"));
                Assert.True(ended.Wait(TimeSpan.FromSeconds(1)));
                session.Stop();
                Assert.Equal(1, session.EventsApplied);
                Assert.True(session.Cancellation.IsCancellationRequested);
            }
        }

        [Fact]
        public async Task AsyncWorkExceptionRestoresAndCallerCancellationReachesWork() {
            TextWriter original = Console.Out;
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            options.CaptureConsole = true;
            using (LiveSession session = new LiveSession(new FakeLogTarget(), options)) {
                await Assert.ThrowsAsync<InvalidOperationException>(() => session.RunAsync(async (s, ct) => {
                    await Task.Yield();
                    Console.WriteLine("async line");
                    throw new InvalidOperationException("async work");
                }));
                Assert.Same(original, Console.Out);
            }
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            using (LiveSession session = new LiveSession(new FakeTarget(), LiveSessionTests.Options(new FakeOutput(false)))) {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.RunAsync(async (s, ct) => {
                    cancellation.Cancel();
                    await Task.Delay(1000, ct);
                }, cancellation.Token));
                Assert.True(session.Cancellation.IsCancellationRequested);
                Assert.False(session.IsRunning);
            }
        }

        [Fact]
        public void SummaryIsWrittenToSameOutputAfterEndLive() {
            FakeOutput output = new FakeOutput();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.Summary = new FakeTarget("summary");
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Run(s => s.Post(new LiveEvent("work", "advance")));
                string[] calls = output.Calls.ToArray();
                Assert.True(Array.IndexOf(calls, "end") < Array.IndexOf(calls, "block"));
                Assert.DoesNotContain("dispose", calls); // Borrowed output remains caller-owned.
            }
        }

        [Fact]
        public void RealLineOutputNarratesWithoutEscapeSequences() {
            StringTerminalWriter writer = new StringTerminalWriter();
            FakeOutput fake = new FakeOutput(false);
            using (LineOutput output = new LineOutput(writer, fake.Profile.WithMode(global::ConsoleMonster.Capabilities.OutputMode.Plain), false)) {
                LiveSessionOptions options = LiveSessionTests.Options(fake);
                options.Output = output;
                using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                    session.Run(s => s.Post(new LiveEvent("work", "detail", text: "status Δ")));
                    Assert.Contains("status Δ", writer.ToString());
                    Assert.DoesNotContain("\u001b", writer.ToString(), StringComparison.Ordinal);
                }
            }
        }
    }
}
