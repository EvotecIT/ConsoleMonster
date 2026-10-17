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
        /// <typeparam name="T">The field type.</typeparam>
        /// <param name="field">The field.</param>
        /// <returns>The field.</returns>
        public T Add<T>(T field) where T : Field {
            Fields.Add(field ?? throw new ArgumentNullException(nameof(field)));
            return field;
        }

        /// <summary>Adds a text field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public TextField Text(string name, string? label = null) => Add(new TextField(name, label));

        /// <summary>Adds a field for several lines of text.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public TextAreaField TextArea(string name, string? label = null) => Add(new TextAreaField(name, label));

        /// <summary>Adds a secret field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public SecretField Secret(string name, string? label = null) => Add(new SecretField(name, label));

        /// <summary>Adds a number field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public NumberField Number(string name, string? label = null) => Add(new NumberField(name, label));

        /// <summary>Adds a toggle.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public ToggleField Toggle(string name, string? label = null) => Add(new ToggleField(name, label));

        /// <summary>Adds a single-choice field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices.</param>
        /// <returns>The field.</returns>
        public ChoiceField Select(string name, string? label = null, IEnumerable<Choice>? choices = null) {
            ChoiceField field = Add(new ChoiceField(name, label, false));
            if (choices != null) {
                foreach (Choice c in choices) {
                    field.Choices.Add(c);
                }
            }
            return field;
        }

        /// <summary>Adds a multi-choice field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices.</param>
        /// <returns>The field.</returns>
        public ChoiceField MultiSelect(string name, string? label = null, IEnumerable<Choice>? choices = null) {
            ChoiceField field = Add(new ChoiceField(name, label, true));
            if (choices != null) {
                foreach (Choice c in choices) {
                    field.Choices.Add(c);
                }
            }
            return field;
        }

        /// <summary>Adds a path field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public PathField Path(string name, string? label = null) => Add(new PathField(name, label));
    }
}
