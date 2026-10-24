using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Draws a <see cref="TextAreaEditor"/>: the text soft-wrapped at word boundaries behind a muted bar, a fixed number
    /// of visible rows scrolled to keep the caret in view, a placeholder while empty, and the caret reported to the
    /// <see cref="CaretSlot"/>. The layout it computes is what Up and Down move through.
    /// </summary>
    public sealed class TextArea : InputControl {
        private readonly TextAreaEditor _editor;
        private readonly CaretSlot? _caret;
        private int _layoutWidth;

        /// <summary>Creates a named multiline input using the shared text-area editor.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="value">The initial text.</param>
        public TextArea(string key, string? value = null) : base(key) {
            _editor = new TextAreaEditor(value);
            _caret = new CaretSlot();
        }

        internal TextArea(TextAreaEditor editor, CaretSlot? caret) : base(nameof(TextArea), false) {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _caret = caret;
            Focused = true;
        }

        /// <summary>Muted text shown when the value is empty.</summary>
        public string? Placeholder { get; set; }

        /// <summary>The visible rows (default 6).</summary>
        public int Height { get; set; } = 6;

        /// <summary>The first visible row (kept between frames).</summary>
        public int ScrollRow { get; set; }

        /// <summary>The multiline text value.</summary>
        public string Value { get => Read(() => _editor.Text); set => Change(() => _editor.Text = value); }

        /// <summary>The maximum text length, in UTF-16 units.</summary>
        public int MaxLength { get => Read(() => _editor.MaxLength); set => Change(() => _editor.MaxLength = value); }

        internal override Caret? CurrentCaret => _caret?.Caret;

        /// <inheritdoc />
        protected override object ReadValueCore() => _editor.Text;

        /// <inheritdoc />
        protected override bool HandleInputCore(Input.InputEvent input, RenderContext context) {
            // Input and callbacks can change text several times before the renderer draws again.
            // Rebuild under InputControl's edit/render lock, using the most recent content width.
            Layout(_editor.Text, _layoutWidth > 0 ? _layoutWidth : Math.Max(1, context.Profile.Width - 6));
            return _editor.Handle(input);
        }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(Math.Max(1, Height), constraints.IsHeightUnbounded ? Height : constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            if (region.Width < 4 || region.Height <= 0) {
                return;
            }
            Style bar = context.Style(Focused ? ThemeToken.Accent : ThemeToken.Border);
            string glyph = context.Glyphs.Get(Glyphs.Glyph.Separator);
            for (int y = 0; y < region.Height; y++) {
                region.Write(0, y, glyph, bar);
            }
            int width = region.Width - 2;
            _layoutWidth = width;
            string text = _editor.Text;
            Layout(text, width);
            if (_editor.IsEmpty && !string.IsNullOrEmpty(Placeholder)) {
                region.Write(2, 0, TextWrapper.Truncate(Placeholder!, width, context.Ellipsis), context.Style(ThemeToken.Muted).With(TextDecoration.Italic));
                if (Focused) {
                    _caret?.Set(region.Bounds.X + 2, region.Bounds.Y);
                }
                return;
            }
            int caretRow = 0;
            for (int r = _editor.Rows.Count - 1; r >= 0; r--) {
                if (_editor.Caret >= _editor.Rows[r][0]) {
                    caretRow = r;
                    break;
                }
            }
            if (caretRow < ScrollRow) {
                ScrollRow = caretRow;
            } else if (caretRow >= ScrollRow + region.Height) {
                ScrollRow = caretRow - region.Height + 1;
            }
            ScrollRow = Math.Max(0, Math.Min(ScrollRow, Math.Max(0, _editor.Rows.Count - region.Height)));
            Style style = context.Style(ThemeToken.Text);
            for (int y = 0; y < region.Height && ScrollRow + y < _editor.Rows.Count; y++) {
                int[] row = _editor.Rows[ScrollRow + y];
                string shown = text.Substring(row[0], Math.Max(0, row[1] - row[0])).TrimEnd('\n');
                region.Write(2, y, shown, style, width);
            }
            if (ScrollRow > 0) {
                region.Write(0, 0, context.Glyphs.Get(Glyphs.Glyph.ArrowUp), context.Style(ThemeToken.Muted));
            }
            if (ScrollRow + region.Height < _editor.Rows.Count) {
                region.Write(0, region.Height - 1, context.Glyphs.Get(Glyphs.Glyph.ArrowDown), context.Style(ThemeToken.Muted));
            }
            if (Focused) {
                int[] caret = _editor.Rows[caretRow];
                int column = DisplayWidth.Measure(text.Substring(caret[0], _editor.Caret - caret[0]));
                _caret?.Set(region.Bounds.X + 2 + Math.Min(width - 1, column), region.Bounds.Y + caretRow - ScrollRow);
            }
        }

        // Soft-wraps each line at word boundaries (or mid-word for words longer than the width).
        private void Layout(string text, int width) {
            List<int[]> rows = _editor.Rows;
            rows.Clear();
            int lineStart = 0;
            while (true) {
                int lineEnd = text.IndexOf('\n', lineStart);
                int contentEnd = lineEnd < 0 ? text.Length : lineEnd;
                int start = lineStart;
                while (true) {
                    int column = 0;
                    int index = start;
                    int lastBreak = -1;
                    while (index < contentEnd) {
                        int next = index + (char.IsHighSurrogate(text[index]) && index + 1 < contentEnd ? 2 : 1);
                        int cell = DisplayWidth.Measure(text.Substring(index, next - index));
                        if (column + cell > width) {
                            break;
                        }
                        column += cell;
                        if (text[index] == ' ') {
                            lastBreak = next;
                        }
                        index = next;
                    }
                    if (index >= contentEnd) {
                        rows.Add(new[] { start, lineEnd < 0 ? text.Length : lineEnd + 1 });
                        break;
                    }
                    int cut = lastBreak > start ? lastBreak : Math.Max(index, start + 1);
                    rows.Add(new[] { start, cut });
                    start = cut;
                }
                if (lineEnd < 0) {
                    break;
                }
                lineStart = lineEnd + 1;
            }
        }
    }
}
