namespace ConsoleMonster.Widgets {
    /// <summary>How horizontal bars are drawn.</summary>
    public enum BarShape {
        /// <summary>
        /// Solid blocks (charts): three-quarter height (<c>▆</c>) on Full and half height (<c>▄</c>) on Basic, so stacked
        /// rows keep a visible gap; <c>#</c> on Ascii. Lengths round to whole cells.
        /// </summary>
        Block = 0,

        /// <summary>
        /// A thin heavy line (<c>━</c>) with half-cell precision on the Full tier, leaving visible space between stacked
        /// rows (progress, lanes). Other tiers draw it like <see cref="Block"/>.
        /// </summary>
        Line
    }
}
