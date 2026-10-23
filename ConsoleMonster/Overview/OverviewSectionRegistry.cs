using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Overview {
    /// <summary>Thread-safe ordered registry of read-only overview probes.</summary>
    public sealed class OverviewSectionRegistry {
        private readonly object _gate = new object();
        private readonly List<OverviewProbe> _probes = new List<OverviewProbe>();

        /// <summary>The process-wide built-in registry, shared to bound concurrent native probes.</summary>
        public static OverviewSectionRegistry Default { get; } = CreateDefault();

        /// <summary>Returns the registered selection keys in display order.</summary>
        public IReadOnlyList<string> Names {
            get { lock (_gate) { return Array.AsReadOnly(_probes.Select(probe => probe.Name).ToArray()); } }
        }

        /// <summary>Registers a section without replacing an existing provider.</summary>
        /// <param name="name">A case-insensitive selection key.</param>
        /// <param name="title">A plain-text panel title.</param>
        /// <param name="collect">A read-only provider returning null when unavailable. It must honor cancellation and release its resources. A non-cooperative delegate cannot be forcibly stopped; the registry retains its single in-flight slot until it returns.</param>
        /// <returns>This registry.</returns>
        public OverviewSectionRegistry Register(string name, string title, Func<CancellationToken, Task<OverviewSection?>> collect) {
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("A section name is required.", nameof(name)); }
            if (title == null) { throw new ArgumentNullException(nameof(title)); }
            if (collect == null) { throw new ArgumentNullException(nameof(collect)); }
            lock (_gate) {
                if (_probes.Any(probe => string.Equals(probe.Name, name, StringComparison.OrdinalIgnoreCase))) {
                    throw new ArgumentException("The section is already registered: " + name, nameof(name));
                }
                _probes.Add(new OverviewProbe(name, title, collect));
            }
            return this;
        }

        internal OverviewProbe[] Select(IReadOnlyList<string>? names) {
            lock (_gate) {
                if (names == null) { return _probes.ToArray(); }
                List<OverviewProbe> selected = new List<OverviewProbe>();
                foreach (string name in names) {
                    OverviewProbe? probe = _probes.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (probe == null) { throw new ArgumentException("Unknown overview section: " + name, nameof(names)); }
                    if (!selected.Contains(probe)) { selected.Add(probe); }
                }
                return selected.ToArray();
            }
        }

        private static OverviewSectionRegistry CreateDefault() => new OverviewSectionRegistry()
            .Register("Resources", "CPU / memory", OverviewPlatform.ResourcesAsync)
            .Register("Disks", "Disks", token => OverviewPlatform.Run(OverviewPlatform.Disks, token))
            .Register("Network", "Network", token => OverviewPlatform.Run(OverviewPlatform.Network, token))
            .Register("Services", "Key services", token => OverviewPlatform.Run(OverviewPlatform.Services, token))
            .Register("Updates", "Updates / reboot", token => OverviewPlatform.Run(OverviewPlatform.Updates, token))
            .Register("Security", "Security (24 h)", token => OverviewPlatform.Run(OverviewSecurity.Collect, token));
    }
}
