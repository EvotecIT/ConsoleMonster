using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The grid screen: the header bar (title, row and selection counts), the filter row, the rows in a panel (one line
    /// each, the focused row on the Focus background, marked rows with a check), and the footer. Typing filters; dialogs
    /// show a row's details and the column chooser.
    /// </summary>
    internal sealed partial class GridView : InteractiveView, IViewHost, IDisposable {
        private readonly Grid _grid;
        private readonly LineEditor _filter;
        private readonly bool[] _hidden;
        private readonly HashSet<int> _marked = new HashSet<int>();
        private readonly List<Modal> _modals = new List<Modal>();
        private readonly CultureInfo _culture;
        private List<int> _visible = new List<int>();
        private string _appliedFilter = "\0";
        private int _sortColumn;
        private bool _descending;
        private int _cursor;
        private int _top;
        private int _pageRows = 10;
        private Table? _table;
        private Capabilities.GlyphTier _tableTier;
        private readonly AppCommandHost? _actions;
        private Rect _bodyBounds;
        private List<int> _rowLines = new List<int>();
        private int _dragAnchor = -1;

        public GridView(Grid grid, GridOptions options, PromptLabels labels, System.Threading.CancellationToken cancellationToken = default) : base(labels) {
            _grid = grid;
            if (options.Commands.Count > 0 || options.Dispatcher != null) { _actions = new AppCommandHost(options.Commands, ActionRows, labels, cancellationToken, commandBar: true, requiresRows: true, dispatcher: options.Dispatcher, selectionLabel: ActionSelectionLabel, completed: (command, rows) => RefreshActionRows(options, command, rows)); }
            _filter = new LineEditor(options.Filter);
            _hidden = new bool[grid.Columns.Count];
            for (int c = 0; c < grid.Columns.Count; c++) {
                _hidden[c] = options.HiddenColumns.Contains(Markup.Strip(grid.Columns[c].Header));
            }
            _sortColumn = options.SortColumn >= 0 && options.SortColumn < grid.Columns.Count ? options.SortColumn : -1;
            _descending = options.SortDescending;
            _culture = grid.Presentation.Culture ?? CultureInfo.CurrentCulture;
            RebuildSearch();
        }

        private void RebuildSearch() {
            foreach (GridRow row in _grid.Rows) {
                row.Search = new string[_grid.Columns.Count];
                for (int c = 0; c < row.Search.Length; c++) {
                    row.Search[c] = c < row.Values.Count ? PresenterValues.ToText(row.Values[c], _culture) : string.Empty;
                }
            }
            _appliedFilter = "\0";
            _table = null;
        }

        private void RefreshActionRows(GridOptions options, AppCommand command, IReadOnlyList<object?> items) {
            if (options.RefreshRow == null) { return; }
            // Match the captured source objects, even if focus or marks changed while the action ran.
            try {
                for (int i = 0; i < _grid.Rows.Count; i++) {
                    GridRow row = _grid.Rows[i];
                    if (items.Any(item => ReferenceEquals(item, row.Item))) {
                        _grid.ReplaceRow(i, options.RefreshRow(command, row) ?? throw new InvalidOperationException("RefreshRow must return a row snapshot."));
                    }
                }
            } finally { RebuildSearch(); }
        }

        public bool Accepted { get; private set; }

        public void ShowModal(Modal modal) => _modals.Add(modal);

        // A grid hosts no panes that take values from other threads.
        public bool TryPost(Action call) => _actions?.TryPost(call) ?? false;

        public void Dispose() => _actions?.Dispose();

        public override bool Poll(TimeSpan now) => base.Poll(now) | (_actions?.Poll() ?? false);

        private IReadOnlyList<object?> ActionRows() {
            Refresh();
            if (_marked.Count > 0) {
                List<object?> rows = new List<object?>();
                foreach (int index in Sorted(AllRows())) { if (_marked.Contains(index)) { rows.Add(_grid.Rows[index].Item); } }
                return rows;
            }
            return FocusedRow == null ? Array.Empty<object?>() : new[] { FocusedRow.Item };
        }

        protected override bool HandlePriorityInput(InputEvent input) {
            if (_modals.Count == 0 && (_actions?.Handle(input) ?? false)) { return true; }
            if (input.Kind == InputEventKind.Resize || input.Kind == InputEventKind.FocusLost) { _dragAnchor = -1; }
            return false;
        }

        /// <summary>The objects of the chosen rows.</summary>
        public List<object?> ChosenItems() {
            List<object?> items = new List<object?>();
            if (_grid.Selection == GridSelection.None) {
                return items;
            }
            if (_grid.Selection == GridSelection.Multiple && _marked.Count > 0) {
                foreach (int index in Sorted(AllRows())) {
                    if (_marked.Contains(index)) {
                        items.Add(_grid.Rows[index].Item);
                    }
                }
                return items;
            }
            Refresh();
            if (_cursor >= 0 && _cursor < _visible.Count) {
                items.Add(_grid.Rows[_visible[_cursor]].Item);
            }
            return items;
        }

        public override IReadOnlyList<StyledLine>? Summary(RenderContext context) {
            if (IsCanceled || !Accepted) {
                return null;
            }
            int count = ChosenItems().Count;
            return new[] {
                new StyledLine(new[] {
                    new TextSpan(context.Glyphs.Get(Glyph.Check) + " ", context.Style(ThemeToken.Pass)),
                    new TextSpan(_grid.Title, context.Style(ThemeToken.Title)),
                    new TextSpan("  " + PromptLabels.Format(Labels.GridSelected, count), context.Style(ThemeToken.Accent))
                })
            };
        }

        protected override IWidget Build(ViewFrame frame) {
            Refresh();
            _modals.RemoveAll(m => m.IsClosed);
            string right = PromptLabels.Format(Labels.GridRows, _visible.Count, _grid.Rows.Count);
            if (_grid.Selection == GridSelection.Multiple && _marked.Count > 0) {
                right += " " + InteractiveGlyphs.For(frame.Context).Separator + " " + PromptLabels.Format(Labels.GridSelected, _marked.Count);
            }
            Rows rows = new Rows();
            rows.Add(new HeaderBar(_grid.Title) { Right = right }, LayoutSize.Fixed(1));
            TextInput input = new TextInput(_filter, _modals.Count == 0 && !(_actions?.HasModal ?? false) && !(_actions?.HasBarFocus ?? false) ? frame.Caret : null) { Placeholder = Labels.TypeToFilter, Focused = _modals.Count == 0 && !(_actions?.HasModal ?? false) && !(_actions?.HasBarFocus ?? false) };
            Columns filter = new Columns(new Sized(new UiLine(context => new StyledLine(new[] { new TextSpan(InteractiveGlyphs.For(context).Pointer + " ", context.Style(ThemeToken.Accent)) })), LayoutSize.Fixed(2)), input) { Gap = 0 };
            rows.Add(new Padding(filter, 1, 1, 1, 0), LayoutSize.Fixed(2));
            Panel panel = new Panel(new GridBody(this), SortTitle()) { BorderRole = ThemeToken.Accent };
            if (_actions?.HasCommands == true) {
                Columns panes = new Columns { CollapseBelow = 70 };
                panes.Add(panel, LayoutSize.Fraction(3)).Add(new Panel(new GridActionDetails(this), "Selected row"), LayoutSize.Fraction(2));
                rows.Add(panes, LayoutSize.Fraction());
            } else { rows.Add(panel, LayoutSize.Fraction()); }
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            if (_actions?.HasCommands == true) {
                foreach (KeyBinding binding in bindings) { binding.InFooter = false; }
                bindings.Add(new KeyBinding(Labels.KeyMove, () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = "↑↓", Order = 10 });
                bindings.Add(new KeyBinding("mark", ToggleMark, new KeyChord(ConsoleKey.Spacebar)) { KeyText = "Space", Order = 15 });
                bindings.Add(new KeyBinding("close", Escape, new KeyChord(ConsoleKey.Escape)) { KeyText = "Esc", Order = 20 });
            }
            _actions?.AddBindings(bindings);
            IWidget body = rows;
            if (_actions != null) { body = new Rows().Add(_actions.Wrap(rows), LayoutSize.Fraction()).Add(new KeyFooter(bindings) { Toast = CurrentToast }, LayoutSize.Fixed(1)); }
            else { rows.Add(new KeyFooter(bindings) { Toast = CurrentToast }, LayoutSize.Fixed(1)); }
            if (_modals.Count > 0) {
                Modal top = _modals[_modals.Count - 1];
                return new Overlay(body, top.Build(frame, frame.Caret), dim: true) { Top = top.Top, MaxWidth = top.MaxWidth };
            }
            return body;
        }

        private string ActionSelectionLabel() {
            Refresh();
            int marked = _visible.Count(index => _marked.Contains(index));
            if (marked > 1) { return marked.ToString(_culture) + " rows"; }
            int index = marked == 1 ? _visible.Find(item => _marked.Contains(item)) : _visible.Count > 0 ? _visible[_cursor] : -1;
            return index >= 0 && _grid.Rows[index].Values.Count > 0 ? PresenterValues.ToText(_grid.Rows[index].Values[0], _culture) : "selected row";
        }

        private string? SortTitle() {
            if (_sortColumn < 0) {
                return null;
            }
            string arrow = _descending ? "↓" : "↑";
            return Markup.Escape(Markup.Strip(_grid.Columns[_sortColumn].Header)) + " " + arrow;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_modals.Count > 0) {
                _modals[_modals.Count - 1].AddBindings(bindings, frame);
                return;
            }
            InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
            bindings.Add(new KeyBinding(Labels.KeyMove, () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = g.KeyUpDown, Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyPage, () => Move(-_pageRows), new KeyChord(ConsoleKey.PageUp)) { KeyText = "pgup/pgdn", Order = 11, InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(_pageRows), new KeyChord(ConsoleKey.PageDown)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(-_visible.Count), new KeyChord(ConsoleKey.Home, KeyModifiers.Control)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(_visible.Count), new KeyChord(ConsoleKey.End, KeyModifiers.Control)) { InFooter = false });
            if (_grid.Selection == GridSelection.Multiple) {
                bindings.Add(new KeyBinding(Labels.KeySelect, ToggleMark, new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "space", Order = 15 });
                bindings.Add(new KeyBinding(Labels.KeyAll, MarkAll, new KeyChord(ConsoleKey.A, KeyModifiers.Control)) { KeyText = "ctrl+a", Order = 16, InFooter = false });
            }
            bindings.Add(new KeyBinding(Labels.KeyDetails, OpenDetails, new KeyChord(ConsoleKey.Enter)) { Order = 30 });
            bindings.Add(new KeyBinding(Labels.KeySort, () => CycleSort(1), new KeyChord(ConsoleKey.F3)) { KeyText = "f3", Order = 32 });
            bindings.Add(new KeyBinding(string.Empty, () => CycleSort(-1), new KeyChord(ConsoleKey.F3, KeyModifiers.Shift)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyOrder, ReverseSort, new KeyChord(ConsoleKey.F4)) { KeyText = "f4", Order = 33, InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyColumns, () => ShowModal(new GridColumnsModal(this)), new KeyChord(ConsoleKey.F2)) { KeyText = "f2", Order = 34 });
            if (_grid.Selection != GridSelection.None) {
                bindings.Add(new KeyBinding(Labels.KeyDone, Done, new KeyChord(ConsoleKey.Enter, KeyModifiers.Control), new KeyChord(ConsoleKey.F10)) { KeyText = "ctrl+enter", Order = 20 });
            }
            bindings.Add(new KeyBinding(_filter.IsEmpty ? Labels.KeyCancel : Labels.KeyClearFilter, Escape, new KeyChord(ConsoleKey.Escape)) { Order = 25 });
            // The essential move/select/done/cancel hints fit at 60 columns. Let secondary hints, including the key-map
            // hint, give way before these actions instead of reserving footer space for F1. F1 still opens every binding.
            KeyBinding help = HelpBinding();
            help.Order = 100;
            bindings.Add(help);
        }

        protected override bool HandleUnbound(InputEvent input) {
            if (_modals.Count > 0) {
                return _modals[_modals.Count - 1].HandleUnbound(input);
            }
            if (input.Kind == InputEventKind.Mouse) { return HandlePointer(input); }
            if (input.Kind == InputEventKind.Key && input.IsText && input.Char == ' ') {
                return false;
            }
            return _filter.Handle(input);
        }

        private bool HandlePointer(InputEvent input) {
            if (input.MouseAction == MouseAction.Up) { _dragAnchor = -1; }
            if (input.Column < _bodyBounds.X || input.Column >= _bodyBounds.Right || input.Row < _bodyBounds.Y || input.Row >= _bodyBounds.Bottom) { return false; }
            if (input.MouseAction == MouseAction.Wheel) { Move(-Math.Sign(input.WheelDelta) * 3); return true; }
            int relative = input.Row - _bodyBounds.Y;
            int hit = _rowLines.FindLastIndex(y => y <= relative);
            if (hit < 0 || hit + _top >= _visible.Count) { return false; }
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                _cursor = hit + _top;
                _dragAnchor = _cursor;
                if ((input.Modifiers & KeyModifiers.Control) != 0 && _grid.Selection == GridSelection.Multiple) { ToggleMark(); }
                return true;
            }
            if (input.MouseAction == MouseAction.Move && _dragAnchor >= 0 && (input.HeldButtons & 1) != 0) {
                _cursor = hit + _top;
                if (_grid.Selection == GridSelection.Multiple) {
                    for (int i = Math.Min(_dragAnchor, _cursor); i <= Math.Max(_dragAnchor, _cursor); i++) { _marked.Add(_visible[i]); }
                    _table = null;
                }
                return true;
            }
            return false;
        }

        private void Escape() {
            if (!_filter.IsEmpty) {
                _filter.Text = string.Empty;
                return;
            }
            IsCanceled = true;
            IsDone = true;
        }

        private void Done() {
            Accepted = true;
            IsDone = true;
        }

        private void Move(int delta) {
            Refresh();
            _cursor = Math.Max(0, Math.Min(_visible.Count - 1, _cursor + delta));
        }

        private void ToggleMark() {
            Refresh();
            if (_cursor < 0 || _cursor >= _visible.Count) {
                return;
            }
            int index = _visible[_cursor];
            if (!_marked.Remove(index)) {
                _marked.Add(index);
            }
            _table = null;
        }

        private void MarkAll() {
            Refresh();
            bool all = _visible.TrueForAll(i => _marked.Contains(i));
            foreach (int index in _visible) {
                if (all) {
                    _marked.Remove(index);
                } else {
                    _marked.Add(index);
                }
            }
            _table = null;
        }

        private void CycleSort(int direction) {
            List<int> shown = ShownColumns();
            int position = shown.IndexOf(_sortColumn);
            int next = position + direction;
            if (position < 0) {
                next = direction > 0 ? 0 : shown.Count - 1;
            }
            _sortColumn = next < 0 || next >= shown.Count ? -1 : shown[next];
            _descending = false;
            Invalidate();
        }

        private void ReverseSort() {
            if (_sortColumn >= 0) {
                _descending = !_descending;
                Invalidate();
            }
        }

        private void OpenDetails() {
            Refresh();
            if (_cursor >= 0 && _cursor < _visible.Count) {
                ShowModal(new GridDetailModal(this));
            }
        }

        internal void Invalidate() {
            _appliedFilter = "\0";
            _table = null;
        }

        internal Grid Grid => _grid;

        internal bool IsHidden(int column) => _hidden[column];

        internal void SetHidden(int column, bool hidden) {
            if (hidden && ShownColumns().Count <= 1) {
                return;
            }
            _hidden[column] = hidden;
            if (hidden && _sortColumn == column) {
                _sortColumn = -1;
            }
            Invalidate();
        }

        internal GridRow? FocusedRow => _cursor >= 0 && _cursor < _visible.Count ? _grid.Rows[_visible[_cursor]] : null;

        internal void StepFocus(int delta) => Move(delta);

        internal List<int> ShownColumns() {
            List<int> shown = new List<int>();
            for (int c = 0; c < _hidden.Length; c++) {
                if (!_hidden[c]) {
                    shown.Add(c);
                }
            }
            return shown;
        }

        private IEnumerable<int> AllRows() {
            for (int i = 0; i < _grid.Rows.Count; i++) {
                yield return i;
            }
        }
    }
}
