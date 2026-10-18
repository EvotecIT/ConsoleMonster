using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Pick any number of values: everything the single selection does, plus tri-state group checkboxes (Space on a group
    /// toggles all of it; arrows collapse and expand), select all/none/invert, a minimum and maximum, a live "24 of 312
    /// selected" count and a selected-only view (Ctrl+O). Returns the chosen values in list order.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public sealed class MultiSelectPrompt<T> : Prompt<IReadOnlyList<T>> {
        /// <summary>Creates a multi-selection from values.</summary>
        /// <param name="title">The question.</param>
        /// <param name="values">The values.</param>
        /// <param name="display">The title of a value; null uses its text.</param>
        /// <param name="group">The group of a value; null for none.</param>
        public MultiSelectPrompt(string title, IEnumerable<T> values, Func<T, string>? display = null, Func<T, string?>? group = null) : base(title) {
            Choices = Choice.From(values, display, group);
        }

        /// <summary>Creates a multi-selection from prepared choices.</summary>
        /// <param name="title">The question.</param>
        /// <param name="choices">The choices.</param>
        public MultiSelectPrompt(string title, IEnumerable<Choice> choices) : base(title) {
            Choices = new List<Choice>(choices ?? throw new ArgumentNullException(nameof(choices)));
        }

        /// <summary>The choices, in display order.</summary>
        public IList<Choice> Choices { get; }

        /// <summary>The fewest values accepted.</summary>
        public int? Minimum { get; set; }

        /// <summary>The most values accepted.</summary>
        public int? Maximum { get; set; }

        /// <summary>Preview content for the focused value.</summary>
        public Func<T, IWidget?>? Preview { get; set; }

        /// <summary>Preview text for the focused value.</summary>
        public Func<T, string?>? PreviewText { get; set; }

        /// <summary>The preferred number of visible rows.</summary>
        public int PageSize { get; set; } = 12;

        /// <summary>Starts with every group collapsed (long grouped lists).</summary>
        public bool Collapsed { get; set; }

        /// <inheritdoc />
        public override string Describe(IReadOnlyList<T> value) {
            List<string> titles = new List<string>();
            foreach (T v in value) {
                foreach (Choice c in Choices) {
                    if (Equals(c.Value, v)) {
                        titles.Add(c.Title);
                        break;
                    }
                }
            }
            if (titles.Count == 0) {
                return PromptLabels.Default.KeyNone;
            }
            if (titles.Count <= 3) {
                return string.Join(", ", titles.ToArray());
            }
            return string.Join(", ", titles.GetRange(0, 3).ToArray()) + " +" + (titles.Count - 3).ToString(System.Globalization.CultureInfo.CurrentCulture);
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

        private void ApplyDefault() {
            if (!HasDefault || Default == null) {
                return;
            }
            foreach (Choice c in Choices) {
                foreach (T v in Default) {
                    if (Equals(c.Value, v)) {
                        c.Selected = true;
                    }
                }
            }
        }

        internal override PromptView<IReadOnlyList<T>> CreateView(PromptLabels labels) {
            ApplyDefault();
            return new MultiView(this, labels);
        }

        internal override IReadOnlyList<T> AskLines(LinePrompter lines, CancellationToken cancellationToken) {
            ApplyDefault();
            List<int> indexes = lines.MultiSelect(Title, Description, new List<Choice>(Choices), Minimum, Maximum, cancellationToken);
            List<T> result = new List<T>();
            foreach (int i in indexes) {
                result.Add((T)Choices[i].Value!);
            }
            return result;
        }

        private sealed class MultiView : ListPromptView<IReadOnlyList<T>> {
            public MultiView(MultiSelectPrompt<T> prompt, PromptLabels labels) : base(prompt, labels, prompt.Choices, true, prompt.PageSize, prompt.PreviewFactory()) {
                List.Minimum = prompt.Minimum;
                List.Maximum = prompt.Maximum;
                if (prompt.Collapsed) {
                    foreach (Choice c in prompt.Choices) {
                        List.SetCollapsed(c.Group, true);
                    }
                }
            }

            protected override void Submit() {
                string? problem = List.ValidateSelection();
                if (problem != null) {
                    Error = problem;
                    return;
                }
                List<T> values = new List<T>();
                foreach (Choice c in List.SelectedChoices) {
                    values.Add((T)c.Value!);
                }
                Accept(values);
            }

            protected override void Picked(Choice choice) {
                List.SetSelected(choice, !List.IsSelected(choice));
            }
        }
    }
}
