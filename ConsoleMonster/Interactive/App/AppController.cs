using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.Interactive {
    /// <summary>Owns callbacks and dependent-widget creation on the input thread; only live payload application runs on the renderer.</summary>
    internal sealed class AppController : IDisposable {
        private readonly AppState _state;
        private readonly List<AppBinding> _bindings = new List<AppBinding>();
        private readonly FocusManager _focus;
        private readonly RenderContext _context;
        private readonly AppViewport _viewport;
        private readonly object _commitGate = new object();
        private readonly Dictionary<string, InputControl> _inputs = new Dictionary<string, InputControl>(StringComparer.OrdinalIgnoreCase);
        private int _dirty;
        private bool _disposed;
        private readonly AppCommandHost _actions;
        private InputControl? _pointerCapture;

        internal AppController(IWidget layout, AppState state, RenderContext context, int reflowBelow, ConsoleAppOptions? options = null, CancellationToken cancellationToken = default) {
            _state = state;
            _context = context;
            Discover(layout, reflowBelow);
            _focus = new FocusManager(layout);
            _viewport = new AppViewport(layout, _focus);
            List<AppCommand> commands = new List<AppCommand>(options?.Commands ?? (IEnumerable<AppCommand>)Array.Empty<AppCommand>());
            foreach (InputControl input in _focus.Controls) {
                if (input is Button commandButton && commandButton.Command != null && !commands.Contains(commandButton.Command)) { commands.Add(commandButton.Command); }
            }
            try {
                _actions = new AppCommandHost(commands, () => options?.Selection?.Invoke(_state) ?? Array.Empty<object?>(), PromptLabels.For(context.Profile.Language), cancellationToken, dispatcher: options?.Dispatcher);
                foreach (InputControl control in _focus.Controls) {
                    _inputs.Add(control.Key, control);
                    state[control.Key] = control.CurrentValue;
                    control.ValueCommitted += Commit;
                    control.Invalidated += Invalidate;
                }
                state.Changed += StateChanged;
                List<KeyBinding> hints = new List<KeyBinding> {
                    new KeyBinding(PromptLabels.For(context.Profile.Language).KeyNext, _focus.MoveNext, new KeyChord(ConsoleKey.Tab)),
                    new KeyBinding(PromptLabels.For(context.Profile.Language).KeyBack, _focus.MovePrevious, new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift)),
                    new KeyBinding(PromptLabels.For(context.Profile.Language).KeyQuit, state.RequestExit, new KeyChord(ConsoleKey.Escape))
                };
                _actions.AddBindings(hints);
                Root = new Rows().Add(_actions.Wrap(_viewport), LayoutSize.Fraction()).Add(new KeyFooter(hints), LayoutSize.Fixed(1));
                foreach (InputControl input in _focus.Controls) {
                    if (input is Button button && button.Command != null) { _actions.Bind(button, button.Command); }
                }
                foreach (AppBinding binding in _bindings) { binding.Refresh(state, null); }
            } catch { Dispose(); throw; }
        }

        internal IWidget Root { get; }
        internal FocusManager Focus => _focus;

        internal CellBuffer PrepareFrame(int width, int height) => Rendering.FrameComposer.Compose(Root, width, height, _context);

        internal void Handle(InputEvent input, bool mouse) {
            bool pointerHandled = false;
            if (input.Kind == InputEventKind.FocusLost || input.Kind == InputEventKind.Resize) { _pointerCapture?.CancelPointer(); _pointerCapture = null; }
            if (_actions.HasCommands && (input.Kind != InputEventKind.Mouse || mouse) && _actions.Handle(input)) { Invalidate(); return; }
            if (input.Kind != InputEventKind.Mouse && input.Kind != InputEventKind.FocusLost && input.Kind != InputEventKind.FocusGained) { _viewport.RevealFocus(); }
            if (input.Is(ConsoleKey.C, KeyModifiers.Control)) { _state.RequestExit(); }
            else if (input.Kind == InputEventKind.Resize) { PrepareFrame(input.Width, input.Height); }
            else if (input.Is(ConsoleKey.Tab)) { _focus.MoveNext(); }
            else if (input.Is(ConsoleKey.Tab, KeyModifiers.Shift)) { _focus.MovePrevious(); }
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
                // A selector can consume Escape to clear a filter; an unhandled Escape quits the app.
                bool handled = current?.HandleInput(input, _context) ?? false;
                if (!handled && input.Is(ConsoleKey.Escape)) { _state.RequestExit(); }
                else if (!handled && input.Is(ConsoleKey.Enter)) { _focus.MoveNext(); }
            }
            if (input.Kind != InputEventKind.Mouse || input.MouseAction != MouseAction.Move || input.HeldButtons != 0 || pointerHandled) { Invalidate(); }
        }

        internal void Refresh(LiveSession? session) {
            if (_actions.Poll()) { Invalidate(); }
            if (Interlocked.Exchange(ref _dirty, 0) == 0) { return; }
            foreach (AppBinding binding in _bindings) { binding.Refresh(_state, session); }
            session?.Invalidate();
        }

        private void Discover(IWidget widget, int reflowBelow) {
            if (widget is AppBinding binding) { _bindings.Add(binding); return; }
            if (widget is Columns columns && columns.CollapseBelow < 0) { columns.CollapseBelow = reflowBelow; }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) { Discover(child, reflowBelow); }
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
            foreach (InputControl control in _focus.Controls) {
                control.ValueCommitted -= Commit;
                control.Invalidated -= Invalidate;
                control.ViewportHeight = Constraints.Unbounded;
            }
            _viewport.Reset();
            _focus.Dispose();
            _actions?.Dispose();
        }
    }
}
