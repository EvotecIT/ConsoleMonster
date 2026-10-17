namespace ConsoleMonster.Widgets {
    // The generated half of TableFluent holds one With/Add method per property; these are the hand-written semantic helpers.
    public static partial class TableFluent {
        /// <summary>
        /// Adds a totals row that sums every numeric column (<see cref="Table.ShowTotals"/> with <see cref="Table.AutoTotals"/>);
        /// columns with their own <see cref="TableColumn.Aggregate"/> keep it.
        /// </summary>
        /// <typeparam name="TSelf">The table type.</typeparam>
        /// <param name="table">The table.</param>
        /// <param name="label">The label of the totals row, or null for the built-in word.</param>
        /// <returns>The same table.</returns>
        public static TSelf WithTotals<TSelf>(this TSelf table, string? label = null) where TSelf : Table {
            table.ShowTotals = true;
            table.AutoTotals = true;
            if (label != null) {
                table.TotalLabel = label;
            }
            return table;
        }

        /// <summary>Adds a column with a header and an optional configuration.</summary>
        /// <typeparam name="TSelf">The table type.</typeparam>
        /// <param name="table">The table.</param>
        /// <param name="header">The header (markup).</param>
        /// <param name="configure">Configures the column (priority, presenter, overflow...), or null.</param>
        /// <returns>The same table.</returns>
        public static TSelf AddColumn<TSelf>(this TSelf table, string header, System.Action<TableColumn> configure) where TSelf : Table {
            TableColumn column = new TableColumn(header);
            configure?.Invoke(column);
            table.AddColumn(column);
            return table;
        }
    }
}
