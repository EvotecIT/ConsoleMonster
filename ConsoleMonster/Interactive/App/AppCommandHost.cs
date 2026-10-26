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
        private readonly AppHostKeys _keys;
        private readonly object _gate = new object();
        private readonly AppCommand[] _commands;
        private readonly Func<IReadOnlyList<object?>> _selection;
        private readonly PromptLabels _labels;
        private readonly AppActionSession _session;
        private Modal? _modal;
        private readonly AppDialogs? _dialogs;
        private readonly AppActions? _actions;
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
        private readonly Func<string>? _selectionLabel;
        private readonly Action<AppCommand>? _completed;
        private long _completionRevision;
        private long _summaryRevision;

        internal AppCommandHost(IEnumerable<AppCommand> commands, Func<IReadOnlyList<object?>> selection, PromptLabels labels, CancellationToken token, bool commandBar = false, bool requiresRows = false, AppDispatcher? dispatcher = null, Func<string>? selectionLabel = null, Action<AppCommand>? completed = null, Func<TimeSpan>? actionClock = null, AppHostKeys? keys = null, AppDialogs? dialogs = null, AppActions? actions = null) {
            _keys = keys ?? new AppHostKeys();
            _completed = completed;
            _selectionLabel = selectionLabel;
            _commands = new List<AppCommand>(commands).ToArray();
            HashSet<string> commandKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<KeyChord> shortcuts = new HashSet<KeyChord>();
            foreach (AppCommand command in _commands) {
                if (command == null || !commandKeys.Add(command.Key)) { throw new ArgumentException("Commands must have unique keys.", nameof(commands)); }
                if (command.Shortcut.HasValue && _keys.Reserved(command.Shortcut.Value) || command.Shortcut.HasValue && command.Shortcut.Value.IsCharacter && command.Shortcut.Value.Modifiers == KeyModifiers.None) { throw new ArgumentException("Command shortcuts cannot replace app navigation, cancellation or the action palette.", nameof(commands)); }
                if (command.Shortcut.HasValue && !shortcuts.Add(command.Shortcut.Value)) { throw new ArgumentException("Command shortcuts must be unique.", nameof(commands)); }
            }
            _requiresRows = requiresRows;
            if (commandBar && _commands.Length > 0) {
                Columns buttons = new Columns();
                foreach (AppCommand command in _commands) {
                    string caption = command.Label + (command.Shortcut.HasValue ? " " + command.Shortcut.Value.ToString().ToUpperInvariant() : "");
                    Button button = new Button(command.Key, caption, false);
                    Bind(button, command);
                    buttons.Add(button, LayoutSize.Fixed(ConsoleMonster.Text.DisplayWidth.Measure(caption) + 6));
                }
                _commandBar = buttons;
                _barFocus = new FocusManager(buttons);
                foreach (InputControl control in _barFocus.Controls) { control.Focused = false; }
            }
            _selection = selection;
            _labels = labels;
            _session = new AppActionSession(token, dispatcher, actionClock);
            _dialogs = dialogs;
            _actions = actions;
            _actions?.Attach(command => { lock (_gate) { PollCore(); if (IsBusy || HasModal) { return false; } Invoke(command); return true; } }, () => IsBusy, _session.Cancel);
            _dialogs?.Attach(modal => { lock (_gate) { ReplaceModal(modal); } }, () => HasModal);
        }

        internal bool HasCommands => _commands.Length != 0 || _actions != null;
        internal bool HasModal { get { lock (_gate) { return _modal != null && !_modal.IsClosed; } } }
        internal bool HasBarFocus => _barActive;
        internal bool IsBusy => _session.IsBusy;
        internal bool TryPost(Action action) => _session.Dispatcher.TryPost(action);

        internal bool Poll() {
            lock (_gate) { return PollCore(); }
        }

        private bool PollCore() {
            bool changed = _session.Dispatcher.Drain() != 0;
            long completion = _session.CompletionRevision;
            if (completion != _completionRevision && !_session.IsBusy) {
                _completionRevision = completion;
                try { if (_session.CompletedCommand is AppCommand command) { _completed?.Invoke(command); } }
                catch (Exception error) { _session.WriteFailure("Refresh", error); }
                changed = true;
            }
            long revision = _session.Revision;
            if (revision != _revision) { _revision = revision; changed = true; }
            bool busy = _session.IsBusy;
            if (busy != _wasBusy) { _wasBusy = busy; changed = true; }
            if (!busy && completion != _summaryRevision && _session.OperationCommand?.ProgressDialog == true && (!HasModal || _modal is OperationProgressModal)) {
                _summaryRevision = completion;
                AppOperationSummary summary = _session.Summary ?? new AppOperationSummary("Completed");
                var lines = new Rows().Add(TextBlock.Plain(summary.Message));
                foreach (string failure in summary.Failures) { lines.Add(TextBlock.Plain(failure, Styling.ThemeToken.Fail)); }
                var dialog = new FormDialog<bool>(_session.OperationCommand.Label, new ScrollView(lines), _ => true) {
                    AcceptText = summary.Retry == null ? "Close" : "Retry failed", CancelText = "Skip", DefaultAccept = false
                };
                ReplaceModal(new FormDialogModal<bool>(dialog, result => { if (result.Accepted && summary.Retry != null) { Invoke(summary.Retry); } }));
                changed = true;
            }
            return changed;
        }

        internal void Invoke(AppCommand command) {
            lock (_gate) {
                if (_session.IsBusy) { return; }
                Poll();
                IReadOnlyList<object?> rows = command.Capture(_selection());
                void Start() {
                    if (_session.Start(command, rows) && command.ProgressDialog) { ReplaceModal(new OperationProgressModal(_session)); }
                }
                if (_requiresRows && rows.Count == 0) { return; }
                if (string.IsNullOrWhiteSpace(command.ConfirmPrompt)) { Start(); }
                else {
                    string target = _selectionLabel?.Invoke() ?? (rows.Count == 1 ? Convert.ToString(rows[0]) ?? "1 item" : rows.Count + " items");
                    ReplaceModal(new CommandModal(command.ConfirmPrompt!.Replace("{Selection}", target), Start, _labels));
                }
            }
        }

        private void SetBarFocus() {
            if (_barFocus != null) { foreach (InputControl control in _barFocus.Controls) { control.Focused = _barActive && control == _barFocus.Current; } }
        }
        private void ReplaceModal(Modal modal) {
            (_modal as IDisposable)?.Dispose(); _modal = modal;
            // Consecutive keyboard events in the same burst need context before the next paint.
            if (_frame != null) { modal.Build(_frame, _frame.Caret); }
        }

        internal bool Handle(InputEvent input) {
            lock (_gate) {
                if (input.Is(ConsoleKey.C, KeyModifiers.Control)) { if (_session.IsBusy) { _session.Cancel(); return true; } return false; }
                if (AppHostKeys.Match(_keys.CancelAction, input) && _session.IsBusy) { _session.Cancel(); return true; }
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
                    if (AppHostKeys.Match(_keys.Actions, input)) { _barActive = !_barActive; SetBarFocus(); return true; }
                    if (_barActive && input.Is(ConsoleKey.Escape)) { _barActive = false; SetBarFocus(); return true; }
                    if (_barActive && (input.Is(ConsoleKey.Tab) || input.Is(ConsoleKey.Tab, KeyModifiers.Shift))) {
                        if (input.Modifiers == KeyModifiers.Shift) { _barFocus.MovePrevious(); } else { _barFocus.MoveNext(); }
                        return true;
                    }
                    if (input.Kind == InputEventKind.Mouse) {
                        if (input.MouseAction == MouseAction.Down && input.MouseButton == 1 && _barFocus.FocusAt(input.Column, input.Row)) {
                            _barActive = true; _barCapture = _barFocus.Current as Button;
                        } else if (input.MouseAction == MouseAction.Down) {
                            _barActive = false; _barCapture?.CancelPointer(); _barCapture = null; SetBarFocus();
                        }
                        if (_barCapture != null) {
                            _barCapture.HandleInput(input, _frame.Context);
                            if (input.MouseAction == MouseAction.Up) { _barCapture = null; }
                            return true;
                        }
                    } else if (_barActive && (_barFocus.Current?.HandleInput(input, _frame.Context) ?? false)) { return true; }
                }
                if (_commands.Length > 0 && AppHostKeys.Match(_keys.Palette, input)) { ReplaceModal(new CommandModal(_commands, Invoke, _labels)); return true; }
                if (AppHostKeys.Match(_keys.CancelAction, input) && _session.IsBusy) { _session.Cancel(); return true; }
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

        internal IWidget Wrap(IWidget content) => HasCommands || _dialogs != null ? new Surface(this, content) : content;
        internal void AddBindings(List<KeyBinding> bindings) {
            if (!HasCommands) { return; }
            if (_commandBar != null && _keys.Actions.HasValue) { bindings.Add(new KeyBinding(_labels.KeyActions, () => { _barActive = !_barActive; SetBarFocus(); }, _keys.Actions.Value) { KeyText = _keys.Actions.Value.ToString().ToLowerInvariant() }); }
            if (_commands.Length > 0 && _keys.Palette.HasValue) { bindings.Add(new KeyBinding(_labels.KeyPalette, () => { lock (_gate) { ReplaceModal(new CommandModal(_commands, Invoke, _labels)); } }, _keys.Palette.Value) { KeyText = _keys.Palette.Value.ToString().ToLowerInvariant(), Order = 31 }); }
            foreach (AppCommand command in _commands) {
                if (command.Shortcut.HasValue) { bindings.Add(new KeyBinding(command.Label, () => Invoke(command), command.Shortcut.Value) { KeyText = command.Shortcut.Value.ToString().ToLowerInvariant(), Order = 30 }); }
            }
            KeyChord cancel = _keys.CancelAction ?? new KeyChord(ConsoleKey.C, KeyModifiers.Control);
            bindings.Add(new KeyBinding(_labels.KeyCancelAction, _session.Cancel, cancel) { InFooter = _session.IsBusy, KeyText = cancel.ToString().ToLowerInvariant(), Order = 32 });
        }

        internal void Bind(Button button, AppCommand command) {
            EventHandler handler = (_, __) => Invoke(command);
            button.Clicked += handler;
            _detach.Add(() => button.Clicked -= handler);
        }
        public void Dispose() { _actions?.Detach(); _dialogs?.Detach(); foreach (Action detach in _detach) { detach(); } lock (_gate) { (_modal as IDisposable)?.Dispose(); _barFocus?.Dispose(); } _session.Dispose(); }
        private static bool Contains(Rect area, int x, int y) => x >= area.X && x < area.Right && y >= area.Y && y < area.Bottom;

        private sealed class Surface : IContainer {
            private readonly AppCommandHost _host;
            private readonly IWidget _content;
            internal Surface(AppCommandHost host, IWidget content) { _host = host; _content = content; }
            public IReadOnlyList<IWidget> Children => new[] { _content };
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth,
                constraints.IsHeightUnbounded ? _content.Measure(new Constraints(constraints.MaxWidth, 30), context).Height + (_host.HasCommands ? 7 + (_host._commandBar?.Measure(new Constraints(constraints.MaxWidth, 30), context).Height ?? 0) : 0) : constraints.MaxHeight);
            public void Render(Region region, RenderContext context) {
                lock (_host._gate) {
                    int logHeight = region.Height < 12 ? 3 : Math.Min(7, region.Height / 3);
                    _host._logBounds = new Rect(region.Bounds.X, region.Bounds.Y + region.Height - logHeight, region.Width, logHeight);
                    LogRegion log = new LogRegion("actions") { ShowElapsed = true, LevelColumn = new ColumnFormat(HorizontalAlignment.Left, 4), ScrollOffset = _host._logScroll };
                    foreach (LogEntry entry in _host._session.Output) { log.Write(entry); }
                    Rows rows = new Rows().Add(_content, LayoutSize.Fraction());
                    if (_host._commandBar != null) {
                        int height = _host._commandBar.Measure(new Constraints(region.Width, Math.Max(1, region.Height - logHeight - 1)), context).Height;
                        rows.Add(_host._commandBar, LayoutSize.Fixed(height));
                    }
                    IWidget body = !_host.HasCommands ? _content : rows.Add(new Panel(log, _host.IsBusy ? PromptLabels.Format(_host._labels.ActionRunning, _host._keys.CancelAction?.ToString() ?? "Ctrl+C") : _host._labels.ActionOutput), LayoutSize.Fixed(logHeight));
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
