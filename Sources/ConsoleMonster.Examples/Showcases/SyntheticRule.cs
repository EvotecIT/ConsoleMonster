namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// A synthetic assessment rule used by the parallel-run recording. Names and reasons are generic examples written for
    /// the showcase, not product or lab data; each rule carries its own failure and warning wording so log lines read coherently.
    /// </summary>
    public sealed class SyntheticRule {
        /// <summary>Creates a rule.</summary>
        /// <param name="category">The category.</param>
        /// <param name="name">The rule name.</param>
        /// <param name="failReason">Why the rule fails.</param>
        /// <param name="warnReason">Why the rule warns.</param>
        /// <param name="weight">Relative duration.</param>
        public SyntheticRule(string category, string name, string failReason, string warnReason, double weight = 1) {
            Category = category;
            Name = name;
            FailReason = failReason;
            WarnReason = warnReason;
            Weight = weight;
        }

        /// <summary>The category.</summary>
        public string Category { get; }

        /// <summary>The rule name.</summary>
        public string Name { get; }

        /// <summary>Why the rule fails.</summary>
        public string FailReason { get; }

        /// <summary>Why the rule warns.</summary>
        public string WarnReason { get; }

        /// <summary>Relative duration.</summary>
        public double Weight { get; }
    }
}
