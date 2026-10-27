using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>How many rows a grid returns.</summary>
    public enum GridSelection {
        /// <summary>Browse only; nothing is returned.</summary>
        None,

        /// <summary>The focused row.</summary>
        Single,

        /// <summary>The rows marked with Space (or the focused row when none is marked).</summary>
        Multiple
    }

    /// <summary>
    /// An interactive table viewer: filter as you type, sort by column, page and scroll, mark rows, choose columns, and see
    /// a row's details. It returns the original objects of the chosen rows. Columns use the table's width negotiation and
    /// presenters; the screen has the same header bar, panel and footer as menus and forms.
    /// </summary>
    public sealed partial class Grid {
        private readonly List<TableColumn> _columns = new List<TableColumn>();
        private readonly List<GridRow> _rows = new List<GridRow>();

        /// <summary>Creates a grid.</summary>
        /// <param name="title">The title in the header bar.</param>
        public Grid(string title) {
            Title = title ?? string.Empty;
        }

        /// <summary>The title.</summary>
        public string Title { get; set; }

        /// <summary>What the grid returns (default <see cref="GridSelection.Multiple"/>).</summary>
        public GridSelection OutputMode { get; set; } = GridSelection.Multiple;

        /// <summary>Culture and display options for the presenters.</summary>
        public PresentationOptions Presentation { get; set; } = new PresentationOptions();

        /// <summary>The columns.</summary>
        public IReadOnlyList<TableColumn> Columns => _columns;

        /// <summary>The rows.</summary>
        public IReadOnlyList<GridRow> Rows => _rows;

        /// <summary>Adds a column.</summary>
        /// <param name="column">The column (header, presenter, alignment, priority).</param>
        /// <returns>The grid.</returns>
        public Grid AddColumn(TableColumn column) {
            _columns.Add(column ?? throw new ArgumentNullException(nameof(column)));
            return this;
        }

        /// <summary>Adds a column by header.</summary>
        /// <param name="header">The header.</param>
        /// <param name="presenter">The presenter, or null for automatic formatting.</param>
        /// <returns>The grid.</returns>
        public Grid AddColumn(string header, ICellPresenter? presenter = null) => AddColumn(new TableColumn(header) { Presenter = presenter });

        /// <summary>Adds a row: the object it stands for and its raw column values.</summary>
        /// <param name="item">The object returned when the row is chosen.</param>
        /// <param name="values">The column values (formatted by the column presenters).</param>
        /// <returns>The grid.</returns>
        public Grid AddRow(object? item, params object?[] values) {
            _rows.Add(new GridRow(item, values ?? new object?[0]));
            return this;
        }

        /// <summary>Adds a row with an explicit stable identity, including lazy cells.</summary>
        public Grid AddRow(GridRow row) {
            _rows.Add(row ?? throw new ArgumentNullException(nameof(row)));
            return this;
        }

        /// <summary>Replaces a listing. Reuse row identities to retain a DataGrid's cursor and marks.</summary>
        public void SetRows(IEnumerable<GridRow> rows) {
            if (rows == null) { throw new ArgumentNullException(nameof(rows)); }
            var replacement = new List<GridRow>(rows);
            var identities = new HashSet<object>();
            foreach (GridRow row in replacement) {
                if (row == null || row.Id == null || !identities.Add(row.Id)) { throw new ArgumentException("Rows need unique, non-null identities.", nameof(rows)); }
            }
            _rows.Clear();
            _rows.AddRange(replacement);
        }

        internal void ReplaceRow(int index, GridRow row) { row.Id = _rows[index].Id; _rows[index] = row; }

        /// <summary>Shows the grid and returns the chosen rows.</summary>
        /// <param name="options">The options, or null for the defaults.</param>
        /// <param name="cancellationToken">Cancels the grid.</param>
        /// <returns>The result.</returns>
        public GridResult Show(GridOptions? options = null, CancellationToken cancellationToken = default) => GridRunner.Run(this, options ?? new GridOptions(), cancellationToken);
    }

    /// <summary>One row of a <see cref="Grid"/>.</summary>
    public sealed class GridRow {
        /// <summary>Creates a row snapshot for a grid refresh, with its original object and raw column values.</summary>
        public GridRow(object? item, params object?[] values) {
            Item = item;
            Values = values ?? Array.Empty<object?>();
        }

        /// <summary>The object the row stands for.</summary>
        public object? Item { get; }

        /// <summary>Stable row identity, independent of the original object and display position. Defaults to a unique identity; set it before binding a refreshable listing.</summary>
        public object Id { get; set; } = new object();

        /// <summary>The raw column values.</summary>
        public IReadOnlyList<object?> Values { get; }

        internal string[]? Search { get; set; }
    }

    /// <summary>Options for <c>Grid.Show</c>.</summary>
    public sealed partial class GridOptions {
        /// <summary>Commands available through the action palette and their shortcuts. Actions keep the grid open.</summary>
        public IList<AppCommand> Command { get; } = new List<AppCommand>();
        /// <summary>Refreshes each acted-on row on the input thread after successful completion. Return a new snapshot in the same column order; selection and row position are retained. Keep this callback short.</summary>
        public Func<AppCommand, GridRow, GridRow>? RefreshRow { get; set; }
        /// <summary>Optional per-run mailbox, drained on the input thread and closed when the grid exits.</summary>
        public AppDispatcher? Dispatcher { get; set; }
        /// <summary>Prompt options (input, capture, theme, labels); null for the defaults.</summary>
        public InteractiveOptions? Interactive { get; set; }

        /// <summary>The filter to start with.</summary>
        public string? Filter { get; set; }

        /// <summary>The column to sort by at the start (by index), or -1 for the input order.</summary>
        public int SortColumn { get; set; } = -1;

        /// <summary>Sorts descending at the start.</summary>
        public bool SortDescending { get; set; }

        /// <summary>Columns hidden at the start (by header).</summary>
        public IList<string> HiddenColumns { get; } = new List<string>();
    }

    /// <summary>What a grid returned.</summary>
    public sealed class GridResult {
        internal GridResult(bool accepted, IReadOnlyList<object?> items) {
            Accepted = accepted;
            Items = items;
        }

        /// <summary>Whether the grid ended with "done" (false when it was cancelled).</summary>
        public bool Accepted { get; }

        /// <summary>The objects of the chosen rows, in display order.</summary>
        public IReadOnlyList<object?> Items { get; }
    }
}
