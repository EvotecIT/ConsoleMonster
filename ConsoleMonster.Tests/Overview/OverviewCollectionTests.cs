using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Overview;
using Xunit;

namespace ConsoleMonster.Tests.Overview {
    public sealed class OverviewCollectionTests {
        [Fact]
        public async Task SelectionPreservesRequestedOrderAndHidesUnavailableSections() {
            OverviewSectionRegistry registry = new OverviewSectionRegistry()
                .Register("First", "First panel", _ => Task.FromResult<OverviewSection?>(Section("provider-name")))
                .Register("Unavailable", "Unavailable", _ => Task.FromResult<OverviewSection?>(null))
                .Register("Empty", "Empty", _ => Task.FromResult<OverviewSection?>(new OverviewSection("Empty", "Empty", Array.Empty<OverviewItem>())))
                .Register("Last", "Last panel", _ => Task.FromResult<OverviewSection?>(Section("Last")));
            OverviewSnapshot snapshot = await new OverviewCollector(registry).CollectAsync(new OverviewOptions { Sections = new[] { "last", "First", "FIRST", "Empty", "Unavailable" } });
            Assert.Equal(new[] { "Last", "First" }, snapshot.Sections.Select(section => section.Name));
            Assert.Equal("First panel", snapshot.Sections[1].Title);
            Assert.All(snapshot.Sections, section => Assert.False(section.IsPending));
        }

        [Fact]
        public async Task UnknownSectionFailsBeforeAnyProbeStarts() {
            int calls = 0;
            OverviewSectionRegistry registry = new OverviewSectionRegistry().Register("One", "One", _ => { Interlocked.Increment(ref calls); return Task.FromResult<OverviewSection?>(Section("One")); });
            await Assert.ThrowsAsync<ArgumentException>(() => new OverviewCollector(registry).CollectAsync(new OverviewOptions { Sections = new[] { "One", "Typo" } }));
            Assert.Equal(0, calls);
            Assert.Throws<ArgumentException>(() => registry.Register("one", "duplicate", _ => Task.FromResult<OverviewSection?>(null)));
        }

        [Fact]
        public async Task SlowProbeReturnsPendingWithoutHidingCompletedDataAndIsCancelled() {
            TaskCompletionSource<bool> cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            OverviewSectionRegistry registry = new OverviewSectionRegistry()
                .Register("Fast", "Fast", _ => Task.FromResult<OverviewSection?>(Section("Fast")))
                .Register("Slow", "Slow", async token => {
                    try { await Task.Delay(5000, token); return Section("Slow"); }
                    finally { cancelled.TrySetResult(true); }
                });
            Stopwatch elapsed = Stopwatch.StartNew();
            OverviewSnapshot snapshot = await new OverviewCollector(registry).CollectAsync(new OverviewOptions { CollectionBudget = TimeSpan.FromMilliseconds(80), ProbeTimeout = TimeSpan.FromMilliseconds(250) });
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(1), "A slow probe consumed the login deadline.");
            Assert.False(snapshot.Sections.Single(section => section.Name == "Fast").IsPending);
            Assert.True(snapshot.Sections.Single(section => section.Name == "Slow").IsPending);
            Assert.Same(cancelled.Task, await Task.WhenAny(cancelled.Task, Task.Delay(2000)));
        }

        [Fact]
        public async Task RepeatedCollectionSharesSingleInFlightSynchronousProvider() {
            using (ManualResetEventSlim release = new ManualResetEventSlim()) {
                TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource<bool> cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                int calls = 0;
                OverviewSectionRegistry registry = new OverviewSectionRegistry().Register("Busy", "Busy", token => {
                    Interlocked.Increment(ref calls);
                    using (token.Register(() => cancelled.TrySetResult(true))) {
                        entered.TrySetResult(true);
                        try {
                            // Model a synchronous native call that cannot stop at its cooperative deadline.
                            release.Wait();
                            return Task.FromResult<OverviewSection?>(Section("Busy"));
                        } finally { exited.TrySetResult(true); }
                    }
                });
                OverviewOptions options = new OverviewOptions { CollectionBudget = TimeSpan.FromMilliseconds(30), ProbeTimeout = TimeSpan.FromMilliseconds(1) };
                OverviewProbe probe = Assert.Single(registry.Select(null));
                Task<OverviewSnapshot> first = new OverviewCollector(registry).CollectAsync(options);
                // The provider stays in flight until explicitly released, so this retrieves the same owned task.
                Task<OverviewSection?> flight = probe.Start(options.ProbeTimeout);
                try {
                    await entered.Task;
                    await cancelled.Task;
                    Assert.True((await first).Sections.Single().IsPending);
                    OverviewSnapshot second = await new OverviewCollector(registry).CollectAsync(options);
                    Assert.True(second.Sections.Single().IsPending);
                    Assert.Same(flight, probe.Start(options.ProbeTimeout));
                    Assert.False(exited.Task.IsCompleted);
                    Assert.Equal(1, calls);
                } finally {
                    release.Set();
                    await exited.Task;
                    await flight;
                    await first;
                }
            }
        }

        [Fact]
        public async Task ProviderFailureIsUnavailableAndDoesNotExposeExceptionText() {
            OverviewSectionRegistry registry = new OverviewSectionRegistry()
                .Register("Denied", "Denied", _ => throw new UnauthorizedAccessException("synthetic-private-detail"))
                .Register("Good", "Good", _ => Task.FromResult<OverviewSection?>(Section("Good")));
            OverviewSnapshot snapshot = await new OverviewCollector(registry).CollectAsync();
            Assert.Equal("Good", Assert.Single(snapshot.Sections).Name);
        }

        [Fact]
        public async Task CallerCancellationDoesNotWaitForProbeTimeout() {
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                OverviewSectionRegistry registry = new OverviewSectionRegistry().Register("Slow", "Slow", async token => {
                    await Task.Delay(1000, token);
                    return null;
                });
                stop.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OverviewCollector(registry).CollectAsync(cancellationToken: stop.Token));
            }
        }

        [Fact]
        public async Task RealCollectionUsesSupportedReadOnlyResourceProbes() {
            OverviewSnapshot snapshot = await new OverviewCollector().CollectAsync(new OverviewOptions { Sections = new[] { "Resources", "Disks", "Network", "Services" }, CollectionBudget = TimeSpan.FromMilliseconds(900) });
            // Scheduler delay is outside the login budget; controlled providers above qualify the wait bound.
            Assert.False(string.IsNullOrWhiteSpace(snapshot.HostName));
            Assert.False(string.IsNullOrWhiteSpace(snapshot.OperatingSystem));
            OverviewSection resources = Assert.Single(snapshot.Sections, section => section.Name == "Resources");
            if (resources.IsPending) { Assert.Empty(resources.Items); }
            else { AssertResourceObservations(resources); }
            // Keep actual machine identities, addresses and observations out of fixtures and test reports.
        }

        [Fact]
        public async Task NativeResourceProbeCompletesIndependentlyOfTheLoginCollectionBudget() {
            // Qualify the native read-only probe itself, outside the short login wait and shared registry flight.
            // This test allowance does not change the production collection or probe deadlines.
            using (CancellationTokenSource stop = new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
                OverviewSection? resources = await OverviewPlatform.ResourcesAsync(stop.Token);
                Assert.NotNull(resources);
                Assert.False(resources.IsPending);
                AssertResourceObservations(resources);
            }
        }

        private static void AssertResourceObservations(OverviewSection resources) {
            Assert.Contains(resources.Items, item => item.Label == "CPU" && item.Percentage >= 0 && item.Percentage <= 100);
            Assert.Contains(resources.Items, item => item.Label == "Memory" && item.Percentage >= 0 && item.Percentage <= 100);
        }

        [Theory]
        [InlineData(0, 2000)]
        [InlineData(901, 2000)]
        [InlineData(750, 2001)]
        public async Task InvalidBudgetsAreRejected(int budget, int probe) {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new OverviewCollector(new OverviewSectionRegistry()).CollectAsync(new OverviewOptions {
                CollectionBudget = TimeSpan.FromMilliseconds(budget), ProbeTimeout = TimeSpan.FromMilliseconds(probe)
            }));
        }

        private static OverviewSection Section(string name) => new OverviewSection(name, name, new[] { new OverviewItem("Observation", "Synthetic") });
    }
}
