using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A form or wizard: fields on one or more pages, a review page with the equivalent command line, named presets and
    /// (opt-in) remembered choices. Show it on its own (<see cref="Show"/>) or open it from a menu item.
    /// </summary>
    public sealed partial class Form {
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

        /// <summary>
        /// The presets the form comes with: listed before the presets people save, marked as built in, never overwritten
        /// or deleted, and applied the same way (by <see cref="FormOptions.Preset"/> or the menu's command palette). A
        /// saved preset whose name matches a built-in one (ignoring case) is hidden.
        /// </summary>
        public IList<FormPreset> Presets { get; } = new List<FormPreset>();

        /// <summary>Adds a built-in preset (see <see cref="Presets"/>).</summary>
        /// <param name="name">The name; a saved preset cannot take it.</param>
        /// <param name="values">Field values by field name, as the fields take them (a list for a multi-choice field).</param>
        /// <param name="description">A sentence about it, or null.</param>
        /// <returns>The form.</returns>
        public Form Preset(string name, IDictionary<string, object?> values, string? description = null) {
            Presets.Add(new FormPreset(name, values) { Description = description });
            return this;
        }

        /// <summary>Adds a built-in preset and configures it (see <see cref="Presets"/>): <c>p =&gt; p.Value("Path", @"D:\Weekly").Describe("...")</c>.</summary>
        /// <param name="name">The name; a saved preset cannot take it.</param>
        /// <param name="configure">Sets the preset's values, description and preview.</param>
        /// <returns>The form.</returns>
        public Form Preset(string name, Action<FormPreset> configure) {
            FormPreset preset = new FormPreset(name, new Dictionary<string, object?>());
            configure?.Invoke(preset);
            Presets.Add(preset);
            return this;
        }

        /// <summary>The built-in preset with a name (ignoring case), or null.</summary>
        internal FormPreset? FindPreset(string name) {
            foreach (FormPreset preset in Presets) {
                if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    return preset;
                }
            }
            return null;
        }

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
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public TextField Text(string key, string? label = null) => Current.Text(key, label);

        /// <summary>Adds a secret field to the last page.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public SecretField Secret(string key, string? label = null) => Current.Secret(key, label);

        /// <summary>Adds a number field to the last page.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public NumberField Number(string key, string? label = null) => Current.Number(key, label);

        /// <summary>Adds a toggle to the last page.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public ToggleField Toggle(string key, string? label = null) => Current.Toggle(key, label);

        /// <summary>Adds a single-choice field to the last page.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices, or null when a loader provides them.</param>
        /// <returns>The field.</returns>
        public ChoiceField Select(string key, string? label = null, IEnumerable<Choice>? choices = null) => Current.Select(key, label, choices);

        /// <summary>Adds a multi-choice field to the last page.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <param name="choices">The choices, or null when a loader provides them.</param>
        /// <returns>The field.</returns>
        public ChoiceField MultiSelect(string key, string? label = null, IEnumerable<Choice>? choices = null) => Current.MultiSelect(key, label, choices);

        /// <summary>Adds a path field to the last page.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        /// <returns>The field.</returns>
        public PathField Path(string key, string? label = null) => Current.Path(key, label);

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

        /// <summary>Applies the choices marked selected as the starting values of choice fields without a default.</summary>
        internal void SeedDefaults() {
            foreach (Field field in Fields) {
                if (field is ChoiceField choice) {
                    choice.SeedFromChoices();
                }
            }
        }

        /// <summary>Finds a field by key.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The field, or null.</returns>
        public Field? Find(string key) {
            foreach (Field field in Fields) {
                if (string.Equals(field.Key, key, StringComparison.OrdinalIgnoreCase)) {
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
                values.Set(field.Key, field.Value, true);
            }
            FormValues result = new FormValues();
            foreach (Field field in Fields) {
                bool visible = field.VisibleWhen == null || SafeVisible(field, values);
                result.Set(field.Key, field.Value, visible);
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
                if (string.IsNullOrEmpty(field.Parameter) || !values.IsVisible(field.Key) || field.IsEmpty(field.Value) || field.IsDefault(field.Value)) {
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
                    stored[field.Key] = value;
                }
            }
            return stored;
        }

        /// <summary>Applies stored values (a preset or remembered choices); returns the names that could not be applied yet.</summary>
        /// <param name="stored">The values.</param>
        /// <returns>Names whose options are not loaded yet (applied after loading).</returns>
        public IReadOnlyList<string> ApplyStored(IDictionary<string, object?> stored) {
            if (stored == null) {
                throw new ArgumentNullException(nameof(stored));
            }
            return new List<string>(Assign(stored, true).Keys);
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
