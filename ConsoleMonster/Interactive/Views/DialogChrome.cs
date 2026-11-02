using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The one dialog look: a panel with an accent border (warn for destructive questions), an optional title on the
    /// border, one blank row above and below the content and one column either side, placed a third of the way down over a dimmed
    /// backdrop. Menu dialogs (confirm, name, output, palette), prompts asked over a menu and prompts asked over a paused
    /// live run all use it. See Docs/Design-System.md.
    /// </summary>
    internal static class DialogChrome {
        /// <summary>The rows the frame adds around its content: two borders and a blank row above and below.</summary>
        public const int ExtraHeight = 4;

        /// <summary>Wraps dialog content in the dialog frame.</summary>
        public static Panel Frame(IWidget content, string? title, ThemeToken border = ThemeToken.Accent) {
            return new Panel(new Padding(content, 0, 1, 0, 1), title == null ? null : Markup.Escape(title)) { BorderRole = border };
        }

        /// <summary>Where a dialog of a size goes in an area: centred, a third of the way down (or near the top).</summary>
        public static Rect Place(int width, int height, int dialogWidth, int dialogHeight, bool top = false) {
            int w = Math.Min(width, dialogWidth);
            int h = Math.Min(height, dialogHeight);
            int x = Math.Max(0, (width - w) / 2);
            int y = top ? Math.Min(2, Math.Max(0, height - h)) : Math.Max(0, (height - h) / 3);
            return new Rect(x, y, w, h);
        }

        /// <summary>
        /// Draws a prompt's frame as a dialog over a backdrop (the app or the paused run): the backdrop dimmed, the dialog
        /// frame around the prompt, and the caret moved to where the prompt's caret lands.
        /// </summary>
        public static CellBuffer Compose(CellBuffer? backdrop, int width, int height, CellBuffer prompt, Caret? caret, RenderContext context, out Caret? moved) {
            CellBuffer composite = new CellBuffer(width, height);
            if (backdrop != null) {
                composite.Blit(backdrop, 0, 0, Math.Min(backdrop.Width, width), Math.Min(backdrop.Height, height), 0, 0);
            }
            Region all = new Region(composite);
            Overlay.Dim(all, context);
            int content = ContentWidth(prompt);
            Rect place = Place(width, height, content + 4, prompt.Height + ExtraHeight);
            Region area = all.Sub(place.X, place.Y, place.Width, place.Height);
            area.Fill(Style.Plain);
            Frame(new BufferView(prompt, content), null).Render(area, context);
            moved = caret.HasValue ? new Caret(place.X + 2 + caret.Value.X, place.Y + 2 + caret.Value.Y) : (Caret?)null;
            return composite;
        }

        /// <summary>The width a prompt frame really uses (its widest row), at least 44 cells, so dialogs fit their content.</summary>
        public static int ContentWidth(CellBuffer frame) {
            int used = 0;
            for (int y = 0; y < frame.Height; y++) {
                used = Math.Max(used, CellEncoder.ContentEnd(frame, y, ColorDepth.TrueColor));
            }
            return Math.Min(frame.Width, Math.Max(44, used + 1));
        }

        /// <summary>Shows already rendered cells as a widget (a prompt frame inside the dialog chrome).</summary>
        private sealed class BufferView : IWidget {
            private readonly CellBuffer _buffer;
            private readonly int _width;

            public BufferView(CellBuffer buffer, int width) {
                _buffer = buffer;
                _width = width;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(Math.Min(_width, constraints.MaxWidth), Math.Min(_buffer.Height, constraints.IsHeightUnbounded ? _buffer.Height : constraints.MaxHeight));

            public void Render(Region region, RenderContext context) {
                for (int y = 0; y < _buffer.Height && y < region.Height; y++) {
                    for (int x = 0; x < _width && x < region.Width; x++) {
                        Cell cell = _buffer[x, y];
                        if (!cell.IsContinuation) {
                            region.Put(x, y, cell.Glyph, cell.Style, cell.Width);
                        }
                    }
                }
            }
        }
    }
}
