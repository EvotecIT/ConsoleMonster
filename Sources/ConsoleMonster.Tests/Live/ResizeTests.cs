using System;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Resize handling in live sessions: coalesced full redraws, viewport moves, and render errors that do not end the session.</summary>
    [Collection("Live sessions")]
    public sealed class ResizeTests {
        [Fact]
        public void Resize_RapidDrag_RedrawsAtMostEveryHundredMillisecondsAndEndsAtTheFinalSize() {
            FakeOutput output = new FakeOutput();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            Stopwatch clock = Stopwatch.StartNew();
            int calls = 0;
            options.WindowSource = () => {
                calls++;
                int width = clock.ElapsedMilliseconds < 600 ? 60 + (calls % 40) : 120;
                return new[] { 0, 0, width, 30 };
            };
            long resizes;
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Start();
                Thread.Sleep(1100);
                resizes = session.Resizes;
            }
            // 600 ms of changing sizes at 60 fps would be ~36 redraws; coalescing keeps it near one per 100 ms.
            Assert.InRange(resizes, 2, 10);
            Assert.Equal(120, output.LastFrameWidth);
        }

        [Fact]
        public void Resize_ViewportScrolled_FullRedraw() {
            FakeOutput output = new FakeOutput();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            int top = 0;
            options.WindowSource = () => new[] { 0, Volatile.Read(ref top), 80, 24 };
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Start();
                Thread.Sleep(150);
                int before = Volatile.Read(ref output.Invalidations);
                Volatile.Write(ref top, 40);
                Thread.Sleep(150);
                Assert.True(Volatile.Read(ref output.Invalidations) > before);
            }
        }

        [Fact]
        public void RenderError_WidgetThrows_SessionKeepsRunning() {
            FakeOutput output = new FakeOutput();
            FakeTarget target = new FakeTarget();
            target.Rendering = context => throw new InvalidOperationException("too small");
            using (LiveSession session = new LiveSession(target, LiveSessionTests.Options(output))) {
                session.Start();
                Thread.Sleep(200);
                Assert.True(session.RenderErrors > 0);
                Assert.True(session.IsRunning);
                Assert.True(output.Frames > 1);
            }
        }
    }
}
