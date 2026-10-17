using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A form or wizard: fields on one or more pages, a review page with the equivalent command line, named presets and
    /// (opt-in) remembered choices. Show it on its own (<see cref="Show"/>) or open it from a menu item.
    /// </summary>
    public sealed class Form {
        /// <summary>Creates a form.</summary>
        /// <param name="title">The title.</param>
        public Form(string title) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        /// <summary>The title.</summary>
        public string Title { get; set; }

        /// <summary>A sentence under the title.</summary>
        public string? Description { get; set; }

        /// <summary>The command the equivalent command line starts with (<c>Install-Toolbox</c>, <c>toolbox install</c>).</summary>
        public string? Command { get; set; }

        /// <summary>How the equivalent command line is written.</summary>
        public CommandStyle CommandStyle { get; set; } = CommandStyle.PowerShell;

        /// <summary>The pages, in order.</summary>
        public IList<FormPage> Pages { get; } = new List<FormPage>();

        /// <summary>Whether a review page (summary, equivalent command, run) comes after the last page.</summary>
        public bool Review { get; set; } = true;

        /// <summary>The caption of the button that submits the form; null uses "Run".</summary>
        public string? SubmitText { get; set; }

        /// <summary>The key presets and remembered choices are stored under; null uses the title.</summary>
        public string? StorageKey { get; set; }

        internal string Key => StorageKey ?? Title;

        /// <summary>Adds a page.</summary>
        /// <param name="title">The page title.</param>
        /// <param name="description">A sentence under it.</param>
        /// <returns>The page.</returns>
        public FormPage Page(string title, string? description = null) {
            FormPage page = new FormPage(title) { Description = description };
            Pages.Add(page);
            return page;
        }

        /// <summary>Adds a page and configures it.</summary>
        /// <param name="title">The page title.</param>
        /// <param name="configure">Adds the fields.</param>
        /// <returns>The form.</returns>
        public Form Page(string title, Action<FormPage> configure) {
            FormPage page = Page(title);
            configure?.Invoke(page);
            return this;
        }

        private FormPage Current {
            get {
                if (Pages.Count == 0) {
                    Pages.Add(new FormPage(Title));
                }
                return Pages[Pages.Count - 1];
            }
        }

        /// <summary>Adds a field to the last page.</summary>
        /// <typeparam name="T">The field type.</typeparam>
        /// <param name="field">The field.</param>
        /// <returns>The field.</returns>
        public T Add<T>(T field) where T : Field => Current.Add(field);

        /// <summary>Adds a text field to the last page.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public TextField Text(string name, string? label = null) => Current.Text(name, label);

        /// <summary>Adds a secret field to the last page.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public SecretField Secret(string name, string? label = null) => Current.Secret(name, label);

        /// <summary>Adds a number field to the last page.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public NumberField Number(string name, string? label = null) => Current.Number(name, label);

        /// <summary>Adds a toggle to the last page.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public ToggleField Toggle(string name, string? label = null) => Current.Toggle(name, label);

        /// <summary>Adds a single-choice field to the last page.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices, or null when a loader provides them.</param>
        /// <returns>The field.</returns>
        public ChoiceField Select(string name, string? label = null, IEnumerable<Choice>? choices = null) => Current.Select(name, label, choices);

        /// <summary>Adds a multi-choice field to the last page.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices, or null when a loader provides them.</param>
        /// <returns>The field.</returns>
        public ChoiceField MultiSelect(string name, string? label = null, IEnumerable<Choice>? choices = null) => Current.MultiSelect(name, label, choices);

        /// <summary>Adds a path field to the last page.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public PathField Path(string name, string? label = null) => Current.Path(name, label);

        /// <summary>Every field, page by page.</summary>
        public IEnumerable<Field> Fields {
            get {
                foreach (FormPage page in Pages) {
                    foreach (Field item in page.Fields) {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>Finds a field by name.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The field, or null.</returns>
        public Field? Find(string name) {
            foreach (Field field in Fields) {
                if (string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    return field;
                }
            }
            return null;
        }

        /// <summary>The current values, with visibility evaluated.</summary>
        /// <returns>The values.</returns>
        public FormValues GetValues() {
            FormValues values = new FormValues();
            foreach (Field field in Fields) {
                values.Set(field.Name, field.Value, true);
            }
            FormValues result = new FormValues();
            foreach (Field field in Fields) {
                bool visible = field.VisibleWhen == null || SafeVisible(field, values);
                result.Set(field.Name, field.Value, visible);
            }
            return result;
        }

        private static bool SafeVisible(Field field, FormValues values) {
            try {
                return field.VisibleWhen!(values);
            } catch (Exception) {
                return true;
            }
        }

        /// <summary>The command line equivalent to the current values.</summary>
        /// <returns>The command line.</returns>
        public string BuildCommand() {
            FormValues values = GetValues();
            List<string> arguments = new List<string>();
            foreach (Field field in Fields) {
                if (string.IsNullOrEmpty(field.Parameter) || !values.IsVisible(field.Name) || field.IsEmpty(field.Value) || field.IsDefault(field.Value)) {
                    continue;
                }
                object? value = field.ArgumentValue(field.Value);
                if (value is CommandLiteral literal) {
                    arguments.Add(CommandLine.Parameter(field.Parameter!, CommandStyle) + (CommandStyle == CommandStyle.Windows ? ":" : " ") + literal.Text);
                    continue;
                }
                if (value is bool b && !b && field.Default is true) {
                    arguments.Add(CommandStyle == CommandStyle.PowerShell ? CommandLine.Parameter(field.Parameter!, CommandStyle) + ":$false" : CommandLine.Argument("no-" + field.Parameter!.TrimStart('-', '/'), true, CommandStyle));
                    continue;
                }
                arguments.Add(CommandLine.Argument(field.Parameter!, value, CommandStyle));
            }
            return CommandLine.Join(Command ?? string.Empty, arguments);
        }

        /// <summary>The storable values (no secrets, only fields that are remembered).</summary>
        /// <returns>The values.</returns>
        public Dictionary<string, object?> ToStored() {
            Dictionary<string, object?> stored = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (Field field in Fields) {
                if (!field.Remember || field.Kind == FieldKind.Secret) {
                    continue;
                }
                object? value = field.ToStored(field.Value);
                if (value != null) {
                    stored[field.Name] = value;
                }
            }
            return stored;
        }

        /// <summary>Applies stored values (a preset or remembered choices); returns the names that could not be applied yet.</summary>
        /// <param name="stored">The values.</param>
        /// <returns>Names whose options are not loaded yet (applied after loading).</returns>
        public IReadOnlyList<string> ApplyStored(IDictionary<string, object?> stored) {
            List<string> pending = new List<string>();
            foreach (KeyValuePair<string, object?> pair in stored) {
                Field? field = Find(pair.Key);
                if (field == null || !field.Remember) {
                    continue;
                }
                if (field is ChoiceField choice && choice.Loader != null && choice.Choices.Count == 0) {
                    pending.Add(field.Name);
                    continue;
                }
                if (field.TryRestore(pair.Value, out object? value)) {
                    field.Value = value;
                }
            }
            return pending;
        }

        /// <summary>Shows the form and waits for it to be submitted or cancelled.</summary>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the form.</param>
        /// <returns>The result.</returns>
        public FormResult Show(FormOptions? options = null, CancellationToken cancellationToken = default) {
            return FormRunner.Run(this, options ?? new FormOptions(), cancellationToken);
        }

        /// <summary>Shows the form on a worker thread.</summary>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the form.</param>
        /// <returns>The result.</returns>
        public Task<FormResult> ShowAsync(FormOptions? options = null, CancellationToken cancellationToken = default) {
            return Task.Factory.StartNew(() => Show(options, cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
