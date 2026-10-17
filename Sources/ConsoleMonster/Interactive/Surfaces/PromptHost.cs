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
            int width = _screen.Width;
            int height = _screen.Height;
            CellBuffer composite = new CellBuffer(width, height);
            if (_backdrop != null) {
                composite.Blit(_backdrop, 0, 0, Math.Min(_backdrop.Width, width), Math.Min(_backdrop.Height, height), 0, 0);
            }
            Region all = new Region(composite);
            Overlay.Dim(all, _context);
            int content = ContentWidth(frame);
            int boxWidth = Math.Min(width, content + 4);
            int boxHeight = Math.Min(height, frame.Height + 2);
            int x = Math.Max(0, (width - boxWidth) / 2);
            int y = Math.Max(0, (height - boxHeight) / 3);
            Region box = all.Sub(x, y, boxWidth, boxHeight);
            box.Fill(Style.Plain);
            DrawBorder(box, _context);
            composite.Blit(frame, 0, 0, Math.Min(content, boxWidth - 4), Math.Min(frame.Height, boxHeight - 2), x + 2, y + 1);
            Caret? moved = caret.HasValue ? new Caret(x + 2 + caret.Value.X, y + 1 + caret.Value.Y) : (Caret?)null;
            _screen.Present(composite, moved);
        }

        /// <summary>The width a prompt frame really uses (its widest row), at least 44 cells, so dialogs fit their content.</summary>
        internal static int ContentWidth(CellBuffer frame) {
            int used = 0;
            for (int y = 0; y < frame.Height; y++) {
                used = Math.Max(used, CellEncoder.ContentEnd(frame, y, ColorDepth.TrueColor));
            }
            return Math.Min(frame.Width, Math.Max(44, used + 1));
        }

        internal static void DrawBorder(Region box, RenderContext context) {
            BoxGlyphs glyphs = context.Glyphs.GetBox(BoxStyle.Rounded);
            Style border = context.Style(ThemeToken.Accent);
            int bw = box.Width;
            int bh = box.Height;
            box.Put(0, 0, glyphs.TopLeft, border);
            box.HorizontalLine(1, 0, bw - 2, glyphs.Horizontal, border);
            box.Put(bw - 1, 0, glyphs.TopRight, border);
            box.VerticalLine(0, 1, bh - 2, glyphs.Vertical, border);
            box.VerticalLine(bw - 1, 1, bh - 2, glyphs.Vertical, border);
            box.Put(0, bh - 1, glyphs.BottomLeft, border);
            box.HorizontalLine(1, bh - 1, bw - 2, glyphs.Horizontal, border);
            box.Put(bw - 1, bh - 1, glyphs.BottomRight, border);
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

        public void BeginLive() => _inner.Invalidate();

        public void RenderFrame(CellBuffer frame) => _inner.RenderFrame(frame);

        public void Invalidate() => _inner.Invalidate();

        public void EndLive() => _inner.Invalidate();

        public void Dispose() {
        }
    }
}
