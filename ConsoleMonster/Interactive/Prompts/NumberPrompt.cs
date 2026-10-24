using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A number: whole or decimal, with a minimum and maximum shown as a hint, Up/Down stepping by <see cref="Step"/>
    /// (PgUp/PgDn by ten steps), and an inline error for text that is not a number or out of range.
    /// </summary>
    public sealed class NumberPrompt : Prompt<decimal> {
        /// <summary>Creates a number prompt.</summary>
        /// <param name="title">The question.</param>
        public NumberPrompt(string title) : base(title) {
        }

        /// <summary>Whether only whole numbers are accepted.</summary>
        public bool Integer { get; set; }

        /// <summary>The smallest accepted value.</summary>
        public decimal? Minimum { get; set; }

        /// <summary>The largest accepted value.</summary>
        public decimal? Maximum { get; set; }

        /// <summary>The step of Up and Down.</summary>
        public decimal Step { get; set; } = 1;

        /// <summary>The culture that parses and formats the number; null uses the current culture.</summary>
        public CultureInfo? Culture { get; set; }

        /// <summary>A validator after range checks: returns an error message, or null.</summary>
        public Func<decimal, string?>? Validator { get; set; }

        private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

        /// <inheritdoc />
        public override string Describe(decimal value) => Format(value);

        /// <summary>Full precision for the editor and stepping, independent of compact hints.</summary>
        internal string FormatInput(decimal value) => DecimalInput.Format(value, Integer, EffectiveCulture);

        internal string Format(decimal value) => value.ToString(Integer ? "0" : "0.############", EffectiveCulture);

        internal string? Parse(string text, PromptLabels labels, out decimal value) {
            value = 0;
            string trimmed = text.Trim();
            if (trimmed.Length == 0) {
                if (HasDefault) {
                    value = Default;
                    return Check(value, labels);
                }
                return labels.Required;
            }
            if (!DecimalInput.TryParse(trimmed, Integer, EffectiveCulture, out value)) {
                return Integer ? labels.NotAWholeNumber : labels.NotANumber;
            }
            if (Integer && decimal.Truncate(value) != value) {
                return labels.NotAWholeNumber;
            }
            return Check(value, labels);
        }

        private string? Check(decimal value, PromptLabels labels) {
            if (Minimum.HasValue && value < Minimum.Value) {
                return PromptLabels.Format(labels.Minimum, Format(Minimum.Value));
            }
            if (Maximum.HasValue && value > Maximum.Value) {
                return PromptLabels.Format(labels.Maximum, Format(Maximum.Value));
            }
            return Validator?.Invoke(value);
        }

        internal string? RangeHint(PromptLabels labels) {
            string? range = null;
            if (Minimum.HasValue && Maximum.HasValue) {
                range = Format(Minimum.Value) + "–" + Format(Maximum.Value);
            } else if (Minimum.HasValue) {
                range = "≥ " + Format(Minimum.Value);
            } else if (Maximum.HasValue) {
                range = "≤ " + Format(Maximum.Value);
            }
            string? defaultHint = HasDefault ? PromptLabels.Format(labels.DefaultHint, Format(Default)) : null;
            if (range != null && defaultHint != null) {
                return range + ", " + defaultHint;
            }
            return range ?? defaultHint;
        }

        internal override PromptView<decimal> CreateView(PromptLabels labels) => new NumberView(this, labels);

        internal override decimal AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            string text = lines.Text(Title + (RangeHint(lines.Labels) is string hint ? " [" + hint + "]" : string.Empty), Description, null, t => Parse(t, lines.Labels, out _), false, cancellationToken);
            Parse(text, lines.Labels, out decimal value);
            return value;
        }

        private sealed class NumberView : EntryView<decimal> {
            private readonly NumberPrompt _prompt;

            public NumberView(NumberPrompt prompt, PromptLabels labels) : base(prompt, labels, prompt.HasDefault ? prompt.FormatInput(prompt.Default) : null) {
                _prompt = prompt;
            }

            protected override string? Hint => _prompt.RangeHint(Labels);

            protected override string? TryParse(string text, out decimal value) => _prompt.Parse(text, Labels, out value);

            protected override void AddEntryBindings(List<KeyBinding> bindings, ViewFrame frame) {
                bindings.Add(new KeyBinding(Labels.KeyStep, () => { }) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 20 });
                bindings.Add(new KeyBinding(string.Empty, () => StepBy(1), new KeyChord(ConsoleKey.UpArrow)) { InFooter = false });
                bindings.Add(new KeyBinding(string.Empty, () => StepBy(-1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
                bindings.Add(new KeyBinding(string.Empty, () => StepBy(10), new KeyChord(ConsoleKey.PageUp)) { InFooter = false });
                bindings.Add(new KeyBinding(string.Empty, () => StepBy(-10), new KeyChord(ConsoleKey.PageDown)) { InFooter = false });
            }

            private void StepBy(int steps) {
                decimal current;
                if (_prompt.Parse(Editor.Text, Labels, out current) != null && !decimal.TryParse(Editor.Text, NumberStyles.Number, _prompt.EffectiveCulture, out current)) {
                    current = _prompt.HasDefault ? _prompt.Default : _prompt.Minimum ?? 0;
                }
                decimal next = current + steps * _prompt.Step;
                if (_prompt.Minimum.HasValue && next < _prompt.Minimum.Value) {
                    next = _prompt.Minimum.Value;
                }
                if (_prompt.Maximum.HasValue && next > _prompt.Maximum.Value) {
                    next = _prompt.Maximum.Value;
                }
                Editor.Text = _prompt.FormatInput(next);
                Error = null;
            }

            protected override bool HandleUnbound(InputEvent input) {
                if (input.IsText) {
                    char c = input.Char;
                    string separators = _prompt.EffectiveCulture.NumberFormat.NumberDecimalSeparator + _prompt.EffectiveCulture.NumberFormat.NumberGroupSeparator + ".,-+  ";
                    if (!char.IsDigit(c) && separators.IndexOf(c) < 0) {
                        return false;
                    }
                }
                return base.HandleUnbound(input);
            }
        }
    }
}
