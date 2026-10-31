using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Owns callbacks and dependent-widget creation on the input thread; only live payload application runs on the renderer.</summary>
    internal sealed class AppController : IDisposable {
        private readonly ConsoleAppOptions _options;
        private readonly AppState _state;
        private readonly List<AppBinding> _bindings = new List<AppBinding>();
        private readonly Dictionary<AppBinding, AppScreen> _bindingScreens = new Dictionary<AppBinding, AppScreen>();
        private LiveSession? _session;
        private bool _hasTabs;
        private readonly List<FooterBar> _footers = new List<FooterBar>();
        private readonly FocusManager _focus;
        private readonly RenderContext _context;
        private readonly AppViewport _viewport;
        private readonly object _commitGate = new object();
        private readonly Dictionary<string, InputControl> _inputs = new Dictionary<string, InputControl>(StringComparer.OrdinalIgnoreCase);
        private int _dirty;
        private bool _disposed;
        private readonly AppCommandHost _actions;
        private InputControl? _pointerCapture;
        private volatile bool _tooSmall;
        private AppScreens? _screens;
        private readonly AppKeyMap _keys;
        private readonly AppOverlay _overlay;
        private readonly FooterBar? _defaultFooter;

        internal AppController(IWidget layout, AppState state, RenderContext context, int reflowBelow, ConsoleAppOptions? options = null, CancellationToken cancellationToken = default, Func<TimeSpan>? actionClock = null) {
            _options = options ?? new ConsoleAppOptions();
            _state = state;
            _context = context;
            Discover(layout, reflowBelow);
            if (_options.Footer != null) { Discover(_options.Footer, reflowBelow); }
            _focus = new FocusManager(layout);
            _viewport = new AppViewport(layout, _focus);
            _keys = new AppKeyMap(_options.KeyBindings, _screens, _focus, context);
            System.Diagnostics.Stopwatch toastClock = System.Diagnostics.Stopwatch.StartNew();
            List<AppCommand> commands = new List<AppCommand>(options?.Command ?? (IEnumerable<AppCommand>)Array.Empty<AppCommand>());
            foreach (InputControl input in _focus.Controls) {
                if (input is Button commandButton && commandButton.Command != null && !commands.Contains(commandButton.Command)) { commands.Add(commandButton.Command); }
            }
            try {
                _actions = new AppCommandHost(commands, () => options?.CaptureSelection(_state) ?? Array.Empty<object?>(), PromptLabels.For(context.Profile.Language), cancellationToken, dispatcher: options?.Dispatcher, actionClock: actionClock, keys: _options.Keys, dialogs: _options.Dialogs, actions: _options.Actions);
                foreach (InputControl control in _focus.Controls) {
                    _inputs.Add(control.Key, control);
                    state[control.Key] = control.CurrentValue;
                    control.ValueCommitted += Commit;
                    control.Invalidated += Invalidate;
                }
                state.Changed += StateChanged;
                if (_options.Footer == null) { _defaultFooter = new FooterBar(); _footers.Add(_defaultFooter); }
                IWidget frame = new Rows().Add(_actions.Wrap(_viewport), LayoutSize.Fraction()).Add(_options.Footer ?? _defaultFooter!, LayoutSize.Fixed(1));
                _actions.ModalOnOverlay = true;
                Log = new AppLog(_actions, _focus.Controls.OfType<LogViewport>().FirstOrDefault());
                _overlay = new AppOverlay(frame, _viewport, _actions, _screens, actionClock ?? (() => toastClock.Elapsed));
                _options.Dialogs?.AttachToasts(_overlay.ShowToast);
                Root = _overlay;
                if (_options.MinimumWidth > 0 || _options.MinimumHeight > 0) { Root = new AppMinimumSize(Root, _options.MinimumWidth, _options.MinimumHeight, small => _tooSmall = small); }
                foreach (InputControl input in _focus.Controls) {
                    if (input is Button button && button.Command != null) { _actions.Bind(button, button.Command); }
                }
                _keys.Validate(new KeyChord?[] { _options.Keys.NextFocus, _options.Keys.PreviousFocus, new KeyChord(ConsoleKey.C, KeyModifiers.Control) }.Where(key => key.HasValue).Select(key => key!.Value));
                if (_screens != null) {
                    _screens.Showing = ShowScreen;
                    _screens.Navigated = Navigated;
                    _screens.Requested = Invalidate;
                    _screens.Attach(state, PromptLabels.For(context.Profile.Language));
                    ConstrainFocus();
                }
                foreach (AppBinding binding in _bindings) { if (!Hidden(binding)) { binding.Refresh(state, null); } }
                Publish();
            } catch { Dispose(); throw; }
        }

        internal IWidget Root { get; }
        internal AppLog Log { get; } = null!;
        internal FocusManager Focus => _focus;

        internal CellBuffer PrepareFrame(int width, int height) => Rendering.FrameComposer.Compose(Root, width, height, _context);

        internal void Handle(InputEvent input, bool mouse) {
            bool reachedKeys = false;
            try { HandleCore(input, mouse, ref reachedKeys); }
            // A key taken before the key map saw it ends a half-typed item number.
            finally { if (input.Kind == InputEventKind.Key && !reachedKeys) { _keys.ForgetPending(); } }
        }

        private void HandleCore(InputEvent input, bool mouse, ref bool reachedKeys) {
            if (_tooSmall && input.Kind != InputEventKind.Resize) {
                if (input.Is(ConsoleKey.C, KeyModifiers.Control) || AppHostKeys.Match(_options.Keys.Exit, input)) { _state.RequestExit(); }
                return;
            }
            bool pointerHandled = false;
            if (input.Kind == InputEventKind.FocusLost || input.Kind == InputEventKind.Resize) { _pointerCapture?.CancelPointer(); _pointerCapture = null; }
            if ((_actions.HasCommands || _actions.HasModal) && (input.Kind != InputEventKind.Mouse || mouse) && _actions.Handle(input)) { Invalidate(); return; }
            if (input.Is(ConsoleKey.C, KeyModifiers.Control)) { _state.RequestExit(); Invalidate(); return; }
            if (_options.HandleInput?.Invoke(_state, input) == true) { Invalidate(); return; }
            if (input.Kind == InputEventKind.Mouse && mouse && _overlay.RoutePointer(input, _context)) { Invalidate(); return; }
            if ((input.Kind != InputEventKind.Mouse || mouse) && _viewport.HandleContainerInput(input)) { Invalidate(); return; }
            if (input.Kind != InputEventKind.Mouse && input.Kind != InputEventKind.FocusLost && input.Kind != InputEventKind.FocusGained) { _viewport.RevealFocus(); }
            if (input.Is(ConsoleKey.C, KeyModifiers.Control)) { _state.RequestExit(); }
            else if (input.Kind == InputEventKind.Resize) { PrepareFrame(input.Width, input.Height); }
            else if (AppHostKeys.Match(_options.Keys.NextFocus, input)) { _focus.MoveNext(); }
            else if (AppHostKeys.Match(_options.Keys.PreviousFocus, input)) { _focus.MovePrevious(); }
            else if (input.Kind == InputEventKind.Mouse) {
                if (mouse) {
                    if (input.MouseAction == MouseAction.Down && input.MouseButton == 1 && _viewport.FocusAt(input.Column, input.Row)) { _pointerCapture = _focus.Current; }
                    InputControl? target = _pointerCapture ?? _focus.Current;
                    if (input.MouseAction == MouseAction.Wheel && _viewport.Scroll(input)) { Invalidate(); return; }
                    if (target != null) { pointerHandled = target.HandleInput(_viewport.TranslatePointer(target, input), _context); }
                    if (input.MouseAction == MouseAction.Up) { _pointerCapture = null; }
                }
            } else {
                InputControl? current = _focus.Current;
                if (current != null && !current.Enabled) { _focus.MoveNext(); current = _focus.Current; }
                // A selector can consume Escape to clear a filter; then the screen (Back) and app bindings; an unhandled Escape quits the app.
                bool handled = _keys.DispatchFocused(input) || (current?.HandleInput(input, _context) ?? false);
                if (!handled) { handled = _keys.Dispatch(input); reachedKeys = true; }
                if (!handled && AppHostKeys.Match(_options.Keys.Exit, input)) { _state.RequestExit(); }
                else if (!handled && input.Is(ConsoleKey.Enter)) { _focus.MoveNext(); }
            }
            if (input.Kind != InputEventKind.Mouse || input.MouseAction != MouseAction.Move || input.HeldButtons != 0 || pointerHandled) { Invalidate(); }
        }

        internal void Refresh(LiveSession? session) {
            if (_actions.Poll()) { Invalidate(); }
            if (_overlay.Expire()) { Invalidate(); }
            if (session != null) { _session = session; }
            if (Interlocked.Exchange(ref _dirty, 0) == 0) { return; }
            _screens?.ApplyRequested();
            // Bindings on screens that are not shown are rebuilt when their screen opens.
            foreach (AppBinding binding in _bindings) { if (!Hidden(binding)) { binding.Refresh(_state, session); } }
            Publish();
            session?.Invalidate();
        }

        // Before a screen switch: its bindings are built, and frames stop reconciling focus until the switch is published.
        private void ShowScreen(AppScreen screen) {
            _keys.ForgetPending();
            foreach (KeyValuePair<AppBinding, AppScreen> owned in _bindingScreens) {
                if (ReferenceEquals(owned.Value, screen)) { owned.Key.Refresh(_state, _session); }
            }
            _focus.Hold();
        }

        private bool Hidden(AppBinding binding) => _screens != null && _bindingScreens.TryGetValue(binding, out AppScreen? screen) && !_screens.IsShown(screen);

        private void Navigated() {
            try { ConstrainFocus(); Publish(); }
            finally { _focus.Release(); }
            Invalidate();
        }

        // Only the shown screen's inputs (and inputs outside the stack) can have focus.
        private void ConstrainFocus() {
            HashSet<InputControl> hidden = new HashSet<InputControl>(_screens!.HiddenInputs());
            _focus.SetMounted(_focus.Controls.Where(control => !hidden.Contains(control)));
        }

        // Publishes what frames show as one snapshot: the shown screen with its crumbs, and the app footer's hints from the
        // key map (or the open dialog's keys). Footers with a source read it.
        private void Publish() {
            foreach (FooterBar footer in _footers) { if (footer.HintsSource != null) { footer.RefreshHints(); } }
            FooterBar? app = _defaultFooter ?? (_options.Footer is FooterBar custom && custom.HintsSource == null && custom.Hints.Count == 0 ? custom : null);
            IReadOnlyList<KeyHint>? hints = null;
            if (app != null) {
                List<KeyBinding>? modal = _actions.ModalBindings();
                bool keepLast;
                hints = modal != null ? _keys.Hints(modal, new KeyChord[0], false, out keepLast) : _keys.Hints(HostBindings(), HostFirst(), true, out keepLast);
                if (app == _defaultFooter) { app.KeepLast = keepLast; }
            }
            _overlay.Publish(_screens?.RefreshCrumbs(), app, hints, _session);
        }

        // Host gestures as hints; the host matches them itself. With screens, "back" means the previous screen, so the
        // Shift+Tab focus hint stays in the key map only.
        private List<KeyBinding> HostBindings() {
            PromptLabels labels = PromptLabels.For(_context.Profile.Language);
            List<KeyBinding> hints = new List<KeyBinding>();
            if (_options.Keys.NextFocus.HasValue) { hints.Add(new KeyBinding(labels.KeyNext, _focus.MoveNext, _options.Keys.NextFocus.Value)); }
            if (_options.Keys.PreviousFocus.HasValue) { hints.Add(new KeyBinding(labels.KeyBack, _focus.MovePrevious, _options.Keys.PreviousFocus.Value) { InFooter = _screens == null }); }
            if (_options.Keys.Exit.HasValue) { hints.Add(new KeyBinding(labels.KeyQuit, _state.RequestExit, _options.Keys.Exit.Value)); }
            _actions.AddBindings(hints);
            return hints;
        }

        // Keys the host matches before the focused input and the key map.
        private HashSet<KeyChord> HostFirst() {
            HashSet<KeyChord> first = new HashSet<KeyChord>(_actions.FirstChords()) { new KeyChord(ConsoleKey.C, KeyModifiers.Control) };
            if (_options.Keys.NextFocus.HasValue) { first.Add(_options.Keys.NextFocus.Value); }
            if (_options.Keys.PreviousFocus.HasValue) { first.Add(_options.Keys.PreviousFocus.Value); }
            if (_hasTabs) { first.Add(new KeyChord(ConsoleKey.PageUp, KeyModifiers.Control)); first.Add(new KeyChord(ConsoleKey.PageDown, KeyModifiers.Control)); }
            return first;
        }

        private void Discover(IWidget widget, int reflowBelow, AppScreen? owner = null) {
            if (widget is FooterBar footer) { _footers.Add(footer); }
            if (widget is Tabs) { _hasTabs = true; }
            if (widget is AppScreens screens) {
                if (_screens != null) { throw new ArgumentException("An app has one screen stack.", nameof(widget)); }
                _screens = screens;
                foreach (AppScreen screen in screens.Screens) { Discover(screen.Content, reflowBelow, screen); }
                return;
            }
            if (widget is AppBinding binding) {
                _bindings.Add(binding);
                if (owner != null) { _bindingScreens[binding] = owner; }
                return;
            }
            if (widget is Columns columns && columns.CollapseBelow < 0) { columns.CollapseBelow = reflowBelow; }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) { Discover(child, reflowBelow, owner); }
            }
        }

        private void Commit(AppValueChangedEventArgs change) {
            lock (_commitGate) {
                // Concurrent value setters can notify in another order; commit the latest control value under one gate.
                if (!_disposed) { _state[change.Key] = _inputs[change.Key].CurrentValue; }
            }
        }
        private void StateChanged(object? sender, AppValueChangedEventArgs change) => Invalidate();
        private void Invalidate() => Interlocked.Exchange(ref _dirty, 1);

        public void Dispose() {
            lock (_commitGate) {
                if (_disposed) { return; }
                _disposed = true;
            }
            _state.Changed -= StateChanged;
            _screens?.Detach();
            foreach (InputControl control in _focus.Controls) {
                control.ValueCommitted -= Commit;
                control.Invalidated -= Invalidate;
                control.ViewportHeight = Constraints.Unbounded;
            }
            _viewport.Reset();
            foreach (FooterBar footer in _footers) { footer.ReleaseHints(); }
            _focus.Dispose();
            _actions?.Dispose();
        }
    }
}
