using System;
using System.Collections.Generic;

namespace ConsoleMonster.Data {
    /// <summary>Controls depth, collection, member filtering and member source selection.</summary>
    public sealed class InspectorOptions {
        /// <summary>Maximum node depth below the root.</summary>
        public int MaxDepth { get; set; } = 4;

        /// <summary>
        /// The depth a tree starts with for an object when none is asked for: wide objects (more than
        /// <see cref="WideObjectFields"/> members, such as a process or a file) open one level, since every nested level
        /// multiplies their size; small records and parsed JSON open <paramref name="narrowDepth"/> levels.
        /// </summary>
        /// <param name="fieldCount">The number of members the object shows.</param>
        /// <param name="narrowDepth">The depth for objects that are not wide (default 3).</param>
        /// <returns>The depth.</returns>
        public static int DefaultDepthFor(int fieldCount, int narrowDepth = 3) => fieldCount > WideObjectFields ? 1 : narrowDepth;

        /// <summary>The member count above which an object counts as wide for <see cref="DefaultDepthFor"/> (12).</summary>
        public const int WideObjectFields = 12;

        /// <summary>Maximum collection items listed.</summary>
        public int MaxItems { get; set; } = 25;

        /// <summary>Maximum tabular columns exposed.</summary>
        public int MaxColumns { get; set; } = 8;

        /// <summary>
        /// Maximum nodes in one inspection (default 5,000). Wide objects grow as members to the power of depth; once the
        /// budget is spent, the inspector adds one "node limit reached" marker and stops reading further members and items.
        /// </summary>
        public int MaxNodes { get; set; } = 5000;

        /// <summary>Optional member-name wildcard allow list.</summary>
        public IList<string> Include { get; } = new List<string>();

        /// <summary>
        /// Optional member-name wildcard deny list. Excluded members are never read, so this is also the skip list for
        /// getters that are slow, block or have side effects.
        /// </summary>
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

        /// <summary>
        /// Optional host callback that gives the identity of a value for cycle detection (for example the object a host
        /// wrapper wraps, since a new wrapper can be created for every read), or null to use the value itself.
        /// </summary>
        public Func<object, object>? Identity { get; set; }

        /// <summary>Optional identity label for objects beyond the depth limit. Return null when no useful label exists.</summary>
        internal Func<object, Values.ShapeBudget, string?>? DepthLimitLabel { get; set; }

        /// <summary>Optional shared budget used for identity reads and inspection.</summary>
        internal Values.ShapeBudget? Budget { get; set; }

        /// <summary>How long one inspection may spend reading values (default 2 seconds; zero or less for no limit).</summary>
        public TimeSpan TimeBudget { get; set; } = TimeSpan.FromSeconds(2);
    }
}
