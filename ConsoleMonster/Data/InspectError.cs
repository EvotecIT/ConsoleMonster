namespace ConsoleMonster.Data {
    /// <summary>A member whose getter threw while being inspected.</summary>
    public sealed class InspectError : InspectNode {
        /// <summary>Creates an error node.</summary>
        public InspectError(string? name, string typeName, string path, string message)
            : base(name, typeName, InspectNodeKind.Error, path) {
            Message = message;
        }

        /// <summary>Getter failure message.</summary>
        public string Message { get; }
    }
}
