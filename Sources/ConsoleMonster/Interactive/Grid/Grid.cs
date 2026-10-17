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
    public sealed class Grid {
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
        public GridSelection Selection { get; set; } = GridSelection.Multiple;

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

        /// <summary>Shows the grid and returns the chosen rows.</summary>
        /// <param name="options">The options, or null for the defaults.</param>
        /// <param name="cancellationToken">Cancels the grid.</param>
        /// <returns>The result.</returns>
        public GridResult Show(GridOptions? options = null, CancellationToken cancellationToken = default) => GridRunner.Run(this, options ?? new GridOptions(), cancellationToken);
    }

    /// <summary>One row of a <see cref="Grid"/>.</summary>
    public sealed class GridRow {
        internal GridRow(object? item, object?[] values) {
            Item = item;
            Values = values;
        }

        /// <summary>The object the row stands for.</summary>
        public object? Item { get; }

        /// <summary>The raw column values.</summary>
        public IReadOnlyList<object?> Values { get; }

        internal string[]? Search { get; set; }
    }

    /// <summary>Options for <see cref="Grid.Show"/>.</summary>
    public sealed class GridOptions {
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
