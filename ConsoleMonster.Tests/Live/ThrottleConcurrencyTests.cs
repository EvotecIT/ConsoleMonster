using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed class ThrottleConcurrencyTests {
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(8)]
        public async Task ForEachAsync_ThrottleBoundsActiveBodiesUnderPressure(int limit) {
            int active = 0;
            int maximum = 0;
            int started = 0;
            TaskCompletionSource<bool> full = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = ConsoleRun.ForEachAsync(Enumerable.Range(0, 64), async (item, token) => {
                int running = Interlocked.Increment(ref active);
                int previous;
                do { previous = Volatile.Read(ref maximum); }
                while (running > previous && Interlocked.CompareExchange(ref maximum, running, previous) != previous);
                if (Interlocked.Increment(ref started) == limit) { full.TrySetResult(true); }
                try {
                    await release.Task.ConfigureAwait(false);
                    // Keep later bodies asynchronous too, so the limit is checked beyond the first held wave.
                    await Task.Yield();
                    token.ThrowIfCancellationRequested();
                    return item * 2;
                } finally { Interlocked.Decrement(ref active); }
            }, new RunOptions { ThrottleLimit = limit, Interactive = false, Print = _ => { } });
            try {
                Assert.Same(full.Task, await Task.WhenAny(full.Task, Task.Delay(TimeSpan.FromSeconds(30))));
                Assert.Equal(limit, Volatile.Read(ref active));
                Assert.Equal(limit, Volatile.Read(ref started));
            } finally { release.TrySetResult(true); }
            Assert.Same(run, await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30))));
            var results = await run;
            Assert.Equal(limit, maximum);
            Assert.Equal(0, active);
            Assert.Equal(64, started);
            Assert.Equal(Enumerable.Range(0, 64).Select(i => i * 2), results.Select(r => r.Value));
            Assert.All(results, result => Assert.True(result.Succeeded));
        }
    }
}
