using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A preset the app comes with (<see cref="Menu.Presets"/>): selections of selection items and values of forms under
    /// one name. A <see cref="MenuItem.Presets"/> item lists it before the presets people save, marked as built in; it
    /// cannot be overwritten or deleted, and applying it changes what it names the way a saved preset does (items and
    /// fields it does not name keep their values). Configure it with the chainable methods:
    /// <c>preset.Selection(targets, "SRV01", "SRV02").Form(patch, values)</c>.
    /// </summary>
    public sealed class MenuPreset {
        private readonly List<KeyValuePair<MenuItem, List<object?>>> _selections = new List<KeyValuePair<MenuItem, List<object?>>>();
        private readonly List<KeyValuePair<Form, IDictionary<string, object?>>> _forms = new List<KeyValuePair<Form, IDictionary<string, object?>>>();

        /// <summary>Creates a built-in preset.</summary>
        /// <param name="name">The name; a saved preset cannot take it.</param>
        public MenuPreset(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("A preset needs a name.", nameof(name));
            }
            Name = name;
        }

        /// <summary>The name.</summary>
        public string Name { get; }

        /// <summary>A sentence about the preset, shown while it has focus in the list, or null.</summary>
        public string? Description { get; set; }

        /// <summary>Content shown beside the list (or below it when narrow) while the preset has focus, such as what it sets; or null.</summary>
        public IWidget? Preview { get; set; }

        /// <summary>The forms whose values the preset sets, with those values.</summary>
        internal IReadOnlyList<KeyValuePair<Form, IDictionary<string, object?>>> FormValues => _forms;

        /// <summary>Sets the description.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The preset.</returns>
        public MenuPreset Describe(string text) {
            Description = text;
            return this;
        }

        /// <summary>Sets the preview (see <see cref="Preview"/>).</summary>
        /// <param name="preview">The content.</param>
        /// <returns>The preset.</returns>
        public MenuPreset WithPreview(IWidget preview) {
            Preview = preview;
            return this;
        }

        /// <summary>Selects values in a selection item (single or multi) of the menu; values it does not list are skipped.</summary>
        /// <param name="item">The item.</param>
        /// <param name="values">The values to select (the choices' values).</param>
        /// <returns>The preset.</returns>
        public MenuPreset Selection(MenuItem item, params object?[] values) => Selection(item, (IEnumerable<object?>)(values ?? new object?[0]));

        /// <summary>Selects values in a selection item (single or multi) of the menu; values it does not list are skipped.</summary>
        /// <param name="item">The item.</param>
        /// <param name="values">The values to select (the choices' values).</param>
        /// <returns>The preset.</returns>
        public MenuPreset Selection(MenuItem item, IEnumerable<object?> values) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            _selections.RemoveAll(pair => ReferenceEquals(pair.Key, item));
            _selections.Add(new KeyValuePair<MenuItem, List<object?>>(item, new List<object?>(values ?? new object?[0])));
            return this;
        }

        /// <summary>Sets field values of a form opened from the menu (see <see cref="Interactive.Form.Apply(IDictionary{string, object})"/> for how they apply).</summary>
        /// <param name="form">The form.</param>
        /// <param name="values">Values by field name, as the fields take them: a string, a number, a bool, or a list for a multi-choice field.</param>
        /// <returns>The preset.</returns>
        public MenuPreset Form(Form form, IDictionary<string, object?> values) {
            if (form == null) {
                throw new ArgumentNullException(nameof(form));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            _forms.RemoveAll(pair => ReferenceEquals(pair.Key, form));
            _forms.Add(new KeyValuePair<Form, IDictionary<string, object?>>(form, new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase)));
            return this;
        }

        /// <summary>Applies built-in choices by value equality, keeping persistence's text matching out of this path.</summary>
        internal void ApplySelections(Menu menu) {
            foreach (MenuItem item in menu.Items) {
                foreach (KeyValuePair<MenuItem, List<object?>> pair in _selections) {
                    if (ReferenceEquals(pair.Key, item)) {
                        if (item.Choices.Count == 0 && item.ChoicesLoader != null) {
                            item.SetSelection(pair.Value);
                            continue;
                        }
                        List<object?> selected = new List<object?>();
                        foreach (Choice choice in item.Choices) {
                            if (choice.IsEnabled && pair.Value.Contains(choice.Value)) {
                                selected.Add(choice.Value);
                                if (item.Kind == MenuItemKind.Select) {
                                    break;
                                }
                            }
                        }
                        item.SetSelection(selected);
                    }
                }
                if (item.SubmenuDefinition != null) {
                    ApplySelections(item.SubmenuDefinition);
                }
            }
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
