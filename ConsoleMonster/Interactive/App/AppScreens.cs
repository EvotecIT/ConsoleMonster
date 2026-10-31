using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The screens of an app as a stack. The first screen added is the root; <see cref="Push"/> opens another and
    /// <see cref="Back"/> returns, which Escape and Backspace do when the focused input leaves them unused. Every screen is
    /// registered up front, so inputs inside screens keep their state values and take part in focus; only the current
    /// screen is mounted. Each screen remembers its focused input and brings its own key bindings to the footer, and a
    /// <see cref="Widgets.HeaderBar"/> without crumbs of its own shows the path. The current key is also the state value
    /// <see cref="StateKey"/>: assigning it from a callback opens that screen.
    /// </summary>
    public sealed class AppScreens : IContainer {
        [ThreadStatic] private static View? _ambient;
        private readonly object _gate = new object();
        private readonly List<AppScreen> _screens = new List<AppScreen>();
        private readonly List<AppScreen> _path = new List<AppScreen>();
        private readonly Dictionary<AppScreen, InputControl> _lastFocus = new Dictionary<AppScreen, InputControl>();
        private AppState? _state;
        private KeyBinding? _back;
        private View? _view;

        /// <summary>Creates a stack; the first screen is the root.</summary>
        /// <param name="screens">The screens.</param>
        public AppScreens(params AppScreen[] screens) {
            if (screens != null) { foreach (AppScreen screen in screens) { Add(screen); } }
        }

        /// <summary>Registers a screen. Add every screen before the app runs.</summary>
        /// <param name="screen">The screen; its key must be unique.</param>
        /// <returns>This instance.</returns>
        public AppScreens Add(AppScreen screen) {
            if (screen == null) { throw new ArgumentNullException(nameof(screen)); }
            lock (_gate) {
                if (Find(screen.Key) != null) { throw new ArgumentException("Screen keys must be unique: '" + screen.Key + "'.", nameof(screen)); }
                _screens.Add(screen);
                if (_path.Count == 0) { _path.Add(screen); }
            }
            return this;
        }

        /// <summary>The state key holding the current screen's key (default <c>Screen</c>).</summary>
        public string StateKey { get; set; } = "Screen";

        /// <summary>Keys that return to the previous screen when the focused input does not use them (default Escape and Backspace).</summary>
        public IList<KeyChord> BackKeys { get; } = new List<KeyChord> { new KeyChord(ConsoleKey.Escape), new KeyChord(ConsoleKey.Backspace) };

        /// <summary>The registered screens, root first.</summary>
        public IReadOnlyList<AppScreen> Screens { get { lock (_gate) { return _screens.ToArray(); } } }

        /// <summary>The open screens from the root to the current one.</summary>
        public IReadOnlyList<AppScreen> Path { get { lock (_gate) { return _path.ToArray(); } } }

        /// <summary>The current screen.</summary>
        public AppScreen Current { get { lock (_gate) { return CurrentCore(); } } }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children { get { lock (_gate) { return _screens.Select(screen => screen.Content).ToArray(); } } }

        /// <summary>Opens a screen on top of the current one. A screen already on the path is returned to instead of stacked twice.</summary>
        /// <param name="key">The screen key.</param>
        public void Push(string key) => Navigate(key ?? throw new ArgumentNullException(nameof(key)));

        /// <summary>Returns to the previous screen.</summary>
        /// <returns>False at the root.</returns>
        public bool Back() {
            AppScreen previous;
            lock (_gate) {
                if (_path.Count < 2) { return false; }
                previous = _path[_path.Count - 2];
            }
            Navigate(previous.Key);
            return true;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Shown().Content.Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Shown().Content.Render(region, context);

        // A frame shows one snapshot of the screen and its crumbs (an app's frame also takes its footer from it), so a
        // header never names a screen that is not drawn.
        private AppScreen Shown() => (_ambient ?? Volatile.Read(ref _view))?.Screen ?? Current;

        // The app builds a screen's bindings and holds focus reconciliation before a switch, moves focus and publishes the
        // frame snapshot after it, and applies a screen requested through state from its next refresh.
        internal Action<AppScreen>? Showing { get; set; }
        internal Action? Navigated { get; set; }
        internal Action? Requested { get; set; }
        private string? _requested;

        internal void Attach(AppState state, PromptLabels labels) {
            if (BackKeys.Count > 0) {
                _back = new KeyBinding(labels.KeyBack, () => Back(), BackKeys.ToArray()) { Order = 10 };
            }
            _state = state;
            if (state.TryGetValue(StateKey, out object? requested) && requested is string key && Find(key) != null) { Navigate(key); }
            state[StateKey] = Current.Key;
            state.Changed += StateChanged;
            RefreshCrumbs();
        }

        internal void Detach() {
            if (_state != null) { _state.Changed -= StateChanged; }
            _state = null;
            Showing = null;
            Navigated = null;
            Requested = null;
        }

        // Inputs on screens other than the current one; they keep their values but cannot have focus.
        internal IEnumerable<InputControl> HiddenInputs() {
            AppScreen current;
            AppScreen[] screens;
            lock (_gate) { current = CurrentCore(); screens = _screens.ToArray(); }
            return screens.Where(screen => !ReferenceEquals(screen, current)).SelectMany(screen => FocusManager.Discover(screen.Content));
        }

        internal bool IsShown(AppScreen screen) { lock (_gate) { return ReferenceEquals(CurrentCore(), screen); } }

        // The current screen's bindings, then Back while there is somewhere to go back to.
        internal IEnumerable<KeyBinding> Bindings() {
            AppScreen current;
            bool nested;
            lock (_gate) { current = CurrentCore(); nested = _path.Count > 1; }
            foreach (KeyBinding binding in current.Bindings) { yield return binding; }
            if (nested && _back != null) { yield return _back; }
        }

        // Titles are built on the app thread, where title callbacks may read state; frames read the published view.
        internal View RefreshCrumbs() {
            AppScreen[] path;
            lock (_gate) { path = _path.ToArray(); }
            string[] titles = new string[path.Length];
            for (int i = 0; i < path.Length; i++) {
                titles[i] = path[i].TitleSource != null && _state != null ? path[i].TitleSource!(_state) ?? string.Empty : path[i].Title;
            }
            View view = new View(path[path.Length - 1], titles);
            Volatile.Write(ref _view, view);
            return view;
        }

        // Header bars and the stack drawn inside this frame read one snapshot without being wired to the stack.
        internal static IDisposable Scope(View? view) => new CrumbScope(view);
        internal static IReadOnlyList<string>? AmbientCrumbs => _ambient?.Crumbs;

        // A callback may assign the screen before the values that screen shows (a script writes its keys in any order),
        // so the app opens it from its refresh, once the callback has finished.
        private void StateChanged(object? sender, AppValueChangedEventArgs change) {
            if (!string.Equals(change.Key, StateKey, StringComparison.OrdinalIgnoreCase) || !(change.Value is string key)) { return; }
            if (Requested == null) { Navigate(key); return; }
            lock (_gate) {
                AppScreen target = Find(key) ?? throw new ArgumentException("No screen has the key '" + key + "'.", nameof(change));
                _requested = ReferenceEquals(target, CurrentCore()) ? null : key;
                if (_requested == null) { return; }
            }
            Requested();
        }

        internal void ApplyRequested() {
            string? key;
            lock (_gate) { key = _requested; _requested = null; }
            if (key != null) { Navigate(key); }
        }

        private void Navigate(string key) {
            AppScreen leaving;
            AppScreen target;
            lock (_gate) {
                target = Find(key) ?? throw new ArgumentException("No screen has the key '" + key + "'.", nameof(key));
                leaving = CurrentCore();
                if (ReferenceEquals(target, leaving)) { return; }
            }
            // The app builds the target's bindings and holds focus reconciliation; only then does the path change. Focus
            // moves outside the stack lock (focus changes invalidate controls, which renders read under their own locks),
            // and the app publishes the new screen with its footer in one snapshot, also when a handler throws.
            Showing?.Invoke(target);
            try {
                lock (_gate) {
                    int index = _path.IndexOf(target);
                    if (index >= 0) { _path.RemoveRange(index + 1, _path.Count - index - 1); }
                    else { _path.Add(target); }
                }
                InputControl? focused = FocusManager.Discover(leaving.Content).FirstOrDefault(control => control.Focused);
                if (focused != null) { lock (_gate) { _lastFocus[leaving] = focused; } }
                InputControl? saved;
                lock (_gate) { _lastFocus.TryGetValue(target, out saved); }
                InputControl? restore = saved != null && !saved.Disabled ? saved : FocusManager.Discover(target.Content).FirstOrDefault(control => !control.Disabled);
                restore?.Focus();
                if (_state != null) { _state[StateKey] = target.Key; }
            } finally {
                if (Navigated != null) { Navigated(); } else { RefreshCrumbs(); }
            }
        }

        private AppScreen CurrentCore() {
            if (_path.Count == 0) { throw new InvalidOperationException("Add a screen before using the stack."); }
            return _path[_path.Count - 1];
        }

        private AppScreen? Find(string key) {
            foreach (AppScreen screen in _screens) {
                if (string.Equals(screen.Key, key, StringComparison.OrdinalIgnoreCase)) { return screen; }
            }
            return null;
        }

        internal sealed class View {
            internal View(AppScreen screen, string[] crumbs) { Screen = screen; Crumbs = crumbs; }
            internal AppScreen Screen { get; }
            internal string[] Crumbs { get; }
        }

        private sealed class CrumbScope : IDisposable {
            private readonly View? _previous;
            internal CrumbScope(View? view) { _previous = _ambient; _ambient = view; }
            public void Dispose() => _ambient = _previous;
        }
    }
}
