using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A secret (password, token): masked or hidden while typed, with Ctrl+R to reveal it for a moment, required by default.
    /// The answer never appears in summaries, logs or presets.
    /// </summary>
    public sealed class SecretPrompt : Prompt<string> {
        /// <summary>Creates a secret prompt.</summary>
        /// <param name="title">The question.</param>
        public SecretPrompt(string title) : base(title) {
        }

        /// <summary>How the secret is shown while typed.</summary>
        public SecretDisplay Display { get; set; } = SecretDisplay.Masked;

        /// <summary>Whether Ctrl+R can reveal the typed text.</summary>
        public bool AllowReveal { get; set; } = true;

        /// <summary>Whether an empty answer is refused.</summary>
        public bool Required { get; set; } = true;

        /// <summary>The fewest characters accepted.</summary>
        public int MinLength { get; set; }

        /// <summary>A validator: returns an error message, or null.</summary>
        public Func<string, string?>? Validator { get; set; }

        /// <inheritdoc />
        public override string Describe(string value) => "••••••••";

        internal string? Validate(string text, PromptLabels labels) {
            if (text.Length == 0) {
                return Required ? labels.Required : null;
            }
            if (text.Length < MinLength) {
                return PromptLabels.Format(labels.MinimumLength, MinLength);
            }
            return Validator?.Invoke(text);
        }

        internal override PromptView<string> CreateView(PromptLabels labels) => new SecretView(this, labels);

        internal override string AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            return lines.Text(Title, Description, null, text => Validate(text, lines.Labels), true, cancellationToken);
        }

        private sealed class SecretView : EntryView<string> {
            private readonly SecretPrompt _prompt;
            private bool _revealed;

            public SecretView(SecretPrompt prompt, PromptLabels labels) : base(prompt, labels, null) {
                _prompt = prompt;
            }

            protected override string? Mask(InteractiveGlyphs glyphs) {
                if (_revealed) {
                    return null;
                }
                if (_prompt.Display == SecretDisplay.Hidden) {
                    return string.Empty;
                }
                return glyphs.BracketFocus ? "*" : "•";
            }

            protected override string? TryParse(string text, out string value) {
                value = text;
                return _prompt.Validate(text, Labels);
            }

            protected override void AddEntryBindings(List<KeyBinding> bindings, ViewFrame frame) {
                if (_prompt.AllowReveal) {
                    bindings.Add(new KeyBinding(_revealed ? Labels.KeyHide : Labels.KeyReveal, () => _revealed = !_revealed, new KeyChord(ConsoleKey.R, KeyModifiers.Control)) { Order = 36 });
                }
            }

            public override IReadOnlyList<Text.StyledLine>? Summary(Canvas.RenderContext context) {
                _revealed = false;
                return base.Summary(context);
            }
        }
    }
}
