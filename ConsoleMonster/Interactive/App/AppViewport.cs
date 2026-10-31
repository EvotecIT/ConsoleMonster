using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Keeps focused inputs visible when a stacked application exceeds the terminal height.</summary>
    internal sealed class AppViewport : IContainer {
        private readonly object _gate = new object();
        private readonly IWidget _content;
        private readonly FocusManager _focus;
        private readonly int _reservedRows;
        private int _scroll;
        private Rect _bounds;
        private InputControl? _lastFocus;
        private bool _revealRequested = true;
        private Size _lastSize;
        private readonly InputArrangement _arrangement = new InputArrangement();
        [ThreadStatic] private static int? _frameVersion;

        internal AppViewport(IWidget content, FocusManager focus, int reservedRows = 0) { _content = content; _focus = focus; _reservedRows = reservedRows; }
        public IReadOnlyList<IWidget> Children => new[] { _content };

        public Size Measure(Constraints constraints, RenderContext context) {
            lock (_gate) {
                ConstrainInputs(constraints.MaxHeight);
                Size natural = _content.Measure(new Constraints(constraints.MaxWidth), context);
                return new Size(natural.Width, Math.Min(natural.Height, constraints.MaxHeight));
            }
        }

        public void Render(Region region, RenderContext context) {
            lock (_gate) {
                _bounds = region.Bounds;
                if (region.Width == 0 || region.Height == 0) { return; }
                ConstrainInputs(region.Height);
                int height = Math.Max(region.Height, _content.Measure(new Constraints(region.Width), context).Height);
                CellBuffer canvas = new CellBuffer(region.Width, height);
                int arrangedAt = _frameVersion ?? _focus.Version;
                _frameVersion = null;
                _arrangement.Focused = _focus.Current;
                using (_arrangement.Begin(region.Bounds.X, region.Bounds.Y - _scroll, region.Bounds)) { _content.Render(new Region(canvas), context); }
                InputControl? beforeFocus = _focus.Current;
                _focus.SetMounted(_arrangement.Mounted, arrangedAt);
                int before = _scroll;
                Rect? focused = _arrangement.FocusBounds;
                bool reveal = _revealRequested || _focus.Current != _lastFocus || _lastSize.Width != region.Width || _lastSize.Height != region.Height;
                _lastFocus = _focus.Current; _lastSize = new Size(region.Width, region.Height); _revealRequested = false;
                if (reveal && focused.HasValue) {
                    if (focused.Value.Y < region.Bounds.Y) { _scroll += focused.Value.Y - region.Bounds.Y; }
                    else if (focused.Value.Bottom > region.Bounds.Bottom) { _scroll += focused.Value.Bottom - region.Bounds.Bottom; }
                }
                _scroll = Math.Max(0, Math.Min(_scroll, height - region.Height));
                if (before != _scroll || beforeFocus != _focus.Current) {
                    canvas = new CellBuffer(region.Width, height);
                    _arrangement.Focused = _focus.Current;
                    using (_arrangement.Begin(region.Bounds.X, region.Bounds.Y - _scroll, region.Bounds)) { _content.Render(new Region(canvas), context); }
                }
                region.Buffer.Blit(canvas, 0, _scroll, region.Width, region.Height, region.Bounds.X, region.Bounds.Y);
            }
        }

        internal bool FocusAt(int column, int row) {
            lock (_gate) {
                if (column < _bounds.X || column >= _bounds.Right || row < _bounds.Y || row >= _bounds.Bottom) { return false; }
                return _arrangement.FocusAt(_focus, column, row);
            }
        }

        internal Input.InputEvent TranslatePointer(InputControl control, Input.InputEvent input) {
            lock (_gate) { return _arrangement.Translate(control, input); }
        }

        internal bool Scroll(Input.InputEvent input) {
            lock (_gate) {
                if (_arrangement.ScrollAt(input)) { return true; }
                return false;
            }
        }
        internal bool ScrollBy(int rows) { lock (_gate) { return _arrangement.ScrollBy(rows); } }
        // The overlay takes the focus version before it reads the frame snapshot, so a snapshot older than a focus change
        // can never reconcile focus against the inputs it shows.
        internal void BeginFrame() => _frameVersion = _focus.Version;
        internal InputArrangement.Popup[] Popups() { lock (_gate) { return new List<InputArrangement.Popup>(_arrangement.Popups).ToArray(); } }
        internal bool HandleContainerInput(Input.InputEvent input) { lock (_gate) { return _arrangement.HandleContainerInput(input, _focus.Current); } }

        internal void RevealFocus() {
            lock (_gate) { _revealRequested = true; RevealNested(_content); }
        }
        private static void RevealNested(IWidget widget) {
            if (widget is Widgets.ScrollView scroll) { scroll.RevealFocus(); }
            if (widget is IContainer container) { foreach (IWidget child in container.Children) { RevealNested(child); } }
        }
        internal void Reset() { lock (_gate) { ConstrainInputs(Constraints.Unbounded); } }

        private static void ConstrainScrollViews(IWidget content, int height) {
            if (content is Widgets.ScrollView scroll) { scroll.ViewportHeight = height; }
            if (content is IContainer container) { foreach (IWidget child in container.Children) { ConstrainScrollViews(child, height); } }
        }

        private void ConstrainInputs(int height) {
            int contentHeight = height == Constraints.Unbounded ? height : Math.Max(1, height - _reservedRows);
            foreach (InputControl control in _focus.Controls) { control.ViewportHeight = contentHeight; }
            ConstrainScrollViews(_content, contentHeight);
        }
    }
}
