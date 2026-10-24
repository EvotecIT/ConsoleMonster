using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Overview;
using Xunit;

namespace ConsoleMonster.Tests.Overview {
    public sealed class OverviewPlatformTests {
        [Fact]
        public void MacShortIntervalWithoutFreshIdleAccountingRemainsMeasuring() {
            Assert.Null(OverviewPlatform.CpuObservation(100, 40, 110, 40, true));
            Assert.NotNull(OverviewPlatform.CpuObservation(100, 40, 110, 45, true));
        }

        [Fact]
        public void UnixIdentityUsesProductAndDistributionMetadata() {
            Assert.Equal("macOS 26.1 (Tahoe)", OverviewPlatform.MacName("26.1"));
            Assert.Equal("macOS 27.0", OverviewPlatform.MacName("27.0"));
            Assert.Equal("Ubuntu 24.04.1 LTS", OverviewPlatform.LinuxName(new[] { "NAME=Ubuntu", "VERSION_ID=24.04", "PRETTY_NAME=\"Ubuntu 24.04.1 LTS\"" }));
        }

        [Theory]
        [InlineData("/", true)]
        [InlineData("/Volumes/Projects", true)]
        [InlineData("/Library/Developer/CoreSimulator/Volumes/iOS_23F77", false)]
        [InlineData("/Volumes/CasaRayRunnerCoreSimulator", false)]
        [InlineData("/System/Volumes/Data", false)]
        [InlineData("/System/Volumes/Preboot", false)]
        [InlineData("/Volumes/VM", false)]
        [InlineData("/Volumes/Preboot", false)]
        public void MacDiskSelectionShowsOnlyUserVolumes(string path, bool visible) => Assert.Equal(visible, OverviewPlatform.VisibleVolume(path, true));

        [Theory]
        [InlineData("en0", false)]
        [InlineData("eth0", false)]
        [InlineData("utun4", true)]
        [InlineData("awdl0", true)]
        [InlineData("llw0", true)]
        public void VirtualDiscoveryInterfacesAreHidden(string name, bool hidden) => Assert.Equal(hidden, OverviewPlatform.HiddenInterface(name));

        [Theory]
        [InlineData(100, 40, 100, 40)]
        [InlineData(100, 40, 90, 35)]
        [InlineData(100, 40, 200, 39)]
        [InlineData(100, 40, 200, 141)]
        public void CachedOrResetCpuCountersDoNotInventAUsagePercentage(double total, double idle, double nextTotal, double nextIdle) {
            Assert.Null(OverviewPlatform.CpuObservation(total, idle, nextTotal, nextIdle, false));
        }

        [Theory]
        [InlineData(100, 40, 200, 40, 100)]
        [InlineData(100, 40, 200, 65, 75)]
        [InlineData(100, 40, 200, 140, 0)]
        public void AdvancingCpuCountersMeasureUsedTime(double total, double idle, double nextTotal, double nextIdle, double expected) {
            OverviewItem? item = OverviewPlatform.CpuObservation(total, idle, nextTotal, nextIdle, false);
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
