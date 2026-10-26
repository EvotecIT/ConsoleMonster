using System;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>An immutable background-operation progress snapshot, independent of any control.</summary>
    public sealed class AppProgress {
        /// <summary>Creates a snapshot. A zero total is indeterminate.</summary>
        public AppProgress(string message, double completed = 0, double total = 0, string? detail = null) {
            if (double.IsNaN(completed) || double.IsInfinity(completed) || completed < 0 || double.IsNaN(total) || double.IsInfinity(total) || total < 0) { throw new ArgumentOutOfRangeException(nameof(completed)); }
            Message = message ?? string.Empty; Completed = completed; Total = total; Detail = detail;
        }
        /// <summary>The current file, step or activity.</summary>
        public string Message { get; }
        /// <summary>Completed units.</summary>
        public double Completed { get; }
        /// <summary>Total units; zero indicates an unknown total.</summary>
        public double Total { get; }
        /// <summary>Optional rate, time-left or backend-specific detail.</summary>
        public string? Detail { get; }
    }

    /// <summary>A completed operation's summary. The backend supplies a retry command over only the failed work.</summary>
    public sealed class AppOperationSummary {
        /// <summary>Creates an immutable summary, copying failures before publishing it to the UI.</summary>
        public AppOperationSummary(string message, IEnumerable<string>? failures = null, AppCommand? retry = null) {
            Message = message ?? string.Empty;
            Failures = Array.AsReadOnly(new List<string>(failures ?? Array.Empty<string>()).ToArray());
            Retry = retry;
        }
        /// <summary>The final outcome.</summary>
        public string Message { get; }
        /// <summary>Failure descriptions in backend order.</summary>
        public IReadOnlyList<string> Failures { get; }
        /// <summary>An explicit retry over failed items; null offers Close/Skip only.</summary>
        public AppCommand? Retry { get; }
    }
}
