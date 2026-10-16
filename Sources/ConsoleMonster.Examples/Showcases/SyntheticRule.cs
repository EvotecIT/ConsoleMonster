namespace ConsoleMonster.Examples.Showcases {
    /// <summary>A synthetic assessment rule used by the parallel-run recording (names are generic, not product data).</summary>
    public sealed class SyntheticRule {
        /// <summary>Creates a rule.</summary>
        /// <param name="category">The category.</param>
        /// <param name="name">The rule name.</param>
        /// <param name="weight">Relative duration.</param>
        public SyntheticRule(string category, string name, double weight = 1) {
            Category = category;
            Name = name;
            Weight = weight;
        }

        /// <summary>The category.</summary>
        public string Category { get; }

        /// <summary>The rule name.</summary>
        public string Name { get; }

        /// <summary>Relative duration.</summary>
        public double Weight { get; }
    }
}
