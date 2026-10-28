namespace ConsoleMonster.Widgets {
    /// <summary>Table border styles. Box styles fall back per glyph tier like panels.</summary>
    public enum TableBorder {
        /// <summary>A rounded box with column separators.</summary>
        Rounded = 0,

        /// <summary>A square box with column separators.</summary>
        Square,

        /// <summary>A double-line box.</summary>
        Double,

        /// <summary>No outer box: bold header, a thin rule under it, columns separated by spaces.</summary>
        Minimal,

        /// <summary>No lines at all; columns separated by spaces.</summary>
        None,

        /// <summary>Borderless data with muted headers and one subtle rule segment per column.</summary>
        Rule
    }
}
