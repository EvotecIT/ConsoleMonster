using System.Collections.Generic;

namespace ConsoleMonster.Data {
    /// <summary>An object represented by member nodes.</summary>
    public sealed class InspectObject : InspectNode {
        /// <summary>Creates an object node.</summary>
        public InspectObject(
            string? name,
            string typeName,
            string path,
            IReadOnlyList<InspectNode> members
        )
            : base(name, typeName, InspectNodeKind.Object, path) {
            Members = members;
        }

        /// <summary>Inspected members in display order.</summary>
        public IReadOnlyList<InspectNode> Members { get; }
    }
}
