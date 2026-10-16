using System;
using System.Collections.Generic;

namespace ConsoleMonster.Data {
    /// <summary>Controls depth, collection, member filtering and member source selection.</summary>
    public sealed class InspectorOptions {
        /// <summary>Maximum node depth below the root.</summary>
        public int MaxDepth { get; set; } = 4;

        /// <summary>Maximum collection items listed.</summary>
        public int MaxItems { get; set; } = 25;

        /// <summary>Maximum tabular columns exposed.</summary>
        public int MaxColumns { get; set; } = 8;

        /// <summary>Optional member-name wildcard allow list.</summary>
        public IList<string> Include { get; } = new List<string>();

        /// <summary>Optional member-name wildcard deny list.</summary>
        public IList<string> Exclude { get; } = new List<string>();

        /// <summary>Member names to place first, in listed order.</summary>
        public IList<string> Order { get; } = new List<string>();

        /// <summary>Whether public-property reflection is used as the final built-in source.</summary>
        public bool UseReflection { get; set; } = true;

        /// <summary>Custom sources, consulted before built-in sources.</summary>
        public IList<IMemberSource> Sources { get; } = new List<IMemberSource>();

        /// <summary>
        /// Optional host callback applied to each non-null value before the inspector classifies it.
        /// Use this to strip host wrapper objects while preserving the wrapped value's identity for cycle detection.
        /// </summary>
        public Func<object, object>? Unwrap { get; set; }
    }
}
