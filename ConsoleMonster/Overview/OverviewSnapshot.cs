using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Overview {
    /// <summary>A point-in-time host summary, independent of terminal rendering.</summary>
    public sealed class OverviewSnapshot {
        /// <summary>Creates a snapshot, including synthetic or application-provided data.</summary>
        /// <param name="hostName">The host's display name.</param>
        /// <param name="operatingSystem">The OS and version description.</param>
        /// <param name="uptime">Time since boot, if available.</param>
        /// <param name="capturedAt">The local collection timestamp.</param>
        /// <param name="sections">Ordered sections, copied on construction.</param>
        public OverviewSnapshot(string hostName, string operatingSystem, TimeSpan? uptime, DateTimeOffset capturedAt, IEnumerable<OverviewSection> sections) {
            HostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            OperatingSystem = operatingSystem ?? throw new ArgumentNullException(nameof(operatingSystem));
            Uptime = uptime;
            CapturedAt = capturedAt;
            OverviewSection[] copy = (sections ?? throw new ArgumentNullException(nameof(sections))).ToArray();
            if (copy.Any(section => section == null)) { throw new ArgumentException("Sections cannot be null.", nameof(sections)); }
            Sections = Array.AsReadOnly(copy);
        }

        /// <summary>The host display name.</summary>
        public string HostName { get; }
        /// <summary>The OS and version description.</summary>
        public string OperatingSystem { get; }
        /// <summary>Time since boot when a supported facility is available.</summary>
        public TimeSpan? Uptime { get; }
        /// <summary>The local collection timestamp.</summary>
        public DateTimeOffset CapturedAt { get; }
        /// <summary>The observations and pending markers, in registry or selected order.</summary>
        public IReadOnlyList<OverviewSection> Sections { get; }
    }
}
