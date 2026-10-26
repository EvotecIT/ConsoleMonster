using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.Apps.Commander {
    /// <summary>
    /// The Commander's command registry: every command with its keys, label and availability, plus key routing
    /// (including type-to-search), the F-key bar and key bindings for the app host. F-keys follow Norton and Midnight
    /// Commander; each has an Alt+number alternative for terminals that swallow F-keys.
    /// </summary>
    public sealed class CommanderCommands : IReadOnlyList<CommanderCommand> {
        private readonly CommanderCommand[] _commands;

        /// <summary>Creates a registry. Keys and key chords must be unique, and each key-bar slot holds one command.</summary>
        /// <param name="commands">The commands.</param>
        public CommanderCommands(IEnumerable<CommanderCommand> commands) {
            _commands = (commands ?? throw new ArgumentNullException(nameof(commands))).ToArray();
            HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<KeyChord, string> chords = new Dictionary<KeyChord, string>();
            HashSet<int> slots = new HashSet<int>();
            foreach (CommanderCommand command in _commands) {
                if (command == null) { throw new ArgumentException("A command is null.", nameof(commands)); }
                if (!keys.Add(command.Key)) { throw new ArgumentException("The command key '" + command.Key + "' is used twice.", nameof(commands)); }
                if (command.FunctionKey < 0 || command.FunctionKey > 10) { throw new ArgumentException("'" + command.Key + "' has key-bar slot " + command.FunctionKey + "; slots are 1 to 10.", nameof(commands)); }
                if (command.FunctionKey != 0 && !slots.Add(command.FunctionKey)) { throw new ArgumentException("Key-bar slot " + command.FunctionKey + " is used twice.", nameof(commands)); }
                foreach (KeyChord chord in command.Keys) {
                    if (chords.TryGetValue(chord, out string? other)) { throw new ArgumentException("'" + chord + "' is bound to both '" + other + "' and '" + command.Key + "'.", nameof(commands)); }
                    chords[chord] = command.Key;
                }
            }
        }

        /// <summary>The number of commands.</summary>
        public int Count => _commands.Length;

        /// <summary>A command by position.</summary>
        /// <param name="index">The position.</param>
        /// <returns>The command.</returns>
        public CommanderCommand this[int index] => _commands[index];

        /// <summary>A command by key, or null.</summary>
        /// <param name="key">The key, such as <c>copy</c>.</param>
        /// <returns>The command.</returns>
        public CommanderCommand? Find(string key) => _commands.FirstOrDefault(command => string.Equals(command.Key, key, StringComparison.OrdinalIgnoreCase));

        /// <summary>The command bound to a key event, or null.</summary>
        /// <param name="input">The event.</param>
        /// <returns>The command.</returns>
        public CommanderCommand? Match(InputEvent input) => input == null ? null : _commands.FirstOrDefault(command => command.Matches(input));

        /// <summary>
        /// Routes a key: while a quick search is active, typing extends it, Backspace shortens it and Esc ends it; otherwise
        /// the bound command runs when available. Unbound printable characters start a quick search.
        /// </summary>
        /// <param name="input">The event.</param>
        /// <param name="context">The context.</param>
        /// <returns>Whether the key was handled; a bound but unavailable command still consumes its key.</returns>
        public bool Dispatch(InputEvent input, CommanderContext context) {
            if (input == null || context == null || input.Kind != InputEventKind.Key) { return false; }
            CommanderPanel panel = context.Active;
            if (panel.QuickSearch.Length != 0) {
                if (input.Is(ConsoleKey.Escape)) { panel.ClearSearch(); return true; }
                if (input.Is(ConsoleKey.Backspace)) { panel.BackspaceSearch(); return true; }
                if (input.IsText) { panel.AppendSearch(input.Char); return true; }
            }
            CommanderCommand? command = Match(input);
            if (command != null) {
                if (command.Key != "quick-search") { panel.ClearSearch(); }
                command.TryExecute(context);
                return true;
            }
            if (input.IsText && !char.IsControl(input.Char) && !char.IsWhiteSpace(input.Char)) {
                panel.AppendSearch(input.Char);
                return true;
            }
            return false;
        }

        /// <summary>The ten F-key bar slots for the current context.</summary>
        /// <param name="context">The context.</param>
        /// <param name="alternatives">Whether to show the Alt+number keys, for terminals that swallow F-keys.</param>
        /// <returns>Ten slots, numbered 1 to 10.</returns>
        public IReadOnlyList<CommanderKeyBarItem> KeyBar(CommanderContext context, bool alternatives = false) {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }
            CommanderKeyBarItem[] items = new CommanderKeyBarItem[10];
            for (int number = 1; number <= 10; number++) {
                CommanderCommand? command = _commands.FirstOrDefault(candidate => candidate.FunctionKey == number);
                string keyText = alternatives ? "Alt+" + (number % 10).ToString(System.Globalization.CultureInfo.InvariantCulture) : "F" + number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                items[number - 1] = new CommanderKeyBarItem(number, command?.BarLabel ?? command?.Label ?? string.Empty, keyText, command != null && command.IsAvailable(context), command);
            }
            return items;
        }

        /// <summary>The commands as key bindings for the app host's routing, footer and key map.</summary>
        /// <param name="context">The context their actions run in.</param>
        /// <returns>One binding per command that has keys.</returns>
        public IReadOnlyList<KeyBinding> Bindings(CommanderContext context) => _commands.Where(command => command.Keys.Count != 0).Select(command => command.ToKeyBinding(context)).ToArray();

        /// <inheritdoc />
        public IEnumerator<CommanderCommand> GetEnumerator() => ((IEnumerable<CommanderCommand>)_commands).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>The standard Commander commands.</summary>
        /// <returns>A new registry.</returns>
        public static CommanderCommands CreateDefault() {
            List<CommanderCommand> commands = new List<CommanderCommand>();
            void Add(string key, string label, Action<CommanderContext> execute, string group, int slot, string? bar, bool host, Func<CommanderContext, bool>? available, params string[] keys) {
                commands.Add(new CommanderCommand(key, label, execute, keys.Select(Chord).ToArray()) { Group = group, FunctionKey = slot, BarLabel = bar, RequiresHost = host, Available = available });
            }
            Func<CommanderContext, bool> file = context => context.Active.Current is PanelEntry entry && !entry.IsFolder;
            Func<CommanderContext, bool> targets = context => context.Active.Targets.Count != 0;

            Add("help", "Help", context => context.Host.ShowHelp(), "App", 1, "Help", true, null, "F1", "Alt+1");
            Add("menu", "Menu", context => context.Host.ShowMenu(), "App", 2, "Menu", true, null, "F2", "Alt+2");
            Add("view", "View file", context => context.Host.View(context.Active.Current!), "Files", 3, "View", true, file, "F3", "Alt+3");
            Add("edit", "Edit file", context => context.Host.Edit(context.Active.Current!), "Files", 4, "Edit", true, file, "F4", "Alt+4");
            Add("copy", "Copy to the other panel", context => Start(context, context.Model.PlanCopy()), "Files", 5, "Copy", true, targets, "F5", "Alt+5");
            Add("move", "Move or rename", context => Start(context, context.Model.PlanMove()), "Files", 6, "Move", true, targets, "F6", "Alt+6");
            Add("mkdir", "New folder", context => context.Host.Prompt("New folder", string.Empty, name => {
                if (!string.IsNullOrWhiteSpace(name)) { context.Host.Start(context.Model.PlanCreateFolder(name)); }
            }), "Files", 7, "Mkdir", true, null, "F7", "Alt+7");
            Add("delete", "Delete to the trash", context => Start(context, context.Model.PlanDelete()), "Files", 8, "Delete", true, targets, "F8", "Alt+8", "Delete");
            Add("delete-permanently", "Delete permanently", context => Start(context, context.Model.PlanDelete(true)), "Files", 0, null, true, targets, "Shift+F8", "Shift+Delete");
            Add("palette", "Command palette", context => context.Host.ShowPalette(), "App", 9, "Palette", true, null, "F9", "Alt+9");
            Add("quit", "Quit", context => context.Host.Quit(), "App", 10, "Quit", true, null, "F10", "Alt+0", "Ctrl+Q");

            Add("switch-panel", "Switch panel", context => context.Model.SwitchPanels(), "Panels", 0, null, false, null, "Tab");
            Add("swap-panels", "Swap panels", context => context.Model.SwapPanels(), "Panels", 0, null, false, null, "Ctrl+\\");
            Add("same-folder", "Same folder in both panels", context => context.Model.SameFolder(), "Panels", 0, null, false, null, "Ctrl+U");
            Add("refresh", "Refresh both panels", context => context.Model.RefreshAll(), "Panels", 0, null, false, null, "Ctrl+R");
            Add("left-root", "Choose drive for the left panel", context => context.Host.ChooseRoot(CommanderSide.Left), "Panels", 0, null, true, null, "Alt+F1");
            Add("right-root", "Choose drive for the right panel", context => context.Host.ChooseRoot(CommanderSide.Right), "Panels", 0, null, true, null, "Alt+F2");
            Add("output", "Show command output", context => context.Host.ShowOutput(), "Panels", 0, null, true, null, "Ctrl+O");
            Add("hidden", "Show or hide hidden files", context => context.Active.SetShowHidden(!context.Active.ShowHidden), "Panels", 0, null, false, null);

            Add("up", "Up", context => context.Active.MoveCursor(-1), "Navigation", 0, null, false, null, "Up");
            Add("down", "Down", context => context.Active.MoveCursor(1), "Navigation", 0, null, false, null, "Down");
            Add("page-up", "Page up", context => context.Active.PageUp(), "Navigation", 0, null, false, null, "PgUp");
            Add("page-down", "Page down", context => context.Active.PageDown(), "Navigation", 0, null, false, null, "PgDn");
            Add("first", "First row", context => context.Active.MoveToStart(), "Navigation", 0, null, false, null, "Home");
            Add("last", "Last row", context => context.Active.MoveToEnd(), "Navigation", 0, null, false, null, "End");
            Add("open", "Open folder or run file", context => {
                if (!context.Active.OpenCurrent() && context.Active.Current is PanelEntry entry && !entry.IsFolder && context.Host.Supports("open")) { context.Host.Open(entry); }
            }, "Navigation", 0, null, false, context => context.Active.Current != null, "Enter");
            Add("parent", "Parent folder", context => context.Active.GoToParent(), "Navigation", 0, null, false, null, "Backspace", "Ctrl+PgUp");
            Add("quick-search", "Quick search (next match)", context => context.Active.SearchNext(), "Navigation", 0, null, false, null, "Ctrl+S");

            Add("mark", "Mark or unmark and move down", context => context.Active.ToggleMark(), "Marks", 0, null, false, null, "Insert", "Space");
            Add("mark-pattern", "Mark by pattern", context => context.Host.Prompt("Mark files", "*", pattern => context.Active.MarkPattern(pattern)), "Marks", 0, null, true, null, "+");
            Add("unmark-pattern", "Unmark by pattern", context => context.Host.Prompt("Unmark files", "*", pattern => context.Active.UnmarkPattern(pattern)), "Marks", 0, null, true, null, "-");
            Add("invert-marks", "Invert marks", context => context.Active.InvertMarks(), "Marks", 0, null, false, null, "*");

            Add("sort-name", "Sort by name", context => context.Active.ToggleSort(PanelSortColumn.Name), "Sort", 0, null, false, null, "Ctrl+F3");
            Add("sort-extension", "Sort by extension", context => context.Active.ToggleSort(PanelSortColumn.Extension), "Sort", 0, null, false, null, "Ctrl+F4");
            Add("sort-modified", "Sort by modified", context => context.Active.ToggleSort(PanelSortColumn.Modified), "Sort", 0, null, false, null, "Ctrl+F5");
            Add("sort-size", "Sort by size", context => context.Active.ToggleSort(PanelSortColumn.Size), "Sort", 0, null, false, null, "Ctrl+F6");
            return new CommanderCommands(commands);
        }

        private static void Start(CommanderContext context, ConsoleMonster.FileSystem.FileOperation? operation) {
            if (operation != null) { context.Host.Start(operation); }
        }

        private static KeyChord Chord(string text) => KeyChord.Parse(text);
    }
}
