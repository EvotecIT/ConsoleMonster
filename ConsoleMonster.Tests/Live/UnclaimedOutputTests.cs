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
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnclaimedInvalidMarkup_IsLiteralAndDoesNotStopLaterEvents(bool live) {
            bool strict = ConsoleMonster.Text.Markup.Strict;
            try {
                ConsoleMonster.Text.Markup.Strict = true;
                FakeOutput output = new FakeOutput(live);
                TaskCompletionSource<bool> applied = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                FakeTarget fence = new FakeTarget { Applying = _ => applied.TrySetResult(true) };
                using (LiveSession session = new LiveSession(fence, LiveSessionTests.Options(output))) {
                    session.Start();
                    session.Post(new LiveEvent("$log", "write", text: "[pass]broken", isMarkup: true));
                    session.Write("later-line");
                    session.Post(new LiveEvent("work", "fence"));
                    await Observed(applied.Task);
                    Assert.True(session.IsRunning);
                    session.Stop();
                    Assert.Equal(new[] { "[pass]broken", "later-line" }, output.Lines);
                    Assert.Equal(1, session.ApplyErrors);
                    Assert.IsType<FormatException>(session.LastError);
                    Assert.Equal(0, session.EventsDropped);
                    Assert.Equal(3, session.EventsApplied);
                }
            } finally { ConsoleMonster.Text.Markup.Strict = strict; }
        }

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
        public async Task NoLog_MultilinePlainAndColouredMessagesKeepTheirLines() {
            foreach (bool live in new[] { false, true }) {
                foreach (string newline in new[] { "\n", "\r\n" }) {
                    FakeOutput output = new FakeOutput(live);
                    TaskCompletionSource<bool> applied = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    TaskCompletionSource<bool> visible = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    FakeTarget fence = new FakeTarget { Applying = _ => applied.TrySetResult(true) };
                    LiveSessionOptions options = LiveSessionTests.Options(output);
                    options.Profile = TerminalProfile.Fixed(60, 18, ColorDepth.None);
                    options.WindowSource = () => new[] { 0, 0, 60, 18 };
                    string[] expected = { "host-first", "host-second", "warning-first", "warning-second", "colour-first", "colour-second" };
                    using (LiveSession session = new LiveSession(fence, options)) {
                        output.Frame = () => {
                            string frame = session.Snapshot()!.ToPlainText();
                            if (expected.All(frame.Contains)) { visible.TrySetResult(true); }
                        };
                        session.Start();
                        session.Post(new LiveEvent("$log", "write", text: "host-first" + newline + "host-second", status: "host"));
                        session.Post(new LiveEvent("$log", "write", text: "warning-first" + newline + "warning-second", status: "warn"));
                        // HostColors emits a balanced styled segment per physical line.
                        session.Post(new LiveEvent("$log", "write", text: "[pass]colour-first[/]" + newline + "[pass]colour-second[/]", isMarkup: true));
                        session.Post(new LiveEvent("work", "fence"));
                        await Observed(applied.Task);
                        if (live) { await Observed(visible.Task); }
                        else { Assert.Equal(expected, output.Lines); }
                        session.Stop();
                        Assert.Equal(expected, output.Lines);
                        Assert.Equal(0, session.EventsDropped);
                        Assert.Equal(session.EventsPosted, session.EventsApplied);
                    }
                }
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
                session.Post(new LiveEvent("$log", "write", text: "[pass]host-line[/]", status: "host", isMarkup: true));
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
