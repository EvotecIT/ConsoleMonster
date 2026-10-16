namespace ConsoleMonster.Presenters {
    /// <summary>A small graphic drawn in a cell next to (or instead of) its text.</summary>
    public enum CellVisualKind {
        /// <summary>Text only.</summary>
        None = 0,

        /// <summary>A horizontal data bar scaled to the column maximum.</summary>
        Bar,

        /// <summary>A one-line sparkline of a series.</summary>
        Sparkline,

        /// <summary>A progress bar for a fraction.</summary>
        Progress
    }
}
