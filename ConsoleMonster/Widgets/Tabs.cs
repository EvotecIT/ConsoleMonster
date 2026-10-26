using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Retained tab subtrees. Ctrl+PageUp/PageDown and header clicks switch tabs; only the active subtree is mounted.</summary>
    public sealed class Tabs : IContainer, IAppInputContainer {
        private readonly object _gate = new object();
        private readonly List<LayoutRegion> _tabs = new List<LayoutRegion>();
        private readonly Dictionary<string, InputControl> _lastFocus = new Dictionary<string, InputControl>();
        private readonly List<Rect> _headers = new List<Rect>();
        private int _selected;
        /// <summary>The active tab index.</summary>
        public int SelectedIndex {
            get { lock (_gate) { return _selected; } }
            set {
                lock (_gate) {
                if (value < 0 || value >= _tabs.Count) { throw new ArgumentOutOfRangeException(nameof(value)); }
                if (_selected == value) { return; }
                LayoutRegion previous = _tabs[_selected];
                InputControl? focused = FocusManager.Discover(previous).FirstOrDefault(control => control.Focused);
                if (focused != null) { _lastFocus[previous.Key] = focused; }
                _selected = value;
                LayoutRegion next = _tabs[_selected];
                InputControl? target = _lastFocus.TryGetValue(next.Key, out InputControl? saved) && !saved.Disabled ? saved : FocusManager.Discover(next).FirstOrDefault(control => !control.Disabled);
                target?.Focus();
                }
            }
        }
        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => _tabs;
        /// <summary>Adds a tab; its key is also its visible label.</summary>
        public Tabs Add(string label, IWidget content) {
            if (_tabs.Exists(tab => tab.Key == label)) { throw new ArgumentException("Tab labels must be unique.", nameof(label)); }
            _tabs.Add(new LayoutRegion(label, content)); return this;
        }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            Size body = _tabs.Count == 0 ? new Size(0, 0) : _tabs[_selected].Measure(constraints.Shrink(0, 1), context);
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, body.Height + 1));
        }
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            lock (_gate) {
            _headers.Clear();
            if (region.Height <= 0 || region.Width <= 0) { return; }
            int x = 0;
            for (int i = 0; i < _tabs.Count && x < region.Width; i++) {
                string text = TextWrapper.Truncate(" " + _tabs[i].Key + " ", region.Width - x, context.Ellipsis);
                int width = DisplayWidth.Measure(text);
                region.Write(x, 0, text, context.Style(i == _selected ? ThemeToken.Focus : ThemeToken.Muted));
                _headers.Add(InputArrangement.FrameBounds(new Rect(region.Bounds.X + x, region.Bounds.Y, width, 1))); x += width + 1;
            }
            if (_tabs.Count > 0 && region.Height > 1) { _tabs[_selected].Render(region.Sub(0, 1, region.Width, region.Height - 1), context); }
            }
        }
        bool IAppInputContainer.HandleContainerInput(InputEvent input, InputControl? focused) {
            lock (_gate) {
            if (_tabs.Count == 0) { return false; }
            if (input.Kind == InputEventKind.Mouse && input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                for (int i = 0; i < _headers.Count; i++) {
                    Rect area = _headers[i];
                    if (input.Column >= area.X && input.Column < area.Right && input.Row == area.Y) { SelectedIndex = i; return true; }
                }
            }
            if (focused != null && !FocusManager.Discover(this).Contains(focused)) { return false; }
            if (input.Is(ConsoleKey.PageDown, KeyModifiers.Control)) { SelectedIndex = (_selected + 1) % _tabs.Count; return true; }
            if (input.Is(ConsoleKey.PageUp, KeyModifiers.Control)) { SelectedIndex = (_selected + _tabs.Count - 1) % _tabs.Count; return true; }
            return false;
            }
        }
    }
}
