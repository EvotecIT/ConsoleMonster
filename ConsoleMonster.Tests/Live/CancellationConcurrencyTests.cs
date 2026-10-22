using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class CancellationConcurrencyTests {
        [Fact]
        public async Task FirstCancellation_DropsLaterPostsAndRestoresWritersExactlyOnce() {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            FakeOutput output = new FakeOutput();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            options.CaptureConsole = true;
            TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                Task run = session.RunAsync(async (s, token) => {
                    s.Post(new LiveEvent("work", "set", text: "before cancellation"));
                    entered.TrySetResult(true);
                    await release.Task;
                    Assert.True(token.IsCancellationRequested);
                }, cancellation.Token);
                try {
                    Assert.Same(entered.Task, await Task.WhenAny(entered.Task, Task.Delay(TimeSpan.FromSeconds(30))));
                    cancellation.Cancel();
                    session.Post(new LiveEvent("work", "set", text: "after cancellation"));
                    Assert.Equal(1, session.EventsDropped);
                } finally { release.TrySetResult(true); }
                Assert.Same(run, await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30))));
                await run;
                Assert.Equal(1, session.EventsPosted);
                Assert.Equal(1, session.EventsApplied);
                Assert.False(session.IsRunning);
                Assert.Same(originalOut, Console.Out);
                Assert.Same(originalError, Console.Error);
                Assert.Equal(1, output.Calls.Count(call => call == "end"));
                session.Stop();
                Assert.Equal(1, output.Calls.Count(call => call == "end"));
            }
        }
    }
}
