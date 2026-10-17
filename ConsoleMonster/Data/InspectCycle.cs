namespace ConsoleMonster.Data {
    /// <summary>A reference to an object already present on the current ancestor path.</summary>
    public sealed class InspectCycle : InspectNode {
        /// <summary>Creates a cycle node.</summary>
        public InspectCycle(string? name, string typeName, string path, string targetPath)
            : base(name, typeName, InspectNodeKind.Cycle, path) {
            TargetPath = targetPath;
        }

        /// <summary>Path of the ancestor this node references.</summary>
        public string TargetPath { get; }
    }
}
