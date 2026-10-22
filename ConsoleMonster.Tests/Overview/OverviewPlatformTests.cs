using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Overview;
using Xunit;

namespace ConsoleMonster.Tests.Overview {
    public sealed class OverviewPlatformTests {
        [Theory]
        [InlineData(100, 40, 100, 40)]
        [InlineData(100, 40, 90, 35)]
        public void CachedOrResetCpuCountersDoNotInventAUsagePercentage(double total, double idle, double nextTotal, double nextIdle) {
            Assert.Null(OverviewPlatform.CpuObservation(total, idle, nextTotal, nextIdle));
        }

        [Theory]
        [InlineData(100, 40, 200, 40, 100)]
        [InlineData(100, 40, 200, 65, 75)]
        [InlineData(100, 40, 200, 140, 0)]
        public void AdvancingCpuCountersMeasureUsedTime(double total, double idle, double nextTotal, double nextIdle, double expected) {
            OverviewItem? item = OverviewPlatform.CpuObservation(total, idle, nextTotal, nextIdle);
            Assert.NotNull(item);
            Assert.Equal("CPU", item.Label);
            Assert.Equal(expected, item.Percentage);
        }

        [Fact]
        public void ZeroAvailableLinuxMemoryRendersFullPressureInsteadOfHidingTheMetric() {
            Assert.True(OverviewPlatform.TryLinuxMemory(new[] { "MemTotal: 1024 kB", "MemAvailable: 0 kB" }, out double total, out double available));
            OverviewItem item = OverviewPlatform.MemoryObservation(total, available);
            Assert.Equal(0, available);
            Assert.Equal(100, item.Percentage);
            OverviewSnapshot snapshot = new OverviewSnapshot("demo", "Synthetic Linux", null, DateTimeOffset.MinValue,
                new[] { new OverviewSection("Resources", "Resources", new[] { item }) });
            string text = Terminal.RenderText(new OverviewWidget(snapshot), TerminalProfile.Fixed(60, 30, ColorDepth.None, GlyphTier.Ascii));
            Assert.Matches(@"Memory\s+[^%]+100% critical", text);
            Assert.False(OverviewPlatform.TryLinuxMemory(new[] { "MemTotal: 1024 kB", "MemFree: 0 kB" }, out _, out _));
        }
    }
}
