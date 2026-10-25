using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>A page of a form.</summary>
    public sealed class FormPage {
        /// <summary>Creates a page.</summary>
        /// <param name="title">The title.</param>
        public FormPage(string title) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        /// <summary>The title (also shown in the progress strip).</summary>
        public string Title { get; set; }

        /// <summary>A sentence under the title.</summary>
        public string? Description { get; set; }

        /// <summary>The fields.</summary>
        public IList<Field> Fields { get; } = new List<Field>();

        /// <summary>Adds a field.</summary>
        /// <typeparam key="T">The field type.</typeparam>
        /// <param name="field">The field.</param>
        /// <returns>The field.</returns>
        public T Add<T>(T field) where T : Field {
            Fields.Add(field ?? throw new ArgumentNullException(nameof(field)));
            return field;
        }

        /// <summary>Adds a text field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public TextField Text(string key, string? label = null) => Add(new TextField(key, label));

        /// <summary>Adds a field for several lines of text.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public TextAreaField TextArea(string key, string? label = null) => Add(new TextAreaField(key, label));

        /// <summary>Adds a secret field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public SecretField Secret(string key, string? label = null) => Add(new SecretField(key, label));

        /// <summary>Adds a number field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public NumberField Number(string key, string? label = null) => Add(new NumberField(key, label));

        /// <summary>Adds a toggle.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public ToggleField Toggle(string key, string? label = null) => Add(new ToggleField(key, label));

        /// <summary>Adds a single-choice field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices.</param>
        /// <returns>The field.</returns>
        public ChoiceField Select(string key, string? label = null, IEnumerable<Choice>? choices = null) {
            ChoiceField field = Add(new ChoiceField(key, label, false));
            if (choices != null) {
                foreach (Choice c in choices) {
                    field.Choices.Add(c);
                }
            }
            return field;
        }

        /// <summary>Adds a multi-choice field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices.</param>
        /// <returns>The field.</returns>
        public ChoiceField MultiSelect(string key, string? label = null, IEnumerable<Choice>? choices = null) {
            ChoiceField field = Add(new ChoiceField(key, label, true));
            if (choices != null) {
                foreach (Choice c in choices) {
                    field.Choices.Add(c);
                }
            }
            return field;
        }

        /// <summary>Adds a path field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public PathField Path(string key, string? label = null) => Add(new PathField(key, label));
    }
}
