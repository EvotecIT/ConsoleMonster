using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Input;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A multi-line text editor: typing, Enter for a new line, paste with line breaks kept (tabs become spaces, control
    /// characters are dropped), deletion, caret movement by character, word, visual row (soft wrap) and document, and
    /// limits on length and lines. Pure state; <see cref="TextArea"/> draws it and keeps the visual rows.
    /// </summary>
    internal sealed class TextAreaEditor {
        private readonly StringBuilder _text = new StringBuilder();
        private int _layoutWidth;

        public TextAreaEditor(string? text = null) {
            if (!string.IsNullOrEmpty(text)) {
                _text.Append(Clean(text!));
                Caret = _text.Length;
            }
        }

        public string Text {
            get => _text.ToString();
            set {
                _text.Clear();
                _text.Append(Clean(value ?? string.Empty));
                Caret = _text.Length;
                Anchor = -1;
                PreferredColumn = -1;
            }
        }

        public int Caret { get; private set; }

        /// <summary>Where the selection started (Shift with a movement key, or a drag), or -1.</summary>
        public int Anchor { get; private set; } = -1;

        public bool HasSelection => Anchor >= 0 && Anchor != Caret;
        public int SelectionStart => HasSelection ? Math.Min(Anchor, Caret) : Caret;
        public int SelectionEnd => HasSelection ? Math.Max(Anchor, Caret) : Caret;

        /// <summary>Moves the caret to a pointer position; with <paramref name="extend"/> the selection grows to it.</summary>
        internal void PlaceCaret(int index, bool extend) {
            index = Math.Max(0, Math.Min(_text.Length, index));
            if (index > 0 && index < _text.Length && char.IsLowSurrogate(_text[index]) && char.IsHighSurrogate(_text[index - 1])) { index--; }
            if (extend) { if (Anchor < 0) { Anchor = Caret; } }
            else { Anchor = index; }
            Caret = index;
            PreferredColumn = -1;
        }

        internal void ClearSelection() => Anchor = -1;

        // A pointer drag is selecting (kept here, so the text box drawn for the next frame continues it).
        internal bool Dragging { get; set; }

        private bool DeleteSelection() {
            if (!HasSelection) { Anchor = -1; return false; }
            Delete(SelectionStart, SelectionEnd);
            Anchor = -1;
            return true;
        }

        public int Length => _text.Length;

        public bool IsEmpty => _text.Length == 0;

        public int MaxLength { get; set; } = int.MaxValue;

        public int MaxLines { get; set; } = int.MaxValue;

        public int LineCount {
            get {
                int lines = 1;
                for (int i = 0; i < _text.Length; i++) {
                    if (_text[i] == '\n') {
                        lines++;
                    }
                }
                return lines;
            }
        }

        /// <summary>The visual rows of the last layout as [start, end) character ranges in the most recent view layout.</summary>
        public List<int[]> Rows { get; } = new List<int[]>();

        /// <summary>The column the caret keeps when moving up and down.</summary>
        public int PreferredColumn { get; set; } = -1;

        // The view sets the same repertoire before layout and before handling edits.
        internal GlyphTier Glyphs { get; set; } = GlyphTier.Full;
        internal int ProjectedWidth(string text) => DisplayWidth.Measure(SymbolFallback.Constrain(text, Glyphs));

        /// <summary>Applies an edit or caret movement; returns true when the event was one.</summary>
        public bool Handle(InputEvent e) {
            if (_layoutWidth > 0) Layout(_layoutWidth, Glyphs);
            if (e.Kind == InputEventKind.Paste) {
                DeleteSelection();
                Insert(Clean(e.Text ?? string.Empty));
                return true;
            }
            if (e.Kind != InputEventKind.Key) {
                return false;
            }
            bool ctrl = (e.Modifiers & KeyModifiers.Control) != 0;
            if (e.IsText && !ctrl) {
                DeleteSelection();
                Insert(e.Char.ToString());
                return true;
            }
            // Shift with a movement key selects (as dragging does); a plain movement drops the selection, and a deletion
            // removes the selected text first.
            switch (e.Key) {
                case ConsoleKey.LeftArrow: case ConsoleKey.RightArrow: case ConsoleKey.Home: case ConsoleKey.End:
                case ConsoleKey.UpArrow: case ConsoleKey.DownArrow: case ConsoleKey.PageUp: case ConsoleKey.PageDown:
                    if ((e.Modifiers & KeyModifiers.Shift) != 0) { if (Anchor < 0) { Anchor = Caret; } }
                    else { Anchor = -1; }
                    break;
                case ConsoleKey.Backspace: case ConsoleKey.Delete:
                    if (DeleteSelection()) { return true; }
                    break;
                case ConsoleKey.Enter when !ctrl:
                    DeleteSelection();
                    break;
                default:
                    Anchor = -1;
                    break;
            }
            switch (e.Key) {
                case ConsoleKey.Enter when !ctrl:
                    Insert("\n");
                    return true;
                case ConsoleKey.Backspace:
                    Delete(ctrl ? WordStart(Caret) : Previous(Caret), Caret);
                    return true;
                case ConsoleKey.Delete:
                    Delete(Caret, ctrl ? WordEnd(Caret) : Next(Caret));
                    return true;
                case ConsoleKey.LeftArrow:
                    Move(ctrl ? WordStart(Caret) : Previous(Caret));
                    return true;
                case ConsoleKey.RightArrow:
                    Move(ctrl ? WordEnd(Caret) : Next(Caret));
                    return true;
                case ConsoleKey.Home:
                    Move(ctrl ? 0 : RowOf(Caret)[0]);
                    return true;
                case ConsoleKey.End:
                    Move(ctrl ? _text.Length : RowEnd(RowOf(Caret)));
                    return true;
                case ConsoleKey.UpArrow:
                    return Vertical(-1);
                case ConsoleKey.DownArrow:
                    return Vertical(1);
                case ConsoleKey.PageUp:
                    return Vertical(-8);
                case ConsoleKey.PageDown:
                    return Vertical(8);
            }
            return false;
        }

        public void Insert(string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            int room = MaxLength - _text.Length;
            if (room <= 0) {
                return;
            }
            if (text.Length > room) {
                text = text.Substring(0, room);
            }
            int newLines = 0;
            foreach (char c in text) {
                if (c == '\n') {
                    newLines++;
                }
            }
            int allowed = Math.Max(0, MaxLines - LineCount);
            if (newLines > allowed) {
                // Over the line limit the remaining line breaks join the lines with spaces.
                StringBuilder sb = new StringBuilder(text.Length);
                int kept = 0;
                foreach (char c in text) {
                    if (c == '\n') {
                        if (kept < allowed) {
                            kept++;
                            sb.Append(c);
                        } else {
                            sb.Append(' ');
                        }
                    } else {
                        sb.Append(c);
                    }
                }
                text = text == "\n" ? string.Empty : sb.ToString();
            }
            _text.Insert(Caret, text);
            Caret += text.Length;
            PreferredColumn = -1;
        }

        internal static string Clean(string text) {
            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (c == '\r') {
                    if (i + 1 < text.Length && text[i + 1] == '\n') {
                        i++;
                    }
                    sb.Append('\n');
                } else if (c == '\n') {
                    sb.Append('\n');
                } else if (c == '\t') {
                    sb.Append("    ");
                } else if (c >= ' ' && c != '\u007f' && !(c >= '\u0080' && c < ' ')) {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        // Soft-wraps each line at word boundaries (or mid-word for words longer than the width).
        internal void Layout(int width, GlyphTier glyphs) {
            _layoutWidth = Math.Max(1, width);
            width = _layoutWidth;
            Glyphs = glyphs;
            string text = Text;
            List<int[]> rows = Rows;
            rows.Clear();
            int lineStart = 0;
            while (true) {
                int lineEnd = text.IndexOf('\n', lineStart);
                int contentEnd = lineEnd < 0 ? text.Length : lineEnd;
                string line = text.Substring(lineStart, contentEnd - lineStart);
                var clusters = new List<TextCluster>(DisplayWidth.Clusters(line));
                int first = 0;
                while (true) {
                    int column = 0;
                    int index = first;
                    int lastBreak = -1;
                    while (index < clusters.Count) {
                        TextCluster cluster = clusters[index];
                        int cells = ProjectedWidth(line.Substring(cluster.Start, cluster.Length));
                        if (column + cells > width) break;
                        column += cells;
                        if (line[cluster.Start] == ' ') lastBreak = index + 1;
                        index++;
                    }
                    int start = first < clusters.Count ? lineStart + clusters[first].Start : contentEnd;
                    if (index >= clusters.Count) {
                        rows.Add(new[] { start, lineEnd < 0 ? text.Length : lineEnd + 1 });
                        break;
                    }
                    int cut = lastBreak > first ? lastBreak : Math.Max(index, first + 1);
                    int end = cut < clusters.Count ? lineStart + clusters[cut].Start : contentEnd;
                    rows.Add(new[] { start, end });
                    first = cut;
                }
                if (lineEnd < 0) break;
                lineStart = lineEnd + 1;
            }
        }

        private void Move(int index) {
            Caret = Math.Max(0, Math.Min(_text.Length, index));
            PreferredColumn = -1;
        }

        private void Delete(int start, int end) {
            if (end <= start) {
                return;
            }
            _text.Remove(start, end - start);
            Caret = start;
            PreferredColumn = -1;
        }

        private bool Vertical(int delta) {
            if (Rows.Count == 0) {
                return false;
            }
            int row = RowIndex(Caret);
            int target = Math.Max(0, Math.Min(Rows.Count - 1, row + delta));
            if (PreferredColumn < 0) {
                PreferredColumn = ProjectedWidth(_text.ToString(Rows[row][0], Caret - Rows[row][0]));
            }
            int start = Rows[target][0];
            int end = RowEnd(Rows[target]);
            int column = 0;
            int index = start;
            string targetText = _text.ToString(start, end - start);
            foreach (TextCluster cluster in DisplayWidth.Clusters(targetText)) {
                int width = ProjectedWidth(targetText.Substring(cluster.Start, cluster.Length));
                if (column + width > PreferredColumn) break;
                column += width;
                index = start + cluster.Start + cluster.Length;
            }
            int keep = PreferredColumn;
            Caret = index;
            PreferredColumn = keep;
            return true;
        }

        private int RowIndex(int index) {
            for (int r = Rows.Count - 1; r >= 0; r--) {
                if (index >= Rows[r][0]) {
                    return r;
                }
            }
            return 0;
        }

        private int[] RowOf(int index) => Rows.Count == 0 ? new[] { 0, _text.Length } : Rows[RowIndex(index)];

        // The last caret position on a row: before its line break, or before the first character of the next wrapped row.
        private int RowEnd(int[] row) {
            int end = Math.Min(row[1], _text.Length);
            if (end > row[0] && end <= _text.Length && end - 1 < _text.Length && _text[end - 1] == '\n') {
                return end - 1;
            }
            return end;
        }

        private int Previous(int index) {
            if (index <= 0) {
                return 0;
            }
            index--;
            if (index > 0 && char.IsLowSurrogate(_text[index]) && char.IsHighSurrogate(_text[index - 1])) {
                index--;
            }
            return index;
        }

        private int Next(int index) {
            if (index >= _text.Length) {
                return _text.Length;
            }
            index++;
            if (index < _text.Length && char.IsLowSurrogate(_text[index]) && char.IsHighSurrogate(_text[index - 1])) {
                index++;
            }
            return index;
        }

        private int WordStart(int index) {
            while (index > 0 && char.IsWhiteSpace(_text[index - 1])) {
                index--;
            }
            while (index > 0 && !char.IsWhiteSpace(_text[index - 1])) {
                index--;
            }
            return index;
        }

        private int WordEnd(int index) {
            while (index < _text.Length && char.IsWhiteSpace(_text[index])) {
                index++;
            }
            while (index < _text.Length && !char.IsWhiteSpace(_text[index])) {
                index++;
            }
            return index;
        }
    }
}
