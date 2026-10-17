using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>A dialog editing several lines of text (a text area field opened from a form).</summary>
    internal sealed class TextAreaDialog : Modal {
        private readonly string _title;
        private readonly TextAreaEditor _editor;
        private readonly Func<string, string?> _validate;
        private readonly Action<string> _accept;
        private readonly PromptLabels _labels;
        private readonly int _height;
        private readonly string? _placeholder;
        private int _scroll;
        private string? _error;

        public TextAreaDialog(string title, string? initial, int maxLength, int maxLines, int height, string? placeholder, Func<string, string?> validate, Action<string> accept, PromptLabels labels) {
            _title = title;
            _editor = new TextAreaEditor(initial) { MaxLength = maxLength, MaxLines = maxLines };
            _validate = validate;
            _accept = accept;
            _labels = labels;
            _height = Math.Max(2, height);
            _placeholder = placeholder;
        }

        public override int MaxWidth => 76;

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            Rows rows = new Rows();
            TextAreaWidget widget = new TextAreaWidget(_editor, caret) { Height = Math.Min(_height, Math.Max(2, frame.Height - 10)), Placeholder = _placeholder, ScrollRow = _scroll };
            rows.Add(new Scrolled(widget, row => _scroll = row));
            string counter = PromptLabels.Format(_labels.TextAreaCounter, _editor.LineCount, _editor.Length);
            rows.Add(new UiLine(context => _error != null ? ChoicePane.Problem(_error, context) : StyledLine.From(counter, context.Style(Styling.ThemeToken.Muted))));
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings));
            return DialogChrome.Frame(rows, _title);
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(_labels.KeySave, () => {
                string text = _editor.Text.TrimEnd('\n', ' ');
                _error = _validate(text);
                if (_error == null) {
                    Close();
                    _accept(text);
                }
            }, new KeyChord(ConsoleKey.Enter, KeyModifiers.Control), new KeyChord(ConsoleKey.S, KeyModifiers.Control)) { KeyText = "ctrl+enter", Order = 30 });
            bindings.Add(new KeyBinding(_labels.KeyNewLine, () => { }) { KeyText = "enter", Order = 32 });
            bindings.Add(new KeyBinding(_labels.KeyCancel, Close, new KeyChord(ConsoleKey.Escape)) { Order = 40 });
        }

        public override bool HandleUnbound(InputEvent input) {
            _error = null;
            return _editor.Handle(input);
        }

        // Keeps the text area's scroll position between frames (the widget is rebuilt each frame).
        private sealed class Scrolled : IWidget {
            private readonly TextAreaWidget _inner;
            private readonly Action<int> _keep;

            public Scrolled(TextAreaWidget inner, Action<int> keep) {
                _inner = inner;
                _keep = keep;
            }

            public Size Measure(Constraints constraints, Canvas.RenderContext context) => _inner.Measure(constraints, context);

            public void Render(Canvas.Region region, Canvas.RenderContext context) {
                _inner.Render(region, context);
                _keep(_inner.ScrollRow);
            }
        }
    }
}
