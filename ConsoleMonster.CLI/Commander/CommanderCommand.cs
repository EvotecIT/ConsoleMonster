using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.FileSystem;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.Apps.Commander {
    /// <summary>
    /// What the Commander's screen provides to its commands: dialogs, the viewer and editor, the terminal handoff and
    /// background operations. The phase 3 app implements it; tests use a recording fake. Every method is called on the
    /// app thread, and callbacks it receives must be invoked on the app thread too.
    /// </summary>
    public interface ICommanderHost {
        /// <summary>Whether the host implements a host-backed command; unsupported commands show dimmed on the key bar.</summary>
        /// <param name="commandKey">The command's <see cref="CommanderCommand.Key"/>.</param>
        /// <returns>True when supported.</returns>
        bool Supports(string commandKey);

        /// <summary>Asks for one line of text, such as a folder name or a mark pattern.</summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="text">The initial text.</param>
        /// <param name="accepted">Called with the text when the user accepts; never called on cancel.</param>
        void Prompt(string title, string text, Action<string> accepted);

        /// <summary>
        /// Confirms and runs a planned operation in the background with progress and a final summary. For copies and
        /// moves the dialog may change the destination and options; deletes default to No, and a permanent delete says so
        /// in its title. Afterwards the host calls <see cref="CommanderModel.AfterOperation"/>.
        /// </summary>
        /// <param name="operation">The planned operation.</param>
        void Start(FileOperation operation);

        /// <summary>Opens a file from Enter: runs it from the command line, asking first for scripts.</summary>
        /// <param name="file">The file.</param>
        void Open(PanelEntry file);

        /// <summary>Shows a file in the pager (F3).</summary>
        /// <param name="file">The file.</param>
        void View(PanelEntry file);

        /// <summary>Edits a file (F4): the text editor, or <c>$EDITOR</c> through the terminal handoff.</summary>
        /// <param name="file">The file.</param>
        void Edit(PanelEntry file);

        /// <summary>Shows the help screen (F1).</summary>
        void ShowHelp();

        /// <summary>Shows the menu (F2).</summary>
        void ShowMenu();

        /// <summary>Shows the command palette (F9) listing <see cref="CommanderCommands"/>.</summary>
        void ShowPalette();

        /// <summary>Hides the panels and shows the last command output until a key is pressed (Ctrl+O).</summary>
        void ShowOutput();

        /// <summary>Lets the user pick a drive or root for one panel (Alt+F1, Alt+F2).</summary>
        /// <param name="side">The panel.</param>
        void ChooseRoot(CommanderSide side);

        /// <summary>Saves state and leaves the app (F10).</summary>
        void Quit();
    }

    /// <summary>What a command acts on.</summary>
    public sealed class CommanderContext {
        /// <summary>Creates a context.</summary>
        /// <param name="model">The panels.</param>
        /// <param name="host">The screen.</param>
        public CommanderContext(CommanderModel model, ICommanderHost host) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>The panels.</summary>
        public CommanderModel Model { get; }

        /// <summary>The screen.</summary>
        public ICommanderHost Host { get; }

        /// <summary>The active panel.</summary>
        public CommanderPanel Active => Model.Active;
    }

    /// <summary>
    /// One Commander command as data: a stable key, a label, its keys (the first is primary, the rest are alternatives
    /// for terminals that swallow F-keys), an optional key-bar slot, when it's available and what it does. The palette,
    /// key bar, help and key routing all come from the same <see cref="CommanderCommands"/>.
    /// </summary>
    public sealed class CommanderCommand {
        private readonly KeyChord[] _keys;

        /// <summary>Creates a command.</summary>
        /// <param name="key">The stable key, such as <c>copy</c>.</param>
        /// <param name="label">What it does, for the palette and help.</param>
        /// <param name="execute">The action, called on the app thread when the command is available.</param>
        /// <param name="keys">Its keys, primary first; none for palette-only commands.</param>
        public CommanderCommand(string key, string label, Action<CommanderContext> execute, params KeyChord[] keys) {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("A command needs a key.", nameof(key)); }
            Key = key;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _keys = keys ?? Array.Empty<KeyChord>();
        }

        /// <summary>The stable key.</summary>
        public string Key { get; }

        /// <summary>What it does.</summary>
        public string Label { get; }

        /// <summary>The action.</summary>
        public Action<CommanderContext> Execute { get; }

        /// <summary>All keys, primary first.</summary>
        public IReadOnlyList<KeyChord> Keys => _keys;

        /// <summary>The primary key, or null for a palette-only command.</summary>
        public KeyChord? Gesture => _keys.Length == 0 ? (KeyChord?)null : _keys[0];

        /// <summary>The alternative keys.</summary>
        public IReadOnlyList<KeyChord> Alternatives => _keys.Skip(1).ToArray();

        /// <summary>The key-bar slot from 1 to 10, or 0 when it has none.</summary>
        public int FunctionKey { get; set; }

        /// <summary>The short key-bar label, such as <c>Mkdir</c>; the label when null.</summary>
        public string? BarLabel { get; set; }

        /// <summary>The help and palette heading.</summary>
        public string Group { get; set; } = "Files";

        /// <summary>Whether the command needs <see cref="ICommanderHost.Supports"/> to be true.</summary>
        public bool RequiresHost { get; set; }

        /// <summary>When the command can run; always when null.</summary>
        public Func<CommanderContext, bool>? Available { get; set; }

        /// <summary>Whether the command can run now; unavailable commands do nothing and show dimmed.</summary>
        /// <param name="context">The context.</param>
        /// <returns>True when available.</returns>
        public bool IsAvailable(CommanderContext context) {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }
            return (!RequiresHost || context.Host.Supports(Key)) && (Available?.Invoke(context) ?? true);
        }

        /// <summary>Runs the command when it's available.</summary>
        /// <param name="context">The context.</param>
        /// <returns>Whether it ran.</returns>
        public bool TryExecute(CommanderContext context) {
            if (!IsAvailable(context)) { return false; }
            Execute(context);
            return true;
        }

        /// <summary>Whether a key event is one of this command's keys.</summary>
        /// <param name="input">The event.</param>
        /// <returns>True on a match.</returns>
        public bool Matches(InputEvent input) {
            foreach (KeyChord chord in _keys) {
                if (chord.Matches(input)) { return true; }
            }
            return false;
        }

        /// <summary>The command as a key binding, for the app host's routing, footer and key map.</summary>
        /// <param name="context">The context its action runs in.</param>
        /// <returns>The binding; its action does nothing while the command is unavailable.</returns>
        public KeyBinding ToKeyBinding(CommanderContext context) {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }
            return new KeyBinding(Label, () => TryExecute(context), _keys) {
                Group = Group,
                KeyText = _keys.Length == 0 ? string.Empty : string.Join(" / ", _keys.Select(KeyBinding.Display).ToArray()),
                Order = FunctionKey == 0 ? 60 : 20 + FunctionKey,
                InFooter = FunctionKey != 0
            };
        }

        /// <inheritdoc />
        public override string ToString() => Key;
    }

    /// <summary>One slot of the F-key bar. Slots without an available command are dimmed, never removed, so the bar doesn't shift.</summary>
    public sealed class CommanderKeyBarItem {
        internal CommanderKeyBarItem(int number, string label, string keyText, bool isEnabled, CommanderCommand? command) {
            Number = number;
            Label = label;
            KeyText = keyText;
            IsEnabled = isEnabled;
            Command = command;
        }

        /// <summary>The slot, 1 to 10.</summary>
        public int Number { get; }

        /// <summary>The short label.</summary>
        public string Label { get; }

        /// <summary>The key to show: <c>F5</c>, or <c>Alt+5</c> where F-keys are swallowed.</summary>
        public string KeyText { get; }

        /// <summary>Whether the command can run now.</summary>
        public bool IsEnabled { get; }

        /// <summary>The command, or null for an empty slot.</summary>
        public CommanderCommand? Command { get; }

        /// <inheritdoc />
        public override string ToString() => Number + Label;
    }
}
