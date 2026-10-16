namespace ConsoleMonster.Widgets {
    /// <summary>How horizontal bars are drawn.</summary>
    public enum BarShape {
        /// <summary>Full-height blocks with eighth-cell precision (charts).</summary>
        Block = 0,

        /// <summary>
        /// A thin heavy line (<c>━</c>) with half-cell precision on the Full tier, leaving visible space between stacked
        /// rows (progress, lanes). Other tiers fall back to <see cref="Block"/>.
        /// </summary>
        Line
    }
}
