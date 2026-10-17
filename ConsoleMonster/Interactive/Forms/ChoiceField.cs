using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// One or several values from a list, given up front or loaded asynchronously from the values of the fields it depends
    /// on (choosing a site loads its servers): a spinner shows while loading, and loading can be cancelled.
    /// </summary>
    public sealed class ChoiceField : Field {
        /// <summary>Creates a choice field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="multiple">Whether several values can be chosen.</param>
        public ChoiceField(string name, string? label = null, bool multiple = false) : base(name, label) {
            Multiple = multiple;
        }

        /// <inheritdoc />
        public override FieldKind Kind => Multiple ? FieldKind.MultiChoice : FieldKind.Choice;

        /// <summary>Whether several values can be chosen.</summary>
        public bool Multiple { get; }

        /// <summary>The choices (replaced by the loader's result when one is set).</summary>
        public IList<Choice> Choices { get; } = new List<Choice>();

        /// <summary>Loads the choices from the current values (dependent fields).</summary>
        public Func<FormValues, CancellationToken, Task<IEnumerable<Choice>>>? Loader { get; set; }

        /// <summary>The fewest values (multiple).</summary>
        public int? Minimum { get; set; }

        /// <summary>The most values (multiple).</summary>
        public int? Maximum { get; set; }

        /// <summary>Preview content for the focused choice in the editor.</summary>
        public Func<Choice, IWidget?>? Preview { get; set; }

        /// <summary>
        /// Takes the starting value from the choices marked <see cref="Choice.Selected"/> when neither a default nor a
        /// value was given, so they behave as the default (left out of the command line while unchanged).
        /// </summary>
        internal void SeedFromChoices() {
            if (HasExplicitValue || Default != null) {
                return;
            }
            List<object?> selected = new List<object?>();
            foreach (Choice c in Choices) {
                if (c.Selected && c.IsEnabled) {
                    selected.Add(c.Value);
                }
            }
            if (selected.Count == 0) {
                return;
            }
            Default = Multiple ? selected : selected[0];
        }

        /// <summary>The number of choices that can be picked (the total shown next to a selection count).</summary>
        internal int EnabledCount {
            get {
                int count = 0;
                foreach (Choice c in Choices) {
                    if (c.IsEnabled) {
                        count++;
                    }
                }
                return count;
            }
        }

        internal Choice? Find(object? value) {
            foreach (Choice c in Choices) {
                if (Equals(c.Value, value)) {
                    return c;
                }
            }
            return null;
        }

        /// <inheritdoc />
        public override string Display(object? value, PromptLabels labels) {
            if (!Multiple) {
                Choice? c = Find(value);
                return c?.Title ?? base.Display(value, labels);
            }
            List<string> titles = new List<string>();
            if (value is IEnumerable list && !(value is string)) {
                foreach (object? item in list) {
                    titles.Add(Find(item)?.Title ?? Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty);
                }
            }
            if (titles.Count == 0) {
                return string.Empty;
            }
            if (titles.Count <= 3) {
                return string.Join(", ", titles.ToArray());
            }
            return string.Join(", ", titles.GetRange(0, 2).ToArray()) + " +" + (titles.Count - 2).ToString(CultureInfo.CurrentCulture);
        }

        internal override string? Check(object? value, FormValues values, PromptLabels labels) {
            if (Multiple) {
                int count = value is ICollection c ? c.Count : 0;
                if (Minimum.HasValue && count < Minimum.Value) {
                    return PromptLabels.Format(labels.MinimumSelected, Minimum.Value);
                }
                if (Maximum.HasValue && count > Maximum.Value) {
                    return PromptLabels.Format(labels.MaximumSelected, Maximum.Value);
                }
            }
            return base.Check(value, values, labels);
        }

        internal override object? ToStored(object? value) {
            if (!Multiple) {
                return value == null ? null : CommandLine.Format(value);
            }
            List<string> stored = new List<string>();
            if (value is IEnumerable list && !(value is string)) {
                foreach (object? item in list) {
                    if (item != null) {
                        stored.Add(CommandLine.Format(item));
                    }
                }
            }
            return stored;
        }

        internal override bool TryRestore(object? stored, out object? value) {
            value = null;
            if (!Multiple) {
                Choice? c = Match(stored as string);
                if (c == null) {
                    return false;
                }
                value = c.Value;
                return true;
            }
            List<object?> values = new List<object?>();
            if (stored is IEnumerable list && !(stored is string)) {
                foreach (object? item in list) {
                    Choice? c = Match(item as string);
                    if (c != null && c.IsEnabled) {
                        values.Add(c.Value);
                    }
                }
            }
            value = values;
            return true;
        }

        private Choice? Match(string? stored) {
            if (stored == null) {
                return null;
            }
            foreach (Choice c in Choices) {
                if (c.Value != null && string.Equals(CommandLine.Format(c.Value), stored, StringComparison.Ordinal)) {
                    return c;
                }
            }
            foreach (Choice c in Choices) {
                if (string.Equals(c.Title, stored, StringComparison.Ordinal)) {
                    return c;
                }
            }
            return null;
        }
    }
}
