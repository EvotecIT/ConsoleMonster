using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Lets a running app (a menu on the alternate screen) host prompts that its actions ask: instead of drawing below the
    /// cursor (which would land in the middle of the app), prompts appear in a box in front of the app's last frame.
    /// </summary>
    internal static class PromptHost {
        [ThreadStatic]
        private static Func<RenderContext, IPromptSurface?>? _current;

        public static Func<RenderContext, IPromptSurface?>? Current {
            get => _current;
            set => _current = value;
        }

        /// <summary>The surface for a prompt asked now: over a live dashboard, over an app, or null for the caller's default.</summary>
        public static IPromptSurface? Resolve(RenderContext context) {
            return Live.LivePromptSurface.TryCreate(context) ?? _current?.Invoke(context);
        }
    }

    /// <summary>Shows prompts in a box over a screen surface's last frame (the app stays visible, dimmed, behind it).</summary>
    internal sealed class BoxedSurface : IPromptSurface {
        private readonly ScreenSurface _screen;
        private readonly CellBuffer? _backdrop;
        private readonly RenderContext _context;

        public BoxedSurface(ScreenSurface screen, CellBuffer? backdrop, RenderContext context) {
            _screen = screen;
            _backdrop = backdrop;
            _context = context;
        }

        public int Width => Math.Max(20, Math.Min(_screen.Width - 6, 96));

        public int Height => Math.Max(3, _screen.Height - 4);

        public bool IsInline => true;

        public bool Refresh() => _screen.Refresh();

        public void Present(CellBuffer frame, Caret? caret) {
            CellBuffer composite = DialogChrome.Compose(_backdrop, _screen.Width, _screen.Height, frame, caret, _context, out Caret? moved);
            _screen.Present(composite, moved);
        }
        public void Complete(IReadOnlyList<StyledLine>? summary) {
            _screen.Invalidate();
        }

        public void Dispose() {
        }
    }

    /// <summary>
    /// The app screen's output lent to a live dashboard started from a menu: frames go to the same alternate screen and
    /// starting or ending the session never leaves it.
    /// </summary>
    internal sealed class BorrowedOutput : ITerminalOutput {
        private readonly ITerminalOutput _inner;

        public BorrowedOutput(ITerminalOutput inner) {
            _inner = inner;
        }

        public TerminalProfile Profile => _inner.Profile;

        public bool SupportsLiveFrames => true;

        public void WriteBlock(CellBuffer buffer) {
        }

        public void WriteLine(StyledLine line) {
        }

        public void WriteInline(StyledLine line) {
        }

        // The screen already shows the app; frames continue as diffs against it, so starting and ending a run never clears.
        public void BeginLive() {
        }

        public void RenderFrame(CellBuffer frame) => _inner.RenderFrame(frame);

        public void Invalidate() => _inner.Invalidate();

        public void EndLive() {
        }

        public void Dispose() {
        }
    }
}
