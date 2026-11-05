using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Retained tab subtrees under a strip of labels; only the active subtree is mounted. The strip is a focus stop: ←/→,
    /// Home/End and the number keys switch tabs while it has focus, and Ctrl+PageUp/PageDown switch them from anywhere
    /// inside. A click on a label opens it. The active label is underlined above a rule, and labels scroll on narrow terminals.
    /// </summary>
    public sealed class Tabs : IContainer, IAppInputContainer, IOwnedInput {
        private static int _instances;
        private readonly object _gate = new object();
        private readonly List<LayoutRegion> _tabs = new List<LayoutRegion>();
        private readonly Dictionary<string, InputControl> _lastFocus = new Dictionary<string, InputControl>();
        private List<TabStrip.Header> _headers = new List<TabStrip.Header>();
        private Strip _strip;
        private string? _key;
        private int _selected;

        /// <summary>Creates an empty tab set whose app-state key is generated; give it a <see cref="Key"/> to read or link it.</summary>
        public Tabs() { _strip = NewStrip(null); }

        /// <summary>Creates an empty tab set.</summary>
        /// <param name="key">The app-state key; its value is the active tab's title.</param>
        public Tabs(string key) : this() { Key = key; }

        /// <summary>The app-state key whose value is the active tab's title; null uses a generated key. Set it before the app runs.</summary>
        public string? Key {
            get { lock (_gate) { return _key; } }
            set { lock (_gate) { _key = string.IsNullOrWhiteSpace(value) ? null : value; _strip = NewStrip(_key); } }
        }

        /// <summary>Raised after the active tab changes, by the keyboard, a click or <see cref="SelectedIndex"/>; the value is the tab's title.</summary>
        public event EventHandler<AppValueChangedEventArgs>? Changed;

        private Strip NewStrip(string? key) {
            var strip = new Strip(this, key ?? "tabs#" + Interlocked.Increment(ref _instances).ToString(CultureInfo.InvariantCulture));
            strip.Changed += (sender, change) => Changed?.Invoke(this, change);
            return strip;
        }

        /// <summary>The active tab index.</summary>
        public int SelectedIndex {
            get { lock (_gate) { return _selected; } }
            set { Strip strip; lock (_gate) { strip = _strip; } strip.Commit(() => Select(value, true)); }
        }

        // Keyboard switching from the strip keeps focus there, so ←/→ go on moving through the tabs; a click on a label and
        // a switch from inside a tab move focus into the tab opened. The strip reports the change: its own input does when it
        // handles a key or click, and every other switch goes through Strip.Commit.
        private void Select(int value, bool keepStrip) {
            lock (_gate) {
                if (value < 0 || value >= _tabs.Count) { throw new ArgumentOutOfRangeException(nameof(value)); }
                if (_selected == value) { return; }
                LayoutRegion previous = _tabs[_selected];
                InputControl? focused = FocusManager.Discover(previous).FirstOrDefault(control => control.Focused);
                if (focused != null) { _lastFocus[previous.Key] = focused; }
                _selected = value;
                if (keepStrip && _strip.Focused) { return; }
                LayoutRegion next = _tabs[_selected];
                // A tab opens on its first input that takes values; a breadcrumb or other navigation stop only when it has nothing else.
                InputControl? target = _lastFocus.TryGetValue(next.Key, out InputControl? saved) && !saved.Disabled ? saved
                    : FocusManager.Discover(next).FirstOrDefault(control => !control.Disabled && !control.NavigationOnly) ?? FocusManager.Discover(next).FirstOrDefault(control => !control.Disabled);
                target?.Focus();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => _tabs;

        InputControl IOwnedInput.OwnedInput { get { lock (_gate) { return _strip; } } }

        /// <summary>Adds a tab.</summary>
        /// <param name="title">The tab's title, shown in the strip; titles are unique.</param>
        /// <param name="content">The tab's content.</param>
        /// <returns>This tab set.</returns>
        public Tabs Add(string title, IWidget content) {
            if (string.IsNullOrEmpty(title)) { throw new ArgumentException("A tab needs a title.", nameof(title)); }
            if (_tabs.Exists(tab => tab.Key == title)) { throw new ArgumentException("Tab titles must be unique.", nameof(title)); }
            _tabs.Add(new LayoutRegion(title, content ?? throw new ArgumentNullException(nameof(content)))); return this;
        }

        /// <summary>Adds a tab defined by <see cref="Ui.Tab"/> or a <see cref="TabOptions"/> initializer.</summary>
        /// <param name="tab">The tab's title and content.</param>
        /// <returns>This tab set.</returns>
        public Tabs Add(TabOptions tab) {
            if (tab == null) { throw new ArgumentNullException(nameof(tab)); }
            return Add(tab.Title, tab.Content);
        }

        /// <summary>Adds a tab whose content the builder composes, as Panel and Rows take content.</summary>
        /// <param name="title">The tab's title, shown in the strip; titles are unique.</param>
        /// <param name="content">Adds the tab's widgets in order.</param>
        /// <returns>This tab set.</returns>
        public Tabs Tab(string title, Action<ContentBuilder> content) => Add(title, ContentBuilder.Build(content ?? throw new ArgumentNullException(nameof(content))));
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            Size body = _tabs.Count == 0 ? new Size(0, 0) : _tabs[_selected].Measure(constraints.Shrink(0, TabStrip.Height), context);
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, body.Height + TabStrip.Height));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            lock (_gate) {
            InputArrangement.Record((IAppInputContainer)this);
            if (region.Height <= 0 || region.Width <= 0) { _headers = new List<TabStrip.Header>(); return; }
            // The strip control records its arranged bounds and draws the labels and rule.
            _strip.Render(region.Sub(0, 0, region.Width, Math.Min(TabStrip.Height, region.Height)), context);
            if (_tabs.Count > 0 && region.Height > TabStrip.Height) { _tabs[_selected].Render(region.Sub(0, TabStrip.Height, region.Width, region.Height - TabStrip.Height), context); }
            }
        }

        bool IAppInputContainer.HandleContainerInput(InputEvent input, InputControl? focused) {
            lock (_gate) {
            if (_tabs.Count == 0) { return false; }
            if (input.Kind == InputEventKind.Mouse && input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                foreach (TabStrip.Header header in _headers) {
                    if (header.Contains(input.Column, input.Row)) { int index = header.Index; _strip.Commit(() => Select(index, false)); return true; }
                }
            }
            if (focused != null && !FocusManager.Discover(this).Contains(focused)) { return false; }
            if (input.Is(ConsoleKey.PageDown, KeyModifiers.Control)) { SelectedIndex = (_selected + 1) % _tabs.Count; return true; }
            if (input.Is(ConsoleKey.PageUp, KeyModifiers.Control)) { SelectedIndex = (_selected + _tabs.Count - 1) % _tabs.Count; return true; }
            return false;
            }
        }

        /// <summary>The number keys that open a tab: one through nine, for at most the first nine tabs.</summary>
        internal static int TabForDigit(InputEvent input, int count) {
            if (input.Kind != InputEventKind.Key || input.Modifiers != KeyModifiers.None) { return -1; }
            int digit = input.Char >= '1' && input.Char <= '9' ? input.Char - '1' : -1;
            return digit >= 0 && digit < count ? digit : -1;
        }

        /// <summary>The footer text for the number keys ("1-4").</summary>
        internal static string DigitKeys(int count) => count <= 1 ? "1" : "1-" + Math.Min(9, count).ToString(CultureInfo.InvariantCulture);

        // The strip: a focus stop whose value is the active tab's label.
        private sealed class Strip : InputControl {
            private readonly Tabs _owner;
            // The labels in the strip's own rendering coordinates, which is how the app hands it pointer events.
            private List<TabStrip.Header> _local = new List<TabStrip.Header>();

            internal Strip(Tabs owner, string key) : base(key, false) { _owner = owner; }

            internal override bool NavigationOnly => true;

            // A switch made outside the strip's own input (a click on the container, Ctrl+PageUp/PageDown, SelectedIndex)
            // reports the change through the strip like a key would.
            internal void Commit(Action select) => Change(select);

            protected override object? ReadValueCore() => _owner._tabs.Count == 0 ? null : _owner._tabs[_owner._selected].Key;

            protected override bool HandleInputCore(InputEvent input, RenderContext context) {
                int count = _owner._tabs.Count;
                if (count == 0) { return false; }
                int target = -1;
                if (input.Is(ConsoleKey.RightArrow)) { target = (_owner._selected + 1) % count; }
                else if (input.Is(ConsoleKey.LeftArrow)) { target = (_owner._selected + count - 1) % count; }
                else if (input.Is(ConsoleKey.Home)) { target = 0; }
                else if (input.Is(ConsoleKey.End)) { target = count - 1; }
                else { target = TabForDigit(input, count); }
                if (target < 0) { return false; }
                _owner.Select(target, true);
                return true;
            }

            internal override bool Takes(KeyChord chord, RenderContext context) {
                if (chord.Modifiers != KeyModifiers.None) { return false; }
                if (chord.Key == ConsoleKey.LeftArrow || chord.Key == ConsoleKey.RightArrow || chord.Key == ConsoleKey.Home || chord.Key == ConsoleKey.End) { return true; }
                return chord.Char >= '1' && chord.Char <= '9';
            }

            internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
                PromptLabels labels = PromptLabels.For(context.Profile.Language);
                keys.Add(new KeyBinding(labels.KeyTabs, () => { }, new KeyChord(ConsoleKey.LeftArrow), new KeyChord(ConsoleKey.RightArrow)) { KeyText = InteractiveGlyphs.For(context).KeyLeftRight, Order = 1 });
                if (_owner._tabs.Count > 1) {
                    keys.Add(new KeyBinding(labels.KeyOpenTab, () => { }, new KeyChord('1')) { KeyText = DigitKeys(_owner._tabs.Count), Order = 2 });
                }
            }

            protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
                if (input.MouseAction != MouseAction.Down || input.MouseButton != 1) { return false; }
                foreach (TabStrip.Header header in _local) {
                    if (header.Contains(input.Column, input.Row)) { _owner.Select(header.Index, false); return true; }
                }
                return false;
            }

            protected override Size MeasureContent(Constraints constraints, RenderContext context) =>
                new Size(constraints.MaxWidth, Math.Min(TabStrip.Height, constraints.MaxHeight));

            protected override void RenderContent(Region region, RenderContext context) {
                List<string> labels = _owner._tabs.Select(tab => tab.Key).ToList();
                List<TabStrip.Header> headers = TabStrip.Draw(region, labels, _owner._selected, Focused && Enabled, context);
                _local = headers;
                _owner._headers = headers.Select(header => new TabStrip.Header(header.Index, InputArrangement.FrameBounds(header.Area))).ToList();
            }
        }
    }
}
