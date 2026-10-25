using System;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Runs a view: draw, wait for input (short waits while something animates, so spinners turn and toasts expire),
    /// apply every queued event, draw once more. Bursts of keys (key repeat, fast typing) are applied together before the
    /// next frame, so the screen keeps up without drawing intermediate states.
    /// </summary>
    internal static class InteractionLoop {
        private static readonly TimeSpan AnimatedWait = TimeSpan.FromMilliseconds(80);
        private static readonly TimeSpan IdleWait = TimeSpan.FromMilliseconds(250);

        public static void Run(InteractiveView view, IInputSource input, IPromptSurface surface, RenderContext context, CancellationToken cancellationToken) {
            if (view == null) {
                throw new ArgumentNullException(nameof(view));
            }
            Stopwatch clock = Stopwatch.StartNew();
            long tick = 0;
            bool dirty = true;
            AppEventLoop.Run(input, () => view.IsDone, () => {
                if (surface.Refresh()) { dirty = true; }
                if (view.Poll(clock.Elapsed)) { dirty = true; }
                if (!view.IsDone && (dirty || view.IsAnimating)) {
                    Draw(view, surface, context, tick++, clock.Elapsed);
                    dirty = false;
                }
            }, e => {
                if (e.Kind == InputEventKind.Resize) { surface.Refresh(); }
                else { view.Handle(e); }
                dirty = true;
            }, cancellationToken, () => view.IsAnimating || view.CurrentToast != null ? AnimatedWait : IdleWait);
        }

        public static void Draw(InteractiveView view, IPromptSurface surface, RenderContext context, long tick, TimeSpan elapsed) {
            RenderContext frameContext = context.WithClock(tick, elapsed);
            int width = Math.Max(1, surface.Width);
            int height = Math.Max(1, surface.Height);
            ViewFrame frame = new ViewFrame(frameContext, width, height, surface.IsInline, elapsed, view.Caret);
            view.Caret.Clear();
            IWidget root = view.BuildFrame(frame);
            CellBuffer buffer;
            if (surface.IsInline) {
                Size size = root.Measure(new Constraints(width, height), frameContext);
                buffer = new CellBuffer(width, Math.Max(1, Math.Min(height, size.Height)));
                root.Render(new Region(buffer), frameContext);
            } else {
                buffer = FrameComposer.Compose(root, width, height, frameContext);
            }
            view.LastRendered = buffer;
            surface.Present(buffer, view.Caret.Caret);
        }
    }
}
