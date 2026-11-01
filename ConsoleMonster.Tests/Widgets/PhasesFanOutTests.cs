using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class PhasesFanOutTests {
        private static string[] Render(Phases phases, int width) => Terminal.RenderText(
            phases, TerminalProfile.Fixed(width, 40, ColorDepth.None, GlyphTier.Ascii), Theme.Dark).Split('\n').Select(line => line.TrimEnd()).ToArray();

        private static Phases Pipeline() => new Phases()
            .Set("fetch", StatusKind.Pass, "Fetch")
            .Set("migrate", StatusKind.Running, "Migrate")
            .Set("verify", StatusKind.Pending, "Verify");

        [Fact]
        public void FanOutKeepsUrgentItemsVisibleAndFoldsTheRestIntoMoreWithStatusCounts() {
            Phases phases = Pipeline();
            phases.FanOut = 3;
            foreach (string user in new[] { "alice", "bob", "carol" }) { phases.SetItem("migrate", user, StatusKind.Pass); }
            Assert.DoesNotContain(Render(phases, 80), line => line.Contains("more"));

            phases.SetItem("migrate", "dave", StatusKind.Pending);
            phases.SetItem("migrate", "erin", StatusKind.Fail, detail: "access denied");
            string[] lines = Render(phases, 80);
            Assert.Equal(4, lines.Count(line => line.Length > 0));
            Assert.Contains("alice", lines[1]);
            Assert.Contains("[FAIL] erin access denied", lines[2]);
            Assert.Contains("+3 more - [OK] 2 - [WAIT] 1", lines[3]);
        }

        [Fact]
        public void CompactChainFoldsNeighboursIntoGroupsThenCountsBeforeShorteningTheCurrentPhase() {
            Phases phases = new Phases { Compact = true }
                .Set("a", StatusKind.Pass, "Discover").Set("b", StatusKind.Pass, "Snapshot")
                .Set("c", StatusKind.Running, "Migrate", "40%")
                .Set("d", StatusKind.Pending, "Verify").Set("e", StatusKind.Pending, "Report");
            Assert.Contains("Snapshot", Render(phases, 120)[0]);
            Assert.Equal("[OK] +2 earlier > [BUSY] Migrate 40% > [WAIT] +2 more", Render(phases, 60)[0]);
            Assert.Equal("[OK] 2 > [BUSY] Migrate 40% > [WAIT] 2", Render(phases, 40)[0]);
            Assert.Equal("[OK] 2 > [BUSY] Migrate > [WAIT] 2", Render(phases, 34)[0]);
        }

        [Fact]
        public void ItemUpdatesWithParentKeyReachThePhaseAndItsFanOut() {
            Phases phases = Pipeline();
            foreach (LiveEvent update in ConsoleUpdate.Events(phases, new WidgetUpdate { ParentKey = "migrate", Key = "alice", Status = StatusKind.Running, Detail = "40%" })) {
                phases.Apply(update);
            }
            Assert.Contains(Render(phases, 80), line => line.Contains("[BUSY] alice 40%"));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(16)]
        [InlineData(24)]
        [InlineData(30)]
        public void NarrowChainKeepsTheCurrentFailureBeforeFoldedCounts(int width) {
            Phases phases = new Phases { Compact = true }
                .Set("prepare", StatusKind.Pass, "Prepare")
                .Set("migrate", StatusKind.Fail, "Migrate")
                .Set("verify", StatusKind.Pending, "Verify");
            string line = Render(phases, width)[0];
            Assert.StartsWith("[FAIL]", line);
            if (width >= 14) { Assert.Contains("Migrate", line); }
            Assert.True(line.Length <= width);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ExplicitItemDetailClearReplacesThePreviousValue(string? clear) {
            Phases phases = Pipeline().SetItem("migrate", "alice", StatusKind.Running, detail: "Old detail");
            phases.SetItem("migrate", "alice", StatusKind.Running, detail: null);
            Assert.Contains(Render(phases, 80), line => line.Contains("alice Old detail"));
            foreach (LiveEvent update in ConsoleUpdate.Events(phases, new WidgetUpdate { ParentKey = "migrate", Key = "alice", Detail = clear })) {
                phases.Apply(update);
            }
            string[] lines = Render(phases, 80);
            Assert.Contains(lines, line => line.Contains("alice"));
            Assert.DoesNotContain(lines, line => line.Contains("Old detail"));
        }
    }
}
