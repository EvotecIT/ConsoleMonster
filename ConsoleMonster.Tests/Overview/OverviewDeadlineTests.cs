using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Overview;
using Xunit;

namespace ConsoleMonster.Tests.Overview {
    public sealed class OverviewDeadlineTests {
        [Fact]
        public async Task RepeatedDeadlinesReleaseCompletedDataWhileCancellationLeavesOtherCallerRunning() {
            TaskCompletionSource<bool> release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<WeakReference> observations = new List<WeakReference>();
            int busyCalls = 0;
            OverviewSectionRegistry registry = new OverviewSectionRegistry()
                .Register("Busy", "Busy", async _ => {
                    Interlocked.Increment(ref busyCalls);
                    try { await release.Task.ConfigureAwait(false); return new OverviewSection("Busy", "Busy", new[] { new OverviewItem("Result", "Synthetic") }); }
                    finally { exited.TrySetResult(true); }
                })
                .Register("Fast", "Fast", _ => {
                    OverviewItem item = new OverviewItem("Transient", "Synthetic");
                    lock (observations) { observations.Add(new WeakReference(item)); }
                    return Task.FromResult<OverviewSection?>(new OverviewSection("Fast", "Fast", new[] { item }));
                });
            OverviewCollector collector = new OverviewCollector(registry);
            try {
                for (int i = 0; i < 16; i++) {
                    OverviewSnapshot snapshot = await collector.CollectAsync(new OverviewOptions { CollectionBudget = TimeSpan.FromMilliseconds(15) });
                    Assert.True(snapshot.Sections.Single(section => section.Name == "Busy").IsPending);
                }
                Assert.Equal(1, busyCalls);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                lock (observations) {
                    Assert.InRange(observations.Count, 8, 16);
                    // A hanging shared flight must not retain every completed sibling's result through per-call aggregates.
                    Assert.InRange(observations.Count(reference => reference.IsAlive), 0, 2);
                }
                using (CancellationTokenSource stop = new CancellationTokenSource()) {
                    Task<OverviewSnapshot> cancelled = collector.CollectAsync(cancellationToken: stop.Token);
                    Task<OverviewSnapshot> continuing = collector.CollectAsync();
                    stop.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
                    Assert.False(continuing.IsCompleted);
                    release.TrySetResult(true);
                    OverviewSnapshot finished = await continuing;
                    Assert.False(finished.Sections.Single(section => section.Name == "Busy").IsPending);
                    Assert.Equal(1, busyCalls);
                }
            } finally {
                release.TrySetResult(true);
                Assert.Same(exited.Task, await Task.WhenAny(exited.Task, Task.Delay(2000)));
            }
        }
    }
}
