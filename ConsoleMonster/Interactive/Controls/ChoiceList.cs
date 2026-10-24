using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The list behind every selection: fuzzy filtering with highlighted matches, groups (tri-state and collapsible in
    /// multi-selection), hotkeys, badges, disabled choices, paging with a scroll bar, select all/none/invert, a minimum
    /// and maximum, and a selected-only view. Draws only the rows; prompts and panes add the filter box, counts and preview.
    /// </summary>
    internal sealed class ChoiceList : IWidget {
        private readonly List<Choice> _choices;
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly HashSet<string> _collapsed = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<string> _groups = new List<string>();
        private int _focus;
        private int _scroll;
        private int _viewport = 10;
        private bool _dirty = true;
        private bool _focusPlaced;

        public ChoiceList(IEnumerable<Choice> choices, bool multi) {
            _choices = new List<Choice>(choices ?? throw new ArgumentNullException(nameof(choices)));
            Multi = multi;
            for (int i = 0; i < _choices.Count; i++) {
                Choice c = _choices[i];
                if (multi && c.Selected && c.IsEnabled) {
                    _selected.Add(i);
                }
                if (c.Hotkey.HasValue) {
                    HasHotkeys = true;
                }
                if (!string.IsNullOrEmpty(c.Group) && !_groups.Contains(c.Group!)) {
                    _groups.Add(c.Group!);
                }
            }
            Filter = new LineEditor();
        }

        public bool Multi { get; }

        public bool HasHotkeys { get; }

        public bool HasGroups => _groups.Count > 0;

        public LineEditor Filter { get; }

        /// <summary>With hotkeys, plain letters pick choices until '/' starts filtering.</summary>
        public bool FilterMode { get; set; }

        public bool SelectedOnly { get; private set; }

        public int? Minimum { get; set; }

        public int? Maximum { get; set; }

        /// <summary>Whether the list draws a focus bar (false while another pane has focus).</summary>
        public bool Focused { get; set; } = true;

        /// <summary>The widest a row gets: lists do not stretch across wide panes (badges stay near their titles).</summary>
        public int MaxRowWidth { get; set; } = 84;

        /// <summary>The narrowest a row gets when titles are short.</summary>
        public int MinRowWidth { get; set; } = 40;

        /// <summary>Whether rows always span the available width (dialogs whose description line uses the full width).</summary>
        public bool FillWidth { get; set; }

        /// <summary>
        /// The width rows need: the widest title with its marks and badge, between <see cref="MinRowWidth"/> and
        /// <see cref="MaxRowWidth"/>, so a short list stays compact instead of spreading badges across the pane.
        /// </summary>
        public int RowWidth(RenderContext context, int available) {
            if (FillWidth) {
                return available;
            }
            int widest = 0;
            foreach (Choice c in _choices) {
                int badge = c.Badge == null ? 0 : DisplayWidth.Measure(c.Badge.PlainText) + 3;
                int group = string.IsNullOrEmpty(c.Group) ? 0 : DisplayWidth.Measure(c.Group) + 4;
                widest = Math.Max(widest, DisplayWidth.Measure(c.Label) + badge);
                widest = Math.Max(widest, group + 6);
            }
            int marks = 2 + (HasHotkeys ? 3 : 0) + (Multi ? 4 + (HasGroups ? 2 : 0) : 0) + 4;
            return Math.Min(available, Math.Max(Math.Min(MinRowWidth, available), Math.Min(MaxRowWidth, widest + marks)));
        }

        /// <summary>Whether the focused row stays marked (bold, with a muted bar) while the list does not have focus.</summary>
        public bool ShowInactiveFocus { get; set; }

        /// <summary>A transient message for the line under the list (limits, disabled reasons).</summary>
        public string? Message { get; set; }

        public PromptLabels Labels { get; set; } = PromptLabels.Default;

        public IReadOnlyList<Choice> Choices => _choices;

        public int Total {
            get {
                int count = 0;
                foreach (Choice c in _choices) {
                    if (c.IsEnabled) {
                        count++;
                    }
                }
                return count;
            }
        }

        public int SelectedCount => _selected.Count;

        public int VisibleItemCount {
            get {
                EnsureRows();
                int count = 0;
                foreach (Row r in _rows) {
                    if (r.Kind == RowKind.Item) {
                        count++;
                    }
                }
                return count;
            }
        }

        public bool IsFiltering => Filter.Text.Trim().Length > 0;

        public Choice? FocusedChoice {
            get {
                EnsureRows();
                if (_focus < 0 || _focus >= _rows.Count || _rows[_focus].Kind != RowKind.Item) {
                    return null;
                }
                return _choices[_rows[_focus].Choice];
            }
        }

        public string? FocusedGroup {
            get {
                EnsureRows();
                return _focus >= 0 && _focus < _rows.Count && _rows[_focus].Kind == RowKind.Group ? _rows[_focus].Group : null;
            }
        }

        public List<Choice> SelectedChoices {
            get {
                List<Choice> list = new List<Choice>();
                for (int i = 0; i < _choices.Count; i++) {
                    if (_selected.Contains(i)) {
                        list.Add(_choices[i]);
                    }
                }
                return list;
            }
        }

        public bool IsSelected(Choice choice) {
            int index = _choices.IndexOf(choice);
            return index >= 0 && _selected.Contains(index);
        }

        public void SetSelected(Choice choice, bool selected) {
            int index = _choices.IndexOf(choice);
            if (index < 0 || !choice.IsEnabled) {
                return;
            }
            if (selected) {
                _selected.Add(index);
            } else {
                _selected.Remove(index);
            }
            _dirty = true;
        }

        /// <summary>Moves the focus to a choice (for defaults and remembered values).</summary>
        public void FocusChoice(Choice? choice) {
            if (choice == null) {
                return;
            }
            EnsureRows();
            _focusPlaced = true;
            int index = _choices.IndexOf(choice);
            for (int r = 0; r < _rows.Count; r++) {
                if (_rows[r].Kind == RowKind.Item && _rows[r].Choice == index) {
                    _focus = r;
                    EnsureVisible();
                    return;
                }
            }
        }

        public void Invalidate() => _dirty = true;

        /// <summary>Replaces the choices (async loading); keeps selection of equal values.</summary>
        public void Replace(IEnumerable<Choice> choices) {
            List<object?> keep = new List<object?>();
            foreach (Choice c in SelectedChoices) {
                keep.Add(c.Value);
            }
            _choices.Clear();
            _choices.AddRange(choices);
            _selected.Clear();
            _groups.Clear();
            for (int i = 0; i < _choices.Count; i++) {
                Choice c = _choices[i];
                if (c.IsEnabled && (c.Selected || keep.Contains(c.Value))) {
                    if (Multi) {
                        _selected.Add(i);
                    }
                }
                if (!string.IsNullOrEmpty(c.Group) && !_groups.Contains(c.Group!)) {
                    _groups.Add(c.Group!);
                }
            }
            _focus = 0;
            _scroll = 0;
            _dirty = true;
        }

        /// <summary>Adds the list's key bindings: movement, paging, toggles, groups and selection helpers.</summary>
        public void AddBindings(List<KeyBinding> bindings, RenderContext context) {
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            bindings.Add(new KeyBinding(Labels.KeyMove, () => { }) { KeyText = g.KeyUpDown, Order = 10, Group = "List" });
            bindings.Add(new KeyBinding(string.Empty, () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { InFooter = false, Group = "List" });
            bindings.Add(new KeyBinding(string.Empty, () => Move(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false, Group = "List" });
            bindings.Add(new KeyBinding(Labels.KeyPage, () => Page(1), new KeyChord(ConsoleKey.PageDown)) { InFooter = false, Group = "List", KeyText = "pgup/pgdn" });
            bindings.Add(new KeyBinding(string.Empty, () => Page(-1), new KeyChord(ConsoleKey.PageUp)) { InFooter = false, Group = "List" });
            bindings.Add(new KeyBinding(string.Empty, () => Edge(false), new KeyChord(ConsoleKey.Home)) { InFooter = false, Group = "List" });
            bindings.Add(new KeyBinding(string.Empty, () => Edge(true), new KeyChord(ConsoleKey.End)) { InFooter = false, Group = "List" });
            if (Multi) {
                bindings.Add(new KeyBinding(Labels.KeyToggle, ToggleFocused, new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "space", Order = 20, Group = "Selection" });
                bindings.Add(new KeyBinding(Labels.KeyAll, SelectAll, new KeyChord(ConsoleKey.A, KeyModifiers.Control)) { Order = 60, Group = "Selection" });
                bindings.Add(new KeyBinding(Labels.KeyNone, SelectNone, new KeyChord(ConsoleKey.N, KeyModifiers.Control)) { Order = 61, Group = "Selection" });
                bindings.Add(new KeyBinding(Labels.KeyInvert, Invert, new KeyChord(ConsoleKey.T, KeyModifiers.Control)) { Order = 62, Group = "Selection", InFooter = false });
                bindings.Add(new KeyBinding(SelectedOnly ? Labels.TypeToFilter : Labels.KeySelectedOnly, ToggleSelectedOnly, new KeyChord(ConsoleKey.O, KeyModifiers.Control)) { Order = 63, Group = "Selection", InFooter = false });
            }
            if (HasGroups && !IsFiltering && FocusedGroup != null) {
                bindings.Add(new KeyBinding(Labels.KeyExpand, () => SetCollapsed(FocusedGroup, !_collapsed.Contains(FocusedGroup!)), new KeyChord(ConsoleKey.RightArrow), new KeyChord(ConsoleKey.LeftArrow)) { KeyText = g.KeyLeftRight, Order = 30, Group = "List" });
            }
            if (HasHotkeys && !FilterMode) {
                bindings.Add(new KeyBinding(Labels.KeyFilter, () => FilterMode = true, new KeyChord('/')) { Order = 40, Group = "List" });
            }
        }

        /// <summary>Typing: hotkeys (returns the picked choice through <paramref name="picked"/>) or filter text.</summary>
        public bool HandleText(InputEvent e, out Choice? picked) {
            picked = null;
            if (HasHotkeys && !FilterMode && e.IsText) {
                for (int i = 0; i < _choices.Count; i++) {
                    Choice c = _choices[i];
                    if (c.Hotkey.HasValue && char.ToLowerInvariant(c.Hotkey.Value) == char.ToLowerInvariant(e.Char)) {
                        FocusChoice(c);
                        if (c.IsEnabled) {
                            picked = c;
                        } else {
                            Message = PromptLabels.Format(Labels.Disabled, c.DisabledReason);
                        }
                        return true;
                    }
                }
                return false;
            }
            if (HasHotkeys && !FilterMode) {
                return false;
            }
            if (Multi && e.IsChar(' ')) {
                return false;
            }
            if (e.Kind == InputEventKind.Key && (e.Key == ConsoleKey.LeftArrow || e.Key == ConsoleKey.RightArrow || e.Key == ConsoleKey.Home || e.Key == ConsoleKey.End)) {
                return false;
            }
            string before = Filter.Text;
            if (Filter.Handle(e)) {
                if (!string.Equals(before, Filter.Text, StringComparison.Ordinal)) {
                    _dirty = true;
                    _focus = 0;
                    _scroll = 0;
                    Message = null;
                }
                return true;
            }
            return false;
        }

        /// <summary>Clears the filter; returns false when it was already empty.</summary>
        public bool ClearFilter() {
            if (Filter.IsEmpty && !FilterMode) {
                return false;
            }
            Filter.Text = string.Empty;
            FilterMode = false;
            _dirty = true;
            return true;
        }

        public void Move(int delta) {
            EnsureRows();
            if (_rows.Count == 0) {
                return;
            }
            int start = _focus;
            int index = _focus;
            for (int step = 0; step < _rows.Count; step++) {
                index += delta;
                if (index < 0) {
                    index = _rows.Count - 1;
                } else if (index >= _rows.Count) {
                    index = 0;
                }
                if (IsFocusable(_rows[index])) {
                    _focus = index;
                    break;
                }
            }
            if (_focus != start) {
                Message = null;
            }
            EnsureVisible();
        }

        public void Page(int direction) {
            EnsureRows();
            int target = Math.Max(0, Math.Min(_rows.Count - 1, _focus + direction * Math.Max(1, _viewport - 1)));
            _focus = Nearest(target, direction);
            EnsureVisible();
        }

        public void Edge(bool end) {
            EnsureRows();
            _focus = Nearest(end ? _rows.Count - 1 : 0, end ? -1 : 1);
            EnsureVisible();
        }

        public void ToggleFocused() {
            EnsureRows();
            if (_focus < 0 || _focus >= _rows.Count) {
                return;
            }
            Row row = _rows[_focus];
            if (row.Kind == RowKind.Group) {
                ToggleGroup(row.Group!);
                return;
            }
            if (row.Kind != RowKind.Item) {
                return;
            }
            Choice c = _choices[row.Choice];
            if (!c.IsEnabled) {
                Message = PromptLabels.Format(Labels.Disabled, c.DisabledReason);
                return;
            }
            if (_selected.Contains(row.Choice)) {
                _selected.Remove(row.Choice);
                Message = null;
            } else if (CanSelect(1)) {
                _selected.Add(row.Choice);
                Message = null;
            }
            _dirty = true;
        }

        public void SelectAll() {
            List<int> shown = ShownItems();
            int adding = 0;
            foreach (int i in shown) {
                if (!_selected.Contains(i) && _choices[i].IsEnabled) {
                    adding++;
                }
            }
            if (!CanSelect(adding)) {
                return;
            }
            foreach (int i in shown) {
                if (_choices[i].IsEnabled) {
                    _selected.Add(i);
                }
            }
            Message = null;
            _dirty = true;
        }

        public void SelectNone() {
            foreach (int i in ShownItems()) {
                _selected.Remove(i);
            }
            Message = null;
            _dirty = true;
        }

        public void Invert() {
            List<int> shown = ShownItems();
            int after = _selected.Count;
            foreach (int i in shown) {
                if (!_choices[i].IsEnabled) {
                    continue;
                }
                after += _selected.Contains(i) ? -1 : 1;
            }
            if (Maximum.HasValue && after > Maximum.Value) {
                Message = PromptLabels.Format(Labels.MaximumSelected, Maximum.Value);
                return;
            }
            foreach (int i in shown) {
                if (!_choices[i].IsEnabled) {
                    continue;
                }
                if (!_selected.Remove(i)) {
                    _selected.Add(i);
                }
            }
            Message = null;
            _dirty = true;
        }

        public void ToggleSelectedOnly() {
            SelectedOnly = !SelectedOnly;
            _dirty = true;
            _focus = 0;
            _scroll = 0;
        }

        public void SetCollapsed(string? group, bool collapsed) {
            if (group == null) {
                return;
            }
            if (collapsed) {
                _collapsed.Add(group);
            } else {
                _collapsed.Remove(group);
            }
            _dirty = true;
        }

        /// <summary>Checks the selection against the minimum and maximum; returns the problem or null.</summary>
        public string? ValidateSelection() {
            if (Minimum.HasValue && _selected.Count < Minimum.Value) {
                return PromptLabels.Format(Labels.MinimumSelected, Minimum.Value);
            }
            if (Maximum.HasValue && _selected.Count > Maximum.Value) {
                return PromptLabels.Format(Labels.MaximumSelected, Maximum.Value);
            }
            return null;
        }

        /// <summary>Rows the list would like to show (for inline prompts that size to content).</summary>
        public int DesiredRows(int pageSize) {
            EnsureRows();
            return Math.Max(1, Math.Min(pageSize, _rows.Count));
        }

        public Size Measure(Constraints constraints, RenderContext context) {
            EnsureRows();
            int rows = Math.Max(1, _rows.Count);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            EnsureRows();
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            _viewport = region.Height;
            EnsureVisible();
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            if (_rows.Count == 1 && _rows[0].Kind == RowKind.Empty) {
                region.Write(2, 0, Labels.NoMatches, context.Style(ThemeToken.Muted).With(TextDecoration.Italic));
                return;
            }
            bool scrollbar = _rows.Count > region.Height;
            int usable = Math.Min(region.Width, Math.Max(20, RowWidth(context, region.Width)));
            int width = scrollbar ? usable - 1 : usable;
            for (int y = 0; y < region.Height; y++) {
                int index = _scroll + y;
                if (index >= _rows.Count) {
                    break;
                }
                DrawRow(region.Sub(0, y, width, 1), _rows[index], index == _focus && Focused, context, g, index == _focus && !Focused && ShowInactiveFocus);
            }
            if (scrollbar) {
                DrawScrollbar(region, context, g);
            }
        }

        private void DrawScrollbar(Region region, RenderContext context, InteractiveGlyphs g) {
            int height = region.Height;
            int total = _rows.Count;
            int thumb = Math.Max(1, height * height / total);
            int range = Math.Max(1, total - height);
            int top = (int)Math.Round((height - thumb) * (_scroll / (double)range));
            int x = Math.Min(region.Width, Math.Max(20, RowWidth(context, region.Width))) - 1;
            for (int y = 0; y < height; y++) {
                bool onThumb = y >= top && y < top + thumb;
                region.Put(x, y, onThumb ? g.ScrollThumb : g.ScrollTrack, context.Style(onThumb ? ThemeToken.Muted : ThemeToken.Subtle));
            }
        }

        private void DrawRow(Region row, Row data, bool focused, RenderContext context, InteractiveGlyphs g, bool marked = false) {
            bool brackets = g.BracketFocus || !context.HasColor;
            Style focus = context.HasColor ? context.Style(ThemeToken.Focus) : Style.Plain;
            Style baseStyle = focused ? focus : Style.Plain;
            if (focused && context.HasColor) {
                row.Fill(0, 0, row.Width, 1, " ", focus);
            }
            int x = 0;
            if (focused) {
                Style bar = context.Style(ThemeToken.Accent);
                if (context.HasColor) {
                    bar = bar.WithBackgroundColor(focus.BackgroundColor);
                }
                row.Put(0, 0, g.FocusBar, bar);
            }
            if (marked) {
                row.Put(0, 0, g.FocusBar, context.Style(ThemeToken.Muted));
            }
            x = 2;
            if (data.Kind == RowKind.Group) {
                DrawGroup(row, data, focused, context, g, baseStyle, ref x);
                return;
            }
            Choice c = _choices[data.Choice];
            bool enabled = c.IsEnabled;
            if (HasHotkeys) {
                string key = c.Hotkey.HasValue ? char.ToLowerInvariant(c.Hotkey.Value).ToString() : " ";
                row.Write(x, 0, key, (focused ? baseStyle : Style.Plain).Combine(context.Style(enabled ? ThemeToken.Accent : ThemeToken.Subtle)));
                x += 3;
            }
            if (Multi) {
                if (HasGroups && !IsFiltering) {
                    x += 2;
                }
                bool on = _selected.Contains(data.Choice);
                string box = on ? g.Checked : g.Unchecked;
                Style boxStyle = context.Style(!enabled ? ThemeToken.Subtle : on ? ThemeToken.Accent : ThemeToken.Muted);
                x = row.Write(x, 0, box, baseStyle.Combine(boxStyle)) + 1;
            }
            RichText? badge = c.Badge;
            int badgeWidth = 0;
            StyledLine? badgeLine = null;
            if (badge != null) {
                badgeLine = badge.ToStyledText(context).Lines.Count > 0 ? badge.ToStyledText(context).Lines[0] : null;
                if (badgeLine != null) {
                    badgeWidth = badgeLine.Width;
                }
            }
            string? groupSuffix = IsFiltering && !string.IsNullOrEmpty(c.Group) ? c.Group : null;
            int right = row.Width - 1 - (badgeWidth > 0 ? badgeWidth + 2 : 0);
            Style titleStyle = baseStyle.Combine(context.Style(enabled ? ThemeToken.Text : ThemeToken.Muted));
            if (focused || marked) {
                titleStyle = titleStyle.With(TextDecoration.Bold);
            }
            List<TextSpan> spans = new List<TextSpan>();
            if (focused && brackets) {
                spans.Add(new TextSpan("[", titleStyle));
            }
            if (data.Match != null && data.Match.Count > 0) {
                AddHighlighted(spans, c.Label, data.Match, titleStyle, baseStyle.Combine(context.Style(ThemeToken.Accent)).With(TextDecoration.Bold | TextDecoration.Underline));
            } else if (c.RichTitle != null && !IsFiltering && enabled) {
                foreach (StyledLine line in c.RichTitle.ToStyledText(context).Lines) {
                    foreach (TextSpan span in line) {
                        spans.Add(new TextSpan(span.Text, titleStyle.Combine(span.Style)));
                    }
                    break;
                }
            } else {
                spans.Add(new TextSpan(c.Label, titleStyle));
            }
            if (focused && brackets) {
                spans.Add(new TextSpan("]", titleStyle));
            }
            if (groupSuffix != null) {
                spans.Add(new TextSpan("  " + g.Separator + " " + groupSuffix, baseStyle.Combine(context.Style(ThemeToken.Muted))));
            }
            StyledLine title = TextWrapper.Truncate(new StyledLine(spans), Math.Max(1, right - x), context.Ellipsis);
            row.Write(x, 0, title);
            if (badgeLine != null && badgeWidth > 0 && row.Width - 1 - badgeWidth > x + 2) {
                row.Write(row.Width - 1 - badgeWidth, 0, badgeLine.WithBaseStyle(baseStyle.Combine(context.Style(ThemeToken.Muted))));
            }
        }

        private void DrawGroup(Region row, Row data, bool focused, RenderContext context, InteractiveGlyphs g, Style baseStyle, ref int x) {
            string group = data.Group!;
            int total = 0;
            int selected = 0;
            for (int i = 0; i < _choices.Count; i++) {
                if (string.Equals(_choices[i].Group, group, StringComparison.Ordinal) && _choices[i].IsEnabled) {
                    total++;
                    if (_selected.Contains(i)) {
                        selected++;
                    }
                }
            }
            if (Multi) {
                if (HasHotkeys) {
                    x += 3;
                }
                bool collapsed = _collapsed.Contains(group);
                row.Write(x, 0, collapsed ? g.Collapsed : g.Expanded, baseStyle.Combine(context.Style(ThemeToken.Muted)));
                x += 2;
                string box = selected == 0 ? g.Unchecked : selected == total ? g.Checked : g.Partial;
                x = row.Write(x, 0, box, baseStyle.Combine(context.Style(selected > 0 ? ThemeToken.Accent : ThemeToken.Muted))) + 1;
                Style title = baseStyle.Combine(context.Style(ThemeToken.Header));
                bool brackets = g.BracketFocus || !context.HasColor;
                string text = focused && brackets ? "[" + group + "]" : group;
                string count = selected.ToString(context.Culture) + "/" + total.ToString(context.Culture);
                int end = row.Width - 1 - count.Length - 2;
                row.Write(x, 0, TextWrapper.Truncate(text, Math.Max(1, end - x), context.Ellipsis), title);
                row.Write(row.Width - 1 - count.Length, 0, count, baseStyle.Combine(context.Style(selected > 0 ? ThemeToken.Accent : ThemeToken.Muted)));
                return;
            }
            Style header = context.Style(ThemeToken.Muted).With(TextDecoration.Bold);
            row.Write(x, 0, TextWrapper.Truncate(group, Math.Max(1, row.Width - x - 1), context.Ellipsis), header);
        }

        private static void AddHighlighted(List<TextSpan> spans, string text, IReadOnlyList<int> positions, Style normal, Style highlight) {
            int p = 0;
            int start = 0;
            for (int i = 0; i < text.Length; i++) {
                bool hit = p < positions.Count && positions[p] == i;
                if (hit) {
                    if (i > start) {
                        spans.Add(new TextSpan(text.Substring(start, i - start), normal));
                    }
                    int end = i;
                    while (p < positions.Count && positions[p] == end) {
                        p++;
                        end++;
                    }
                    spans.Add(new TextSpan(text.Substring(i, end - i), highlight));
                    i = end - 1;
                    start = end;
                }
            }
            if (start < text.Length) {
                spans.Add(new TextSpan(text.Substring(start), normal));
            }
        }

        private bool CanSelect(int adding) {
            if (Maximum.HasValue && _selected.Count + adding > Maximum.Value) {
                Message = PromptLabels.Format(Labels.MaximumSelected, Maximum.Value);
                return false;
            }
            return true;
        }

        private void ToggleGroup(string group) {
            List<int> members = new List<int>();
            for (int i = 0; i < _choices.Count; i++) {
                if (string.Equals(_choices[i].Group, group, StringComparison.Ordinal) && _choices[i].IsEnabled) {
                    members.Add(i);
                }
            }
            bool all = true;
            foreach (int i in members) {
                if (!_selected.Contains(i)) {
                    all = false;
                    break;
                }
            }
            if (all) {
                foreach (int i in members) {
                    _selected.Remove(i);
                }
            } else {
                int adding = 0;
                foreach (int i in members) {
                    if (!_selected.Contains(i)) {
                        adding++;
                    }
                }
                if (!CanSelect(adding)) {
                    return;
                }
                foreach (int i in members) {
                    _selected.Add(i);
                }
            }
            Message = null;
            _dirty = true;
        }

        private List<int> ShownItems() {
            EnsureRows();
            List<int> shown = new List<int>();
            if (!IsFiltering && !SelectedOnly) {
                for (int i = 0; i < _choices.Count; i++) {
                    shown.Add(i);
                }
                return shown;
            }
            foreach (Row r in _rows) {
                if (r.Kind == RowKind.Item) {
                    shown.Add(r.Choice);
                }
            }
            return shown;
        }

        private bool IsFocusable(Row row) {
            return row.Kind == RowKind.Item || (row.Kind == RowKind.Group && Multi);
        }

        private int Nearest(int target, int direction) {
            for (int i = target; i >= 0 && i < _rows.Count; i += direction == 0 ? 1 : direction) {
                if (IsFocusable(_rows[i])) {
                    return i;
                }
            }
            for (int i = target; i >= 0 && i < _rows.Count; i -= direction == 0 ? 1 : direction) {
                if (IsFocusable(_rows[i])) {
                    return i;
                }
            }
            return _focus;
        }

        private void EnsureVisible() {
            if (_focus < _scroll) {
                _scroll = _focus;
                // Keep the group heading of the first visible item in view when scrolling up to it.
                if (_scroll > 0 && _rows[_scroll - 1].Kind == RowKind.Group && !Multi) {
                    _scroll--;
                }
            }
            if (_focus >= _scroll + _viewport) {
                _scroll = _focus - _viewport + 1;
            }
            _scroll = Math.Max(0, Math.Min(_scroll, Math.Max(0, _rows.Count - _viewport)));
        }

        private void EnsureRows() {
            if (!_dirty) {
                return;
            }
            _dirty = false;
            int focusedChoice = _focus >= 0 && _focus < _rows.Count && _rows[_focus].Kind == RowKind.Item ? _rows[_focus].Choice : -1;
            string? focusedGroup = _focus >= 0 && _focus < _rows.Count && _rows[_focus].Kind == RowKind.Group ? _rows[_focus].Group : null;
            _rows.Clear();
            string pattern = Filter.Text.Trim();
            if (pattern.Length == 0) {
                BuildGrouped();
            } else {
                BuildFiltered(pattern);
            }
            if (_rows.Count == 0) {
                _rows.Add(new Row(RowKind.Empty, -1, null, null));
                _focus = 0;
                return;
            }
            int restored = -1;
            for (int r = 0; r < _rows.Count; r++) {
                if ((focusedChoice >= 0 && _rows[r].Kind == RowKind.Item && _rows[r].Choice == focusedChoice)
                    || (focusedGroup != null && _rows[r].Kind == RowKind.Group && _rows[r].Group == focusedGroup)) {
                    restored = r;
                    break;
                }
            }
            if (restored >= 0 && pattern.Length == 0) {
                _focus = restored;
            } else if (_focus >= _rows.Count || !IsFocusable(_rows[Math.Max(0, _focus)])) {
                _focus = Nearest(Math.Min(Math.Max(0, _focus), _rows.Count - 1), 1);
            }
            if (!IsFocusable(_rows[Math.Max(0, Math.Min(_focus, _rows.Count - 1))])) {
                _focus = Nearest(0, 1);
            }
            if (!_focusPlaced) {
                // Start on the first choice rather than on a group heading.
                _focusPlaced = true;
                for (int r = 0; r < _rows.Count; r++) {
                    if (_rows[r].Kind == RowKind.Item) {
                        _focus = r;
                        break;
                    }
                }
            }
        }

        private void BuildGrouped() {
            for (int i = 0; i < _choices.Count; i++) {
                if (string.IsNullOrEmpty(_choices[i].Group) && (!SelectedOnly || _selected.Contains(i))) {
                    _rows.Add(new Row(RowKind.Item, i, null, null));
                }
            }
            foreach (string group in _groups) {
                List<int> members = new List<int>();
                for (int i = 0; i < _choices.Count; i++) {
                    if (string.Equals(_choices[i].Group, group, StringComparison.Ordinal) && (!SelectedOnly || _selected.Contains(i))) {
                        members.Add(i);
                    }
                }
                if (members.Count == 0) {
                    continue;
                }
                _rows.Add(new Row(RowKind.Group, -1, group, null));
                if (Multi && _collapsed.Contains(group)) {
                    continue;
                }
                foreach (int i in members) {
                    _rows.Add(new Row(RowKind.Item, i, group, null));
                }
            }
        }

        private void BuildFiltered(string pattern) {
            List<int> indexes = new List<int>();
            for (int i = 0; i < _choices.Count; i++) {
                if (!SelectedOnly || _selected.Contains(i)) {
                    indexes.Add(i);
                }
            }
            IReadOnlyList<FuzzyResult<int>> ranked = FuzzyMatcher.Rank(indexes, i => _choices[i].Label, pattern);
            HashSet<int> added = new HashSet<int>();
            foreach (FuzzyResult<int> result in ranked) {
                _rows.Add(new Row(RowKind.Item, result.Item, _choices[result.Item].Group, result.Match.Positions));
                added.Add(result.Item);
            }
            // Choices whose group matches come after direct title matches ("security" finds every security item).
            IReadOnlyList<FuzzyResult<int>> byGroup = FuzzyMatcher.Rank(indexes, i => _choices[i].Group ?? string.Empty, pattern);
            foreach (FuzzyResult<int> result in byGroup) {
                if (!added.Contains(result.Item) && !string.IsNullOrEmpty(_choices[result.Item].Group) && result.Match.Score > 0) {
                    _rows.Add(new Row(RowKind.Item, result.Item, _choices[result.Item].Group, null));
                    added.Add(result.Item);
                }
            }
        }

        private enum RowKind {
            Item,
            Group,
            Empty
        }

        private readonly struct Row {
            public Row(RowKind kind, int choice, string? group, IReadOnlyList<int>? match) {
                Kind = kind;
                Choice = choice;
                Group = group;
                Match = match;
            }

            public RowKind Kind { get; }

            public int Choice { get; }

            public string? Group { get; }

            public IReadOnlyList<int>? Match { get; }
        }
    }
}
