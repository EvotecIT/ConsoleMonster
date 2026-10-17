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

        // The same dialog as menu dialogs and prompts over a menu; the live output has no cursor, so the caret is drawn.
        public void Present(CellBuffer frame, Caret? caret) {
            CellBuffer composite = DialogChrome.Compose(_pause.Backdrop, _width, _height, frame, caret, _context, out Caret? moved);
            if (moved.HasValue && moved.Value.X < _width && moved.Value.Y < _height) {
                Cell cell = composite[moved.Value.X, moved.Value.Y];
                composite.SetStyle(moved.Value.X, moved.Value.Y, cell.Style.With(TextDecoration.Reverse));
                if (string.IsNullOrEmpty(cell.Glyph) || cell.Glyph == " ") {
                    composite.Set(moved.Value.X, moved.Value.Y, " ", cell.Style.With(TextDecoration.Reverse));
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
