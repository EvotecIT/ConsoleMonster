using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Two panes with a divider between them. Dragging the divider resizes the panes, and so do Ctrl+Shift+←/→
    /// (Ctrl+Shift+↑/↓ when <see cref="Vertical"/>) when the focused input leaves those keys free. Text editing takes
    /// precedence. Inside an app, a splitter with a
    /// <see cref="Key"/> keeps its ratio in the app state under that key, so a rebuilt layout or a dependent binding keeps it,
    /// and a callback can read or set it. A vertical splitter is as tall as its panes need; give it a fixed or fractional
    /// size in its parent layout to fill the space.
    /// </summary>
    public sealed class Splitter : IContainer, IAppInputContainer {
        private const int Step = 2;
        private readonly object _gate = new object();
        private IWidget _first;
        private IWidget _second;
        private double _ratio = 0.5;
        private Rect _divider;
        private Rect _area;
        private bool _dragging;
        private AppState? _state;
        private RenderContext? _context;

        /// <summary>Creates a splitter with empty panes.</summary>
        public Splitter() : this(new Rows(), new Rows()) { }

        /// <summary>Creates a splitter.</summary>
        /// <param name="first">The left pane, or the top pane when <see cref="Vertical"/>.</param>
        /// <param name="second">The right pane, or the bottom pane when <see cref="Vertical"/>.</param>
        public Splitter(IWidget first, IWidget second) {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        /// <summary>The left pane, or the top pane when <see cref="Vertical"/>.</summary>
        public IWidget First { get { lock (_gate) { return _first; } } set { lock (_gate) { _first = value ?? throw new ArgumentNullException(nameof(value)); } } }

        /// <summary>The right pane, or the bottom pane when <see cref="Vertical"/>.</summary>
        public IWidget Second { get { lock (_gate) { return _second; } } set { lock (_gate) { _second = value ?? throw new ArgumentNullException(nameof(value)); } } }

        /// <summary>Stacks the panes top and bottom with a horizontal divider instead of side by side.</summary>
        public bool Vertical { get; set; }

        /// <summary>The first pane's share of the space, between 0.05 and 0.95 (default 0.5).</summary>
        public double Ratio {
            get { lock (_gate) { return _ratio; } }
            set {
                if (double.IsNaN(value) || value < 0.05 || value > 0.95) { throw new ArgumentOutOfRangeException(nameof(value), "The ratio must be between 0.05 and 0.95."); }
                lock (_gate) { _ratio = value; }
                Remember();
            }
        }

        /// <summary>The app-state key that holds the ratio (a number from 0.05 to 0.95); null keeps it with this instance only.</summary>
        public string? Key { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children { get { lock (_gate) { return new[] { _first, _second }; } } }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            IWidget first, second;
            double ratio;
            lock (_gate) { first = _first; second = _second; ratio = EffectiveRatio(InputArrangement.CurrentState); }
            if (Vertical) {
                int top = first.Measure(new Constraints(constraints.MaxWidth), context).Height;
                int bottom = second.Measure(new Constraints(constraints.MaxWidth), context).Height;
                return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, FittingHeight(top, bottom, constraints.MaxHeight, ratio)));
            }
            int left = Split(constraints.MaxWidth, ratio);
            int right = Math.Max(0, constraints.MaxWidth - left - 1);
            int height = Math.Max(first.Measure(constraints.Width(left), context).Height, second.Measure(constraints.Width(right), context).Height);
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, height));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            IWidget first, second;
            bool dragging, publish;
            lock (_gate) {
                _context = context;
                _state = InputArrangement.CurrentState;
                publish = !Restore();
                first = _first; second = _second; dragging = DraggingCore;
            }
            // A keyed splitter that finds no ratio in the app state publishes its own, so the state reads it from the start.
            if (publish) { Remember(); }
            InputArrangement.Record((IAppInputContainer)this);
            if (region.Width <= 0 || region.Height <= 0) { return; }
            bool ascii = context.Glyphs.Tier == GlyphTier.Ascii;
            Style line = context.Style(dragging || FocusInside() ? ThemeToken.Accent : ThemeToken.Border);
            Rect divider;
            if (Vertical) {
                int top = Split(region.Height);
                region.HorizontalLine(0, top, region.Width, ascii ? "-" : "─", line);
                divider = new Rect(region.Bounds.X, region.Bounds.Y + top, region.Width, 1);
                if (top > 0) { first.Render(region.Sub(0, 0, region.Width, top), context); }
                if (region.Height - top - 1 > 0) { second.Render(region.Sub(0, top + 1, region.Width, region.Height - top - 1), context); }
            } else {
                int left = Split(region.Width);
                region.VerticalLine(left, 0, region.Height, ascii ? "|" : "│", line);
                divider = new Rect(region.Bounds.X + left, region.Bounds.Y, 1, region.Height);
                if (left > 0) { first.Render(region.Sub(0, 0, left, region.Height), context); }
                if (region.Width - left - 1 > 0) { second.Render(region.Sub(left + 1, 0, region.Width - left - 1, region.Height), context); }
            }
            lock (_gate) { _divider = InputArrangement.FrameBounds(divider); _area = InputArrangement.FrameBounds(region.Bounds); }
        }

        bool IAppInputContainer.HandleContainerInput(InputEvent input, InputControl? focused) {
            if (input.Kind == InputEventKind.FocusLost || input.Kind == InputEventKind.Resize) {
                lock (_gate) { DraggingCore = false; }
                return false;
            }
            if (input.Kind == InputEventKind.Mouse) { return Pointer(input); }
            if (input.Kind != InputEventKind.Key || focused == null || !FocusManager.Discover(this).Contains(focused)) { return false; }
            ConsoleKey shrink = Vertical ? ConsoleKey.UpArrow : ConsoleKey.LeftArrow;
            ConsoleKey grow = Vertical ? ConsoleKey.DownArrow : ConsoleKey.RightArrow;
            KeyModifiers chord = KeyModifiers.Control | KeyModifiers.Shift;
            if (!input.Is(shrink, chord) && !input.Is(grow, chord)) { return false; }
            RenderContext? context;
            lock (_gate) { context = _context; }
            if (context == null || !ResizeKeyIsFree(focused, new KeyChord(input.Key, input.Modifiers), context)) { return false; }
            lock (_gate) {
                int extent = Math.Max(1, Vertical ? _area.Height : _area.Width);
                Resize(Split(extent) + (input.Is(grow, chord) ? Step : -Step), extent);
            }
            Remember();
            return true;
        }

        private static bool ResizeKeyIsFree(InputControl focused, KeyChord chord, RenderContext context) =>
            !focused.Takes(chord, context) && !focused.Bindings.Any(binding => binding.Claims.Contains(chord));

        // The keys a footer lists while focus is inside this splitter.
        internal KeyChord[] ResizeKeys => Vertical
            ? new[] { new KeyChord(ConsoleKey.UpArrow, KeyModifiers.Control | KeyModifiers.Shift), new KeyChord(ConsoleKey.DownArrow, KeyModifiers.Control | KeyModifiers.Shift) }
            : new[] { new KeyChord(ConsoleKey.LeftArrow, KeyModifiers.Control | KeyModifiers.Shift), new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift) };

        private bool Pointer(InputEvent input) {
            lock (_gate) {
                if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                    DraggingCore = Contains(_divider, input.Column, input.Row);
                    return DraggingCore;
                }
                if (!DraggingCore) { return false; }
                if (input.MouseAction == MouseAction.Up) { DraggingCore = false; return true; }
                if (input.MouseAction != MouseAction.Move) { return false; }
                int extent = Vertical ? _area.Height : _area.Width;
                Resize(Vertical ? input.Row - _area.Y : input.Column - _area.X, extent);
            }
            Remember();
            return true;
        }

        // A keyed splitter in an app keeps its drag in the app state, so the splitter a binding rebuilt mid-drag goes on with it.
        private bool DraggingCore {
            get => _state != null && !string.IsNullOrEmpty(Key) ? _state.Dragging.ContainsKey(Key!) : _dragging;
            set {
                if (_state != null && !string.IsNullOrEmpty(Key)) {
                    if (value) { _state.Dragging[Key!] = true; } else { _state.Dragging.TryRemove(Key!, out _); }
                } else { _dragging = value; }
            }
        }

        // Places the divider at a cell, keeping a cell for each pane, and remembers the result.
        private void Resize(int cells, int extent) {
            if (extent < 3) { return; }
            int at = Math.Max(1, Math.Min(extent - 2, cells));
            _ratio = Math.Max(0.05, Math.Min(0.95, (at + 0.5) / extent));
        }

        // The least height whose split by the ratio gives the top pane at least its rows and the bottom pane at least its own,
        // so a splitter sized to its content draws both panes in full.
        private static int FittingHeight(int top, int bottom, int limit, double ratio) {
            int height = top + 1 + bottom;
            int most = limit == Constraints.Unbounded ? height * 21 : limit;
            for (int candidate = Math.Max(3, height); candidate <= most; candidate++) {
                int split = Split(candidate, ratio);
                if (split >= top && candidate - split - 1 >= bottom) { return candidate; }
            }
            return height;
        }

        // The first pane's extent for a given width (or height), so a host can lay out its content before drawing.
        internal int FirstExtent(int extent) => Split(extent);

        private int Split(int extent) {
            double ratio;
            lock (_gate) { ratio = _ratio; }
            return Split(extent, ratio);
        }

        private static int Split(int extent, double ratio) {
            if (extent < 3) { return Math.Max(0, extent - 1) / 2; }
            return Math.Max(1, Math.Min(extent - 2, (int)Math.Floor(extent * ratio)));
        }

        // A keyed splitter takes the ratio the app state holds, so a rebuilt one and a callback that set it agree with the last drag.
        // Returns false when a keyed splitter in an app finds no ratio under its key, so the caller publishes its own.
        private bool Restore() {
            if (_state == null || string.IsNullOrEmpty(Key)) { return true; }
            if (!_state.TryGetValue(Key!, out _)) { return false; }
            _ratio = EffectiveRatio(_state);
            return true;
        }

        // The ratio a keyed splitter uses with this state: the number stored under its key, else its own. Read-only, so
        // measuring (which can run before the splitter is drawn, and on a rebuilt instance) agrees with drawing.
        private double EffectiveRatio(AppState? state) {
            if (state == null || string.IsNullOrEmpty(Key) || !state.TryGetValue(Key!, out object? stored)) { return _ratio; }
            double? ratio = stored is double d ? d : stored is float f ? f : stored is decimal m ? (double)m : stored is int i ? i : (double?)null;
            return ratio.HasValue && !double.IsNaN(ratio.Value) ? Math.Max(0.05, Math.Min(0.95, ratio.Value)) : _ratio;
        }

        // Writes the ratio to the app state after the splitter lock is released: the write notifies the app and its bindings.
        private void Remember() {
            AppState? state;
            string? key = Key;
            double ratio;
            lock (_gate) { state = _state; ratio = _ratio; }
            if (state != null && !string.IsNullOrEmpty(key)) { state[key!] = ratio; }
        }

        private bool FocusInside() {
            InputControl? focused = InputArrangement.CurrentFocus;
            return focused != null && FocusManager.Discover(this).Contains(focused);
        }

        private static bool Contains(Rect area, int x, int y) => x >= area.X && x < area.Right && y >= area.Y && y < area.Bottom;
    }
}
