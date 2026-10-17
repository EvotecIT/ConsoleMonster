using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class TerminalGuardTests {
        [Fact]
        public void RestoreRunsOnceAndNeverThrowsEvenWhenCallbackFails() {
            int calls = 0;
            using (TerminalGuard guard = new TerminalGuard(() => {
                Interlocked.Increment(ref calls);
                throw new InvalidOperationException("restore failure");
            }, false)) {
                Parallel.For(0, 24, _ => guard.Restore());
                Assert.True(guard.RestoreCompleted);
                Assert.Equal(1, calls);
            }
            Assert.Equal(1, calls);
        }

        [Fact]
        public void FirstCancelRequestsGracefulStopAndSecondRestores() {
            int calls = 0;
            using (TerminalGuard guard = new TerminalGuard(() => calls++, false)) {
                Assert.True(guard.RequestCancellation());
                Assert.True(guard.Cancellation.IsCancellationRequested);
                Assert.False(guard.RestoreCompleted);
                Assert.Equal(0, calls);
                Assert.False(guard.RequestCancellation());
                Assert.True(guard.RestoreCompleted);
                Assert.Equal(1, calls);
            }
        }

        [Fact]
        public void DisposeCancelsAndRestoresAndCanBeRepeated() {
            int calls = 0;
            TerminalGuard guard = new TerminalGuard(() => calls++, false);
            CancellationToken token = guard.Cancellation;
            guard.Dispose();
            guard.Dispose();
            Assert.Equal(1, calls);
            Assert.True(token.IsCancellationRequested);
            Assert.True(guard.Cancellation.IsCancellationRequested);
            Assert.True(guard.RestoreCompleted);
        }

        [Fact]
        public void CancellationCallbackFailureCannotEscapeSignalHandling() {
            using (TerminalGuard guard = new TerminalGuard(() => { }, false)) {
                using (guard.Cancellation.Register(() => throw new InvalidOperationException("callback failure"))) {
                    Assert.True(guard.RequestCancellation());
                    Assert.True(guard.Cancellation.IsCancellationRequested);
                    Assert.False(guard.RequestCancellation());
                    Assert.True(guard.RestoreCompleted);
                }
            }
        }
    }
}
