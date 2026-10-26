using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// An entry of a <see cref="Menu"/>: title, description, hotkey, icon, badge, state text, a disabled reason, and what it
    /// opens (an action, a submenu, a form, a selection, a page). Configure it with the chainable methods:
    /// <c>item.Hotkey('r').Form(form).Configure(value =&gt; value.Badge = () =&gt; RichText.Plain("24 tasks"))</c>.
    /// </summary>
    public sealed class MenuItem {
        private readonly List<Choice> _choices = new List<Choice>();
        private bool _selectionInitialized;
        private readonly List<object?> _selection = new List<object?>();

        /// <summary>Creates an item.</summary>
        /// <param name="label">The title.</param>
        public MenuItem(string label) {
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        /// <summary>The title.</summary>
        public string Label { get; set; }

        /// <summary>A sentence shown with the item.</summary>
        public string? Description { get; set; }

        /// <summary>The key that opens the item directly.</summary>
        public char? Hotkey { get; set; }

        /// <summary>A short mark before the title (one or two cells), as free text. Setting it clears a preceding semantic IconName.</summary>
        public string? Icon { get => _icon; set { _icon = value; IconName = null; } }
        private string? _icon;

        /// <summary>A semantic icon resolved for the glyph tier (shapes in Full, WGL4 in Basic, ASCII in Ascii), or null.</summary>
        public GlyphIcon? IconName { get; set; }

        /// <summary>What the item does.</summary>
        public MenuItemKind Kind { get; set; } = MenuItemKind.Action;

        /// <summary>Computes the badge (right-aligned tag) each time the menu is drawn.</summary>
        public Func<RichText?>? Badge { get; set; }

        /// <summary>Computes the state text ("24 selected") each time the menu is drawn.</summary>
        public Func<string?>? Detail { get; set; }

        /// <summary>Computes why the item is unavailable (null when available).</summary>
        public Func<string?>? DisabledReason { get; set; }

        private Action<MenuContext>? _action;
        /// <summary>The action of an action item.</summary>
        public Action<MenuContext>? Action { get => _action; set { _action = value; if (value != null) { Kind = MenuItemKind.Action; } } }

        private Func<MenuContext, CancellationToken, Task>? _actionAsync;
        /// <summary>The asynchronous action of an action item (runs without blocking the screen; Esc cancels it).</summary>
        public Func<MenuContext, CancellationToken, Task>? ActionAsync { get => _actionAsync; set { _actionAsync = value; if (value != null) { Kind = MenuItemKind.Action; } } }

        private Menu? _submenu;
        /// <summary>The submenu of a submenu item.</summary>
        public Menu? Submenu { get => _submenu; set { _submenu = value; if (value != null) { Kind = MenuItemKind.Submenu; } } }

        private Form? _form;
        /// <summary>The form of a form item.</summary>
        public Form? Form { get => _form; set { _form = value; if (value != null) { Kind = MenuItemKind.Form; } } }

        /// <summary>Called when the form is submitted.</summary>
        public Action<MenuContext, FormResult>? OnSubmit { get; set; }

        private Func<MenuContext, IWidget>? _page;
        /// <summary>Builds the content of a page item.</summary>
        public Func<MenuContext, IWidget>? Page { get => _page; set { _page = value; if (value != null) { Kind = MenuItemKind.Page; } } }

        /// <summary>The choices of a selection item.</summary>
        public IList<Choice> Choices => _choices;

        /// <summary>Preview content for the focused choice of a selection item.</summary>
        public Func<Choice, IWidget?>? Preview { get; set; }

        /// <summary>The fewest values of a multi-selection item.</summary>
        public int? Minimum { get; set; }

        /// <summary>The most values of a multi-selection item.</summary>
        public int? Maximum { get; set; }

        private Func<MenuContext, CancellationToken, Task<IEnumerable<Choice>>>? _loadChoices;
        /// <summary>Loads the choices of a selection item asynchronously (a spinner shows meanwhile; Ctrl+R reloads).</summary>
        public Func<MenuContext, CancellationToken, Task<IEnumerable<Choice>>>? LoadChoices { get => _loadChoices; set { _loadChoices = value; if (value != null) { Kind = MenuItemKind.MultiSelect; } } }

        private Func<MenuContext, ReviewModel>? _review;
        /// <summary>Builds the review of a review item each time it is drawn.</summary>
        public Func<MenuContext, ReviewModel>? Review { get => _review; set { _review = value; if (value != null) { Kind = MenuItemKind.Review; } } }

        /// <summary>Called when the selection of a selection item changes.</summary>
        public Action<MenuContext>? OnChange { get; set; }

        /// <summary>The selected values of a selection item, in list order.</summary>
        public IReadOnlyList<object?> Selection => _selection;

        /// <summary>The selected value of a single-selection item, or null.</summary>
        public object? SelectedValue => _selection.Count > 0 ? _selection[0] : null;

        internal object? Tag { get; set; }

        /// <summary>The badge now.</summary>
        public RichText? CurrentBadge => Safe(Badge);

        /// <summary>The state text now.</summary>
        public string? CurrentDetail => Safe(Detail) ?? DefaultState();

        /// <summary>The disabled reason now, or null.</summary>
        public string? CurrentDisabledReason => Safe(DisabledReason);

        private string? DefaultState() {
            if (Kind == MenuItemKind.MultiSelect) {
                int enabled = 0;
                foreach (Choice c in _choices) {
                    if (c.IsEnabled) {
                        enabled++;
                    }
                }
                return _selection.Count.ToString(System.Globalization.CultureInfo.CurrentCulture) + "/" + enabled.ToString(System.Globalization.CultureInfo.CurrentCulture);
            }
            if (Kind == MenuItemKind.Select && _selection.Count > 0) {
                foreach (Choice c in _choices) {
                    if (Equals(c.Value, _selection[0])) {
                        return c.Label;
                    }
                }
            }
            return null;
        }

        private static T? Safe<T>(Func<T?>? source) where T : class {
            if (source == null) {
                return null;
            }
            try {
                return source();
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>Sets the selected values of a selection item (pre-selection, presets, programmatic changes).</summary>
        /// <param name="values">The values.</param>
        public void SetSelection(IEnumerable<object?> values) {
            List<object?> copy = new List<object?>(values);
            _selection.Clear();
            _selection.AddRange(copy);
            _selectionInitialized = true;
        }

        /// <summary>The selection as typed values.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <returns>The values.</returns>
        public IReadOnlyList<T> GetSelection<T>() {
            List<T> list = new List<T>();
            foreach (object? v in _selection) {
                if (v is T typed) {
                    list.Add(typed);
                }
            }
            return list;
        }









        /// <summary>The icon text for a glyph set: the semantic icon in its tier, else the free text, else null.</summary>
        /// <param name="glyphs">The glyph set.</param>
        /// <returns>The icon text, or null.</returns>
        public string? IconText(IGlyphSet glyphs) => IconName.HasValue ? glyphs.Icon(IconName.Value) : (string.IsNullOrEmpty(Icon) ? null : Icon);





















        /// <summary>Makes the item pick one value.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="values">The values.</param>
        /// <param name="display">The title of a value.</param>
        /// <param name="group">The group of a value.</param>
        /// <param name="preview">Preview content for a value.</param>
        /// <returns>The item.</returns>
        public MenuItem Select<T>(IEnumerable<T> values, Func<T, string>? display = null, Func<T, string?>? group = null, Func<T, IWidget?>? preview = null) {
            SetChoices(Choice.From(values, display, group), preview);
            Kind = MenuItemKind.Select;
            return this;
        }

        /// <summary>Makes the item pick several values.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="values">The values.</param>
        /// <param name="display">The title of a value.</param>
        /// <param name="group">The group of a value.</param>
        /// <param name="preview">Preview content for a value.</param>
        /// <returns>The item.</returns>
        public MenuItem MultiSelect<T>(IEnumerable<T> values, Func<T, string>? display = null, Func<T, string?>? group = null, Func<T, IWidget?>? preview = null) {
            SetChoices(Choice.From(values, display, group), preview);
            Kind = MenuItemKind.MultiSelect;
            List<object?> preselected = new List<object?>();
            foreach (Choice c in _choices) {
                if (c.Selected) {
                    preselected.Add(c.Value);
                }
            }
            SetSelection(preselected);
            return this;
        }





        /// <summary>
        /// Makes the item list the menu's presets: the built-in ones (<see cref="Menu.Presets"/> of the root menu) first,
        /// marked as built in, then the saved ones. Enter applies one; Delete removes a saved one (Shell and Classic layouts; the Inline layout applies them only). A saved preset whose name
        /// matches a built-in one (ignoring case) is hidden.
        /// </summary>
        /// <returns>The item.</returns>
        public MenuItem Presets() {
            Kind = MenuItemKind.Presets;
            return this;
        }

        /// <summary>Imports loaded choices, reconciling explicit selections once and dropping unavailable values.</summary>
        internal void ReplaceChoices(IEnumerable<Choice> choices) {
            _choices.Clear();
            _choices.AddRange(choices);
            List<object?> selected = new List<object?>();
            foreach (Choice choice in _choices) {
                if (!choice.IsEnabled) { continue; }
                bool wanted = !_selectionInitialized && choice.Selected;
                foreach (object? value in _selection) {
                    wanted |= Equals(choice.Value, value) || (choice.Value != null && value is string stored && CommandLine.Format(choice.Value) == stored);
                }
                if (wanted) {
                    selected.Add(choice.Value);
                    if (Kind == MenuItemKind.Select) { break; }
                }
            }
            foreach (Choice choice in _choices) { choice.Selected = selected.Contains(choice.Value) && choice.IsEnabled; }
            SetSelection(selected);
        }

        private void SetChoices<T>(List<Choice> choices, Func<T, IWidget?>? preview) {
            _choices.Clear();
            _choices.AddRange(choices);
            Preview = preview == null ? null : c => c.Value is T v ? preview(v) : null;
        }

        /// <inheritdoc />
        public override string ToString() => Label;
    }
}
