using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A table column definition: header, width rules, overflow, priority (which columns hide first when space is short),
    /// a presenter for raw values, tone rules and how the column is summarised in subtotals and totals. The same column
    /// definitions drive record cards and record views, so a dataset is described once.
    /// </summary>
    public sealed class TableColumn {
        private ColumnOverflow _overflow = ColumnOverflow.Wrap;
        private int _maxLines;

        /// <summary>Creates a column.</summary>
        /// <param name="header">The header (markup).</param>
        public TableColumn(string header) {
            Header = header ?? string.Empty;
        }

        /// <summary>Creates a column with a multicolour header.</summary>
        /// <param name="header">The header.</param>
        public TableColumn(Text.RichText header) {
            Header = (header ?? throw new System.ArgumentNullException(nameof(header))).ToMarkup();
        }

        /// <summary>The header (markup).</summary>
        public string Header { get; set; }

        /// <summary>An identifier for the column (a property name), or null to use the header.</summary>
        public string? Key { get; set; }

        /// <summary>Alignment; <c>null</c> = automatic (right for numeric columns, left otherwise). The header follows it.</summary>
        public HorizontalAlignment? Alignment { get; set; }

        /// <summary>
        /// Whether values may wrap onto several lines when space is short (default true). Setting it maps to
        /// <see cref="Overflow"/>: true is <see cref="ColumnOverflow.Wrap"/>, false is <see cref="ColumnOverflow.Never"/> (identifiers keep their width while other columns give way).
        /// </summary>
        public bool Wrap {
            get => _overflow == ColumnOverflow.Wrap;
            set => _overflow = value ? ColumnOverflow.Wrap : ColumnOverflow.Never;
        }

        /// <summary>What happens to values wider than the column: wrap (default), truncate at the end, truncate in the middle, or never shorten.</summary>
        public ColumnOverflow Overflow {
            get => _overflow;
            set => _overflow = value;
        }

        /// <summary>A <see cref="MaxLines"/> value that shows every line of the column's values, whatever the table's <see cref="Table.WrapLines"/>.</summary>
        public const int Unlimited = int.MaxValue;

        /// <summary>The most lines a wrapped value may use: 0 (the default) follows the table's <see cref="Table.WrapLines"/> (3 unless changed), <see cref="Unlimited"/> shows every line. The last line ends with an ellipsis when cut.</summary>
        public int MaxLines {
            get => _maxLines;
            set => _maxLines = value < 0 ? 0 : value;
        }

        /// <summary>
        /// The column's importance when the table is too wide: columns with the lowest priority hide first (ties hide from
        /// the right). The default is 0. Key columns never hide.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>Whether this is a key column: it never hides, and it titles the record cards.</summary>
        public bool IsKey { get; set; }

        /// <summary>Whether this column takes spare width when the table expands (the main text column).</summary>
        public bool Flexible { get; set; }

        /// <summary>A minimum width, or <c>null</c>.</summary>
        public int? MinWidth { get; set; }

        /// <summary>A maximum width, or <c>null</c>.</summary>
        public int? MaxWidth { get; set; }

        /// <summary>A fixed width, or <c>null</c>.</summary>
        public int? Width { get; set; }

        /// <summary>How raw cell values are shown, or null for automatic (by value type). Markup cells are shown as written.</summary>
        public ICellPresenter? Presenter { get; set; }

        /// <summary>Conditional tone rules for raw values; the first matching rule wins.</summary>
        public IList<ToneRule> ToneRules { get; } = new List<ToneRule>();

        /// <summary>How the column is summarised in group subtotals and the totals row.</summary>
        public ColumnAggregate Aggregate { get; set; }

        /// <summary>The identifier: <see cref="Key"/>, or the header without markup.</summary>
        public string Id => Key ?? Text.Markup.Strip(Header);
    }
}
