using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Data;

namespace ConsoleMonster.Values {
    /// <summary>Options for <see cref="ValueShaper"/>: how records are read and how values become text.</summary>
    public sealed class ShapeOptions {
        /// <summary>Record adapters tried before the built-in ones (for example PowerShell objects).</summary>
        public IList<IMemberSource> Sources { get; } = new List<IMemberSource>();

        /// <summary>Turns a wrapper into the value it wraps (for example a PowerShell object into its base object), or null.</summary>
        public Func<object, object>? Unwrap { get; set; }

        /// <summary>
        /// The identity of a value for cycle detection (for example the object a host wrapper wraps, since a new wrapper can be
        /// created for every read), or null to use the value itself.
        /// </summary>
        public Func<object, object>? Identity { get; set; }

        /// <summary>Extra type names for a value (for example PowerShell type names), used to find registered shapers, or null.</summary>
        public Func<object, IEnumerable<string>>? TypeNames { get; set; }

        /// <summary>How collections are written inside cells and titles (default <see cref="CollectionDisplay.Join"/>).</summary>
        public CollectionDisplay Collections { get; set; } = CollectionDisplay.Join;

        /// <summary>The separator for <see cref="CollectionDisplay.Join"/> and <see cref="CollectionDisplay.FirstN"/> (default ", ").</summary>
        public string Separator { get; set; } = ", ";

        /// <summary>How many items a collection shows before the "+N" marker (default 5).</summary>
        public int ItemLimit { get; set; } = 5;

        /// <summary>The culture for numbers and dates, or null for the current culture.</summary>
        public CultureInfo? Culture { get; set; }

        /// <summary>
        /// Whether plain .NET objects may be read through reflection (default true). Callers using trimming must preserve
        /// those public members, or set this to false and supply explicit adapters or shapers. Adapter-only mode does not
        /// make arbitrary caller delegates or adapters trim-safe.
        /// </summary>
        public bool UseReflection { get; set; } = true;

        /// <summary>The most fields a record contributes as default columns (default 12).</summary>
        public int MaxFields { get; set; } = 12;

        /// <summary>How long one shaping pass (a table, a list, a tree) may spend reading values (default 2 seconds).</summary>
        public TimeSpan TimeBudget { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>How many values one shaping pass may read (default 20 000).</summary>
        public int MaxNodes { get; set; } = 20000;

        // Fields per record for the lifetime of these options (one command): enumerating a remote object's members can take
        // seconds, so each record is enumerated once per pass.
        internal Dictionary<object, IReadOnlyList<InspectMember>> FieldCache { get; } = new Dictionary<object, IReadOnlyList<InspectMember>>(ValueShaper.ReferenceIdentity.Instance);

        // What registered shapers turned each object into, for the lifetime of these options (one command).
        internal Dictionary<object, object?> PreparedCache { get; } = new Dictionary<object, object?>(ValueShaper.ReferenceIdentity.Instance);

        /// <summary>The culture to format with: <see cref="Culture"/> or the current culture.</summary>
        public CultureInfo ResolvedCulture => Culture ?? CultureInfo.CurrentCulture;

        /// <summary>A copy with the same adapters, delegates and limits.</summary>
        /// <returns>The copy.</returns>
        public ShapeOptions Clone() {
            ShapeOptions copy = new ShapeOptions {
                Unwrap = Unwrap,
                Identity = Identity,
                TypeNames = TypeNames,
                Collections = Collections,
                Separator = Separator,
                ItemLimit = ItemLimit,
                Culture = Culture,
                UseReflection = UseReflection,
                MaxFields = MaxFields,
                TimeBudget = TimeBudget,
                MaxNodes = MaxNodes
            };
            foreach (IMemberSource source in Sources) {
                copy.Sources.Add(source);
            }
            return copy;
        }
    }
}
