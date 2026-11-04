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

        // pointer false drops mouse events (the opt-out): native Windows input reports them whether or not mouse
        // reporting is on.
        public static void Run(InteractiveView view, IInputSource input, IPromptSurface surface, RenderContext context, CancellationToken cancellationToken, bool pointer = true) {
            if (view == null) {
                throw new ArgumentNullException(nameof(view));
            }
            Stopwatch clock = Stopwatch.StartNew();
            long tick = 0;
            bool dirty = true;
            TimeSpan? lastFrame = null;
            AppEventLoop.Run(input, () => view.IsDone, () => {
                if (surface.Refresh()) { dirty = true; }
                if (view.Poll(clock.Elapsed)) { dirty = true; }
                if (!view.IsDone && (dirty || (view.IsAnimating && !context.ReducedMotion))
                    && (!context.ReducedMotion || !lastFrame.HasValue || clock.Elapsed - lastFrame.Value >= TimeSpan.FromSeconds(1))) {
                    Draw(view, surface, context, tick++, clock.Elapsed);
                    dirty = false;
                    lastFrame = clock.Elapsed;
                }
            }, e => {
                if (e.Kind == InputEventKind.Resize) { surface.Refresh(); }
                if (e.Kind == InputEventKind.Mouse) {
                    if (!pointer) { return; }
                    // Frames are drawn from their own first row; an inline frame starts lower in the window.
                    int? origin = PointerOrigin(surface);
                    if (origin == null || e.Row < origin.Value) { return; }
                    if (origin.Value != 0) { e = e.WithPosition(e.Column, e.Row - origin.Value); }
                }
                view.Handle(e);
                dirty = true;
            }, cancellationToken, () => view.IsAnimating || view.CurrentToast != null ? AnimatedWait : IdleWait);
        }

        /// <summary>
        /// Turns on mouse reporting for a view when its surface can map pointer rows to its frames (the full screen, or an
        /// inline area on Windows) and the options allow it. Null leaves the mouse to the terminal.
        /// </summary>
        internal static IDisposable? OpenMouse(InteractiveOptions options, Capabilities.TerminalProfile profile, IInputSource input, IPromptSurface surface, bool customSurface) {
            bool locatable = surface is ScreenSurface || surface is InlineSurface inline && inline.CanLocate;
            return locatable ? MouseReporting.Open(options.ResolveMouse(), options, profile, input, customSurface) : null;
        }

        // The window row of a frame's first row, or null when it is unknown. Recorded surfaces (tests, captures) use frame
        // coordinates.
        internal static int? PointerOrigin(IPromptSurface surface) {
            switch (surface) {
                case ScreenSurface _: case RecordingSurface _: return 0;
                case InlineSurface inline: return inline.PointerOrigin;
                default: return null;
            }
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
