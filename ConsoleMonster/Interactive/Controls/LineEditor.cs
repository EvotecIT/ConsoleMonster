using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A single-line text editor: insert, delete, word-wise movement and deletion, paste (line breaks become spaces), an
    /// optional length limit and history. Pure state; the widget that draws it is <see cref="TextInput"/>.
    /// </summary>
    internal sealed class LineEditor {
        private readonly StringBuilder _text = new StringBuilder();
        private readonly List<string> _history = new List<string>();
        private int _historyIndex = -1;
        private string _draft = string.Empty;

        public LineEditor(string? text = null) {
            if (!string.IsNullOrEmpty(text)) {
                _text.Append(text);
                Caret = _text.Length;
            }
        }

        public string Text {
            get => _text.ToString();
            set {
                _text.Clear();
                _text.Append(value ?? string.Empty);
                Caret = _text.Length;
                ScrollOffset = 0;
            }
        }

        /// <summary>The caret position in UTF-16 units (never inside a surrogate pair).</summary>
        public int Caret { get; private set; }

        /// <summary>The first visible cell when the text is wider than its box (kept by the widget).</summary>
        public int ScrollOffset { get; set; }

        public int MaxLength { get; set; } = int.MaxValue;

        public int Length => _text.Length;

        public bool IsEmpty => _text.Length == 0;

        /// <summary>Entries offered with Up and Down (oldest first).</summary>
        public IList<string> History => _history;

        public bool HasHistory => _history.Count > 0;

        /// <summary>Applies an edit; returns true when the event was an edit or caret movement.</summary>
        public bool Handle(InputEvent e) {
            if (e.Kind == InputEventKind.Paste) {
                Insert(Clean(e.Text ?? string.Empty));
                return true;
            }
            if (e.Kind != InputEventKind.Key) {
                return false;
            }
            if (e.IsText) {
                Insert(e.Char.ToString());
                return true;
            }
            bool ctrl = (e.Modifiers & KeyModifiers.Control) != 0;
            bool alt = (e.Modifiers & KeyModifiers.Alt) != 0;
            switch (e.Key) {
                case ConsoleKey.Backspace:
                    if (ctrl || alt) {
                        DeleteWordBefore();
                    } else {
                        DeleteBefore();
                    }
                    return true;
                case ConsoleKey.Delete:
                    if (ctrl) {
                        DeleteWordAfter();
                    } else {
                        DeleteAfter();
                    }
                    return true;
                case ConsoleKey.LeftArrow:
                    Caret = ctrl ? WordStart(Caret) : Previous(Caret);
                    return true;
                case ConsoleKey.RightArrow:
                    Caret = ctrl ? WordEnd(Caret) : Next(Caret);
                    return true;
                case ConsoleKey.Home:
                    Caret = 0;
                    return true;
                case ConsoleKey.End:
                    Caret = _text.Length;
                    return true;
                case ConsoleKey.W when ctrl:
                    DeleteWordBefore();
                    return true;
                case ConsoleKey.U when ctrl:
                    _text.Remove(0, Caret);
                    Caret = 0;
                    return true;
            }
            return false;
        }

        /// <summary>Moves through the history (Up = older); returns false without history.</summary>
        public bool HistoryStep(int direction) {
            if (_history.Count == 0) {
                return false;
            }
            if (_historyIndex < 0) {
                if (direction > 0) {
                    return false;
                }
                _draft = Text;
                _historyIndex = _history.Count;
            }
            int next = _historyIndex + direction;
            if (next < 0) {
                next = 0;
            }
            if (next >= _history.Count) {
                _historyIndex = -1;
                Text = _draft;
                return true;
            }
            _historyIndex = next;
            Text = _history[next];
            return true;
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
                if (text.Length > 0 && char.IsHighSurrogate(text[text.Length - 1])) {
                    text = text.Substring(0, text.Length - 1);
                }
            }
            _text.Insert(Caret, text);
            Caret += text.Length;
        }

        private static string Clean(string text) {
            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (c == '\r') {
                    if (i + 1 < text.Length && text[i + 1] == '\n') {
                        i++;
                    }
                    sb.Append(' ');
                } else if (c == '\n' || c == '\t') {
                    sb.Append(' ');
                } else if (c >= ' ' && c != '\u007f') {
                    sb.Append(c);
                }
            }
            return sb.ToString().TrimEnd();
        }

        private void DeleteBefore() {
            if (Caret == 0) {
                return;
            }
            int start = Previous(Caret);
            _text.Remove(start, Caret - start);
            Caret = start;
        }

        private void DeleteAfter() {
            if (Caret >= _text.Length) {
                return;
            }
            int end = Next(Caret);
            _text.Remove(Caret, end - Caret);
        }

        private void DeleteWordBefore() {
            int start = WordStart(Caret);
            _text.Remove(start, Caret - start);
            Caret = start;
        }

        private void DeleteWordAfter() {
            int end = WordEnd(Caret);
            _text.Remove(Caret, end - Caret);
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
            while (index > 0 && IsSeparator(_text[index - 1])) {
                index--;
            }
            while (index > 0 && !IsSeparator(_text[index - 1])) {
                index--;
            }
            return index;
        }

        private int WordEnd(int index) {
            while (index < _text.Length && IsSeparator(_text[index])) {
                index++;
            }
            while (index < _text.Length && !IsSeparator(_text[index])) {
                index++;
            }
            return index;
        }

        private static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c == '\\' || c == '/' || c == '.' || c == ',' || c == ';' || c == '-' || c == '_' || c == ':';
    }
}
