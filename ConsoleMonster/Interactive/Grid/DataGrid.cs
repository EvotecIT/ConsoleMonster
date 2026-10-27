using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;

namespace ConsoleMonster.Interactive {
    /// <summary>A retained, embeddable grid. Each instance owns its cursor, viewport, search and marks; the application owns the terminal.</summary>
    public sealed partial class DataGrid : InputControl, IDisposable {
        private readonly Grid _grid;
        private readonly HashSet<object> _marked = new HashSet<object>();
        private readonly HashSet<int> _hidden = new HashSet<int>();
        private CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly HashSet<DataGridCell> _cells = new HashSet<DataGridCell>();
        private readonly Action _cellChanged;
        private IReadOnlyList<GridRow> _rows = Array.Empty<GridRow>();
        private List<int> _visible = new List<int>();
        private List<int> _order = new List<int>();
        private IReadOnlyList<object?>? _markedSelection;
        private int _cursor;
        private int _top;
        private int _pageRows = 10;
        private string _filter = string.Empty;
        private string _quickSearch = string.Empty;
        private int _sortColumn = -1;
        private bool _descending;
        private int _dragAnchor = -1;
        private bool _disposed;

        /// <summary>Creates a grid over an existing listing, or an empty listing.</summary>
        public DataGrid(string key, Grid? grid = null) : base(key) {
            _grid = grid ?? new Grid(key);
            _cellChanged = CellChanged;
            Label = _grid.Title;
            RefreshRows();
        }

        /// <summary>The listing and column definitions. Call RefreshRows after changing them.</summary>
        public Grid Grid => _grid;
        /// <summary>The maximum natural content height; constrained further by the host viewport.</summary>
        public int Height { get; set; } = 20;
        /// <summary>Whether unbound text performs prefix search. Disable when an app registry owns search.</summary>
        public bool QuickSearch { get; set; } = true;
        /// <summary>The focused source row, or null for an empty view.</summary>
        public GridRow? CurrentRow => Read(() => CurrentRowCore);
        /// <summary>The focused visible row index.</summary>
        public int Cursor { get => Read(() => _cursor); set => Change(() => _cursor = Clamp(value)); }
        /// <summary>The first visible row index.</summary>
        public int ScrollRow => Read(() => _top);
        /// <summary>The number of rows matching the filter.</summary>
        public int VisibleCount => Read(() => _visible.Count);
        /// <summary>The active prefix search, shown under the rows.</summary>
        public string Search => Read(() => _quickSearch);
        /// <summary>The active substring filter; words must match displayed columns.</summary>
        public string Filter { get => Read(() => _filter); set => Change(() => { _filter = value ?? string.Empty; Rebuild(); }); }
        /// <summary>Marked original objects in display sort order, including filtered marks.</summary>
        public IReadOnlyList<object?> Marks => Read(MarkedSelection);
        /// <summary>Marked stable identities.</summary>
        public IReadOnlyList<object> MarkedIds => Read(() => (IReadOnlyList<object>)_marked.ToArray());
        /// <summary>The number of marked identities, including filtered rows.</summary>
        public int MarkCount => Read(() => _marked.Count);
        /// <summary>The original objects selected by the grid's output mode.</summary>
        public IReadOnlyList<object?> Selection => Read(SelectionCore);

        private GridRow? CurrentRowCore => _cursor >= 0 && _cursor < _visible.Count ? _rows[_visible[_cursor]] : null;
        private CultureInfo Culture => _grid.Presentation.Culture ?? CultureInfo.CurrentCulture;
        private int Clamp(int cursor) => Math.Max(0, Math.Min(Math.Max(0, _visible.Count - 1), cursor));
        private IEnumerable<int> AllRows() { for (int i = 0; i < _rows.Count; i++) { yield return i; } }

        /// <summary>Rebinds a listing while retaining the focused identity and surviving marks. Call on the app thread.</summary>
        public void SetRows(IEnumerable<GridRow> rows) {
            Change(() => { _grid.SetRows(rows); RefreshRowsCore(); });
        }

        /// <summary>Refreshes column, sort and filter mappings after changing the listing.</summary>
        public void RefreshRows() => Change(RefreshRowsCore);
        private void RefreshRowsCore() {
            object? focused = CurrentRowCore?.Id;
            ResetCells();
            _rows = _grid.Rows.ToArray();
            Rebuild(focused); PruneMarks();
        }
        private void PruneMarks() {
            var ids = new HashSet<object>(_rows.Select(row => row.Id));
            if (ids.Count != _rows.Count || ids.Contains(null!)) { throw new ArgumentException("Rows need unique, non-null identities."); }
            _marked.IntersectWith(ids); _markedSelection = null;
        }

        /// <summary>Moves the cursor without changing marks.</summary>
        public void Move(int delta) => Change(() => _cursor = Clamp(_cursor + delta));
        /// <summary>Toggles the current mark; optionally advances afterwards.</summary>
        public void ToggleMark(bool advance = false) => Change(() => {
            GridRow? row = CurrentRowCore;
            if (row == null || _grid.OutputMode != GridSelection.Multiple) { return; }
            if (!_marked.Remove(row.Id)) { _marked.Add(row.Id); }
            _markedSelection = null;
            if (advance) { _cursor = Clamp(_cursor + 1); }
        });

        /// <summary>Toggles all rows in the current filtered view.</summary>
        public void MarkAll() => Change(() => {
            bool all = _visible.All(i => _marked.Contains(_rows[i].Id));
            foreach (int i in _visible) { if (all) { _marked.Remove(_rows[i].Id); } else { _marked.Add(_rows[i].Id); } }
            _markedSelection = null;
        });

        /// <summary>Marks matching names in the current view. Asterisk matches any sequence and question mark one character.</summary>
        public void MarkPattern(string pattern, int column = 0) => ApplyPattern(pattern, column, true);
        /// <summary>Unmarks matching names in the current view.</summary>
        public void UnmarkPattern(string pattern, int column = 0) => ApplyPattern(pattern, column, false);
        /// <summary>Inverts marks in the current view.</summary>
        public void InvertMarks() => Change(() => { foreach (int i in _visible) { object id = _rows[i].Id; if (!_marked.Remove(id)) { _marked.Add(id); } } _markedSelection = null; });

        /// <summary>Jumps to the first prefix match without forcing pending cell factories.</summary>
        public bool SearchPrefix(string prefix, int column = 0) {
            bool found = false;
            Change(() => found = SearchPrefixCore(prefix, column));
            return found;
        }
        private bool SearchPrefixCore(string prefix, int column = 0) {
            _quickSearch = prefix ?? string.Empty;
            for (int i = 0; i < _visible.Count; i++) {
                if (Text(_rows[_visible[i]], column).StartsWith(_quickSearch, StringComparison.OrdinalIgnoreCase)) { _cursor = i; return true; }
            }
            return false;
        }

        /// <summary>Clears prefix search.</summary>
        public void ClearSearch() => Change(() => _quickSearch = string.Empty);
        /// <summary>Sets the sort column; -1 restores listing order.</summary>
        public void Sort(int column, bool descending = false) => Change(() => {
            if (column < -1 || column >= _grid.Columns.Count) { throw new ArgumentOutOfRangeException(nameof(column)); }
            _sortColumn = column; _descending = descending; Rebuild();
        });
        internal int SortColumn => _sortColumn;
        internal bool Descending => _descending;
        internal int PageRows => _pageRows;
        internal int[] SelectedIndices() => Read(() => _marked.Count > 0
            ? _order.Where(i => _marked.Contains(_rows[i].Id)).ToArray()
            : CurrentRowCore == null ? Array.Empty<int>() : new[] { _visible[_cursor] });
        internal bool IsHidden(int column) => _hidden.Contains(column);
        internal List<int> ShownColumns() => Enumerable.Range(0, _grid.Columns.Count).Where(c => !_hidden.Contains(c)).ToList();
        internal void SetHidden(int column, bool hidden) => Change(() => {
            if (hidden && ShownColumns().Count <= 1) { return; }
            if (hidden) { _hidden.Add(column); } else { _hidden.Remove(column); }
            if (hidden && _sortColumn == column) { _sortColumn = -1; }
            Rebuild();
        });

        private IReadOnlyList<object?> SelectionCore() {
            if (_grid.OutputMode == GridSelection.None) { return Array.Empty<object?>(); }
            if (_grid.OutputMode == GridSelection.Multiple && _marked.Count != 0) { return MarkedSelection(); }
            return CurrentRowCore == null ? Array.Empty<object?>() : new[] { CurrentRowCore.Item };
        }
        private IReadOnlyList<object?> MarkedSelection() => _markedSelection ??= Array.AsReadOnly(_order.Where(i => _marked.Contains(_rows[i].Id)).Select(i => _rows[i].Item).ToArray());
        /// <inheritdoc />
        protected override object? ReadValueCore() => SelectionCore();
        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, Math.Max(1, Height)));
        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (input.Is(ConsoleKey.UpArrow)) { _cursor = Clamp(_cursor - 1); }
            else if (input.Is(ConsoleKey.DownArrow)) { _cursor = Clamp(_cursor + 1); }
            else if (input.Is(ConsoleKey.PageUp)) { _cursor = Clamp(_cursor - _pageRows); }
            else if (input.Is(ConsoleKey.PageDown)) { _cursor = Clamp(_cursor + _pageRows); }
            else if (input.Is(ConsoleKey.Home)) { _cursor = 0; }
            else if (input.Is(ConsoleKey.End)) { _cursor = Clamp(_visible.Count - 1); }
            else if (input.Is(ConsoleKey.Spacebar) || input.Is(ConsoleKey.Insert) || input.IsText && input.Char == ' ') {
                if (_grid.OutputMode != GridSelection.Multiple) { return false; }
                GridRow? row = CurrentRowCore;
                if (row != null && !_marked.Remove(row.Id)) { _marked.Add(row.Id); }
                _markedSelection = null;
            } else if (QuickSearch && input.Is(ConsoleKey.Escape) && _quickSearch.Length > 0) { _quickSearch = string.Empty; }
            else if (QuickSearch && input.Is(ConsoleKey.Backspace) && _quickSearch.Length > 0) { SearchPrefixCore(_quickSearch.Substring(0, _quickSearch.Length - 1)); }
            else if (QuickSearch && input.IsText && !char.IsControl(input.Char)) { SearchPrefixCore(_quickSearch + input.Char); }
            else { return false; }
            return true;
        }

        /// <summary>Cancels pending cell factories when the control is permanently retired.</summary>
        public void Dispose() {
            if (_disposed) { return; }
            _disposed = true;
            foreach (DataGridCell cell in _cells) { cell.Release(_cellChanged); }
            _cells.Clear();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
        private void ResetCells() {
            foreach (DataGridCell cell in _cells) { cell.Release(_cellChanged); }
            _cells.Clear();
            _lifetime.Cancel(); _lifetime.Dispose();
            _lifetime = new CancellationTokenSource();
        }
        private void CellChanged() { if (!_disposed) { Invalidate(); } }
    }
}
