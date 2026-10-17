namespace ConsoleMonster.Data {
    /// <summary>A scalar value, including null and values without a registered member source.</summary>
    public sealed class InspectScalar : InspectNode {
        /// <summary>Creates a scalar node.</summary>
        public InspectScalar(string? name, string typeName, string path, object? value)
            : base(name, typeName, InspectNodeKind.Scalar, path) {
            Value = value;
        }

        /// <summary>The original scalar value.</summary>
        public object? Value { get; }
    }
}
