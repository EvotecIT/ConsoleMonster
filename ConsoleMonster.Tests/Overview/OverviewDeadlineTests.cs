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
        public async Task RepeatedDeadlinesReleaseCompletedData() {
            TaskCompletionSource<bool> release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> fastRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<WeakReference> observations = new List<WeakReference>();
            int busyCalls = 0;
            OverviewSectionRegistry registry = new OverviewSectionRegistry()
                .Register("Busy", "Busy", async _ => {
                    Interlocked.Increment(ref busyCalls);
                    entered.TrySetResult(true);
                    try { await release.Task.ConfigureAwait(false); return new OverviewSection("Busy", "Busy", new[] { new OverviewItem("Result", "Synthetic") }); }
                    finally { exited.TrySetResult(true); }
                })
                .Register("Fast", "Fast", async _ => {
                    await fastRelease.Task.ConfigureAwait(false);
                    OverviewItem item = new OverviewItem("Transient", "Synthetic");
                    lock (observations) { observations.Add(new WeakReference(item)); }
                    return new OverviewSection("Fast", "Fast", new[] { item });
                });
            OverviewCollector collector = new OverviewCollector(registry);
            // Controlled flights qualify retention independently of the native probe timeout and runner scheduling.
            Task<OverviewSection?> flight = registry.Select(new[] { "Busy" }).Single().Start(Timeout.InfiniteTimeSpan);
            OverviewProbe fast = registry.Select(new[] { "Fast" }).Single();
            try {
                await entered.Task;
                for (int i = 0; i < 16; i++) {
                    fastRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    Task<OverviewSection?> observation = fast.Start(Timeout.InfiniteTimeSpan);
                    try {
                        OverviewSnapshot snapshot = await collector.CollectAsync(new OverviewOptions { CollectionBudget = TimeSpan.FromMilliseconds(15) });
                        Assert.True(snapshot.Sections.Single(section => section.Name == "Busy").IsPending);
                    } finally {
                        // Complete each sibling explicitly after the deadline; none need win a 15 ms scheduling race.
                        fastRelease.TrySetResult(true);
                        await observation;
                    }
                }
                Assert.Equal(1, busyCalls);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                lock (observations) {
                    Assert.Equal(16, observations.Count);
                    // A hanging shared flight must not retain every completed sibling's result through per-call aggregates.
                    Assert.InRange(observations.Count(reference => reference.IsAlive), 0, 2);
                }
            } finally {
                release.TrySetResult(true);
                await flight;
                Assert.True(exited.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task CallerCancellationLeavesOtherCallerRunning() {
            TaskCompletionSource<bool> release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int busyCalls = 0;
            OverviewSectionRegistry registry = new OverviewSectionRegistry().Register("Busy", "Busy", async token => {
                Interlocked.Increment(ref busyCalls);
                entered.TrySetResult(true);
                try {
                    await release.Task.ConfigureAwait(false);
                    Assert.False(token.IsCancellationRequested);
                    return new OverviewSection("Busy", "Busy", new[] { new OverviewItem("Result", "Synthetic") });
                } finally { exited.TrySetResult(true); }
            });
            OverviewCollector collector = new OverviewCollector(registry);
            // This controlled flight tests caller cancellation, not the independent probe deadline.
            Task<OverviewSection?> flight = registry.Select(null).Single().Start(Timeout.InfiniteTimeSpan);
            try {
                // Provider startup can be scheduled after the entire short collection budget on a busy runner.
                await entered.Task;
                using (CancellationTokenSource stop = new CancellationTokenSource()) {
                    Task<OverviewSnapshot> continuing = collector.CollectAsync();
                    Task<OverviewSnapshot> cancelled = collector.CollectAsync(cancellationToken: stop.Token);
                    stop.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
                    Assert.False(flight.IsCompleted);
                    release.TrySetResult(true);
                    OverviewSnapshot finished = await continuing;
                    // A delayed caller may legitimately return a pending snapshot; its shared probe must survive.
                    Assert.Equal("Busy", Assert.Single(finished.Sections).Name);
                    OverviewSection? observed = await flight;
                    Assert.NotNull(observed);
                    Assert.False(observed.IsPending);
                    Assert.Equal("Synthetic", Assert.Single(observed.Items).Value);
                    Assert.Equal(1, busyCalls);
                }
            } finally {
                release.TrySetResult(true);
                await flight;
                Assert.True(exited.Task.IsCompleted);
            }
        }
    }
}
