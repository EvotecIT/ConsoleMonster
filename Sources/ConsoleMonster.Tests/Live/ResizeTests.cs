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
        public void ResizeCoalescer_RapidDrag_RedrawsAtMostEveryHundredMillisecondsAndEndsAtTheFinalSize() {
            // A drag that changes the size on every 60 fps frame for 600 ms, then holds 120 columns. Same decision as
            // RenderFinal: a frame that sees a new size redraws only when the coalescer allows it.
            ResizeCoalescer coalescer = new ResizeCoalescer(TimeSpan.FromMilliseconds(100));
            int frameWidth = 80;
            int redraws = 0;
            for (int frame = 0; frame * 1000.0 / 60 < 1100; frame++) {
                TimeSpan now = TimeSpan.FromMilliseconds(frame * 1000.0 / 60);
                int width = now.TotalMilliseconds < 600 ? 60 + (frame % 40) : 120;
                if (width != frameWidth && coalescer.TryRedraw(now)) {
                    frameWidth = width;
                    redraws++;
                }
            }
            // About 36 size changes; coalescing allows one redraw per 100 ms plus the settled final size.
            Assert.InRange(redraws, 6, 8);
            Assert.Equal(120, frameWidth);
        }

        [Fact]
        public void Resize_RapidDrag_LiveSessionEndsAtTheFinalSize() {
            FakeOutput output = new FakeOutput();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            Stopwatch clock = Stopwatch.StartNew();
            int calls = 0;
            options.WindowSource = () => {
                calls++;
                int width = clock.ElapsedMilliseconds < 300 ? 60 + (calls % 40) : 120;
                return new[] { 0, 0, width, 30 };
            };
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Start();
                // Wall-clock independent: wait for the outcome, not for a frame count.
                Assert.True(SpinWait.SpinUntil(() => clock.ElapsedMilliseconds > 300 && output.LastFrameWidth == 120, TimeSpan.FromSeconds(10)));
                Assert.True(session.Resizes >= 1);
            }
        }
        [Fact]
        public void Resize_ViewportScrolled_FullRedraw() {
            FakeOutput output = new FakeOutput();
            LiveSessionOptions options = LiveSessionTests.Options(output);
            int top = 0;
            options.WindowSource = () => new[] { 0, Volatile.Read(ref top), 80, 24 };
            using (LiveSession session = new LiveSession(new FakeTarget(), options)) {
                session.Start();
                Assert.True(SpinWait.SpinUntil(() => output.Frames >= 2, TimeSpan.FromSeconds(10)));
                int before = Volatile.Read(ref output.Invalidations);
                Volatile.Write(ref top, 40);
                Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref output.Invalidations) > before, TimeSpan.FromSeconds(10)));
            }
        }

        [Fact]
        public void RenderError_WidgetThrows_SessionKeepsRunning() {
            FakeOutput output = new FakeOutput();
            FakeTarget target = new FakeTarget();
            target.Rendering = context => throw new InvalidOperationException("too small");
            using (LiveSession session = new LiveSession(target, LiveSessionTests.Options(output))) {
                session.Start();
                Assert.True(SpinWait.SpinUntil(() => session.RenderErrors > 0 && output.Frames > 1, TimeSpan.FromSeconds(10)));
                Assert.True(session.IsRunning);
            }
        }
    }
}
