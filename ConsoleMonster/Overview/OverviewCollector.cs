using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Overview {
    /// <summary>Collects a host snapshot without waiting past the caller budget for optional probes.</summary>
    public sealed class OverviewCollector {
        private readonly OverviewSectionRegistry _registry;

        /// <summary>Creates a collector using the process-wide built-ins or an application registry.</summary>
        /// <param name="registry">A registry; null uses the built-ins.</param>
        public OverviewCollector(OverviewSectionRegistry? registry = null) {
            _registry = registry ?? OverviewSectionRegistry.Default;
        }

        /// <summary>Collects selected sections concurrently, returning pending markers for slow probes.</summary>
        /// <param name="options">Selection and latency limits; null uses defaults.</param>
        /// <param name="cancellationToken">Cancels the caller's wait. Shared probes retain their own timeout.</param>
        /// <returns>An immutable snapshot; missing or inaccessible sections are omitted.</returns>
        /// <remarks>The budget bounds waiting, not OS scheduling or rendering. Probes receive at most a two-second cooperative timeout. No child processes or privileged operations are used.</remarks>
        public async Task<OverviewSnapshot> CollectAsync(OverviewOptions? options = null, CancellationToken cancellationToken = default) {
            options = options ?? new OverviewOptions();
            options.Validate();
            cancellationToken.ThrowIfCancellationRequested();
            Stopwatch elapsed = Stopwatch.StartNew();
            OverviewProbe[] probes = _registry.Select(options.Sections);
            Task<OverviewSection?>[] tasks = new Task<OverviewSection?>[probes.Length];
            for (int i = 0; i < probes.Length; i++) { tasks[i] = probes[i].Start(options.ProbeTimeout); }
            // Never attach a per-caller continuation to a shared flight: an uncooperative provider could
            // otherwise retain every expired caller's aggregate and task array indefinitely.
            while (Array.Exists(tasks, task => !task.IsCompleted)) {
                TimeSpan remaining = options.CollectionBudget - elapsed.Elapsed;
                if (remaining <= TimeSpan.Zero) { break; }
                await Task.Delay(remaining < TimeSpan.FromMilliseconds(5) ? remaining : TimeSpan.FromMilliseconds(5), cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            List<OverviewSection> sections = new List<OverviewSection>();
            for (int i = 0; i < probes.Length; i++) {
                OverviewSection? section = tasks[i].Status == TaskStatus.RanToCompletion ? tasks[i].Result : probes[i].Pending();
                if (section != null && (section.IsPending || section.Items.Count > 0)) { sections.Add(section); }
            }
            return new OverviewSnapshot(Environment.MachineName, RuntimeInformation.OSDescription, OverviewPlatform.Uptime(), DateTimeOffset.Now, sections);
        }
    }
}
