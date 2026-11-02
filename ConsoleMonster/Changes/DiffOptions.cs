using System;
using System.Collections.Generic;
using ConsoleMonster.Data;

namespace ConsoleMonster.Changes {
    /// <summary>Options for <see cref="ObjectDiff"/>.</summary>
    public sealed class DiffOptions {
        /// <summary>The members whose values identify an object (like Compare-Object -Property for matching). Empty: Id, Name, Key...</summary>
        public IList<string> Key { get; } = new List<string>();

        /// <summary>A function that returns an object's key, instead of <see cref="Key"/>.</summary>
        public Func<object, object?>? KeySelector { get; set; }

        /// <summary>The members to compare, or empty for all of them.</summary>
        public IList<string> Properties { get; } = new List<string>();

        /// <summary>Members (or paths such as <c>Address.City</c>, with <c>*</c> wildcards) left out of the comparison.</summary>
        public IList<string> Ignore { get; } = new List<string>();

        /// <summary>How deep nested objects are compared (default 3).</summary>
        public int Depth { get; set; } = 3;

        /// <summary>Whether unchanged objects are listed too (they are always counted).</summary>
        public bool IncludeUnchanged { get; set; }

        /// <summary>Turns a host wrapper into the value to compare (for example a PowerShell object around a dictionary), or null.</summary>
        public Func<object, object>? Unwrap { get; set; }

        /// <summary>How long getters may stall in one comparison before slow members stop being read (default 2 seconds).</summary>
        public TimeSpan TimeBudget { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>Compare string values and match keys with ordinal case sensitivity. Default false.</summary>
        public bool CaseSensitive { get; set; }

        /// <summary>Comparison-only transforms keyed by exact property path (case-insensitive lookup). Display keeps original values.</summary>
        public IDictionary<string, Func<object?, object?>> Normalize { get; } = new Dictionary<string, Func<object?, object?>>(StringComparer.OrdinalIgnoreCase);

        // The stall guard of the comparison in progress, and the field cache shared by every read in it.
        internal Values.ShapeBudget? Budget { get; set; }

        internal Values.ShapeOptions? Shape { get; set; }

        // Member paths skipped in the current comparison because reading them stalled.
        internal List<string> NotCompared { get; } = new List<string>();

        /// <summary>Member sources tried before reflection (PowerShell objects, dictionaries, JSON).</summary>
        public IList<IMemberSource> Sources { get; } = new List<IMemberSource>();
    }

}
