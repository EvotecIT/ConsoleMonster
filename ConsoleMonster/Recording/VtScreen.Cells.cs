using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Sessions {
    internal sealed partial class VtScreen {
        private void Print(string glyph) {
            CellBuffer screen = Current;
            Style style = _link != null ? _style.WithLink(_link) : _style;
            if (TryJoiningCell(glyph, out int px, out Cell previous)) {
                string joined = previous.Glyph + glyph;
                // Use the canonical grapheme segmenter even when a cluster spans UTF-8 reads or cast events.
                if (joined.Length > 4096) { throw new FormatException("The terminal grapheme exceeds the playback input limit."); }
                glyph = joined;
                style = previous.Style;
                screen.Set(px, _y, " ", previous.Style);
                _x = px;
                _pendingWrap = false;
            }
            int width = DisplayWidth.Measure(glyph);
            if (width == 0) { return; }
            if (_pendingWrap) {
                if (_autowrap) {
                    _x = 0;
                    LineFeed();
                }
                _pendingWrap = false;
            }
            if (width == 2 && _x == Width - 1) {
                if (_autowrap) {
                    screen.Set(_x, _y, " ", _style);
                    _x = 0;
                    LineFeed();
                } else {
                    return;
                }
            }
            screen.Set(_x, _y, glyph, style, width);
            if (_x + width >= Width) {
                _x = Width - 1;
                _pendingWrap = true;
            } else {
                _x += width;
            }
        }

        private void LineFeed() {
            _pendingWrap = false;
            if (_y == _bottom) {
                ScrollUp(1);
            } else if (_y < Height - 1) {
                _y++;
            }
        }

        private void ScrollUp(int lines, bool remember = true) {
            CellBuffer screen = Current;
            lines = Math.Min(lines, _bottom - _top + 1);
            for (int n = 0; n < lines; n++) {
                if (_keepScrollback && remember && !_onAlternate && _top == 0) {
                    Cell[] row = new Cell[Width];
                    for (int x = 0; x < Width; x++) {
                        row[x] = screen[x, 0];
                    }
                    _scrollback.Add(row);
                }
                for (int y = _top; y < _bottom; y++) {
                    CopyRow(screen, y + 1, y);
                }
                ClearRow(screen, _bottom, 0, Width);
            }
        }

        private void ScrollDown(int lines) {
            CellBuffer screen = Current;
            lines = Math.Min(lines, _bottom - _top + 1);
            for (int n = 0; n < lines; n++) {
                for (int y = _bottom; y > _top; y--) {
                    CopyRow(screen, y - 1, y);
                }
                ClearRow(screen, _top, 0, Width);
            }
        }

        private void ShiftCells(int delta) {
            CellBuffer screen = Current;
            Cell[] before = new Cell[Width];
            for (int x = 0; x < Width; x++) { before[x] = screen[x, _y]; }
            ClearRow(screen, _y, _x, Width);
            for (int x = _x; x < Width; x++) {
                int from = x - delta;
                if (from >= _x && from < Width && !before[from].IsContinuation) { Put(screen, x, _y, before[from]); }
            }
        }

        private void CopyRow(CellBuffer screen, int from, int to) {
            for (int x = 0; x < Width; x++) {
                Cell cell = screen[x, from];
                if (cell.IsContinuation) {
                    continue;
                }
                screen.Set(x, to, string.IsNullOrEmpty(cell.Glyph) ? " " : cell.Glyph, cell.Style, Math.Max(1, (int)cell.Width));
                if (cell.Width < 2 && x + 1 < Width && screen[x + 1, to].IsContinuation && !screen[x + 1, from].IsContinuation) {
                    screen.Set(x + 1, to, " ", Style.Plain);
                }
            }
        }

        private void ClearRow(CellBuffer screen, int y, int from, int to) {
            // Erased cells take the current background, as terminals do (back-colour erase).
            Style blank = _style.BackgroundColor.IsDefault ? Style.Plain : new Style(Color.Default, _style.BackgroundColor);
            for (int x = Math.Max(0, from); x < Math.Min(Width, to); x++) {
                screen.Set(x, y, " ", blank);
            }
        }

    }
}
