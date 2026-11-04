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
    internal sealed class TextArea : InputControl {
        private readonly TextAreaEditor _editor;
        private readonly CaretSlot? _caret;
        private int _layoutWidth;
        private readonly FindBar? _find;

        /// <summary>Creates a named multiline input using the shared text-area editor.</summary>
        /// <param name="key">The application-state key.</param>
        /// <param name="value">The initial text.</param>
        public TextArea(string key, string? value = null) : base(key) {
            _editor = new TextAreaEditor(value);
            _caret = new CaretSlot();
            _find = new FindBar();
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
        public string Value { get => Read(() => _editor.Text); set => Change(() => {
            _editor.Text = value;
            if (_find != null && _find.Active) { _find.Search(Lines(), CaretLine()); }
        }); }

        /// <summary>The maximum text length, in UTF-16 units.</summary>
        public int MaxLength { get => Read(() => _editor.MaxLength); set => Change(() => _editor.MaxLength = value); }

        internal override Caret? CurrentCaret => _caret?.Caret;

        /// <inheritdoc />
        protected override object ReadValueCore() => _editor.Text;

        internal override bool Takes(Input.KeyChord chord, RenderContext context) => (_find != null && (Read(() => _find.Open) && FindBar.Takes(chord) || chord.Equals(new Input.KeyChord(ConsoleKey.F, Input.KeyModifiers.Control)))) || LineEditor.Takes(chord) || chord.Key == ConsoleKey.Enter || chord.Key == ConsoleKey.UpArrow
            || chord.Key == ConsoleKey.DownArrow || chord.Key == ConsoleKey.PageUp || chord.Key == ConsoleKey.PageDown;

        /// <inheritdoc />
        protected override bool HandleInputCore(Input.InputEvent input, RenderContext context) {
            // Input and callbacks can change text several times before the renderer draws again.
            // Rebuild under InputControl's edit/render lock, using the most recent content width.
            if (_layoutWidth <= 0 || _editor.Glyphs != context.Glyphs.Tier) {
                _layoutWidth = _layoutWidth > 0 ? _layoutWidth : Math.Max(1, context.Profile.Width - 6);
                _editor.Layout(_layoutWidth, context.Glyphs.Tier);
            }
            if (_find != null) {
                if (_find.Open) {
                    switch (_find.Handle(input)) {
                        case FindKey.Edited: _find.Search(Lines(), CaretLine()); Reveal(); return true;
                        case FindKey.Next: if (_find.Step(1)) { Reveal(); } return true;
                        case FindKey.Previous: if (_find.Step(-1)) { Reveal(); } return true;
                        case FindKey.Closed: _find.Close(false); return true;
                        case FindKey.Kept: return true;
                        default: return false;
                    }
                }
                if (input.Is(ConsoleKey.F, Input.KeyModifiers.Control)) { _find.Start(); _find.Search(Lines(), CaretLine()); return true; }
            }
            return _editor.Handle(input);
        }

        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            if (_find == null) { return; }
            PromptLabels labels = PromptLabels.For(context.Profile.Language);
            if (Read(() => _find.Open)) { FindBar.AddKeys(keys, labels); }
            else { keys.Add(new KeyBinding(labels.KeySearch, () => { }, new Input.KeyChord(ConsoleKey.F, Input.KeyModifiers.Control)) { Order = 70 }); }
        }

        internal override void ClosePopupCore() { if (_find != null && _find.Open) { _find.Close(false); } }

        private List<string> Lines() => new List<string>(_editor.Text.Split('\n'));

        private int CaretLine() {
            string text = _editor.Text;
            int line = 0;
            for (int i = 0; i < _editor.Caret && i < text.Length; i++) { if (text[i] == '\n') { line++; } }
            return line;
        }

        // The current match as an offset into the text.
        private int Offset(FindBar.Match match) {
            string text = _editor.Text;
            int line = 0, index = 0;
            while (line < match.Line && index < text.Length) { if (text[index++] == '\n') { line++; } }
            return index + match.Start;
        }

        // Moves the caret to the current match; drawing scrolls it into view.
        private void Reveal() {
            if (_find != null && _find.Current >= 0) { _editor.MoveTo(Offset(_find.Matches[_find.Current])); }
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
            _editor.Layout(width, context.Glyphs.Tier);
            if (_editor.IsEmpty && !string.IsNullOrEmpty(Placeholder)) {
                region.Write(2, 0, TextWrapper.Truncate(Glyphs.SymbolFallback.Constrain(Placeholder!, context.Glyphs.Tier), width, context.Ellipsis), context.Style(ThemeToken.Muted).With(TextDecoration.Italic));
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
                region.Write(2, y, Glyphs.SymbolFallback.Constrain(shown, context.Glyphs.Tier), style, width);
            }
            if (_find != null && _find.Active) {
                for (int i = 0; i < _find.Matches.Count; i++) {
                    FindBar.Match match = _find.Matches[i];
                    int at = Offset(match);
                    for (int y = 0; y < region.Height && ScrollRow + y < _editor.Rows.Count; y++) {
                        int[] row = _editor.Rows[ScrollRow + y];
                        int start = Math.Max(at, row[0]), end = Math.Min(at + match.Length, row[1]);
                        if (start >= end) { continue; }
                        int from = 2 + _editor.ProjectedWidth(text.Substring(row[0], start - row[0]));
                        int to = 2 + _editor.ProjectedWidth(text.Substring(row[0], end - row[0]));
                        _find.HighlightCells(region, y, from, to, i == _find.Current, context);
                    }
                }
            }
            if (_find != null && _find.Open) { InputArrangement.RecordBar(this, _find.Widget(PromptLabels.For(context.Profile.Language))); }
            if (ScrollRow > 0) {
                region.Write(0, 0, context.Glyphs.Get(Glyphs.Glyph.ArrowUp), context.Style(ThemeToken.Muted));
            }
            if (ScrollRow + region.Height < _editor.Rows.Count) {
                region.Write(0, region.Height - 1, context.Glyphs.Get(Glyphs.Glyph.ArrowDown), context.Style(ThemeToken.Muted));
            }
            if (Focused) {
                int[] caret = _editor.Rows[caretRow];
                int column = _editor.ProjectedWidth(text.Substring(caret[0], _editor.Caret - caret[0]));
                _caret?.Set(region.Bounds.X + 2 + Math.Min(width - 1, column), region.Bounds.Y + caretRow - ScrollRow);
            }
        }

    }
}
