namespace ConsoleMonster.Widgets {
    /// <summary>Ordering of bars or sparkline groups before applying a row limit.</summary>
    public enum PipelineChartSort {
        /// <summary>Retain sample order, or the order in which group labels first appear.</summary>
        None,
        /// <summary>Lowest value first; sparkline groups are compared by their latest value.</summary>
        Ascending,
        /// <summary>Highest value first; sparkline groups are compared by their latest value.</summary>
        Descending
    }
}
