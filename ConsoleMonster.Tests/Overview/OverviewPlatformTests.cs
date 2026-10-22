using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Overview;
using Xunit;

namespace ConsoleMonster.Tests.Overview {
    public sealed class OverviewPlatformTests {
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
