using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Pick one value from a list: type to filter (fuzzy, matches highlighted), groups, hotkeys, descriptions, badges,
    /// disabled choices with their reason, paging and an optional preview pane. Returns the value itself (any object).
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public sealed class SelectPrompt<T> : Prompt<T> {
        /// <summary>Creates a selection from values.</summary>
        /// <param name="title">The question.</param>
        /// <param name="values">The values.</param>
        /// <param name="display">The title of a value; null uses its text.</param>
        /// <param name="group">The group of a value; null for none.</param>
        public SelectPrompt(string title, IEnumerable<T> values, Func<T, string>? display = null, Func<T, string?>? group = null) : base(title) {
            Choices = Choice.From(values, display, group);
        }

        /// <summary>Creates a selection from prepared choices (their values must be <typeparamref name="T"/>).</summary>
        /// <param name="title">The question.</param>
        /// <param name="choices">The choices.</param>
        public SelectPrompt(string title, IEnumerable<Choice> choices) : base(title) {
            Choices = new List<Choice>(choices ?? throw new ArgumentNullException(nameof(choices)));
        }

        /// <summary>The choices, in display order.</summary>
        public IList<Choice> Choices { get; }

        /// <summary>Preview content for the focused value.</summary>
        public Func<T, IWidget?>? Preview { get; set; }

        /// <summary>Preview text for the focused value (wrapped in the preview pane).</summary>
        public Func<T, string?>? PreviewText { get; set; }

        /// <summary>The preferred number of visible rows.</summary>
        public int PageSize { get; set; } = 10;

        /// <inheritdoc />
        public override string Describe(T value) {
            foreach (Choice c in Choices) {
                if (Equals(c.Value, value)) {
                    return c.Label;
                }
            }
            return Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
        }

        internal Func<Choice, IWidget?>? PreviewFactory() {
            if (Preview != null) {
                return c => c.Value is T v ? Preview(v) : null;
            }
            if (PreviewText != null) {
                return c => c.Value is T v && PreviewText(v) is string text ? new UiParagraph(text, Styling.ThemeToken.Text, 40) : null;
            }
            return null;
        }

        internal override PromptView<T> CreateView(PromptLabels labels) => new SelectView(this, labels);

        internal override T AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            int defaultIndex = -1;
            if (HasDefault) {
                for (int i = 0; i < Choices.Count; i++) {
                    if (Equals(Choices[i].Value, Default)) {
                        defaultIndex = i;
                    }
                }
            }
            int index = lines.Select(Title, Description, new List<Choice>(Choices), defaultIndex, cancellationToken);
            return (T)Choices[index].Value!;
        }

        private sealed class SelectView : ListPromptView<T> {
            private readonly SelectPrompt<T> _prompt;

            public SelectView(SelectPrompt<T> prompt, PromptLabels labels) : base(prompt, labels, prompt.Choices, false, prompt.PageSize, prompt.PreviewFactory()) {
                _prompt = prompt;
                if (prompt.HasDefault) {
                    foreach (Choice c in prompt.Choices) {
                        if (Equals(c.Value, prompt.Default)) {
                            List.FocusChoice(c);
                            break;
                        }
                    }
                }
            }

            protected override void Submit() {
                Choice? focused = List.FocusedChoice;
                if (focused == null) {
                    return;
                }
                if (!focused.IsEnabled) {
                    List.Message = PromptLabels.Format(Labels.Disabled, focused.DisabledReason);
                    return;
                }
                Accept((T)focused.Value!);
            }

            protected override void Picked(Choice choice) => Accept((T)choice.Value!);
        }
    }
}
