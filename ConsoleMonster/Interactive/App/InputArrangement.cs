using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>One arrangement snapshot maps clipped hit rectangles back to each control's rendering coordinates.</summary>
    internal sealed class InputArrangement {
        [ThreadStatic] private static Scope? _current;
        private readonly Dictionary<InputControl, Entry> _inputs = new Dictionary<InputControl, Entry>();
        private readonly List<Tuple<Widgets.ScrollView, Rect>> _scrolls = new List<Tuple<Widgets.ScrollView, Rect>>();
        private readonly List<IAppInputContainer> _containers = new List<IAppInputContainer>();
        private readonly List<Popup> _popups = new List<Popup>();
        internal InputControl? Focused { get; set; }
        internal IEnumerable<InputControl> Mounted => _inputs.Keys;
        internal IReadOnlyList<Popup> Popups => _popups;
        internal Dictionary<InputControl, int> Heights() {
            Dictionary<InputControl, int> heights = new Dictionary<InputControl, int>();
            foreach (KeyValuePair<InputControl, Entry> input in _inputs) { heights[input.Key] = input.Value.Bounds.Height; }
            return heights;
        }
        internal Rect? FocusBounds => Focused != null && _inputs.TryGetValue(Focused, out Entry? entry) ? entry.Bounds : (Rect?)null;
        internal IDisposable Begin(int x, int y, Rect clip) { _inputs.Clear(); _scrolls.Clear(); _containers.Clear(); _popups.Clear(); return new Scope(this, x, y, clip); }
        // A control asks for content drawn above the frame, anchored to where it was arranged; the app overlay paints it
        // after the content pass. Outside an app (a one-off render) there is no layer and the request is dropped.
        internal static void RecordPopup(InputControl owner, IWidget popup) {
            Scope? scope = _current;
            if (scope == null || !scope.Owner._inputs.TryGetValue(owner, out Entry? entry)) { return; }
            Rect anchor = entry.Bounds.Intersect(entry.Clip);
            if (anchor.Width > 0 && anchor.Height > 0) { scope.Owner._popups.Add(new Popup(owner, popup, anchor)); }
        }
        internal static void Record(IAppInputContainer container) => _current?.Owner._containers.Add(container);
        internal bool HandleContainerInput(InputEvent input, InputControl? focused) {
            for (int i = _containers.Count - 1; i >= 0; i--) { if (_containers[i].HandleContainerInput(input, focused)) { return true; } }
            return false;
        }
        internal static IDisposable? Translate(int x, int y, Rect viewport) {
            Scope? parent = _current;
            return parent == null ? null : new Scope(parent.Owner, parent.X + x, parent.Y + y,
                Move(viewport, parent.X, parent.Y).Intersect(parent.Clip));
        }
        internal static InputControl? CurrentFocus => _current?.Owner.Focused;
        internal static Rect FrameBounds(Rect bounds) => _current == null ? bounds : Move(bounds, _current.X, _current.Y).Intersect(_current.Clip);
        internal static Rect? FocusWithin(IWidget content) {
            Scope? scope = _current;
            if (scope == null || scope.Owner.Focused == null) { return null; }
            foreach (InputControl control in FocusManager.Discover(content)) {
                if (control == scope.Owner.Focused && scope.Owner._inputs.TryGetValue(control, out Entry? entry)) { return Move(entry.Bounds, -scope.X, -scope.Y); }
            }
            return null;
        }
        internal static void Record(InputControl input, Rect bounds) {
            Scope? scope = _current;
            if (scope != null) { scope.Owner._inputs[input] = new Entry(Move(bounds, scope.X, scope.Y), scope.Clip, scope.X, scope.Y); }
        }
        internal static void Record(Widgets.ScrollView scroll, Rect bounds) {
            Scope? scope = _current;
            if (scope != null) { scope.Owner._scrolls.Add(Tuple.Create(scroll, Move(bounds, scope.X, scope.Y).Intersect(scope.Clip))); }
        }
        internal bool FocusAt(FocusManager focus, int x, int y) {
            foreach (InputControl input in focus.Controls) {
                if (input.Enabled && _inputs.TryGetValue(input, out Entry? entry) && Contains(entry.Bounds.Intersect(entry.Clip), x, y)) { focus.Focus(input); return true; }
            }
            return false;
        }
        internal InputEvent Translate(InputControl input, InputEvent pointer) => _inputs.TryGetValue(input, out Entry? entry)
            ? pointer.WithPosition(pointer.Column - entry.X, pointer.Row - entry.Y) : pointer;
        internal bool ScrollAt(InputEvent pointer) {
            for (int i = _scrolls.Count - 1; i >= 0; i--) {
                if (Contains(_scrolls[i].Item2, pointer.Column, pointer.Row)) { _scrolls[i].Item1.ScrollBy(-Math.Sign(pointer.WheelDelta) * 3); return true; }
            }
            return false;
        }
        internal bool ScrollBy(int rows) {
            if (_scrolls.Count == 0) { return false; }
            _scrolls[_scrolls.Count - 1].Item1.ScrollBy(rows); return true;
        }
        private static Rect Move(Rect bounds, int x, int y) => new Rect(bounds.X + x, bounds.Y + y, bounds.Width, bounds.Height);
        private static bool Contains(Rect area, int x, int y) => x >= area.X && x < area.Right && y >= area.Y && y < area.Bottom;
        internal sealed class Popup {
            internal Popup(InputControl owner, IWidget content, Rect anchor) { Owner = owner; Content = content; Anchor = anchor; }
            internal InputControl Owner { get; } internal IWidget Content { get; } internal Rect Anchor { get; }
        }
        private sealed class Entry {
            internal Entry(Rect bounds, Rect clip, int x, int y) { Bounds = bounds; Clip = clip; X = x; Y = y; }
            internal Rect Bounds { get; } internal Rect Clip { get; } internal int X { get; } internal int Y { get; }
        }
        private sealed class Scope : IDisposable {
            private readonly Scope? _previous;
            internal Scope(InputArrangement owner, int x, int y, Rect clip) { Owner = owner; X = x; Y = y; Clip = clip; _previous = _current; _current = this; }
            internal InputArrangement Owner { get; } internal int X { get; } internal int Y { get; } internal Rect Clip { get; }
            public void Dispose() => _current = _previous;
        }
    }
}
