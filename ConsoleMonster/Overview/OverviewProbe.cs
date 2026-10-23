using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Overview {
    // One slot per registered provider bounds even a non-cooperative native call across repeated collections.
    internal sealed class OverviewProbe {
        private readonly object _gate = new object();
        private readonly Func<CancellationToken, Task<OverviewSection?>> _collect;
        private Task<OverviewSection?>? _flight;

        internal OverviewProbe(string name, string title, Func<CancellationToken, Task<OverviewSection?>> collect) {
            Name = name;
            Title = title;
            _collect = collect;
        }

        internal string Name { get; }
        internal string Title { get; }

        internal Task<OverviewSection?> Start(TimeSpan timeout) {
            lock (_gate) {
                if (_flight == null || _flight.IsCompleted) {
                    _flight = Task.Run(async () => {
                        using (CancellationTokenSource stop = new CancellationTokenSource(timeout)) {
                            try {
                                OverviewSection? result = await _collect(stop.Token).ConfigureAwait(false);
                                if (stop.IsCancellationRequested) { return Pending(); }
                                return result == null ? null : new OverviewSection(Name, Title, result.Items, result.IsPending);
                            } catch (OperationCanceledException) {
                                return Pending();
                            } catch (Exception ex) when (!(ex is OutOfMemoryException)) {
                                // Access denied, missing optional facilities and platform failures hide the section.
                                // Provider exception text can contain local paths or account data, so it is not displayed.
                                return null;
                            }
                        }
                    });
                }
                return _flight;
            }
        }

        internal OverviewSection Pending() => new OverviewSection(Name, Title, Array.Empty<OverviewItem>(), true);
    }
}
