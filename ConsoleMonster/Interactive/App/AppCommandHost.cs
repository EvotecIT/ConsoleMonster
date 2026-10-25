using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Shared action, modal, palette and log services used by composed apps and the grid viewer.</summary>
    internal sealed class AppCommandHost : IDisposable {
        private readonly object _gate = new object();
        private readonly AppCommand[] _commands;
        private readonly Func<IReadOnlyList<object?>> _selection;
        private readonly PromptLabels _labels;
        private readonly AppActionSession _session;
        private CommandModal? _modal;
        private ViewFrame? _frame;
        private long _revision;
        private bool _wasBusy;
        private int _logScroll;
        private Rect _logBounds;
        private readonly bool _requiresRows;
        private readonly IWidget? _commandBar;
        private readonly FocusManager? _barFocus;
        private bool _barActive;
        private Button? _barCapture;
        private readonly List<Action> _detach = new List<Action>();

        internal AppCommandHost(IEnumerable<AppCommand> commands, Func<IReadOnlyList<object?>> selection, PromptLabels labels, CancellationToken token, bool commandBar = false, bool requiresRows = false, AppDispatcher? dispatcher = null) {
            _commands = new List<AppCommand>(commands).ToArray();
            HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<KeyChord> shortcuts = new HashSet<KeyChord>();
            foreach (AppCommand command in _commands) {
                if (command == null || !keys.Add(command.Key)) { throw new ArgumentException("Commands must have unique keys.", nameof(commands)); }
                if (command.Shortcut.HasValue && Reserved(command.Shortcut.Value)) { throw new ArgumentException("Command shortcuts cannot replace app navigation, cancellation or the action palette.", nameof(commands)); }
                if (command.Shortcut.HasValue && !shortcuts.Add(command.Shortcut.Value)) { throw new ArgumentException("Command shortcuts must be unique.", nameof(commands)); }
            }
            _requiresRows = requiresRows;
            if (commandBar && _commands.Length > 0) {
                Columns buttons = new Columns();
                foreach (AppCommand command in _commands) {
                    Button button = new Button(command);
                    Bind(button, command);
                    buttons.Add(button);
                }
                _commandBar = buttons;
                _barFocus = new FocusManager(buttons);
                foreach (InputControl control in _barFocus.Controls) { control.Focused = false; }
            }
            _selection = selection;
            _labels = labels;
            _session = new AppActionSession(token, dispatcher);
        }

        internal bool HasCommands => _commands.Length != 0;
        internal bool HasModal { get { lock (_gate) { return _modal != null && !_modal.IsClosed; } } }
        internal bool HasBarFocus => _barActive;
        internal bool IsBusy => _session.IsBusy;
        internal bool TryPost(Action action) => _session.Dispatcher.TryPost(action);

        internal bool Poll() {
            bool changed = _session.Dispatcher.Drain() != 0;
            long revision = _session.Revision;
            if (revision != _revision) { _revision = revision; changed = true; }
            bool busy = _session.IsBusy;
            if (busy != _wasBusy) { _wasBusy = busy; changed = true; }
            return changed;
        }

        internal void Invoke(AppCommand command) {
            lock (_gate) {
                if (_session.IsBusy) { return; }
                IReadOnlyList<object?> rows = command.Capture(_selection());
                if (_requiresRows && rows.Count == 0) { return; }
                if (string.IsNullOrWhiteSpace(command.ConfirmPrompt)) { _session.Start(command, rows); }
                else { ReplaceModal(new CommandModal(command.ConfirmPrompt!, () => _session.Start(command, rows), _labels)); }
            }
        }

        private void SetBarFocus() {
            if (_barFocus != null) { foreach (InputControl control in _barFocus.Controls) { control.Focused = _barActive && control == _barFocus.Current; } }
        }
        private void ReplaceModal(CommandModal modal) {
            _modal?.Dispose(); _modal = modal;
            // Consecutive keyboard events in the same burst need context before the next paint.
            if (_frame != null) { modal.Build(_frame, _frame.Caret); }
        }

        internal bool Handle(InputEvent input) {
            lock (_gate) {
                if (input.Is(ConsoleKey.C, KeyModifiers.Control)) { return false; }
                if (input.Kind == InputEventKind.FocusLost || input.Kind == InputEventKind.Resize) {
                    _modal?.CancelPointer(); _barCapture?.CancelPointer(); _barCapture = null; return false;
                }
                if (_modal != null && !_modal.IsClosed && _frame != null) {
                    List<KeyBinding> bindings = new List<KeyBinding>();
                    _modal.AddBindings(bindings, _frame);
                    if (!KeyFooter.Dispatch(bindings, input)) { _modal.HandleUnbound(input); }
                    return true;
                }
                if (_barFocus != null && _frame != null) {
                    if (input.Is(ConsoleKey.F8)) { _barActive = !_barActive; SetBarFocus(); return true; }
                    if (_barActive && input.Is(ConsoleKey.Escape)) { _barActive = false; SetBarFocus(); return true; }
                    if (_barActive && (input.Is(ConsoleKey.Tab) || input.Is(ConsoleKey.Tab, KeyModifiers.Shift))) {
                        if (input.Modifiers == KeyModifiers.Shift) { _barFocus.MovePrevious(); } else { _barFocus.MoveNext(); }
                        return true;
                    }
                    if (input.Kind == InputEventKind.Mouse) {
                        if (input.MouseAction == MouseAction.Down && input.MouseButton == 1 && _barFocus.FocusAt(input.Column, input.Row)) {
                            _barActive = true; _barCapture = _barFocus.Current as Button;
                        }
                        if (_barCapture != null) {
                            _barCapture.HandleInput(input, _frame.Context);
                            if (input.MouseAction == MouseAction.Up) { _barCapture = null; }
                            return true;
                        }
                    } else if (_barActive && (_barFocus.Current?.HandleInput(input, _frame.Context) ?? false)) { return true; }
                }
                if (input.Is(ConsoleKey.K, KeyModifiers.Control)) { ReplaceModal(new CommandModal(_commands, Invoke, _labels)); return true; }
                if (input.Is(ConsoleKey.F6) && _session.IsBusy) { _session.Cancel(); return true; }
                foreach (AppCommand command in _commands) {
                    if (command.Shortcut.HasValue && !input.IsText && command.Shortcut.Value.Matches(input)) { Invoke(command); return true; }
                }
                if (input.Kind == InputEventKind.Mouse && input.MouseAction == MouseAction.Wheel && Contains(_logBounds, input.Column, input.Row)) {
                    _logScroll = Math.Max(0, Math.Min(Math.Max(0, _session.Output.Count - 1), _logScroll + Math.Sign(input.WheelDelta) * 3));
                    return true;
                }
                if (input.Is(ConsoleKey.PageUp, KeyModifiers.Alt)) { _logScroll = Math.Min(Math.Max(0, _session.Output.Count - 1), _logScroll + 5); return true; }
                if (input.Is(ConsoleKey.PageDown, KeyModifiers.Alt)) { _logScroll = Math.Max(0, _logScroll - 5); return true; }
                return false;
            }
        }

        internal IWidget Wrap(IWidget content) => HasCommands ? new Surface(this, content) : content;
        internal void AddBindings(List<KeyBinding> bindings) {
            if (!HasCommands) { return; }
            if (_commandBar != null) { bindings.Add(new KeyBinding("Focus actions", () => { _barActive = !_barActive; SetBarFocus(); }, new KeyChord(ConsoleKey.F8))); }
            bindings.Add(new KeyBinding("Actions", () => { lock (_gate) { ReplaceModal(new CommandModal(_commands, Invoke, _labels)); } }, new KeyChord(ConsoleKey.K, KeyModifiers.Control)));
            foreach (AppCommand command in _commands) {
                if (command.Shortcut.HasValue) { bindings.Add(new KeyBinding(command.Label, () => Invoke(command), command.Shortcut.Value)); }
            }
            bindings.Add(new KeyBinding("Cancel action", _session.Cancel, new KeyChord(ConsoleKey.F6)) { InFooter = _session.IsBusy });
        }

        internal void Bind(Button button, AppCommand command) {
            EventHandler handler = (_, __) => Invoke(command);
            button.Clicked += handler;
            _detach.Add(() => button.Clicked -= handler);
        }
        public void Dispose() { foreach (Action detach in _detach) { detach(); } lock (_gate) { _modal?.Dispose(); _barFocus?.Dispose(); } _session.Dispose(); }
        private static bool Reserved(KeyChord shortcut) => shortcut.IsCharacter && shortcut.Modifiers == KeyModifiers.None
            || shortcut.Equals(new KeyChord(ConsoleKey.C, KeyModifiers.Control))
            || shortcut.Equals(new KeyChord(ConsoleKey.K, KeyModifiers.Control))
            || shortcut.Equals(new KeyChord(ConsoleKey.Escape)) || shortcut.Equals(new KeyChord(ConsoleKey.Tab))
            || shortcut.Equals(new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift))
            || shortcut.Equals(new KeyChord(ConsoleKey.F6)) || shortcut.Equals(new KeyChord(ConsoleKey.F8));
        private static bool Contains(Rect area, int x, int y) => x >= area.X && x < area.Right && y >= area.Y && y < area.Bottom;

        private sealed class Surface : IContainer {
            private readonly AppCommandHost _host;
            private readonly IWidget _content;
            internal Surface(AppCommandHost host, IWidget content) { _host = host; _content = content; }
            public IReadOnlyList<IWidget> Children => new[] { _content };
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth,
                constraints.IsHeightUnbounded ? _content.Measure(new Constraints(constraints.MaxWidth, 30), context).Height + 7 : constraints.MaxHeight);
            public void Render(Region region, RenderContext context) {
                lock (_host._gate) {
                    int logHeight = region.Height < 12 ? 3 : Math.Min(7, region.Height / 3);
                    _host._logBounds = new Rect(region.Bounds.X, region.Bounds.Y + region.Height - logHeight, region.Width, logHeight);
                    LogRegion log = new LogRegion("actions") { ShowTime = false, ScrollOffset = _host._logScroll };
                    foreach (LogEntry entry in _host._session.Output) { log.Write(entry); }
                    Rows rows = new Rows().Add(_content, LayoutSize.Fraction());
                    if (_host._commandBar != null) { rows.Add(_host._commandBar, LayoutSize.Fixed(3)); }
                    IWidget body = rows.Add(new Panel(log, _host.IsBusy ? "Action running · F6 cancels" : "Action output · Alt+PgUp/PgDn scroll"), LayoutSize.Fixed(logHeight));
                    _host._frame = new ViewFrame(context, region.Width, region.Height, false, context.Elapsed);
                    if (_host._modal != null && !_host._modal.IsClosed) {
                        body = new Overlay(body, _host._modal.Build(_host._frame, _host._frame.Caret)) { MaxWidth = _host._modal.MaxWidth };
                    }
                    body.Render(region, context);
                }
            }
        }
    }
}
