namespace ConsoleMonster.Widgets {
    /// <summary>The kind of a <see cref="RecordView"/> item.</summary>
    public enum RecordItemKind {
        /// <summary>A section or group heading.</summary>
        Heading = 0,

        /// <summary>A label and value.</summary>
        Field,

        /// <summary>A nested widget (a table, a tree...).</summary>
        Widget
    }
}
