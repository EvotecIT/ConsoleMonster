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
    /// <c>item.Hotkey('r').Form(form).Badge(() =&gt; "24 tasks")</c>.
    /// </summary>
    public sealed class MenuItem {
        private readonly List<Choice> _choices = new List<Choice>();
        private readonly List<object?> _selection = new List<object?>();

        /// <summary>Creates an item.</summary>
        /// <param name="title">The title.</param>
        public MenuItem(string title) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        /// <summary>The title.</summary>
        public string Title { get; set; }

        /// <summary>A sentence shown with the item.</summary>
        public string? Description { get; set; }

        /// <summary>The key that opens the item directly.</summary>
        public char? HotkeyChar { get; set; }

        /// <summary>A short mark before the title (one or two cells), as free text. <see cref="IconGlyph"/> takes precedence.</summary>
        public string? Icon { get; set; }

        /// <summary>A semantic icon resolved for the glyph tier (shapes in Full, WGL4 in Basic, ASCII in Ascii), or null.</summary>
        public GlyphIcon? IconGlyph { get; set; }

        /// <summary>What the item does.</summary>
        public MenuItemKind Kind { get; set; } = MenuItemKind.Action;

        /// <summary>Computes the badge (right-aligned tag) each time the menu is drawn.</summary>
        public Func<RichText?>? BadgeSource { get; set; }

        /// <summary>Computes the state text ("24 selected") each time the menu is drawn.</summary>
        public Func<string?>? StateSource { get; set; }

        /// <summary>Computes why the item is unavailable (null when available).</summary>
        public Func<string?>? DisabledSource { get; set; }

        /// <summary>The action of an action item.</summary>
        public Action<MenuContext>? Handler { get; set; }

        /// <summary>The asynchronous action of an action item (runs without blocking the screen; Esc cancels it).</summary>
        public Func<MenuContext, CancellationToken, Task>? AsyncHandler { get; set; }

        /// <summary>The submenu of a submenu item.</summary>
        public Menu? SubmenuDefinition { get; set; }

        /// <summary>The form of a form item.</summary>
        public Form? FormDefinition { get; set; }

        /// <summary>Called when the form is submitted.</summary>
        public Action<MenuContext, FormResult>? FormSubmitted { get; set; }

        /// <summary>Builds the content of a page item.</summary>
        public Func<MenuContext, IWidget>? PageFactory { get; set; }

        /// <summary>The choices of a selection item.</summary>
        public IList<Choice> Choices => _choices;

        /// <summary>Preview content for the focused choice of a selection item.</summary>
        public Func<Choice, IWidget?>? Preview { get; set; }

        /// <summary>The fewest values of a multi-selection item.</summary>
        public int? Minimum { get; set; }

        /// <summary>The most values of a multi-selection item.</summary>
        public int? Maximum { get; set; }

        /// <summary>Loads the choices of a selection item asynchronously (a spinner shows meanwhile; Ctrl+R reloads).</summary>
        public Func<MenuContext, CancellationToken, Task<IEnumerable<Choice>>>? ChoicesLoader { get; set; }

        /// <summary>Builds the review of a review item each time it is drawn.</summary>
        public Func<MenuContext, ReviewModel>? ReviewFactory { get; set; }

        /// <summary>Called when the selection of a selection item changes.</summary>
        public Action<MenuContext>? SelectionChanged { get; set; }

        /// <summary>The selected values of a selection item, in list order.</summary>
        public IReadOnlyList<object?> Selection => _selection;

        /// <summary>The selected value of a single-selection item, or null.</summary>
        public object? SelectedValue => _selection.Count > 0 ? _selection[0] : null;

        internal object? Tag { get; set; }

        /// <summary>The badge now.</summary>
        public RichText? CurrentBadge => Safe(BadgeSource);

        /// <summary>The state text now.</summary>
        public string? CurrentState => Safe(StateSource) ?? DefaultState();

        /// <summary>The disabled reason now, or null.</summary>
        public string? CurrentDisabledReason => Safe(DisabledSource);

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
                        return c.Title;
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
            _selection.Clear();
            _selection.AddRange(values);
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

        /// <summary>Sets the hotkey.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The item.</returns>
        public MenuItem Hotkey(char key) {
            HotkeyChar = key;
            return this;
        }

        /// <summary>Sets the description.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The item.</returns>
        public MenuItem Describe(string text) {
            Description = text;
            return this;
        }

        /// <summary>Sets the icon.</summary>
        /// <param name="icon">One or two cells.</param>
        /// <returns>The item.</returns>
        public MenuItem WithIcon(string icon) {
            Icon = icon;
            IconGlyph = null;
            return this;
        }

        /// <summary>Sets a semantic icon that follows the glyph tier (see <see cref="GlyphIcon"/>).</summary>
        /// <param name="icon">The icon.</param>
        /// <returns>The item.</returns>
        public MenuItem WithIcon(GlyphIcon icon) {
            IconGlyph = icon;
            return this;
        }

        /// <summary>The icon text for a glyph set: the semantic icon in its tier, else the free text, else null.</summary>
        /// <param name="glyphs">The glyph set.</param>
        /// <returns>The icon text, or null.</returns>
        public string? IconText(IGlyphSet glyphs) => IconGlyph.HasValue ? glyphs.Icon(IconGlyph.Value) : (string.IsNullOrEmpty(Icon) ? null : Icon);

        /// <summary>Sets a computed badge.</summary>
        /// <param name="badge">Returns the badge text.</param>
        /// <returns>The item.</returns>
        public MenuItem Badge(Func<string?> badge) {
            BadgeSource = badge == null ? null : () => badge() is string s && s.Length > 0 ? RichText.Plain(s) : null;
            return this;
        }

        /// <summary>Sets a computed styled badge.</summary>
        /// <param name="badge">Returns the badge.</param>
        /// <returns>The item.</returns>
        public MenuItem Badge(Func<RichText?> badge) {
            BadgeSource = badge;
            return this;
        }

        /// <summary>Sets computed state text.</summary>
        /// <param name="state">Returns the state.</param>
        /// <returns>The item.</returns>
        public MenuItem State(Func<string?> state) {
            StateSource = state;
            return this;
        }

        /// <summary>Makes the item unavailable while the function returns a reason.</summary>
        /// <param name="reason">Returns the reason, or null when available.</param>
        /// <returns>The item.</returns>
        public MenuItem Disabled(Func<string?> reason) {
            DisabledSource = reason;
            return this;
        }

        /// <summary>Makes the item run an action.</summary>
        /// <param name="action">The action.</param>
        /// <returns>The item.</returns>
        public MenuItem Action(Action<MenuContext> action) {
            Handler = action;
            Kind = MenuItemKind.Action;
            return this;
        }

        /// <summary>Makes the item run an asynchronous action.</summary>
        /// <param name="action">The action.</param>
        /// <returns>The item.</returns>
        public MenuItem ActionAsync(Func<MenuContext, CancellationToken, Task> action) {
            AsyncHandler = action;
            Kind = MenuItemKind.Action;
            return this;
        }

        /// <summary>Makes the item open a submenu.</summary>
        /// <param name="menu">The submenu.</param>
        /// <returns>The item.</returns>
        public MenuItem Submenu(Menu menu) {
            SubmenuDefinition = menu;
            Kind = MenuItemKind.Submenu;
            return this;
        }

        /// <summary>Makes the item open a submenu built in place.</summary>
        /// <param name="configure">Adds the submenu's items.</param>
        /// <returns>The item.</returns>
        public MenuItem Submenu(Action<Menu> configure) {
            Menu menu = new Menu(Title);
            configure?.Invoke(menu);
            return Submenu(menu);
        }

        /// <summary>Makes the item open a form.</summary>
        /// <param name="form">The form.</param>
        /// <param name="submitted">Called when it is submitted.</param>
        /// <returns>The item.</returns>
        public MenuItem Form(Form form, Action<MenuContext, FormResult>? submitted = null) {
            FormDefinition = form;
            FormSubmitted = submitted;
            Kind = MenuItemKind.Form;
            return this;
        }

        /// <summary>Makes the item show content.</summary>
        /// <param name="page">Builds the content each time it is drawn.</param>
        /// <returns>The item.</returns>
        public MenuItem Page(Func<MenuContext, IWidget> page) {
            PageFactory = page;
            Kind = MenuItemKind.Page;
            return this;
        }

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

        /// <summary>Loads the choices of a selection item asynchronously instead of listing them up front.</summary>
        /// <param name="loader">Returns the choices.</param>
        /// <param name="multiple">Whether several values can be picked.</param>
        /// <returns>The item.</returns>
        public MenuItem LoadChoices(Func<MenuContext, CancellationToken, Task<IEnumerable<Choice>>> loader, bool multiple = true) {
            ChoicesLoader = loader;
            Kind = multiple ? MenuItemKind.MultiSelect : MenuItemKind.Select;
            return this;
        }

        /// <summary>Makes the item show a review (summary, equivalent command, Run, Copy command, Save preset).</summary>
        /// <param name="review">Builds the review from the app's current state.</param>
        /// <returns>The item.</returns>
        public MenuItem Review(Func<MenuContext, ReviewModel> review) {
            ReviewFactory = review;
            Kind = MenuItemKind.Review;
            return this;
        }

        /// <summary>
        /// Makes the item list the menu's presets: the built-in ones (<see cref="Menu.Presets"/> of the root menu) first,
        /// marked as built in, then the saved ones. Enter applies one; Delete removes a saved one. A saved preset whose name
        /// matches a built-in one (ignoring case) is hidden.
        /// </summary>
        /// <returns>The item.</returns>
        public MenuItem Presets() {
            Kind = MenuItemKind.Presets;
            return this;
        }

        internal void ReplaceChoices(IEnumerable<Choice> choices) {
            _choices.Clear();
            _choices.AddRange(choices);
        }

        private void SetChoices<T>(List<Choice> choices, Func<T, IWidget?>? preview) {
            _choices.Clear();
            _choices.AddRange(choices);
            Preview = preview == null ? null : c => c.Value is T v ? preview(v) : null;
        }

        /// <inheritdoc />
        public override string ToString() => Title;
    }
}
