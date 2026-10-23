using System;
using System.Collections.Generic;

namespace ConsoleMonster.Overview {
    /// <summary>Controls section selection and bounded collection latency.</summary>
    public sealed class OverviewOptions {
        /// <summary>Section keys, in requested order; null selects all registered sections.</summary>
        public IReadOnlyList<string>? Sections { get; set; }
        /// <summary>The caller's collection budget, positive and at most 900 ms; default 750 ms.</summary>
        public TimeSpan CollectionBudget { get; set; } = TimeSpan.FromMilliseconds(750);
        /// <summary>The cooperative timeout for each probe, positive and at most two seconds.</summary>
        public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(2);

        internal void Validate() {
            if (CollectionBudget <= TimeSpan.Zero || CollectionBudget > TimeSpan.FromMilliseconds(900)) {
                throw new ArgumentOutOfRangeException(nameof(CollectionBudget), "The collection budget must be positive and at most 900 ms.");
            }
            if (ProbeTimeout <= TimeSpan.Zero || ProbeTimeout > TimeSpan.FromSeconds(2)) {
                throw new ArgumentOutOfRangeException(nameof(ProbeTimeout), "The probe timeout must be positive and at most two seconds.");
            }
        }
    }
}
