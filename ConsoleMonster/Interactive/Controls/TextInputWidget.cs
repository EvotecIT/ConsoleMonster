using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Draws a <see cref="LineEditor"/> on one row: the text (or its mask), scrolled horizontally so the caret stays visible,
    /// a muted placeholder while empty, and the caret position reported to the <see cref="CaretSlot"/> when focused.
    /// </summary>
    internal sealed class TextInputWidget : IWidget {
        private readonly LineEditor _editor;
        private readonly CaretSlot? _caret;

        public TextInputWidget(LineEditor editor, CaretSlot? caret) {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _caret = caret;
        }

        public string? Placeholder { get; set; }

        /// <summary>The mask character for secrets, or null to show the text. An empty string hides the text entirely.</summary>
        public string? Mask { get; set; }

        public bool Focused { get; set; } = true;

        public ThemeToken Token { get; set; } = ThemeToken.Text;

        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            int width = region.Width;
            Style style = context.Style(Token);
            if (_editor.IsEmpty && !string.IsNullOrEmpty(Placeholder)) {
                region.Write(0, 0, TextWrapper.Truncate(Placeholder!, Math.Max(0, width - 1), context.Ellipsis), context.Style(ThemeToken.Muted).With(TextDecoration.Italic));
                if (Focused) {
                    _caret?.Set(region.Bounds.X, region.Bounds.Y);
                }
                return;
            }
            string shown = Display(_editor.Text);
            string beforeCaret = Display(_editor.Text.Substring(0, _editor.Caret));
            int caretColumn = DisplayWidth.Measure(beforeCaret);
            int usable = Math.Max(1, width - 1);
            int offset = _editor.ScrollOffset;
            if (caretColumn - offset > usable) {
                offset = caretColumn - usable;
            }
            if (caretColumn < offset) {
                offset = Math.Max(0, caretColumn - usable / 3);
            }
            if (DisplayWidth.Measure(shown) <= usable) {
                offset = 0;
            }
            _editor.ScrollOffset = offset;
            int x = 0;
            int column = 0;
            foreach (TextCell cell in DisplayWidth.Cells(shown)) {
                if (column + cell.Width <= offset) {
                    column += cell.Width;
                    continue;
                }
                if (column < offset) {
                    column += cell.Width;
                    continue;
                }
                if (x + cell.Width > width) {
                    break;
                }
                region.Put(x, 0, cell.Glyph, style, cell.Width);
                x += cell.Width;
                column += cell.Width;
            }
            if (offset > 0) {
                region.Put(0, 0, context.Glyphs.Tier != Capabilities.GlyphTier.Ascii ? "…" : "<", context.Style(ThemeToken.Muted));
            }
            if (Focused) {
                _caret?.Set(region.Bounds.X + Math.Min(width - 1, caretColumn - offset), region.Bounds.Y);
            }
        }

        private string Display(string text) {
            if (Mask == null) {
                return text;
            }
            if (Mask.Length == 0) {
                return string.Empty;
            }
            int count = 0;
            for (int i = 0; i < text.Length; i++) {
                if (!char.IsLowSurrogate(text[i])) {
                    count++;
                }
            }
            List<string> parts = new List<string>(count);
            for (int i = 0; i < count; i++) {
                parts.Add(Mask);
            }
            return string.Concat(parts);
        }
    }
}
