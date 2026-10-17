namespace ConsoleMonster.Styling {
    /// <summary>
    /// A status shared by status lists, lanes, phases, tables and logs. Each status has a theme token and a glyph,
    /// so status is never shown by colour alone.
    /// </summary>
    public enum StatusKind {
        /// <summary>Not started.</summary>
        Pending = 0,

        /// <summary>In progress.</summary>
        Running,

        /// <summary>Succeeded.</summary>
        Pass,

        /// <summary>Failed.</summary>
        Fail,

        /// <summary>Completed with a warning.</summary>
        Warn,

        /// <summary>Skipped or not applicable.</summary>
        Skip,

        /// <summary>Informational.</summary>
        Info
    }
}
