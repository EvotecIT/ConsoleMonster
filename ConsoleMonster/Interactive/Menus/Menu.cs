using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A menu tree: items that run actions, open submenus, forms, selections and pages. <see cref="Run"/> shows it as an
    /// app (Shell), a centred menu (Classic) or a scrollback list (Inline), with a breadcrumb, Back/Home/Exit, the command
    /// palette (Ctrl+K or :), the key map (?) and a footer generated from the active keys.
    /// </summary>
    public sealed class Menu {
        /// <summary>Creates a menu.</summary>
        /// <param name="title">The title (the app name for the root menu).</param>
        public Menu(string title) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        /// <summary>The title.</summary>
        public string Title { get; set; }

        /// <summary>A sentence shown above the items.</summary>
        public string? Description { get; set; }

        /// <summary>Muted text at the right of the title bar (a version, an environment).</summary>
        public string? Subtitle { get; set; }

        /// <summary>The layout this menu prefers when <see cref="MenuOptions.Layout"/> is Auto.</summary>
        public MenuLayout Layout { get; set; } = MenuLayout.Auto;

        /// <summary>The items.</summary>
        public IList<MenuItem> Items { get; } = new List<MenuItem>();

        /// <summary>Extra sources for the command palette.</summary>
        public IList<ICommandProvider> CommandProviders { get; } = new List<ICommandProvider>();

        /// <summary>App-wide key bindings (shown in the key map and, by priority, in the footer).</summary>
        public IList<KeyBinding> KeyBindings { get; } = new List<KeyBinding>();

        /// <summary>Adds an item and configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the item.</param>
        /// <returns>The menu.</returns>
        public Menu Item(string title, Action<MenuItem>? configure = null) {
            MenuItem item = new MenuItem(title);
            configure?.Invoke(item);
            Items.Add(item);
            return this;
        }

        /// <summary>Adds an item.</summary>
        /// <param name="item">The item.</param>
        /// <returns>The menu.</returns>
        public Menu Add(MenuItem item) {
            Items.Add(item ?? throw new ArgumentNullException(nameof(item)));
            return this;
        }

        /// <summary>Adds a divider with an optional heading.</summary>
        /// <param name="title">The heading, or null.</param>
        /// <returns>The menu.</returns>
        public Menu Separator(string? title = null) {
            Items.Add(new MenuItem(title ?? string.Empty) { Kind = MenuItemKind.Separator });
            return this;
        }

        /// <summary>Adds the exit item.</summary>
        /// <param name="title">The title; null uses "Exit".</param>
        /// <returns>The menu.</returns>
        public Menu Exit(string? title = null) {
            Items.Add(new MenuItem(title ?? PromptLabels.Default.Exit) { Kind = MenuItemKind.Exit, HotkeyChar = null });
            return this;
        }

        /// <summary>Finds an item by title anywhere in the tree.</summary>
        /// <param name="title">The title.</param>
        /// <returns>The item, or null.</returns>
        public MenuItem? Find(string title) {
            foreach (MenuItem item in Items) {
                if (string.Equals(item.Title, title, StringComparison.OrdinalIgnoreCase)) {
                    return item;
                }
                MenuItem? nested = item.SubmenuDefinition?.Find(title);
                if (nested != null) {
                    return nested;
                }
            }
            return null;
        }

        /// <summary>Shows the menu until the user exits.</summary>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Ends the menu.</param>
        public void Run(MenuOptions? options = null, CancellationToken cancellationToken = default) {
            MenuRunner.Run(this, options ?? new MenuOptions(), cancellationToken);
        }

        /// <summary>Shows the menu on a worker thread until the user exits.</summary>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Ends the menu.</param>
        /// <returns>Completion.</returns>
        public Task RunAsync(MenuOptions? options = null, CancellationToken cancellationToken = default) {
            return Task.Factory.StartNew(() => Run(options, cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        internal bool HasContent {
            get {
                foreach (MenuItem item in Items) {
                    if (item.Kind == MenuItemKind.Form || item.Kind == MenuItemKind.Select || item.Kind == MenuItemKind.MultiSelect || item.Kind == MenuItemKind.Page || item.Kind == MenuItemKind.Submenu) {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
