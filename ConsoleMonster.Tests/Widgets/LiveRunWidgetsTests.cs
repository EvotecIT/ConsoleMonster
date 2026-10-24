using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.LiveRuns {
    /// <summary>The widgets for runs of many items: cell map, summary bar, activity list, lane options and footer segments.</summary>
    public sealed class LiveRunWidgetsTests {
        private static string[] Render(IWidget widget, int width, GlyphTier tier = GlyphTier.Full, TimeSpan elapsed = default) {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(width, 30, ColorDepth.None, tier), Theme.Dark, null, 0, elapsed);
            return FrameComposer.RenderBlock(widget, width, context).ToPlainText().Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        }

        [Fact]
        public void CellMap_EachStateHasItsOwnSymbol_InEveryTier() {
            CellMap map = new CellMap();
            map.Add(StatusKind.Pass);
            map.Add(StatusKind.Fail);
            map.Add(StatusKind.Warn);
            map.Add(StatusKind.Skip);
            map.Add(StatusKind.Pending);
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii }) {
                string cells = Render(map, 40, tier)[0];
                string[] symbols = cells.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(5, symbols.Length);
                Assert.Equal(5, symbols.Distinct().Count());
            }
        }

        [Fact]
        public void CellMap_FlowsToTheWidth_AndCountsEachStateInTheLegend() {
            CellMap map = new CellMap().Resize(30);
            for (int i = 0; i < 10; i++) {
                map.Set(i, StatusKind.Pass);
            }
            map.Set(10, StatusKind.Fail);
            string[] lines = Render(map, 20);
            // 20 cells wide with a gap: 10 symbols per row, 3 rows, then the legend.
            Assert.Equal(4, lines.Length);
            string legend = Render(map, 80)[1];
            Assert.Contains("10 passed", legend);
            Assert.Contains("1 failed", legend);
            Assert.Contains("19 queued", legend);
        }

        [Fact]
        public void CellMap_LiveEvents_SetAddAndCount() {
            CellMap map = new CellMap();
            map.Apply(new LiveEvent("cells", "count", value: 3));
            map.Apply(new LiveEvent("cells", "set", key: "1", status: "failed"));
            map.Apply(new LiveEvent("cells", "add", status: "passed"));
            Assert.Equal(4, map.Count);
            Assert.Equal(StatusKind.Fail, map[1]);
            Assert.Equal(StatusKind.Pass, map[3]);
        }

        [Fact]
        public void CellMap_InAShortRegion_KeepsTheLegend() {
            CellMap map = new CellMap().Resize(200);
            map.Set(0, StatusKind.Fail);
            RenderContext context = new RenderContext(TerminalProfile.Fixed(20, 30, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            CellBuffer buffer = new CellBuffer(40, 4);
            map.Render(new Region(buffer), context);
            string[] lines = buffer.ToPlainText().Replace("\r\n", "\n").Split('\n');
            Assert.Contains("more", lines[2]);
            Assert.Contains("1 failed", lines[3]);
        }

        [Fact]
        public void CellMap_ActiveWindow_FollowsTheRunningCell() {
            CellMap map = new CellMap { MaxRows = 3, ShowLegend = false, Window = CellMap.CellWindow.Active, SpinRunning = false }.Resize(100);
            for (int i = 0; i < 70; i++) {
                map.Set(i, StatusKind.Pass);
            }
            map.Set(65, StatusKind.Fail);
            map.Set(70, StatusKind.Running);
            // 20 cells wide with a gap: 10 per row; the window ends at row 7 (cell 70).
            string[] lines = Render(map, 20);
            Assert.Equal(4, lines.Length);
            Assert.Contains("earlier", lines[0]);
            Assert.Contains("✗", lines[1]);
            Assert.Contains("●", lines[2]);
            Assert.Contains("more", lines[3]);
        }

        [Fact]
        public void CellMap_BadLiveEvents_AreIgnored() {
            CellMap map = new CellMap().Resize(3);
            map.Apply(new LiveEvent("cells", "set", key: "2000000000", status: "failed"));
            map.Apply(new LiveEvent("cells", "count"));
            map.Apply(new LiveEvent("cells", "count", value: double.NaN));
            map.Apply(new LiveEvent("cells", "count", value: -5));
            Assert.Equal(3, map.Count);
        }

        [Fact]
        public void ActivityList_WithoutAPercentage_KeepsItsStatus_AndIdsKeepTheTitlesInLine() {
            ActivityList list = new ActivityList { ShowIds = true };
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: "9", text: "Copying", status: "file 3 of 10", value: -1, total: -1));
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: "10", text: "Hashing", status: "chunk 2", value: -1, total: -1));
            string[] lines = Render(list, 60);
            Assert.Contains("file 3 of 10", lines[0]);
            Assert.Contains("chunk 2", lines[1]);
            Assert.Equal(lines[0].IndexOf("Copying", StringComparison.Ordinal), lines[1].IndexOf("Hashing", StringComparison.Ordinal));
        }

        [Fact]
        public void FooterBar_TooNarrow_DropsSegmentsFromTheEndFirst() {
            FooterBar footer = new FooterBar(new[] { new KeyHint("ctrl+c", "stop") }) { Right = ConsoleMonster.Text.StyledLine.From("running", Style.Plain) };
            footer.Segments.Add(new FooterSegment("a very long report link segment that cannot fit"));
            string line = Render(footer, 50)[0];
            Assert.Contains("running", line);
            Assert.DoesNotContain("report link", line);
        }

        [Fact]
        public void SummaryBar_ShowsProgressPercentAndCounters_WithZerosKept() {
            SummaryBar bar = new SummaryBar { Elapsed = TimeSpan.FromSeconds(30) }
                .SetProgress(30, 60)
                .SetCounter("passed", 28, StatusKind.Pass)
                .SetCounter("failed", 2, StatusKind.Fail)
                .SetCounter("skipped", 0, StatusKind.Skip);
            string line = Render(bar, 100)[0];
            Assert.Contains("30/60", line);
            Assert.Contains("50%", line);
            Assert.Contains("30s", line);
            Assert.Contains("28 passed", line);
            Assert.Contains("2 failed", line);
            Assert.Contains("0 skipped", line);
            Assert.Equal(TimeSpan.FromSeconds(30), bar.Remaining);
        }

        [Fact]
        public void ActivityList_ShowsIdsAndElapsedTimeRightAligned() {
            ActivityList list = new ActivityList { ShowIds = true };
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: "7", text: "Checking dc01", value: -1, total: -1, time: 1000));
            string line = Render(list, 50, GlyphTier.Full, TimeSpan.FromSeconds(13))[0];
            Assert.Contains("#7", line);
            Assert.Contains("Checking dc01", line);
            Assert.EndsWith("12s", line.TrimEnd());
        }

        [Fact]
        public void Lanes_CollapseWhen_FoldsLanesIntoOneRow_AndCountsRunning() {
            Lanes lanes = new Lanes { CollapseWhen = StatusKind.Skip, ShowRunningCount = true, BoldActive = true };
            lanes.Add("a", "Alpha", 46).Status = StatusKind.Skip;
            lanes.Add("b", "Beta", 5).Status = StatusKind.Skip;
            Lane gamma = lanes.Add("c", "Gamma", 10);
            gamma.Status = StatusKind.Running;
            gamma.Done = 4;
            string[] lines = Render(lanes, 80);
            Assert.Contains(lines, line => line.Contains("Gamma"));
            Assert.Contains(lines, line => line.Contains("2 skipped: Alpha (46)") && line.Contains("Beta (5)"));
            Assert.Contains(lines, line => line.Contains("1 running"));
            Assert.Equal(3, lines.Length);
        }

        [Fact]
        public void FooterBar_SegmentsSitOnTheRight() {
            FooterBar footer = new FooterBar(new[] { new KeyHint("ctrl+c", "stop") });
            footer.Segments.Add(new FooterSegment("run 42"));
            footer.Segments.Add(new FooterSegment("report", "https://example.test/run/42", ThemeToken.Accent));
            string line = Render(footer, 60)[0];
            Assert.Contains("ctrl+c stop", line);
            Assert.Contains("run 42", line);
            Assert.EndsWith("report", line.TrimEnd());
        }
    }
}
