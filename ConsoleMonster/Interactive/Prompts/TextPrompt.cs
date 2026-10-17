using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A line of text: a default (shown as a hint, accepted with Enter on an empty line, Tab puts it in the box to edit),
    /// a placeholder, required/length/pattern checks and a custom validator with an inline error, and history on Up/Down.
    /// </summary>
    public sealed class TextPrompt : Prompt<string> {
        /// <summary>Creates a text prompt.</summary>
        /// <param name="title">The question.</param>
        public TextPrompt(string title) : base(title) {
        }

        /// <summary>Muted text in the empty box.</summary>
        public string? Placeholder { get; set; }

        /// <summary>Whether an empty answer is refused (when there is no default).</summary>
        public bool Required { get; set; }

        /// <summary>The fewest characters accepted.</summary>
        public int MinLength { get; set; }

        /// <summary>The most characters the box takes.</summary>
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary>A pattern the whole answer must match.</summary>
        public string? Pattern { get; set; }

        /// <summary>The message when <see cref="Pattern"/> does not match.</summary>
        public string? PatternMessage { get; set; }

        /// <summary>A validator: returns an error message, or null when the answer is fine.</summary>
        public Func<string, string?>? Validator { get; set; }

        /// <summary>Earlier answers offered with Up and Down (oldest first).</summary>
        public IList<string> History { get; } = new List<string>();

        /// <summary>Starts with the default in the box (editable) instead of showing it as a hint.</summary>
        public bool PrefillDefault { get; set; }

        /// <inheritdoc />
        public override string Describe(string value) => value;

        internal string? Validate(string text, PromptLabels labels) {
            if (text.Length == 0) {
                return Required ? labels.Required : null;
            }
            if (text.Length < MinLength) {
                return PromptLabels.Format(labels.MinimumLength, MinLength);
            }
            if (!string.IsNullOrEmpty(Pattern) && !Regex.IsMatch(text, "^(?:" + Pattern + ")$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))) {
                return PatternMessage ?? labels.InvalidFormat;
            }
            return Validator?.Invoke(text);
        }

        internal override PromptView<string> CreateView(PromptLabels labels) => new TextView(this, labels);

        internal override string AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            return lines.Text(Title, Description, HasDefault ? Default : null, text => Validate(text, lines.Labels), false, cancellationToken);
        }

        private sealed class TextView : EntryView<string> {
            private readonly TextPrompt _prompt;

            public TextView(TextPrompt prompt, PromptLabels labels) : base(prompt, labels, prompt.PrefillDefault && prompt.HasDefault ? prompt.Default : null) {
                _prompt = prompt;
                Editor.MaxLength = prompt.MaxLength;
                foreach (string entry in prompt.History) {
                    Editor.History.Add(entry);
                }
            }

            protected override string? Fill => _prompt.HasDefault && !_prompt.PrefillDefault && !string.IsNullOrEmpty(_prompt.Default) ? _prompt.Default : null;

            protected override string? Hint => _prompt.HasDefault && !_prompt.PrefillDefault && !string.IsNullOrEmpty(_prompt.Default) ? PromptLabels.Format(Labels.DefaultHint, _prompt.Default) : null;

            protected override string? Placeholder => _prompt.Placeholder;

            protected override string? TryParse(string text, out string value) {
                value = text.Length == 0 && _prompt.HasDefault ? _prompt.Default : text;
                return _prompt.Validate(value, Labels);
            }

            protected override void AddEntryBindings(List<KeyBinding> bindings, ViewFrame frame) {
                if (Editor.HasHistory) {
                    bindings.Add(new KeyBinding(Labels.KeyHistory, () => { }) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 20 });
                    bindings.Add(new KeyBinding(string.Empty, () => Editor.HistoryStep(-1), new KeyChord(ConsoleKey.UpArrow)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => Editor.HistoryStep(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
                }
            }
        }
    }
}
