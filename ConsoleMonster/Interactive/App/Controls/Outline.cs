using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>One entry of an <see cref="Outline"/>: a title at a nesting level, and optionally the control it leads to.</summary>
    public sealed class OutlineItem {
        /// <summary>Creates an entry.</summary>
        /// <param name="title">The visible title.</param>
        /// <param name="level">The nesting level, one for the top; deeper entries are indented.</param>
        public OutlineItem(string title, int level = 1) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            if (level < 1) { throw new ArgumentOutOfRangeException(nameof(level)); }
            Level = level;
        }

        /// <summary>The visible title.</summary>
        public string Title { get; }

        /// <summary>The nesting level, one for the top.</summary>
        public int Level { get; }

        /// <summary>
        /// The control the entry leads to, or null. Opening the entry focuses it, which scrolls a surrounding scroll view to
        /// it, and unfolds it when it is a <see cref="Collapsible"/>.
        /// </summary>
        public InputControl? Target { get; set; }
    }

    /// <summary>
    /// A list of headings to jump between: a table of contents, settings groups, the sections of a long form. The current
    /// entry (where the reader is) is marked and can follow a view as it scrolls; ↑/↓, Home/End and the wheel move the
    /// selection, and Enter or a click opens it. Opening makes the entry current, which is the control's value (so
    /// <see cref="InputControl.Changed"/> and OnChange report it), and leads to its <see cref="OutlineItem.Target"/> when it has one.
    /// </summary>
    public sealed class Outline : InputControl {
        private readonly List<OutlineItem> _items = new List<OutlineItem>();
        private int _current = -1;
        private int _selected = -1;
        private int _top;
        private int _rows = 1;
        // The entry the view last scrolled to keep in sight, at that height; the view follows only when either changes, so
        // the wheel can scroll the list away from it.
        private int _anchor = -2;
        private int _anchorRows = -1;

        /// <summary>Creates an outline.</summary>
        /// <param name="key">The application-state key; its value is the current entry's title.</param>
        public Outline(string key) : base(key, false) { }

        /// <summary>The entries, in order. Configure them before the app runs.</summary>
        public IList<OutlineItem> Items => _items;
        // Replaces the entries while running (the document viewer switching parts). The same list rebuilt (a theme change)
        // keeps its current entry, selection and scroll position; a different list starts with none.
        internal void SetItems(IEnumerable<OutlineItem> items, bool sameList = false) {
            if (items == null) { throw new ArgumentNullException(nameof(items)); }
            Change(() => {
                int count = _items.Count;
                _items.Clear();
                _items.AddRange(items);
                if (sameList && count == _items.Count) { return; }
                _current = -1; _selected = -1; _top = 0; _anchor = -2;
            });
        }

        /// <summary>
        /// The index of the entry the reader is on, or -1. A view that scrolls sets it to follow the reading position; the
        /// selection follows it unless the outline has focus.
        /// </summary>
        public int SelectedIndex {
            get => Read(() => _current);
            set => Change(() => {
                _current = value >= 0 && value < _items.Count ? value : -1;
                if (!Focused || _selected < 0) { _selected = _current; }
            });
        }

        // The selected entry, which Enter opens, or -1.
        internal int HighlightedIndex {
            get => Read(() => _selected);
            set => Change(() => _selected = value >= 0 && value < _items.Count ? value : -1);
        }

        // Raised when an entry is opened, also when it was already current (the document viewer jumps back to it).
        internal event EventHandler<OutlineItem>? Opened;

        /// <inheritdoc />
        protected override object? ReadValueCore() => _current >= 0 ? _items[_current].Title : null;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (_items.Count == 0) { return false; }
            if (input.Is(ConsoleKey.UpArrow)) { return Select(Math.Max(0, _selected - 1)); }
            if (input.Is(ConsoleKey.DownArrow)) { return Select(Math.Min(_items.Count - 1, _selected + 1)); }
            if (input.Is(ConsoleKey.PageUp)) { return Select(Math.Max(0, _selected - Math.Max(1, _rows - 1))); }
            if (input.Is(ConsoleKey.PageDown)) { return Select(Math.Min(_items.Count - 1, _selected + Math.Max(1, _rows - 1))); }
            if (input.Is(ConsoleKey.Home)) { return Select(0); }
            if (input.Is(ConsoleKey.End)) { return Select(_items.Count - 1); }
            if (input.Is(ConsoleKey.Enter) && _selected >= 0) { OpenSelected(); return true; }
            return false;
        }

        internal override bool Takes(KeyChord chord, RenderContext context) => chord.Modifiers == KeyModifiers.None
            && (chord.Key == ConsoleKey.UpArrow || chord.Key == ConsoleKey.DownArrow || chord.Key == ConsoleKey.PageUp || chord.Key == ConsoleKey.PageDown
                || chord.Key == ConsoleKey.Home || chord.Key == ConsoleKey.End || chord.Key == ConsoleKey.Enter);

        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            PromptLabels labels = PromptLabels.For(context.Profile.Language);
            keys.Add(new KeyBinding(labels.KeyMove, () => { }, new KeyChord(ConsoleKey.UpArrow), new KeyChord(ConsoleKey.DownArrow)) { KeyText = InteractiveGlyphs.For(context).KeyUpDown, Order = 1 });
            keys.Add(new KeyBinding(labels.KeyOpen, () => { }, new KeyChord(ConsoleKey.Enter)) { Order = 2 });
        }

        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            Rect bounds = Bounds;
            bool inside = input.Column >= bounds.X && input.Column < bounds.Right && input.Row >= bounds.Y && input.Row < bounds.Bottom;
            if (!inside) { return false; }
            if (input.MouseAction == MouseAction.Wheel && input.WheelDelta != 0) {
                _top = Math.Max(0, Math.Min(Math.Max(0, _items.Count - _rows), _top - Math.Sign(input.WheelDelta) * 3));
                return true;
            }
            if (input.MouseAction != MouseAction.Down || input.MouseButton != 1) { return false; }
            int index = _top + input.Row - bounds.Y;
            // A click takes focus, as the keyboard has it after Tab, so ↑/↓ and Enter go on from the entry clicked.
            Focus();
            if (index < 0 || index >= _items.Count) { return true; }
            _selected = index;
            OpenSelected();
            return true;
        }

        /// <summary>Opens an entry as Enter on it would (a breadcrumb opening one of the outline's headings).</summary>
        internal void Open(int index) {
            Change(() => {
                if (index < 0 || index >= _items.Count) { return; }
                _selected = index;
                OpenSelected();
            });
        }

        private bool Select(int index) {
            _selected = index;
            return true;
        }

        // Runs inside the control's edit: the value changes, then the target is focused and unfolded after the edit.
        private void OpenSelected() {
            _current = _selected;
            OutlineItem item = _items[_selected];
            Opened?.Invoke(this, item);
            InputControl? target = item.Target;
            if (target is Collapsible section) { section.Expanded = true; }
            target?.Focus();
        }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) {
            int count = Read(() => _items.Count);
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Math.Max(1, count)));
        }

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) { return; }
            _rows = region.Height;
            int anchor = Focused ? _selected : _current;
            if (anchor >= 0 && (anchor != _anchor || region.Height != _anchorRows)) {
                if (anchor < _top) { _top = anchor; }
                if (anchor >= _top + region.Height) { _top = anchor - region.Height + 1; }
            }
            _anchor = anchor;
            _anchorRows = region.Height;
            _top = Math.Max(0, Math.Min(_top, Math.Max(0, _items.Count - region.Height)));
            int minimum = int.MaxValue;
            foreach (OutlineItem item in _items) { minimum = Math.Min(minimum, item.Level); }
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
            bool focused = Focused && Enabled;
            for (int y = 0; y < region.Height && _top + y < _items.Count; y++) {
                int index = _top + y;
                OutlineItem item = _items[index];
                bool selected = focused && index == _selected;
                bool current = index == _current;
                int indent = 2 + (item.Level - minimum) * 2;
                Style style = selected ? context.Style(ThemeToken.Focus).With(TextDecoration.Bold)
                    : current ? context.Style(ThemeToken.Accent).With(TextDecoration.Bold)
                    : context.Style(item.Level == minimum ? ThemeToken.Text : ThemeToken.Muted);
                if (selected) { region.Write(0, y, glyphs.Pointer, context.Style(ThemeToken.Focus)); }
                else if (current) { region.Write(0, y, glyphs.FocusBar, context.Style(ThemeToken.Accent)); }
                if (indent < region.Width) {
                    string title = Glyphs.SymbolFallback.Constrain(item.Title, context.Glyphs.Tier);
                    region.Write(indent, y, TextWrapper.Truncate(title, region.Width - indent, context.Ellipsis), style);
                }
            }
        }
    }
}
