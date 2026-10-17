using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Live {
    /// <summary>
    /// Shows a prompt over a running live dashboard: drawing pauses, the last frame stays as a dimmed backdrop, and the
    /// prompt appears in a bordered box in front of it. When the prompt ends, the dashboard resumes and redraws with
    /// everything that happened meanwhile.
    /// </summary>
    internal sealed class LivePromptSurface : IPromptSurface {
        private readonly LivePause _pause;
        private readonly RenderContext _context;
        private int _width;
        private int _height;

        private LivePromptSurface(LivePause pause, RenderContext context) {
            _pause = pause;
            _context = context;
            Refresh();
        }

        public static IPromptSurface? TryCreate(RenderContext context) {
            LiveSession? session = LiveSession.Current;
            if (session == null || !session.CanOverlay) {
                return null;
            }
            return new LivePromptSurface(session.Pause(), context);
        }

        public int Width => Math.Max(20, Math.Min(_width - 6, 96));

        public int Height => Math.Max(3, _height - 4);

        public bool IsInline => true;

        public bool Refresh() {
            int width = _pause.Backdrop?.Width ?? 0;
            int height = _pause.Backdrop?.Height ?? 0;
            if (TerminalSize.TryGet(out int w, out int h) && w > 0 && h > 0) {
                width = w;
                height = h;
            }
            if (width <= 0 || height <= 0) {
                width = _context.Profile.Width;
                height = _context.Profile.Height;
            }
            bool changed = width != _width || height != _height;
            _width = width;
            _height = height;
            return changed;
        }

        public void Present(CellBuffer frame, Caret? caret) {
            CellBuffer composite = new CellBuffer(_width, _height);
            CellBuffer? backdrop = _pause.Backdrop;
            if (backdrop != null) {
                composite.Blit(backdrop, 0, 0, Math.Min(backdrop.Width, _width), Math.Min(backdrop.Height, _height), 0, 0);
            }
            Region all = new Region(composite);
            Overlay.Dim(all, _context);
            int content = BoxedSurface.ContentWidth(frame);
            int boxWidth = Math.Min(_width, content + 4);
            int boxHeight = Math.Min(_height, frame.Height + 2);
            int x = Math.Max(0, (_width - boxWidth) / 2);
            int y = Math.Max(0, (_height - boxHeight) / 3);
            Region box = all.Sub(x, y, boxWidth, boxHeight);
            box.Fill(Style.Plain);
            BoxGlyphs glyphs = _context.Glyphs.GetBox(BoxStyle.Rounded);
            Style border = _context.Style(ThemeToken.Accent);
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
            composite.Blit(frame, 0, 0, Math.Min(content, bw - 4), Math.Min(frame.Height, bh - 2), x + 2, y + 1);
            if (caret.HasValue) {
                int cx = x + 2 + caret.Value.X;
                int cy = y + 1 + caret.Value.Y;
                if (cx < _width && cy < _height) {
                    Cell cell = composite[cx, cy];
                    composite.SetStyle(cx, cy, cell.Style.With(TextDecoration.Reverse));
                    if (string.IsNullOrEmpty(cell.Glyph) || cell.Glyph == " ") {
                        composite.Set(cx, cy, " ", cell.Style.With(TextDecoration.Reverse));
                    }
                }
            }
            _pause.Present(composite);
        }

        public void Complete(IReadOnlyList<StyledLine>? summary) {
        }

        public void Dispose() {
            _pause.Dispose();
        }
    }
}
