using System;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class LiveSessionFlushTests {
        [Theory] [InlineData(1, false)] [InlineData(3, false)] [InlineData(3, true)]
        public async System.Threading.Tasks.Task PhysicalWriteFailureCompletesEveryAcceptedFlushBarrierAndRetainsItsCause(int barrierCount, bool batch) {
            var failure = new IOException("Scrollback sink failed");
            var writer = new FailingWriter(failure);
            var profile = TerminalProfile.Fixed(80, 24, ColorDepth.None);
            ITerminalOutput output = batch ? (ITerminalOutput)new InlineOutput(writer, profile, false)
                : new FakeOutput { WritingLine = line => throw failure };
            var target = new FakeTarget();
            using (var entered = new ManualResetEventSlim())
            using (var proceed = new ManualResetEventSlim())
            using (var session = new LiveSession(target, new LiveSessionOptions {
                Output = output, Profile = profile, CaptureConsole = false, HandleCancelKey = false,
                WindowSource = () => new[] { 0, 0, 80, 24 }
            })) {
                target.Applying = item => {
                    entered.Set();
                    if (!proceed.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException("The producer did not release the renderer."); }
                };
                session.Start();
                try {
                    session.Post(new LiveEvent("work", "advance"));
                    Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
                    for (int i = 0; i < barrierCount; i++) {
                        Assert.True(session.TryWriteFallback(new ConsoleWriteBlock(new[] { StyledLine.From("finished item " + i) }, true)));
                    }
                    var flush = System.Threading.Tasks.Task.WhenAll(Enumerable.Range(0, barrierCount).Select(i => session.QueueFlush()));
                    writer.Armed = true;
                    proceed.Set();
                    Assert.Same(flush, await System.Threading.Tasks.Task.WhenAny(flush, System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5))));
                    Assert.All(await flush, result => Assert.False(result));
                    Assert.Same(failure, session.Failure);
                    Assert.True(session.Cancellation.IsCancellationRequested);
                } finally { proceed.Set(); }
            }
        }

        private sealed class FailingWriter : ITerminalWriter {
            private readonly Exception _failure;
            internal bool Armed;
            internal FailingWriter(Exception failure) { _failure = failure; }
            public bool IsConsole => false;
            public void Write(string text) {
                if (Armed) { Armed = false; throw _failure; }
            }
            public void Flush() { }
            public void Dispose() { }
        }
    }
}
