using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A table that negotiates its width instead of overflowing or silently dropping data: columns shrink to their
    /// minimum (wrapping, truncating at the end or in the middle, per column), then hide by ascending
    /// <see cref="TableColumn.Priority"/> with a footer that names them (<c>+3 columns (Owner, Site, Notes)</c>); the key
    /// column never hides. Below <see cref="CardsBelow"/> cells the rows become record cards, and a single row becomes a
    /// record view. Rows can be grouped with subtotals, followed by a totals row, and limited to the top N with a
    /// <c>Showing 50 of 1,234</c> footer. Raw values are formatted by the column presenters. <see cref="For{T}(IEnumerable{T})"/>
    /// makes a <see cref="Table{T}"/> whose columns are read from typed items.
    /// </summary>
    public partial class Table : IContainer {
        private readonly List<TableColumn> _columns = new List<TableColumn>();
        private readonly List<TableCell[]> _rows = new List<TableCell[]>();
        private readonly List<int> _rowGroups = new List<int>();
        private readonly List<string> _groups = new List<string>();
        private readonly Dictionary<string, int> _stableWidths = new Dictionary<string, int>(StringComparer.Ordinal);
        private TableCell[]? _footer;

        /// <summary>Widgets held in value cells, including the footer, for dashboard discovery and recording replay.</summary>
        public IReadOnlyList<IWidget> Children {
            get {
                Prepare();
                var widgets = new List<IWidget>();
                foreach (TableCell[] row in _rows) foreach (TableCell cell in row) if (WidgetCellLayout.GetWidget(cell.Value) is IWidget widget) widgets.Add(widget);
                if (_footer != null) foreach (TableCell cell in _footer) if (WidgetCellLayout.GetWidget(cell.Value) is IWidget widget) widgets.Add(widget);
                return widgets;
            }
        }

        /// <summary>The columns.</summary>
        public IReadOnlyList<TableColumn> Columns {
            get {
                Prepare();
                return _columns;
            }
        }

        /// <summary>The rows.</summary>
        public IReadOnlyList<TableCell[]> Rows {
            get {
                Prepare();
                return _rows;
            }
        }

        /// <summary>
        /// With <see cref="ShowTotals"/>, also sums every column whose values are all numbers (not the key column and not
        /// identifier columns such as Id or Port) when it has no <see cref="TableColumn.Aggregate"/> of its own.
        /// </summary>
        public bool AutoTotals { get; set; }

        /// <summary>A title shown above the table (markup).</summary>
        public string? Title { get; set; }

        /// <summary>Optional grid-title indentation in cells; null retains the border-aligned default. Record and card views retain their own title layout.</summary>
        public int? TitleIndent { get; set; }

        /// <summary>A caption shown below the table (markup, muted).</summary>
        public string? Caption { get; set; }

        /// <summary>The border style (default rounded).</summary>
        public TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary>Whether the header row is shown (default true).</summary>
        public bool ShowHeader { get; set; } = true;

        /// <summary>Wrap column headings instead of shortening them (default false).</summary>
        public bool WrapHeaders { get; set; }

        /// <summary>Keep all columns by rendering consecutive grids with the key repeated when the full grid cannot fit.</summary>
        public bool KeepAllColumns { get; set; }

        /// <summary>
        /// Whether a separator row is drawn under the header (default true). When false the header is underlined instead,
        /// so data starts on the row directly below it.
        /// </summary>
        public bool HeaderSeparator { get; set; } = true;

        /// <summary>Whether group labels occupy full-width section rows instead of labelled border rules (default false).</summary>
        public bool GroupHeadersAsRows { get; set; }

        /// <summary>Whether the table fills the available width: the free-text columns share the spare width in proportion to their widths, unless columns set <see cref="TableColumn.Weight"/> or <see cref="TableColumn.Flexible"/>; number, date and status columns keep their natural width.</summary>
        public bool Expand { get; set; }

        /// <summary>
        /// Below this many cells of width the rows render as record cards (key column as the card title); 0 disables
        /// cards. Default 60. Cards are used only when the grid cannot fit its minimum widths; they also take over when hiding columns would leave only the key column.
        /// </summary>
        public int CardsBelow { get; set; } = 60;

        /// <summary>Shows the rows as record cards at any width (the Cards view of Show-Console).</summary>
        public bool AlwaysCards { get; set; }

        /// <summary>Show the record count beneath cards (default true). Omitted-row notices remain visible when false.</summary>
        public bool ShowCardCount { get; set; } = true;

        /// <summary>A table with at most this many rows renders as a record view (default 1; 0 disables).</summary>
        public int RecordViewRows { get; set; } = 1;

        /// <summary>Also show the complete key as a wrapped field in record/card layouts, where headings may be omitted or truncated. Default false.</summary>
        public bool ShowKeyField { get; set; }

        /// <summary>
        /// Titles for the rows in record and card views, by data row index (a null or empty entry uses the key column), so a
        /// caller can title a record "pwsh (90836)" while the key column stays the name. Null for none.
        /// </summary>
        public IList<string?>? RowTitles { get; set; }

        /// <summary>Whether a footer names the columns hidden for lack of width (default true).</summary>
        public bool ShowHiddenColumns { get; set; } = true;

        /// <summary>
        /// Whether column widths never shrink while the same table is re-rendered (live tables; default true), so columns
        /// do not jump as values change. Widths still fit the available space.
        /// </summary>
        public bool StableWidths { get; set; } = true;

        /// <summary>Whether each group ends with a subtotal row for columns that have an <see cref="TableColumn.Aggregate"/>.</summary>
        public bool ShowSubtotals { get; set; }

        /// <summary>Whether a totals row summarises all rows (columns with an <see cref="TableColumn.Aggregate"/>), unless a footer is set.</summary>
        public bool ShowTotals { get; set; }

        /// <summary>The label of the totals row, or null (default) for the built-in word (<c>Total</c> in English).</summary>
        public string? TotalLabel { get; set; }

        /// <summary>Shows only the first N rows (0 = all) followed by <c>Showing N of M</c>; totals still cover every row.</summary>
        public int MaxRows { get; set; }

        private int _wrapLines = 3;

        /// <summary>
        /// The most lines a wrapped value uses when its column sets no <see cref="TableColumn.MaxLines"/> (default 3; 0 =
        /// no limit): a long description stays a few lines with an ellipsis instead of taking the screen.
        /// </summary>
        public int WrapLines {
            get => _wrapLines;
            set => _wrapLines = value < 0 ? 0 : value;
        }

        /// <summary>
        /// The total number of records when the caller added only some of them (for example the first 50 of 1,234), so the
        /// footer can say <c>Showing 50 of 1,234</c>; null when every record was added.
        /// </summary>
        public int? TotalRowCount { get; set; }

        /// <summary>Culture, clock and time zone for raw values.</summary>
        public PresentationOptions Presentation { get; set; } = new PresentationOptions();

        /// <summary>Adds a column.</summary>
        /// <param name="header">The header (markup).</param>
        /// <param name="alignment">Alignment, or <c>null</c> for automatic.</param>
        /// <returns>This instance.</returns>
        public Table AddColumn(string header, HorizontalAlignment? alignment = null) {
            _columns.Add(new TableColumn(header) { Alignment = alignment });
            return this;
        }

        /// <summary>Adds a column definition.</summary>
        /// <param name="column">The column.</param>
        /// <returns>This instance.</returns>
        public Table AddColumn(TableColumn column) {
            _columns.Add(column ?? throw new ArgumentNullException(nameof(column)));
            return this;
        }

        /// <summary>Starts a group: rows added after this belong to it until the next group.</summary>
        /// <param name="title">The group heading (markup).</param>
        /// <returns>This instance.</returns>
        public Table AddGroup(string title) {
            _groups.Add(title ?? string.Empty);
            return this;
        }

        /// <summary>Adds a row of cells (strings convert to markup cells). Missing cells are blank.</summary>
        /// <param name="cells">The cells.</param>
        /// <returns>This instance.</returns>
        public Table AddRow(params TableCell[] cells) {
            _rows.Add(Normalize(cells));
            _rowGroups.Add(_groups.Count - 1);
            return this;
        }

        /// <summary>Adds a row of raw values, formatted by the column presenters and summarised by their aggregates.</summary>
        /// <param name="values">The values.</param>
        /// <returns>This instance.</returns>
        public Table AddValues(params object?[] values) {
            TableCell[] cells = new TableCell[values?.Length ?? 0];
            for (int i = 0; i < cells.Length; i++) {
                cells[i] = values![i] is TableCell cell ? cell : TableCell.FromValue(values[i]);
            }
            return AddRow(cells);
        }

        /// <summary>Removes every row (columns, options and remembered column widths stay), so the table can be refilled.</summary>
        public void ClearRows() {
            _rows.Clear();
            _rowGroups.Clear();
            _groups.Clear();
        }

        /// <summary>Adds a row of plain text values (brackets are shown literally).</summary>
        /// <param name="values">The values.</param>
        /// <returns>This instance.</returns>
        public Table AddPlainRow(params string?[] values) {
            TableCell[] cells = new TableCell[values?.Length ?? 0];
            for (int i = 0; i < cells.Length; i++) {
                cells[i] = new TableCell(Markup.Escape(values![i] ?? string.Empty));
            }
            return AddRow(cells);
        }

        /// <summary>Sets a footer row (totals), separated by a line. It replaces the automatic totals row.</summary>
        /// <param name="cells">The cells.</param>
        /// <returns>This instance.</returns>
        public Table SetFooter(params TableCell[] cells) {
            _footer = Normalize(cells);
            return this;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            TableLayout layout = ComputeLayout(constraints.MaxWidth, context);
            if (layout.Record != null) {
                return layout.Record.Measure(constraints, context);
            }
            int height = layout.TotalHeight;
            return new Size(Math.Min(layout.TotalWidth, constraints.MaxWidth), constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            TableLayout layout = ComputeLayout(region.Width, context);
            if (layout.Record != null) {
                layout.Record.Render(region, context);
                return;
            }
            Draw(region, layout, context);
        }

        /// <summary>
        /// Makes a table from typed items. Add columns with <see cref="Table{T}.Column(Func{T, object}, Action{TableColumn}, string)"/>
        /// (the header comes from the expression: <c>s =&gt; s.Name</c> is "Name"); without any, every public property
        /// of <typeparamref name="T"/> is a column. Rows are read from the items when the table is laid out.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items.</param>
        /// <returns>The table.</returns>
        public static Table<T> For<
#if NET8_0_OR_GREATER
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEnumerable<T> items) => new Table<T>(items);

        // Columns summed because of AutoTotals, chosen again at every layout from the current rows; the columns'
        // own Aggregate is never changed.
        private readonly HashSet<TableColumn> _autoSums = new HashSet<TableColumn>();

        // Called before the columns or rows are read and before every layout: Table<T> reads its items here, and AutoTotals
        // picks the numeric columns.
        private protected virtual void Prepare() {
            _autoSums.Clear();
            if (!ShowTotals || !AutoTotals) {
                return;
            }
            int key = KeyColumn;
            for (int c = 0; c < _columns.Count; c++) {
                TableColumn column = _columns[c];
                if (c == key || column.IsKey || column.Aggregate != ColumnAggregate.None || column.Presenter is IdentifierPresenter || IdentifierPresenter.LooksLikeIdentifier(column.Id)) {
                    continue;
                }
                bool numeric = false;
                bool other = false;
                foreach (TableCell[] row in _rows) {
                    object? value = c < row.Length && row[c].HasValue ? row[c].Value : null;
                    if (PresenterValues.IsEmpty(value)) {
                        continue;
                    }
                    if (!(value is string) && PresenterValues.TryGetNumber(value, out _)) {
                        numeric = true;
                    } else {
                        other = true;
                        break;
                    }
                }
                if (numeric && !other) {
                    _autoSums.Add(column);
                }
            }
        }

        // The column's own aggregate, or Sum when AutoTotals chose it.
        private ColumnAggregate EffectiveAggregate(TableColumn column) =>
            column.Aggregate != ColumnAggregate.None ? column.Aggregate : _autoSums.Contains(column) ? ColumnAggregate.Sum : ColumnAggregate.None;

        // Table<T> replaces the property columns it inferred once explicit columns are added.
        private protected void RemoveColumn(TableColumn column) => _columns.Remove(column);
        private int KeyColumn {
            get {
                for (int c = 0; c < _columns.Count; c++) {
                    if (_columns[c].IsKey) {
                        return c;
                    }
                }
                return 0;
            }
        }

        private TableCell[] Normalize(TableCell[]? cells) {
            int count = Math.Max(_columns.Count, cells?.Length ?? 0);
            TableCell[] result = new TableCell[count];
            for (int i = 0; i < count; i++) {
                result[i] = cells != null && i < cells.Length && cells[i] != null ? cells[i] : new TableCell(string.Empty);
            }
            return result;
        }
    }
}
