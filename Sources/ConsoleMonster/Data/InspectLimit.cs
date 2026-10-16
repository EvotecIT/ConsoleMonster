namespace ConsoleMonster.Data {
    /// <summary>A value summarized because the configured depth was reached.</summary>
    public sealed class InspectLimit : InspectNode {
        /// <summary>Creates a depth-limit node.</summary>
        public InspectLimit(string? name, string typeName, string path, string summary)
            : base(name, typeName, InspectNodeKind.Limit, path) {
            Summary = summary;
        }

        /// <summary>One-line summary of the omitted value.</summary>
        public string Summary { get; }
    }
}
