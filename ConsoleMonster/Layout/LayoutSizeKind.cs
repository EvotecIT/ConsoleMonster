namespace ConsoleMonster.Layout {
    /// <summary>How a row or column claims space inside <see cref="Rows"/>, <see cref="Columns"/> or <see cref="Grid"/>.</summary>
    public enum LayoutSizeKind {
        /// <summary>As much as the child measures.</summary>
        Auto = 0,

        /// <summary>A fixed number of cells.</summary>
        Fixed,

        /// <summary>A share of the space left after fixed and auto children, by weight.</summary>
        Fraction
    }
}
