using System;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class ConsoleFontWatcherTests {
        [Fact]
        public void CheckChanged_WithinInterval_DoesNotReadAgain() {
            TimeSpan now = TimeSpan.Zero;
            int reads = 0;
            string? currentFace = "Consolas";
            int currentHeight = 16;
            ConsoleFontWatcher watcher = new ConsoleFontWatcher(TimeSpan.FromSeconds(1), Read, () => now);
            Assert.False(watcher.CheckChanged(out string? face, out int height));
            Assert.Equal("Consolas", face);
            Assert.Equal(16, height);
            currentFace = "Cascadia Mono";
            now = TimeSpan.FromMilliseconds(999);
            Assert.False(watcher.CheckChanged(out face, out height));
            Assert.Equal(1, reads);
            now = TimeSpan.FromSeconds(1);
            Assert.True(watcher.CheckChanged(out face, out height));
            Assert.Equal("Cascadia Mono", face);
            Assert.Equal(2, reads);

            bool Read(out string? faceValue, out int heightValue) {
                reads++;
                faceValue = currentFace;
                heightValue = currentHeight;
                return true;
            }
        }

        [Fact]
        public void CheckChanged_HeightChangesAfterFirstReading_ReportsChange() {
            TimeSpan now = TimeSpan.Zero;
            int currentHeight = 16;
            ConsoleFontWatcher watcher = new ConsoleFontWatcher(TimeSpan.FromSeconds(1), Read, () => now);
            Assert.False(watcher.CheckChanged(out _, out _));
            currentHeight = 18;
            now = TimeSpan.FromSeconds(1);
            Assert.True(watcher.CheckChanged(out string? face, out int height));
            Assert.Equal("Consolas", face);
            Assert.Equal(18, height);

            bool Read(out string? faceValue, out int heightValue) {
                faceValue = "Consolas";
                heightValue = currentHeight;
                return true;
            }
        }
    }
}
