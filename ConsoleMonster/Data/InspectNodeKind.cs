namespace ConsoleMonster.Data {
    /// <summary>Describes how an inspected value is represented.</summary>
    public enum InspectNodeKind {
        /// <summary>A leaf value.</summary>
        Scalar,

        /// <summary>An object with members.</summary>
        Object,

        /// <summary>An enumerable sequence.</summary>
        Collection,

        /// <summary>A reference to an ancestor.</summary>
        Cycle,

        /// <summary>A value beyond the configured depth.</summary>
        Limit,

        /// <summary>A member getter failed.</summary>
        Error,
    }
}
