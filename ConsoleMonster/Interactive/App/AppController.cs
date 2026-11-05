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
        private readonly List<Tabs> _tabSets = new List<Tabs>();
        private readonly List<Splitter> _splitters = new List<Splitter>();
        private LogRegion? _logRegion;
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
        private InputArrangement.ScrollbarDrag? _scrollCapture;
        private bool _containerCapture;
        private readonly ClickCounter _clicks = new ClickCounter();
        private volatile bool _tooSmall;
        private AppScreens? _screens;
        private readonly AppKeyMap _keys;
        private readonly AppOverlay _overlay;
        private readonly FooterBar? _defaultFooter;
        private readonly List<ILinkedSelection> _linked = new List<ILinkedSelection>();
        // The control whose selection is being written to state: it already shows it, so it does not follow its own write.
        private ILinkedSelection? _sharing;
        // Keys written while one input event is handled, including by the control's own Changed handlers: a selection
        // the input changed is not published over a value written after it.
        private readonly HashSet<string> _written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal AppController(IWidget layout, AppState state, RenderContext context, int reflowBelow, ConsoleAppOptions? options = null, CancellationToken cancellationToken = default, Func<TimeSpan>? actionClock = null, bool staticView = false) {
            _options = options ?? new ConsoleAppOptions();
            _state = state;
            _context = context;
            Discover(layout, reflowBelow);
            if (_options.Footer != null) { Discover(_options.Footer, reflowBelow); }
            _focus = new FocusManager(layout);
            _viewport = new AppViewport(layout, _focus, state: state);
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
                ShareInitialSelections();
                if (_options.Footer == null) { _defaultFooter = new FooterBar(); _footers.Add(_defaultFooter); }
                IWidget frame = staticView ? _actions.Wrap(_viewport)
                    : new Rows().Add(_actions.Wrap(_viewport), LayoutSize.Fraction()).Add(_options.Footer ?? _defaultFooter!, LayoutSize.Fixed(1));
                _actions.ModalOnOverlay = true;
                Log = new AppLog(_actions, _focus.Controls.OfType<LogViewport>().FirstOrDefault(), _logRegion);
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
            lock (_written) { _written.Clear(); }
            if (_tooSmall && input.Kind != InputEventKind.Resize) {
                if (input.Is(ConsoleKey.C, KeyModifiers.Control) || AppHostKeys.Match(_options.Keys.Exit, input)) { _state.RequestExit(); }
                return;
            }
            bool pointerHandled = false;
            int clicks = mouse ? _clicks.Count(input) : 0;
            if (input.Kind == InputEventKind.FocusLost || input.Kind == InputEventKind.Resize) {
                _pointerCapture?.CancelPointer(); _pointerCapture = null; _scrollCapture = null; _containerCapture = false; _overlay.Targets.Cancel(); _clicks.Reset();
                if (_overlay.Menu != null) { _overlay.Menu = null; Invalidate(); }
            }
            if (input.Kind == InputEventKind.Mouse && !mouse) { return; }
            // A dialog a callback opened takes the pointer at once, before a frame shows it: what was pressed, dragged or
            // drawn under it (a popup, a menu, the hints of the last frame) no longer gets events.
            bool modal = _actions.HasModal;
            if (modal && (_pointerCapture != null || _scrollCapture != null || _containerCapture || _overlay.Menu != null)) {
                _pointerCapture?.CancelPointer(); _pointerCapture = null; _scrollCapture = null; _containerCapture = false; _overlay.Menu = null; _overlay.Targets.Cancel();
                Invalidate();
            }
            if (_overlay.Menu is AppContextMenu menu && (input.Kind == InputEventKind.Key || input.Kind == InputEventKind.Mouse)) {
                // The menu lists one scope's bindings: once another input, screen or dialog has the keys it is stale, and the
                // key or press meant for it does nothing else.
                if (!Equals(menu.Scope, CurrentScope())) { _overlay.Menu = null; Invalidate(); return; }
                AppKeyMap.Entry? chosen = input.Kind == InputEventKind.Key ? menu.HandleKey(input, out bool close, out bool used) : menu.HandlePointer(input, out close, out used);
                if (close) { _overlay.Menu = null; }
                Invalidate();
                // The entry runs only while its key still reaches the binding it named (a callback may have changed scope).
                if (chosen != null) { if (StillReaches(chosen)) { HandleCore(chosen.Chord!.Value.ToInput(), mouse, ref reachedKeys); } return; }
                if (used) { return; }
            }
            // A drag a container started (a splitter's divider) keeps its moves and release, also over hints or popups.
            if (input.Kind == InputEventKind.Mouse && _containerCapture) {
                _viewport.HandleContainerInput(input);
                if (input.MouseAction == MouseAction.Up || (input.MouseAction == MouseAction.Move && (input.HeldButtons & 1) == 0)) { _containerCapture = false; }
                Invalidate();
                return;
            }
            // A scroll bar drag keeps the pointer until the button is released, wherever that happens.
            if (input.Kind == InputEventKind.Mouse && _scrollCapture != null) {
                if ((input.HeldButtons & 1) != 0) { _scrollCapture.DragScrollbar(input.Row); }
                if (input.MouseAction == MouseAction.Up || (input.MouseAction == MouseAction.Move && (input.HeldButtons & 1) == 0)) { _scrollCapture = null; }
                Invalidate();
                return;
            }
            // An open popup is drawn over everything else, so it takes the pointer first; a press outside closes it.
            if (input.Kind == InputEventKind.Mouse && !modal && _pointerCapture == null && _overlay.RoutePointer(input, _context, out InputControl? popupOwner, out int? popupBefore)) { Share(popupOwner, popupBefore); Invalidate(); return; }
            // A click on a footer hint presses its key; a click on a breadcrumb returns to that screen.
            if (input.Kind == InputEventKind.Mouse && _pointerCapture == null && (!modal || _overlay.ModalDrawn) && _overlay.Targets.Route(input, clicks, out KeyChord? pressed, CurrentScope())) {
                Invalidate();
                if (pressed.HasValue) { HandleCore(pressed.Value.ToInput(), mouse, ref reachedKeys); }
                return;
            }
            if ((_actions.HasCommands || _actions.HasModal) && _actions.Handle(input)) { Invalidate(); return; }
            if (input.Kind == InputEventKind.Mouse && input.MouseAction == MouseAction.Down && input.MouseButton == 3) {
                _viewport.FocusAt(input.Column, input.Row);
                OpenMenu(input.Column, input.Row);
                return;
            }
            if (input.Is(ConsoleKey.C, KeyModifiers.Control)) { _state.RequestExit(); Invalidate(); return; }
            if (_options.HandleInput?.Invoke(_state, input) == true) { Invalidate(); return; }
            if (input.Kind == InputEventKind.Mouse && _pointerCapture != null && _overlay.RoutePointer(input, _context, out InputControl? capturedOwner, out int? capturedBefore)) { Share(capturedOwner, capturedBefore); Invalidate(); return; }
            if (!ClaimedResizeKey(input) && _viewport.HandleContainerInput(input)) {
                if (input.Kind == InputEventKind.Mouse && input.MouseAction == MouseAction.Down && input.MouseButton == 1) { _containerCapture = true; }
                Invalidate();
                return;
            }
            if (input.Kind != InputEventKind.Mouse && input.Kind != InputEventKind.FocusLost && input.Kind != InputEventKind.FocusGained) { _viewport.RevealFocus(); }
            if (input.Is(ConsoleKey.C, KeyModifiers.Control)) { _state.RequestExit(); }
            else if (input.Kind == InputEventKind.Resize) { PrepareFrame(input.Width, input.Height); }
            else if (AppHostKeys.Match(_options.Keys.NextFocus, input)) { _focus.MoveNext(); }
            else if (AppHostKeys.Match(_options.Keys.PreviousFocus, input)) { _focus.MovePrevious(); }
            else if (input.Kind == InputEventKind.Mouse) {
                if (input.MouseAction == MouseAction.Down && input.MouseButton == 1 && _pointerCapture == null && (_scrollCapture = _viewport.ScrollbarAt(input)) != null) {
                    // A press on a scroll bar scrolls there; dragging keeps scrolling (above) until the release.
                    _scrollCapture.DragScrollbar(input.Row);
                    Invalidate();
                    return;
                }
                if (input.MouseAction == MouseAction.Down && input.MouseButton == 1 && _viewport.FocusAt(input.Column, input.Row)) { _pointerCapture = _focus.Current; }
                InputControl? target = _pointerCapture ?? _focus.Current;
                // A focused chart under the pointer zooms with the wheel before a scroll view around it scrolls.
                int? targetBefore = Selected(target);
                if (input.MouseAction == MouseAction.Wheel && target != null && target.TakesWheel && _viewport.FrameBounds(target) is Rect over
                    && input.Column >= over.X && input.Column < over.Right && input.Row >= over.Y && input.Row < over.Bottom
                    && target.HandleInput(_viewport.TranslatePointer(target, input), _context)) { Share(target, targetBefore); Invalidate(); return; }
                if (input.MouseAction == MouseAction.Wheel && _viewport.Scroll(input)) { Invalidate(); return; }
                if (target != null) { pointerHandled = target.HandleInput(_viewport.TranslatePointer(target, input), _context); if (pointerHandled) { Share(target, targetBefore); } }
                // A double click opens the row, as Enter does: the input's own bindings first, then the input, then the
                // screen's and the app's bindings. Focus does not move on, as it would after an unused Enter.
                if (clicks == 2 && input.MouseButton == 1 && pointerHandled && target != null && target.OpensOnDoubleClick && ReferenceEquals(target, _focus.Current)) {
                    InputEvent enter = InputEvent.Press(ConsoleKey.Enter);
                    if (!_keys.DispatchFocused(enter) && !target.HandleInput(enter, _context)) { _keys.Dispatch(enter); }
                    reachedKeys = true;
                }
                if (input.MouseAction == MouseAction.Up) { _pointerCapture = null; }
            } else {
                InputControl? current = _focus.Current;
                if (current != null && !current.Enabled) { _focus.MoveNext(); current = _focus.Current; }
                // A selector can consume Escape to clear a filter; then the screen (Back) and app bindings; an unhandled Escape quits the app.
                bool handled = _keys.DispatchFocused(input);
                reachedKeys = handled;
                int? currentBefore = Selected(current);
                if (!handled && current != null && current.HandleInput(input, _context)) { handled = true; Share(current, currentBefore); }
                if (!handled) { handled = _keys.Dispatch(input); reachedKeys = true; }
                // The Menu key and Shift+F10 open the context menu at the focused input, unless a binding took them.
                if (!handled && AppContextMenu.IsOpenKey(input) && !_actions.HasModal) {
                    Rect? at = current == null ? null : _viewport.FrameBounds(current);
                    OpenMenu(at?.X + 1 ?? 0, at?.Y + 1 ?? 0);
                    handled = true;
                }
                if (!handled && AppHostKeys.Match(_options.Keys.Exit, input)) { _state.RequestExit(); }
                else if (!handled && input.Is(ConsoleKey.Enter)) { _focus.MoveNext(); }
            }
            if (input.Kind != InputEventKind.Mouse || input.MouseAction != MouseAction.Move || input.HeldButtons != 0 || pointerHandled) { Invalidate(); }
        }

        // Lists what the keys reach right now; an empty menu does not open.
        private void OpenMenu(int column, int row) {
            if (_actions.HasModal) { return; }
            AppContextMenu menu = new AppContextMenu(_keys.MenuEntries(HostBindings(), HostFirst()), column, row, CurrentScope());
            if (!menu.IsEmpty) { _overlay.Menu = menu; }
            Invalidate();
        }

        private bool ClaimedResizeKey(InputEvent input) => input.Kind == InputEventKind.Key &&
            _splitters.Any(splitter => splitter.ResizeKeys.Any(chord => chord.Matches(input) && ResizeKeyClaimed(chord)));

        private bool ResizeKeyClaimed(KeyChord chord) => _keys.Claims(chord) || HostFirst().Contains(chord) || Equals(_options.Keys.Exit, chord);

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

        // The hold ends before the snapshot is published, so the snapshot carries the focus version that matches it.
        // Host and control entries are built again for every list, so an entry is recognised by what it says and the key
        // that reaches it in the current scope; a screen that reuses the key for something else does not match.
        private bool StillReaches(AppKeyMap.Entry chosen) =>
            chosen.Chord.HasValue && _keys.MenuEntries(HostBindings(), HostFirst()).Any(entry => Nullable.Equals(entry.Chord, chosen.Chord)
                && string.Equals(entry.Binding.Label, chosen.Binding.Label, StringComparison.Ordinal) && string.Equals(entry.Keys, chosen.Keys, StringComparison.Ordinal));

        private KeyScope CurrentScope() {
            InputControl? focused = _focus.Current;
            return new KeyScope(focused != null && !focused.Disabled ? focused : null, _screens?.Current, _actions.Modal);
        }

        private void Navigated() {
            // A menu lists one screen's bindings; it does not survive the switch.
            _overlay.Menu = null;
            try { ConstrainFocus(); }
            finally { _focus.Release(); }
            Publish();
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
                app.Status = modal == null ? _focus.Current?.FooterStatus(_context) : null;
            }
            KeyScope scope = CurrentScope();
            // A menu another scope listed is not drawn again.
            if (_overlay.Menu is AppContextMenu menu && !Equals(menu.Scope, scope)) { _overlay.Menu = null; }
            _overlay.Publish(_screens?.RefreshCrumbs(), app, hints, _focus.Version, scope, _session);
        }

        // Host gestures as hints; the host matches them itself. With screens, "back" means the previous screen, so the
        // Shift+Tab focus hint stays in the key map only.
        private List<KeyBinding> HostBindings() {
            PromptLabels labels = PromptLabels.For(_context.Profile.Language);
            List<KeyBinding> hints = new List<KeyBinding>();
            if (_options.Keys.NextFocus.HasValue) { hints.Add(new KeyBinding(labels.KeyNext, _focus.MoveNext, _options.Keys.NextFocus.Value)); }
            if (_options.Keys.PreviousFocus.HasValue) { hints.Add(new KeyBinding(labels.KeyBack, _focus.MovePrevious, _options.Keys.PreviousFocus.Value) { InFooter = _screens == null }); }
            if (_options.Keys.Exit.HasValue) { hints.Add(new KeyBinding(labels.KeyQuit, _state.RequestExit, _options.Keys.Exit.Value)); }
            // Text editing keeps its chords; list resize only while the focused input leaves it free.
            InputControl? focused = _focus.Current;
            Splitter? around = focused == null ? null : _splitters.LastOrDefault(splitter => FocusManager.Discover(splitter).Contains(focused));
            if (around != null && around.ResizeKeys.All(chord => !ResizeKeyClaimed(chord))) { hints.Add(new KeyBinding(labels.KeyResize, () => { }, around.ResizeKeys) { KeyText = around.Vertical ? "ctrl+shift+↑/↓" : "ctrl+shift+←/→", Order = 60 }); }
            // Inside a tab set the tab keys work from any control, so the footer lists them there too.
            if (focused != null && !focused.NavigationOnly && _tabSets.Any(tabs => FocusManager.Discover(tabs).Contains(focused))) {
                hints.Add(new KeyBinding(labels.KeyTabs, () => { }, new KeyChord(ConsoleKey.PageUp, KeyModifiers.Control), new KeyChord(ConsoleKey.PageDown, KeyModifiers.Control)) { KeyText = "ctrl+pgup/pgdn", Order = 61 });
            }
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
            if (widget is Tabs tabs) { _hasTabs = true; _tabSets.Add(tabs); }
            if (widget is Accordion accordion) { accordion.UseLabels(PromptLabels.For(_context.Profile.Language)); }
            if (widget is Splitter splitter) { _splitters.Add(splitter); }
            if (widget is LogRegion region && _logRegion == null) { _logRegion = region; }
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
        private void StateChanged(object? sender, AppValueChangedEventArgs change) {
            lock (_written) { _written.Add(change.Key); }
            foreach (ILinkedSelection control in _linked) {
                if (!string.Equals(control.LinkKey, change.Key, StringComparison.OrdinalIgnoreCase)) { continue; }
                // A notification delivered after a nested write carries an older value; follow what state holds now.
                int? index = LinkIndex(_state.TryGetValue(change.Key, out object? current) ? current : change.Value);
                // The publisher already shows its own write; a nested write to another index still reaches it.
                if (ReferenceEquals(control, _sharing) && control.LinkIndex == index) { continue; }
                control.FollowLink(index);
            }
            Invalidate();
        }

        // Controls sharing a selection key start on one selection: a value already in state, else the first one's.
        private void ShareInitialSelections() {
            _linked.AddRange(_focus.Controls.OfType<ILinkedSelection>().Where(control => !string.IsNullOrEmpty(control.LinkKey)));
            foreach (IGrouping<string, ILinkedSelection> group in _linked.GroupBy(control => control.LinkKey!, StringComparer.OrdinalIgnoreCase)) {
                if (_state.TryGetValue(group.Key, out object? value)) {
                    int? index = LinkIndex(value);
                    foreach (ILinkedSelection control in group) { control.FollowLink(index); }
                } else {
                    ILinkedSelection? first = group.FirstOrDefault(control => control.LinkIndex.HasValue);
                    if (first != null) { Share(first, null); }
                }
            }
        }

        // After the user moved a linked control's selection, the others follow through state.
        // An input publishes only a selection it changed, so a key, a hover or a scroll that leaves the selection where it
        // was never overwrites a value another control or a callback wrote.
        private void Share(object? input, int? before) {
            if (!(input is ILinkedSelection control)) { return; }
            string? key = control.LinkKey;
            int? index = string.IsNullOrEmpty(key) ? null : control.LinkIndex;
            if (key == null || !index.HasValue || index == before) { return; }
            lock (_written) { if (_written.Contains(key)) { return; } }
            _sharing = control;
            try { _state[key] = index.Value; }
            finally { _sharing = null; }
        }

        private static int? Selected(InputControl? control) => (control as ILinkedSelection)?.LinkIndex;

        private static int? LinkIndex(object? value) {
            if (value is int i) { return i; }
            if (value is IConvertible convertible && !(value is string)) {
                try { return Convert.ToInt32(convertible, System.Globalization.CultureInfo.InvariantCulture); } catch (FormatException) { } catch (InvalidCastException) { } catch (OverflowException) { }
            }
            if (value is string text && int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int parsed)) { return parsed; }
            return null;
        }
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
