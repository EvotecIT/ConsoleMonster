using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    /// <summary>A table column definition.</summary>
    public sealed class TableColumn {
        /// <summary>Creates a column.</summary>
        /// <param name="header">The header (markup).</param>
        public TableColumn(string header) {
            Header = header ?? string.Empty;
        }

        /// <summary>The header (markup).</summary>
        public string Header { get; set; }

        /// <summary>Alignment; <c>null</c> = automatic (right for numeric columns, left otherwise). The header follows it.</summary>
        public HorizontalAlignment? Alignment { get; set; }

        /// <summary>Whether values may wrap onto several lines when space is short (default true). Identifiers usually set false.</summary>
        public bool Wrap { get; set; } = true;

        /// <summary>Whether this column takes spare width when the table expands (the main text column).</summary>
        public bool Flexible { get; set; }

        /// <summary>A minimum width, or <c>null</c>.</summary>
        public int? MinWidth { get; set; }

        /// <summary>A maximum width, or <c>null</c>.</summary>
        public int? MaxWidth { get; set; }

        /// <summary>A fixed width, or <c>null</c>.</summary>
        public int? Width { get; set; }
    }
}
