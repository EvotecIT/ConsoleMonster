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
    public sealed class TextInput : InputControl {
        private readonly LineEditor _editor;
        private readonly CaretSlot? _caret;
        private readonly bool _reservePlaceholderCell;

        /// <summary>Creates a named, focusable single-line input using the shared line editor.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="value">The initial text.</param>
        public TextInput(string key, string? value = null) : base(key) {
            _editor = new LineEditor(value);
            _caret = new CaretSlot();
            _reservePlaceholderCell = true;
        }

        internal TextInput(LineEditor editor, CaretSlot? caret) : base(nameof(TextInput), false) {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _caret = caret;
            Focused = true;
        }

        /// <summary>Muted text shown while the value is empty.</summary>
        public string? Placeholder { get; set; }

        /// <summary>The mask character for secrets, or null to show the text. An empty string hides the text entirely.</summary>
        public string? Mask { get; set; }

        /// <summary>The theme token for entered text.</summary>
        public ThemeToken Role { get; set; } = ThemeToken.Text;

        /// <summary>The text value; edits notify the application after the control lock is released.</summary>
        public string Value { get => Read(() => _editor.Text); set => Change(() => _editor.Text = value); }

        /// <summary>The maximum text length, in UTF-16 units.</summary>
        public int MaxLength { get => Read(() => _editor.MaxLength); set => Change(() => _editor.MaxLength = value); }

        internal override Caret? CurrentCaret => _caret?.Caret;

        /// <inheritdoc />
        protected override object ReadValueCore() => _editor.Text;

        /// <inheritdoc />
        protected override bool HandleInputCore(Input.InputEvent input, RenderContext context) => _editor.Handle(input);

        private Rect _textBounds;


        /// <inheritdoc />
        protected override bool HandlePointerCore(Input.InputEvent input, RenderContext context) {
            // A click places the caret; dragging selects, as Shift with the arrows does.
            Rect text = _textBounds;
            bool inside = input.Column >= text.X && input.Column < text.Right && input.Row >= text.Y && input.Row < text.Bottom;
            if (input.MouseAction == Input.MouseAction.Down && input.MouseButton == 1) {
                if (!inside) { return false; }
                _editor.Dragging = true;
                _editor.PlaceCaret(IndexAt(input.Column - text.X + _editor.ScrollOffset, context), false);
                return true;
            }
            if (input.MouseAction == Input.MouseAction.Move && _editor.Dragging && (input.HeldButtons & 1) != 0) {
                _editor.PlaceCaret(IndexAt(Math.Max(0, input.Column - text.X + _editor.ScrollOffset), context), true);
                return true;
            }
            if (input.MouseAction == Input.MouseAction.Up && _editor.Dragging) {
                _editor.Dragging = false;
                if (!_editor.HasSelection) { _editor.ClearSelection(); }
                return true;
            }
            return false;
        }

        internal override void CancelPointer() => _editor.Dragging = false;
        internal override object PointerIdentity => _editor;

        // The caret index nearest a display column: a click on the right half of a character goes after it.
        private int IndexAt(int column, RenderContext context) {
            // Whole display clusters, as drawn: a click never lands inside a joined emoji.
            string value = _editor.Text;
            int used = 0;
            foreach (TextCluster cluster in DisplayWidth.Clusters(value)) {
                int width = DisplayWidth.Measure(Display(value.Substring(cluster.Start, cluster.Length), context));
                if (column < used + (width + 1) / 2) { return cluster.Start; }
                used += width;
            }
            return value.Length;
        }
        internal override bool Takes(Input.KeyChord chord, RenderContext context) => LineEditor.Takes(chord);

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            int width = region.Width;
            _textBounds = region.Bounds;
            Style style = context.Style(Role);
            if (_editor.IsEmpty && !string.IsNullOrEmpty(Placeholder)) {
                // Public controls paint their own caret; existing prompt compositions own its surrounding space.
                int placeholderColumn = Focused && _reservePlaceholderCell ? 1 : 0;
                region.Write(placeholderColumn, 0, TextWrapper.Truncate(Glyphs.SymbolFallback.Constrain(Placeholder!, context.Glyphs.Tier), Math.Max(0, width - placeholderColumn - 1), context.Ellipsis), context.Style(ThemeToken.Muted).With(TextDecoration.Italic));
                if (Focused) {
                    _caret?.Set(region.Bounds.X, region.Bounds.Y);
                }
                return;
            }
            string shown = Display(_editor.Text, context);
            string beforeCaret = Display(_editor.Text.Substring(0, _editor.Caret), context);
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
            int selectFrom = _editor.HasSelection ? DisplayWidth.Measure(Display(_editor.Text.Substring(0, _editor.SelectionStart), context)) : -1;
            int selectTo = _editor.HasSelection ? DisplayWidth.Measure(Display(_editor.Text.Substring(0, _editor.SelectionEnd), context)) : -1;
            Style selected = style.With(TextDecoration.Reverse);
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
                region.Put(x, 0, cell.Glyph, column >= selectFrom && column < selectTo ? selected : style, cell.Width);
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

        private string Display(string text, RenderContext context) {
            if (Mask == null) {
                return Glyphs.SymbolFallback.Constrain(text, context.Glyphs.Tier);
            }
            if (Mask.Length == 0) {
                return string.Empty;
            }
            // One mask per display cluster, the unit the caret and a click move by, so an accented letter is one cell.
            int count = 0;
            foreach (TextCluster _ in DisplayWidth.Clusters(text)) {
                count++;
            }
            List<string> parts = new List<string>(count);
            for (int i = 0; i < count; i++) {
                parts.Add(Mask);
            }
            return Glyphs.SymbolFallback.Constrain(string.Concat(parts), context.Glyphs.Tier);
        }
    }
}
