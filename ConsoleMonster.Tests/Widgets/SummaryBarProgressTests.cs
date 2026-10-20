using System;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    /// <summary>A progress update that leaves out done or total (<see cref="SummaryBar.Keep"/>) keeps what the bar has.</summary>
    public sealed class SummaryBarProgressTests {
        [Fact]
        public void Apply_ProgressKeepingTheTotal_ChangesOnlyDone() {
            SummaryBar bar = new SummaryBar().SetProgress(0, 40);
            bar.Apply(new LiveEvent("summary", "progress", value: 12, total: SummaryBar.Keep));
            Assert.Equal(12, bar.Done);
            Assert.Equal(40, bar.Total);
        }

        [Fact]
        public void Apply_ProgressKeepingDone_ChangesOnlyTheTotal() {
            SummaryBar bar = new SummaryBar().SetProgress(12, 40);
            bar.Apply(new LiveEvent("summary", "progress", value: double.NaN, total: 80));
            Assert.Equal(12, bar.Done);
            Assert.Equal(80, bar.Total);
        }

        [Fact]
        public void Apply_UpdatesInOrder_EachKeepsTheOneBefore() {
            SummaryBar bar = new SummaryBar();
            bar.Apply(new LiveEvent("summary", "progress", value: 0, total: 25));
            bar.Apply(new LiveEvent("summary", "progress", value: 5, total: SummaryBar.Keep));
            bar.Apply(new LiveEvent("summary", "progress", value: 9, total: SummaryBar.Keep));
            Assert.Equal(9, bar.Done);
            Assert.Equal(25, bar.Total);
        }

        [Fact]
        public void Apply_ZeroTotal_StillMeansUnknown() {
            SummaryBar bar = new SummaryBar().SetProgress(3, 10);
            bar.Apply(new LiveEvent("summary", "progress", value: 4, total: 0));
            Assert.Equal(4, bar.Done);
            Assert.Equal(0, bar.Total);
            Assert.Null(bar.Remaining);
        }
    }
}
