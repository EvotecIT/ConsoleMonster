using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A preset a form comes with (<see cref="Form.Presets"/>): a name and field values. It is listed before the presets
    /// people save, marked as built in, and cannot be overwritten or deleted; applying it sets the fields the way a saved
    /// preset does.
    /// </summary>
    public sealed class FormPreset {
        /// <summary>Creates a built-in preset.</summary>
        /// <param name="name">The name; a saved preset cannot take it.</param>
        /// <param name="values">Field values by field name (ignoring case), as the fields take them: a string, a number, a bool, or a list for a multi-choice field.</param>
        public FormPreset(string name, IDictionary<string, object?> values) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("A preset needs a name.", nameof(name));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            Name = name;
            Values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>The name.</summary>
        public string Name { get; }

        /// <summary>A sentence about the preset, shown with it, or null.</summary>
        public string? Description { get; set; }

        /// <summary>The field values by field name (ignoring case).</summary>
        public IDictionary<string, object?> Values { get; }

        /// <summary>
        /// Content describing the preset (what it fills in), for lists that show a preview while a preset has focus; the
        /// command palette, where a form's presets are listed today, shows the <see cref="Description"/> only.
        /// </summary>
        public IWidget? Preview { get; set; }

        /// <summary>Sets a field's value.</summary>
        /// <param name="field">The field name (ignoring case).</param>
        /// <param name="value">The value, as the field takes it: a string, a number, a bool, or a list for a multi-choice field.</param>
        /// <returns>The preset.</returns>
        public FormPreset Value(string field, object? value) {
            if (string.IsNullOrEmpty(field)) {
                throw new ArgumentException("A field name is required.", nameof(field));
            }
            Values[field] = value;
            return this;
        }





        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
