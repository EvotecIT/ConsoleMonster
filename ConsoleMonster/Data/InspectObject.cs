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

        // The inspected object's own type (a FileInfo or MemoryStream behind a PowerShell wrapper), for presenters that depend
        // on the source, such as a bare Length being a byte count.
        internal System.Type? SourceType { get; set; }
    }
}
