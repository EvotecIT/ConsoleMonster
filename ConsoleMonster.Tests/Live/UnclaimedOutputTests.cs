using System;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class UnclaimedOutputTests {
        [Theory]
        [InlineData(LiveMode.FullScreen)]
        [InlineData(LiveMode.Inline)]
        public async Task StreamWithoutLog_WritesCapturedAndPostedLinesBeforeStopInApplyOrder(LiveMode mode) {
            FakeOutput output = new FakeOutput(false);
            TaskCompletionSource<bool> applied = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            FakeTarget fence = new FakeTarget { Applying = e => applied.TrySetResult(true) };
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.Mode = mode;
            options.CaptureConsole = true;
            using (LiveSession session = new LiveSession(fence, options)) {
                session.Start();
                Console.WriteLine("console-line");
                session.Write("session-line");
                session.Post(new LiveEvent("$log", "write", text: "[pass]host-line[/]", isMarkup: true));
                session.Post(new LiveEvent("work", "fence"));
                await Observed(applied.Task);
                Assert.True(session.IsRunning);
                Assert.Equal(new[] { "console-line", "session-line", "host-line" }, output.Lines);
                Assert.DoesNotContain("end", output.Calls);
                session.Stop();
                Assert.Equal(0, session.EventsDropped);
                Assert.Equal(session.EventsPosted, session.EventsApplied);
            }
        }

        [Fact]
        public async Task FullScreenWithoutLog_ShowsABoundedCaptureTailAndRetainsScrollback() {
            FakeOutput output = new FakeOutput();
            TaskCompletionSource<string> visible = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.Profile = TerminalProfile.Fixed(50, 12, ColorDepth.None);
            options.WindowSource = () => new[] { 0, 0, 50, 12 };
            options.CaptureConsole = true;
            using (LiveSession session = new LiveSession(new TextBlock("working"), options)) {
                output.Frame = () => {
                    string text = session.Snapshot()!.ToPlainText();
                    if (text.Contains("console-line") && text.Contains("host-line") && text.Contains("warning-line")) { visible.TrySetResult(text); }
                };
                session.Start();
                Console.WriteLine("console-line");
                session.Post(new LiveEvent("$log", "write", text: "[pass]host-line[/]", state: "host", isMarkup: true));
                session.Write("warning-line", "warn");
                await Observed(visible.Task);
                Assert.True(session.IsRunning);
                TaskCompletionSource<string> tail = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                output.Frame = () => {
                    string text = session.Snapshot()!.ToPlainText();
                    if (text.Contains("capture-19")) { tail.TrySetResult(text); }
                };
                for (int i = 0; i < 20; i++) { session.Write("capture-" + i.ToString("D2")); }
                string frame = await Observed(tail.Task);
                Assert.Contains("working", frame);
                Assert.DoesNotContain("capture-00", frame);
                Assert.Equal(4, frame.Split('\n').Count(line => line.Contains("capture-")));
                session.Stop();
                Assert.Equal(new[] { "console-line", "host-line", "warning-line" }.Concat(Enumerable.Range(0, 20).Select(i => "capture-" + i.ToString("D2"))), output.Lines);
                Assert.Equal(0, session.EventsDropped);
                Assert.Equal(23, session.EventsPosted);
                Assert.Equal(session.EventsPosted, session.EventsApplied);
            }
        }

        private static async Task<T> Observed<T>(Task<T> task) {
            Assert.Same(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30))));
            return await task;
        }
    }
}
