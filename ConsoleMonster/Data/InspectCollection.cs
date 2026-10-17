using System.Collections.Generic;

namespace ConsoleMonster.Data {
    /// <summary>An enumerable represented by its listed items and optional tabular columns.</summary>
    public sealed class InspectCollection : InspectNode {
        /// <summary>Creates a collection node.</summary>
        public InspectCollection(
            string? name,
            string typeName,
            string path,
            IReadOnlyList<InspectNode> items,
            int? count,
            int omitted,
            IReadOnlyList<string>? columns,
            int hiddenColumns
        )
            : base(name, typeName, InspectNodeKind.Collection, path) {
            Items = items;
            Count = count;
            Omitted = omitted;
            Columns = columns;
            HiddenColumns = hiddenColumns;
        }

        /// <summary>Listed item nodes.</summary>
        public IReadOnlyList<InspectNode> Items { get; }

        /// <summary>Total count when available.</summary>
        public int? Count { get; }

        /// <summary>Unlisted item count, or -1 when additional items are known but total is unknown.</summary>
        public int Omitted { get; }

        /// <summary>Tabular member columns, or null for ordinary collections.</summary>
        public IReadOnlyList<string>? Columns { get; }

        /// <summary>Number of tabular columns hidden by the configured cap.</summary>
        public int HiddenColumns { get; }
    }
}
