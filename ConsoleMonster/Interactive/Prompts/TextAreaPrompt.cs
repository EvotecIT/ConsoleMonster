using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Several lines of text, for notes and change reasons: soft wrap, Enter for a new line, Ctrl+Enter (or Ctrl+S) to
    /// save, paste with line breaks kept, a line and length limit, required/minimum checks and a validator with an inline
    /// error, and a counter. In line mode the answer is read line by line until an empty line.
    /// </summary>
    public sealed class TextAreaPrompt : Prompt<string> {
        /// <summary>Creates the prompt.</summary>
        /// <param name="title">The question.</param>
        public TextAreaPrompt(string title) : base(title) {
        }

        /// <summary>Muted text in the empty box.</summary>
        public string? Placeholder { get; set; }

        /// <summary>Whether an empty answer is refused.</summary>
        public bool Required { get; set; }

        /// <summary>The fewest characters accepted.</summary>
        public int MinLength { get; set; }

        /// <summary>The most characters the box takes.</summary>
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary>The most lines the box takes.</summary>
        public int MaxLines { get; set; } = int.MaxValue;

        /// <summary>The visible rows (default 6); longer text scrolls.</summary>
        public int Height { get; set; } = 6;

        /// <summary>A validator: returns an error message, or null when the answer is fine.</summary>
        public Func<string, string?>? Validator { get; set; }

        /// <inheritdoc />
        public override string Describe(string value) {
            string first = value.Split('\n')[0];
            int lines = value.Length == 0 ? 0 : value.Split('\n').Length;
            return lines <= 1 ? first : first + " (+" + (lines - 1).ToString(CultureInfo.InvariantCulture) + ")";
        }

        internal string? Validate(string text, PromptLabels labels) {
            string trimmed = text.Trim();
            if (trimmed.Length == 0) {
                return Required ? labels.Required : null;
            }
            if (text.Length < MinLength) {
                return PromptLabels.Format(labels.MinimumLength, MinLength);
            }
            if (text.Length > MaxLength) {
                return PromptLabels.Format(labels.MaximumLength, MaxLength);
            }
            if (text.Split('\n').Length > MaxLines) {
                return PromptLabels.Format(labels.MaximumLines, MaxLines);
            }
            return Validator?.Invoke(text);
        }

        internal override PromptView<string> CreateView(PromptLabels labels) => new TextAreaView(this, labels);

        internal override string AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            return lines.Lines(Title, Description, HasDefault ? Default : null, text => Validate(text, lines.Labels), cancellationToken);
        }

        private sealed class TextAreaView : PromptView<string> {
            private readonly TextAreaPrompt _prompt;
            private readonly TextAreaEditor _editor;
            private readonly TextArea _widget;
            private bool _validateLive;

            public TextAreaView(TextAreaPrompt prompt, PromptLabels labels) : base(prompt, labels) {
                _prompt = prompt;
                _editor = new TextAreaEditor(prompt.HasDefault ? prompt.Default : null) { MaxLength = prompt.MaxLength, MaxLines = prompt.MaxLines };
                _widget = new TextArea(_editor, Caret) { Height = Math.Max(2, prompt.Height), Placeholder = prompt.Placeholder };
            }

            protected override IWidget Build(ViewFrame frame) {
                Rows rows = new Rows();
                rows.Add(Header(null));
                if (!string.IsNullOrEmpty(Prompt.Description)) {
                    rows.Add(new Padding(new UiParagraph(Prompt.Description!), 2, 0, 0, 0));
                }
                rows.Add(new Padding(_widget, 2, 0, 1, 0));
                string lines = _prompt.MaxLines < int.MaxValue ? _editor.LineCount.ToString(CultureInfo.InvariantCulture) + " of " + _prompt.MaxLines.ToString(CultureInfo.InvariantCulture) : _editor.LineCount.ToString(CultureInfo.InvariantCulture);
                string counter = PromptLabels.Format(Labels.TextAreaCounter, lines, _editor.Length);
                rows.Add(MessageLine(Error, counter));
                rows.Add(Footer(frame));
                return rows;
            }

            public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
                bindings.Add(new KeyBinding(Labels.KeySave, Submit, new KeyChord(ConsoleKey.Enter, KeyModifiers.Control), new KeyChord(ConsoleKey.S, KeyModifiers.Control)) { KeyText = "ctrl+enter", Order = 30 });
                bindings.Add(new KeyBinding(Labels.KeyNewLine, () => { }) { KeyText = "enter", Order = 32 });
                bindings.Add(new KeyBinding(Labels.KeyCancel, Decline, new KeyChord(ConsoleKey.Escape)) { Order = 40 });
            }

            private void Submit() {
                string text = _editor.Text.TrimEnd('\n', ' ');
                string? error = _prompt.Validate(text, Labels);
                if (error != null) {
                    Error = error;
                    _validateLive = true;
                    return;
                }
                Error = null;
                Accept(text);
            }

            protected override bool HandleUnbound(InputEvent input) {
                bool changed = _editor.Handle(input);
                if (changed && _validateLive) {
                    Error = _prompt.Validate(_editor.Text.TrimEnd('\n', ' '), Labels);
                }
                return changed;
            }
        }
    }
}
