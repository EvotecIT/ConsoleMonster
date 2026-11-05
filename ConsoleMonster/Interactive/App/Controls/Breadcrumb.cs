using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A path on one row, such as <c>Settings › Network › Proxy</c>: the last segment is where you are, the others lead back
    /// up. It shows its own <see cref="Segments"/>, or follows an <see cref="Outline"/>, showing the headings around the
    /// outline's current entry after any <see cref="Ancestors"/> segments. When the row is too narrow the leading segments give
    /// way to an ellipsis. It is a focus stop that initial focus skips: ←/→ choose a segment and Enter opens it, as a click
    /// does. Opening a segment makes it the value, and opening a followed outline's heading opens that outline entry.
    /// </summary>
    public sealed class Breadcrumb : InputControl {
        private readonly List<string> _segments = new List<string>();
        private readonly List<string> _ancestors = new List<string>();
        private readonly List<Rect> _areas = new List<Rect>();
        private Outline? _outline;
        private int _selected = -1;
        private string? _opened;

        /// <summary>Creates a breadcrumb.</summary>
        /// <param name="key">The application-state key; its value is the last segment opened.</param>
        public Breadcrumb(string key) : base(key, false) { }

        /// <summary>The segments shown when no outline is followed, outermost first.</summary>
        public IList<string> Segments => _segments;
        /// <summary>Segments shown before a followed outline's headings, such as the screen and tab names.</summary>
        public IList<string> Ancestors => _ancestors;

        /// <summary>The outline whose current entry this breadcrumb shows, or null to show <see cref="Segments"/>.</summary>
        public Outline? Outline { get => Read(() => _outline); set => Change(() => _outline = value); }

        /// <inheritdoc />
        protected override object? ReadValueCore() => _opened;

        internal override bool NavigationOnly => true;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            int count = Shown().Count;
            if (count == 0) { return false; }
            int selected = _selected < 0 || _selected >= count ? count - 1 : _selected;
            if (input.Is(ConsoleKey.LeftArrow)) { _selected = Math.Max(0, selected - 1); return true; }
            if (input.Is(ConsoleKey.RightArrow)) { _selected = Math.Min(count - 1, selected + 1); return true; }
            if (input.Is(ConsoleKey.Home)) { _selected = 0; return true; }
            if (input.Is(ConsoleKey.End)) { _selected = count - 1; return true; }
            if (input.Is(ConsoleKey.Enter)) { Open(selected); return true; }
            return false;
        }

        internal override bool Takes(KeyChord chord, RenderContext context) => chord.Modifiers == KeyModifiers.None
            && (chord.Key == ConsoleKey.LeftArrow || chord.Key == ConsoleKey.RightArrow || chord.Key == ConsoleKey.Home || chord.Key == ConsoleKey.End || chord.Key == ConsoleKey.Enter);

        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            PromptLabels labels = PromptLabels.For(context.Profile.Language);
            keys.Add(new KeyBinding(labels.KeyMove, () => { }, new KeyChord(ConsoleKey.LeftArrow), new KeyChord(ConsoleKey.RightArrow)) { KeyText = InteractiveGlyphs.For(context).KeyLeftRight, Order = 1 });
            keys.Add(new KeyBinding(labels.KeyOpen, () => { }, new KeyChord(ConsoleKey.Enter)) { Order = 2 });
        }

        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            if (input.MouseAction != MouseAction.Down || input.MouseButton != 1) { return false; }
            for (int i = 0; i < _areas.Count; i++) {
                Rect area = _areas[i];
                if (area.Width > 0 && input.Column >= area.X && input.Column < area.Right && input.Row >= area.Y && input.Row < area.Bottom) {
                    _selected = i;
                    Open(i);
                    return true;
                }
            }
            return false;
        }

        // Opening a heading of a followed outline opens that outline entry (its jump); a plain segment only becomes the value.
        private void Open(int index) {
            List<Segment> shown = Shown();
            if (index < 0 || index >= shown.Count) { return; }
            _opened = shown[index].Title;
            Outline? outline = _outline;
            int entry = shown[index].Entry;
            if (outline != null && entry >= 0) { outline.Open(entry); }
        }

        private List<Segment> Shown() {
            List<Segment> shown = new List<Segment>();
            if (_outline == null) {
                foreach (string segment in _segments) { shown.Add(new Segment(segment, -1)); }
                return shown;
            }
            foreach (string segment in _ancestors) { shown.Add(new Segment(segment, -1)); }
            IList<OutlineItem> items = _outline.Items;
            int current = _outline.SelectedIndex;
            if (current < 0 || current >= items.Count) { return shown; }
            List<Segment> path = new List<Segment> { new Segment(items[current].Title, current) };
            int level = items[current].Level;
            for (int i = current - 1; i >= 0 && level > 1; i--) {
                if (items[i].Level < level) { path.Insert(0, new Segment(items[i].Title, i)); level = items[i].Level; }
            }
            shown.AddRange(path);
            return shown;
        }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) =>
            new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            _areas.Clear();
            if (region.Width <= 0 || region.Height <= 0) { return; }
            List<Segment> shown = Shown();
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
            string separator = " " + glyphs.Breadcrumb + " ";
            int separatorWidth = DisplayWidth.Measure(separator);
            string[] titles = new string[shown.Count];
            int total = 0;
            for (int i = 0; i < shown.Count; i++) {
                titles[i] = Glyphs.SymbolFallback.Constrain(shown[i].Title, context.Glyphs.Tier);
                total += DisplayWidth.Measure(titles[i]) + (i > 0 ? separatorWidth : 0);
            }
            // Leading segments give way to an ellipsis until the rest fits; the last one is truncated only on its own.
            int first = 0;
            int ellipsis = DisplayWidth.Measure(context.Ellipsis) + separatorWidth;
            while (first < shown.Count - 1 && total + (first > 0 ? ellipsis : 0) > region.Width) {
                total -= DisplayWidth.Measure(titles[first]) + separatorWidth;
                first++;
            }
            bool focused = Focused && Enabled;
            int selected = focused ? (_selected < 0 || _selected >= shown.Count ? shown.Count - 1 : _selected) : -1;
            Style muted = context.Style(ThemeToken.Muted);
            int x = 0;
            for (int i = 0; i < first; i++) { _areas.Add(new Rect(0, 0, 0, 0)); }
            if (first > 0) {
                x = region.Write(x, 0, context.Ellipsis, muted);
                x = region.Write(x, 0, separator, context.Style(ThemeToken.Subtle));
            }
            for (int i = first; i < shown.Count && x < region.Width; i++) {
                if (i > first) { x = region.Write(x, 0, separator, context.Style(ThemeToken.Subtle)); }
                bool last = i == shown.Count - 1;
                Style style = i == selected ? context.Style(ThemeToken.Focus).With(TextDecoration.Bold | TextDecoration.Underline)
                    : last ? context.Style(ThemeToken.Accent).With(TextDecoration.Bold) : muted;
                string text = TextWrapper.Truncate(titles[i], Math.Max(0, region.Width - x), context.Ellipsis);
                int start = x;
                x = region.Write(x, 0, text, style);
                _areas.Add(new Rect(region.Bounds.X + start, region.Bounds.Y, x - start, 1));
            }
        }

        private readonly struct Segment {
            internal Segment(string title, int entry) { Title = title; Entry = entry; }
            internal string Title { get; }
            internal int Entry { get; }
        }
    }
}
