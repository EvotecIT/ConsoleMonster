using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Overview {
    /// <summary>An immutable set of observations from one probe; empty sections are hidden.</summary>
    public sealed class OverviewSection {
        /// <summary>Creates a section from collected observations.</summary>
        /// <param name="name">The stable registry key used by section selection.</param>
        /// <param name="title">The plain-text panel title.</param>
        /// <param name="items">The observations, copied on construction.</param>
        /// <param name="isPending">Whether collection has not completed within its budget.</param>
        public OverviewSection(string name, string title, IEnumerable<OverviewItem> items, bool isPending = false) {
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("A section name is required.", nameof(name)); }
            Name = name;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            OverviewItem[] copy = (items ?? throw new ArgumentNullException(nameof(items))).ToArray();
            if (copy.Any(item => item == null)) { throw new ArgumentException("Observations cannot be null.", nameof(items)); }
            Items = Array.AsReadOnly(copy);
            IsPending = isPending;
        }

        /// <summary>The registry key.</summary>
        public string Name { get; }
        /// <summary>The plain-text panel title.</summary>
        public string Title { get; }
        /// <summary>The collected observations, in display order.</summary>
        public IReadOnlyList<OverviewItem> Items { get; }
        /// <summary>True when the probe was still running or timed out.</summary>
        public bool IsPending { get; }
    }
}
