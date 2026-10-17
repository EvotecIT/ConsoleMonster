namespace ConsoleMonster.Widgets {
    /// <summary>How a column is summarised in group subtotal rows and the totals row.</summary>
    public enum ColumnAggregate {
        /// <summary>No summary (the cell stays empty).</summary>
        None = 0,

        /// <summary>The sum of the numeric values.</summary>
        Sum,

        /// <summary>The number of non-empty values.</summary>
        Count,

        /// <summary>The mean of the numeric values.</summary>
        Average,

        /// <summary>The smallest numeric value.</summary>
        Min,

        /// <summary>The largest numeric value.</summary>
        Max
    }
}
