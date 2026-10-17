using System;
using System.Linq;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Stat columns line up across rows and hold still while values grow (9 to 10) during a live session.</summary>
    public sealed class ColumnAlignmentTests {
        private static RenderContext Context(int width, GlyphTier tier = GlyphTier.Full) {
            return new RenderContext(TerminalProfile.Fixed(width, 30, ColorDepth.None, tier), Theme.Dark);
        }

        private static string[] Render(IWidget widget, int width, GlyphTier tier = GlyphTier.Full) {
            return FrameComposer.RenderBlock(widget, width, Context(width, tier)).ToPlainText().Split('\n');
        }

        private static Lanes ThreeLanes(double total) {
            Lanes lanes = new Lanes { ShowDetail = false };
            lanes.Add("a", "alpha", total);
            lanes.Add("b", "beta", total);
            lanes.Add("c", "gamma", total);
            Step(lanes, "a", "pass", 9);
            Step(lanes, "b", "pass", 13);
            Step(lanes, "b", "fail", 3);
            Step(lanes, "c", "warn", 1);
            return lanes;
        }

        private static void Step(Lanes lanes, string key, string state, int count) {
            for (int i = 0; i < count; i++) {
                lanes.Apply(new LiveEvent("lanes", "advance", key, state: state, time: 1));
            }
        }

        [Fact]
        public void Lanes_CountsOfDifferentWidths_ShareColumnsAcrossRows() {
            string[] rows = Render(ThreeLanes(40), 70);
            int[] crosses = rows.Take(3).Select(r => r.IndexOf('✗')).ToArray();
            int[] warnings = rows.Take(3).Select(r => r.IndexOf('▴')).ToArray();
            Assert.All(crosses, c => Assert.Equal(crosses[0], c));
            Assert.All(warnings, c => Assert.Equal(warnings[0], c));
            // Numbers are right-aligned inside their column: "✓ 9" under "✓13".
            Assert.Contains("✓ 9", rows[0]);
            Assert.Contains("✓13", rows[1]);
        }

        [Fact]
        public void Lanes_NineToTenWithKnownTotal_ColumnsHoldStill() {
            Lanes lanes = ThreeLanes(40);
            string before = Render(lanes, 70)[0];
            Step(lanes, "a", "pass", 1);
            string after = Render(lanes, 70)[0];
            Assert.Equal(before.IndexOf('✗'), after.IndexOf('✗'));
            Assert.Equal(before.IndexOf('▴'), after.IndexOf('▴'));
            Assert.Contains("✓10", after);
        }

        [Fact]
        public void Lanes_ReservedDigitsWithoutTotal_ColumnsHoldStill() {
            Lanes lanes = ThreeLanes(0);
            lanes.CountColumn = new ColumnFormat(reserveDigits: 3);
            string before = Render(lanes, 70)[2];
            Step(lanes, "c", "warn", 99);
            string after = Render(lanes, 70)[2];
            Assert.Equal(before.IndexOf('✗'), after.IndexOf('✗'));
            Assert.Contains("▴100", after);
        }

        [Fact]
        public void StackedLegend_EntriesShareOneGrid() {
            StackedBar bar = new StackedBar()
                .Add("Passed", 169, ThemeToken.Pass)
                .Add("Failed", 11, ThemeToken.Fail)
                .Add("Warnings", 24, ThemeToken.Warn)
                .Add("Skipped", 10, ThemeToken.Skip);
            string[] rows = Render(bar, 44);
            Assert.Equal(3, rows.Length);
            // Two columns per row; counts and percentages end at the same column in both rows.
            int CountEnd(string row, string label) {
                int start = row.IndexOf(label, StringComparison.Ordinal);
                int percent = row.IndexOf('%', start);
                return percent;
            }
            Assert.Equal(CountEnd(rows[1], "Passed"), CountEnd(rows[2], "Warnings"));
            Assert.Equal(CountEnd(rows[1], "Failed"), CountEnd(rows[2], "Skipped"));
            Assert.Equal(rows[1].IndexOf("169", StringComparison.Ordinal) + 2, rows[2].IndexOf("24", StringComparison.Ordinal) + 1);
        }

        [Fact]
        public void Progress_NineToTen_BarKeepsItsLength() {
            ProgressBar bar = new ProgressBar("Build") { Value = 9, Total = 50 };
            string before = Render(bar, 50)[0];
            bar.Value = 10;
            string after = Render(bar, 50)[0];
            Assert.Equal(before.IndexOf('%'), after.IndexOf('%'));
            Assert.EndsWith(" 9/50", before.TrimEnd());
            Assert.EndsWith("10/50", after.TrimEnd());
        }

        [Fact]
        public void KeyValue_LeadingNumbers_AreRightAligned() {
            KeyValueList list = new KeyValueList()
                .Add("Evaluated", "234 / 332")
                .Add("Average rate", "8.4 rules/s")
                .Add("Remaining", "~12s")
                .Add("Targets failing", "4");
            string[] rows = Render(list, 40);
            int end = rows[0].IndexOf("234", StringComparison.Ordinal) + 3;
            Assert.Equal(end, rows[1].IndexOf("8.4", StringComparison.Ordinal) + 3);
            Assert.Equal(end, rows[2].IndexOf("~12s", StringComparison.Ordinal) + 4);
            Assert.Equal(end, rows[3].IndexOf('4') + 1);
        }

        [Fact]
        public void KeyValue_CultureGroupedNumbers_AlignAsOneNumber() {
            KeyValueList list = new KeyValueList()
                .Add("Updates posted", "6\u00A0064\u00A0519")
                .Add("Frames", "234");
            string[] rows = Render(list, 40);
            Assert.Equal(rows[0].TrimEnd().Length, rows[1].TrimEnd().Length);
        }
        [Fact]
        public void BarChart_ValueColumn_NeverShrinks() {
            BarChart chart = new BarChart().Add("alpha", 120).Add("beta", 7);
            Assert.EndsWith("  7", Render(chart, 40)[1]);
            chart.Add("alpha", 9);
            string[] rows = Render(chart, 40);
            Assert.EndsWith("  9", rows[0]);
            Assert.EndsWith("  7", rows[1]);
        }

        [Fact]
        public void ColumnsGolden_LiveNineToTenTransition() {
            StringBuilder text = new StringBuilder();
            Lanes lanes = ThreeLanes(40);
            StackedBar legend = new StackedBar()
                .Add("Passed", 9, ThemeToken.Pass)
                .Add("Failed", 3, ThemeToken.Fail)
                .Add("Warnings", 1, ThemeToken.Warn)
                .Add("Skipped", 0, ThemeToken.Skip);
            Rows scene = new Rows(new Panel(lanes, "Targets"), new Panel(legend, "Results"));
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Ascii }) {
                text.Append("== ").Append(tier).Append(" before\n").Append(string.Join("\n", Render(scene, 64, tier))).Append('\n');
            }
            Step(lanes, "a", "pass", 1);
            legend.Add("Passed", 10);
            legend.Add("Skipped", 100);
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Ascii }) {
                text.Append("== ").Append(tier).Append(" after 9 -> 10\n").Append(string.Join("\n", Render(scene, 64, tier))).Append('\n');
            }
            Golden.Match("columns.transition.txt", text.ToString());
        }
    }
}
