using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// One option in a selection prompt, menu or choice field: the value returned when it is chosen, the title shown, and
    /// optional group, description, hotkey, badge, disabled reason and initial selection.
    /// </summary>
    public sealed class Choice {
        /// <summary>Creates a choice.</summary>
        /// <param name="value">The value returned when chosen (any object).</param>
        /// <param name="label">The title shown; null uses the value's text.</param>
        public Choice(object? value, string? label = null) {
            Value = value;
            Label = label ?? Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
        }

        /// <summary>The value returned when chosen.</summary>
        public object? Value { get; }

        /// <summary>The title, also what the filter matches.</summary>
        public string Label { get; set; }

        /// <summary>A styled title shown instead of <see cref="Label"/> while no filter is active.</summary>
        public RichText? RichTitle { get; set; }

        /// <summary>A sentence shown under the list while the choice has focus.</summary>
        public string? Description { get; set; }

        /// <summary>The group heading the choice is listed under.</summary>
        public string? Group { get; set; }

        /// <summary>A key that picks the choice directly (letters and digits).</summary>
        public char? Hotkey { get; set; }

        /// <summary>Short right-aligned text: a count, a state, a tag ("new", "24 tasks").</summary>
        public RichText? Badge { get; set; }

        /// <summary>Why the choice cannot be picked; null when it can. Disabled choices are dimmed and show the reason on focus.</summary>
        public string? DisabledReason { get; set; }

        /// <summary>Whether a multi-selection starts with this choice selected.</summary>
        public bool Selected { get; set; }

        /// <summary>Content for the preview pane while the choice has focus (overrides the prompt's preview function).</summary>
        public IWidget? Preview { get; set; }

        /// <summary>Any extra object for the caller.</summary>
        public object? Tag { get; set; }

        /// <summary>Whether the choice can be picked.</summary>
        public bool IsEnabled => DisabledReason == null;

        /// <summary>Builds choices from values using the shared display and metadata selectors.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="choices">The choices.</param>
        /// <param name="display">The label for a value; null uses its text.</param>
        /// <param name="groupBy">The group heading for a value, or null.</param>
        /// <param name="describe">Help text for a value, or null.</param>
        /// <param name="badge">A styled badge for a value, or null.</param>
        /// <param name="disabledReason">Why a value cannot be selected; null or empty keeps it enabled.</param>
        /// <returns>The choices.</returns>
        public static List<Choice> From<T>(IEnumerable<T> choices, Func<T, string>? display = null, Func<T, string?>? groupBy = null,
            Func<T, string?>? describe = null, Func<T, RichText?>? badge = null, Func<T, string?>? disabledReason = null) {
            if (choices == null) {
                throw new ArgumentNullException(nameof(choices));
            }
            List<Choice> result = new List<Choice>();
            foreach (T value in choices) {
                if (value is Choice existing) {
                    result.Add(existing);
                    continue;
                }
                string? reason = disabledReason?.Invoke(value);
                result.Add(new Choice(value, display?.Invoke(value)) {
                    Group = groupBy?.Invoke(value), Description = describe?.Invoke(value), Badge = badge?.Invoke(value),
                    DisabledReason = string.IsNullOrEmpty(reason) ? null : reason
                });
            }
            return result;
        }

        /// <inheritdoc />
        public override string ToString() => Label;
    }
}
