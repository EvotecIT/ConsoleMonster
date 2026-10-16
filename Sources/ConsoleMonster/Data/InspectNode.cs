namespace ConsoleMonster.Data {
    /// <summary>Base node in the neutral object inspection tree.</summary>
    public abstract class InspectNode {
        /// <summary>Creates a node.</summary>
        protected InspectNode(string? name, string typeName, InspectNodeKind kind, string path) {
            Name = name;
            TypeName = typeName;
            Kind = kind;
            Path = path;
        }

        /// <summary>Member name, or null for the root.</summary>
        public string? Name { get; }

        /// <summary>Short friendly type name.</summary>
        public string TypeName { get; }

        /// <summary>Node representation kind.</summary>
        public InspectNodeKind Kind { get; }

        /// <summary>Path from the root.</summary>
        public string Path { get; }
    }
}
