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
        private readonly DataGrid _data;
        private readonly List<Modal> _modals = new List<Modal>();
        private readonly AppCommandHost? _actions;
        private int[] _actionIndices = Array.Empty<int>();

        public GridView(Grid grid, GridOptions options, PromptLabels labels, System.Threading.CancellationToken cancellationToken = default, Func<TimeSpan>? actionClock = null) : base(labels) {
            _grid = grid;
            if (options.Command.Count > 0 || options.Dispatcher != null) { _actions = new AppCommandHost(options.Command, ActionRows, labels, cancellationToken, commandBar: true, requiresRows: true, dispatcher: options.Dispatcher, selectionLabel: ActionSelectionLabel, completed: command => RefreshActionRows(options, command), actionClock: actionClock); }
            _filter = new LineEditor(options.Filter);
            _data = new DataGrid("Grid", grid) { Focused = true, QuickSearch = false, StatusPresenters = _actions?.HasCommands == true, ShowPosition = _actions?.HasCommands == true };
            for (int c = 0; c < grid.Columns.Count; c++) {
                if (options.HiddenColumns.Contains(Markup.Strip(grid.Columns[c].Header))) { _data.SetHidden(c, true); }
            }
            _data.Sort(options.SortColumn >= 0 && options.SortColumn < grid.Columns.Count ? options.SortColumn : -1, options.SortDescending);
            _data.Filter = _filter.Text;
        }

        private void RebuildSearch() => _data.RefreshRows();

        private void RefreshActionRows(GridOptions options, AppCommand command) {
            if (options.RefreshRow == null) { return; }
            List<Exception> errors = new List<Exception>();
            try {
                foreach (int index in _actionIndices) {
                    try {
                        _grid.ReplaceRow(index, options.RefreshRow(command, _grid.Rows[index]) ?? throw new InvalidOperationException("RefreshRow must return a row snapshot."));
                    } catch (Exception error) { errors.Add(error); }
                }
            } finally { RebuildSearch(); }
            if (errors.Count > 0) { throw new AggregateException("Some rows could not be refreshed.", errors); }
        }

        public bool Accepted { get; private set; }

        public void ShowModal(Modal modal) => _modals.Add(modal);

        // A grid hosts no panes that take values from other threads.
        public bool TryPost(Action call) => _actions?.TryPost(call) ?? false;

        public void Dispose() { _actions?.Dispose(); _data.Dispose(); }

        public override bool Poll(TimeSpan now) => base.Poll(now) | (_actions?.Poll() ?? false);

        private IReadOnlyList<object?> ActionRows() {
            Refresh();
            _actionIndices = _data.SelectedIndices();
            return _actionIndices.Select(index => _grid.Rows[index].Item).ToArray();
        }

        protected override bool HandlePriorityInput(InputEvent input) {
            if (_modals.Count == 0 && (_actions?.Handle(input) ?? false)) { return true; }
            if (input.Kind == InputEventKind.Resize || input.Kind == InputEventKind.FocusLost) { _data.CancelPointer(); }
            return false;
        }

        /// <summary>The objects of the chosen rows.</summary>
        public List<object?> ChosenItems() { Refresh(); return new List<object?>(_data.Selection); }

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
            string right = PromptLabels.Format(Labels.GridRows, _data.VisibleCount, _grid.Rows.Count);
            if (_grid.OutputMode == GridSelection.Multiple && _data.MarkCount > 0) {
                right += " " + InteractiveGlyphs.For(frame.Context).Separator + " " + PromptLabels.Format(Labels.GridSelected, _data.MarkCount);
            }
            Rows rows = new Rows();
            rows.Add(new HeaderBar(_grid.Title) { HeaderRight = right }, LayoutSize.Fixed(1));
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
                if (_grid.OutputMode == GridSelection.Multiple) { bindings.Add(new KeyBinding("mark", ToggleMark, new KeyChord(ConsoleKey.Spacebar)) { KeyText = "Space", Order = 15 }); }
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
            if (_actionIndices.Length > 1) { return _actionIndices.Length.ToString((_grid.Presentation.Culture ?? CultureInfo.CurrentCulture)) + " rows"; }
            int index = _actionIndices.Length == 1 ? _actionIndices[0] : -1;
            return index >= 0 && _grid.Rows[index].Values.Count > 0 ? PresenterValues.ToText(_grid.Rows[index].Values[0], (_grid.Presentation.Culture ?? CultureInfo.CurrentCulture)) : "selected row";
        }

        private string? SortTitle() {
            if (_data.SortColumn < 0) {
                return null;
            }
            string arrow = _data.Descending ? "↓" : "↑";
            return Markup.Escape(Markup.Strip(_grid.Columns[_data.SortColumn].Header)) + " " + arrow;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_modals.Count > 0) {
                _modals[_modals.Count - 1].AddBindings(bindings, frame);
                return;
            }
            InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
            bindings.Add(new KeyBinding(Labels.KeyMove, () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = g.KeyUpDown, Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => Move(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyPage, () => Move(-_data.PageRows), new KeyChord(ConsoleKey.PageUp)) { KeyText = "pgup/pgdn", Order = 11, InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(_data.PageRows), new KeyChord(ConsoleKey.PageDown)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(-_data.VisibleCount), new KeyChord(ConsoleKey.Home, KeyModifiers.Control)) { InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Move(_data.VisibleCount), new KeyChord(ConsoleKey.End, KeyModifiers.Control)) { InFooter = false });
            if (_grid.OutputMode == GridSelection.Multiple) {
                bindings.Add(new KeyBinding(Labels.KeySelect, ToggleMark, new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "space", Order = 15 });
                bindings.Add(new KeyBinding(Labels.KeyAll, MarkAll, new KeyChord(ConsoleKey.A, KeyModifiers.Control)) { KeyText = "ctrl+a", Order = 16, InFooter = false });
            }
            bindings.Add(new KeyBinding(Labels.KeyDetails, OpenDetails, new KeyChord(ConsoleKey.Enter)) { Order = 30 });
            bindings.Add(new KeyBinding(Labels.KeySort, () => CycleSort(1), new KeyChord(ConsoleKey.F3)) { KeyText = "f3", Order = 32 });
            bindings.Add(new KeyBinding(string.Empty, () => CycleSort(-1), new KeyChord(ConsoleKey.F3, KeyModifiers.Shift)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyOrder, ReverseSort, new KeyChord(ConsoleKey.F4)) { KeyText = "f4", Order = 33, InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyColumns, () => ShowModal(new GridColumnsModal(this)), new KeyChord(ConsoleKey.F2)) { KeyText = "f2", Order = 34 });
            if (_grid.OutputMode != GridSelection.None) {
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

        private bool HandlePointer(InputEvent input) => LastFrame != null && _data.Pointer(input, LastFrame.Context);

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

        private void Refresh() { if (_data.Filter != _filter.Text.Trim()) { _data.Filter = _filter.Text.Trim(); } }
        private void Move(int delta) { Refresh(); _data.Move(delta); }
        private void ToggleMark() { Refresh(); _data.ToggleMark(); }
        private void MarkAll() { Refresh(); _data.MarkAll(); }

        private void CycleSort(int direction) {
            List<int> shown = ShownColumns();
            int position = shown.IndexOf(_data.SortColumn);
            int next = position + direction;
            if (position < 0) {
                next = direction > 0 ? 0 : shown.Count - 1;
            }
            _data.Sort(next < 0 || next >= shown.Count ? -1 : shown[next]);
        }

        private void ReverseSort() {
            if (_data.SortColumn >= 0) {
                _data.Sort(_data.SortColumn, !_data.Descending);
            }
        }

        private void OpenDetails() {
            Refresh();
            if (_data.CurrentRow != null) {
                ShowModal(new GridDetailModal(this));
            }
        }

        internal void Invalidate() => _data.RefreshRows();
        internal Grid Grid => _grid;
        internal bool IsHidden(int column) => _data.IsHidden(column);
        internal void SetHidden(int column, bool hidden) => _data.SetHidden(column, hidden);
        internal GridRow? FocusedRow => _data.CurrentRow;
        internal void StepFocus(int delta) => Move(delta);
        internal List<int> ShownColumns() => _data.ShownColumns();

        private sealed class GridBody : IWidget {
            private readonly GridView _view;
            internal GridBody(GridView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 10 : constraints.MaxHeight);
            public void Render(Region region, RenderContext context) {
                _view.Refresh();
                _view._data.Focused = !(_view._actions?.HasBarFocus ?? false) && !(_view._actions?.HasModal ?? false) && _view._modals.Count == 0;
                _view._data.RenderRows(region, context);
            }
        }
    }
}
