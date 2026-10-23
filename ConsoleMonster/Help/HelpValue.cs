namespace ConsoleMonster.Help {
    /// <summary>A named value or value family shown in compact CLI help.</summary>
    public sealed class HelpValue {
        /// <summary>Creates a catalog entry.</summary>
        /// <param name="name">The literal value syntax.</param>
        /// <param name="description">Its description.</param>
        /// <param name="group">An optional group heading.</param>
        public HelpValue(string name, string? description, string? group = null) {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Group = group;
        }
        /// <summary>The literal value syntax.</summary>
        public string Name { get; }
        /// <summary>The description.</summary>
        public string Description { get; }
        /// <summary>The optional group heading.</summary>
        public string? Group { get; }
        /// <summary>An optional dim note displayed separately after the description.</summary>
        public string? Note { get; set; }
    }
}
