namespace ConsoleMonster.Widgets {
    /// <summary>The presentation used by a chart of pipeline samples.</summary>
    public enum PipelineChartType {
        /// <summary>One horizontal bar per sample, including its label and value.</summary>
        Bar,
        /// <summary>A distribution counted into automatically spaced buckets.</summary>
        Histogram,
        /// <summary>One trend per label, retaining sample arrival order within each trend.</summary>
        Sparkline
    }
}
