using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Layout contracts that keep dashboards tidy: shared heights, fill rows, collapse instead of squeeze.</summary>
    public sealed class LayoutContractTests {
        private static RenderContext Context(int width = 80, GlyphTier tier = GlyphTier.Full) {
            return new RenderContext(TerminalProfile.Fixed(width, 30, ColorDepth.None, tier), Theme.Dark);
        }

        [Fact]
        public void PanelsInColumnsShareTheRowHeight() {
            Columns columns = new Columns(
                new Panel(new TextBlock("one line"), "A"),
                new Panel(new TextBlock("1\n2\n3\n4\n5"), "B"));
            CellBuffer buffer = FrameComposer.RenderBlock(columns, 80, Context());
            string[] rows = buffer.ToPlainText().Split('\n');
            Assert.Equal(7, rows.Length);
            Assert.StartsWith("╰", rows[6]);
            Assert.Contains("╯ ╰", rows[6]);
        }

        [Fact]
        public void ColumnsStackVerticallyWhenNarrowerThanTheirMinimums() {
            Columns columns = new Columns(new TextBlock("left"), new TextBlock("right"));
            Size wide = columns.Measure(new Constraints(80), Context());
            Size narrow = columns.Measure(new Constraints(20), Context(20));
            Assert.Equal(1, wide.Height);
            Assert.Equal(2, narrow.Height);
        }

        [Fact]
        public void FractionRowTakesTheRemainingHeightOfALiveFrame() {
            LogRegion log = new LogRegion { PreferredHeight = 2 };
            Rows rows = new Rows().Add(new TextBlock("header")).Add(new Panel(log, "Log"), LayoutSize.Fraction());
            CellBuffer frame = FrameComposer.Compose(rows, 40, 20, Context(40));
            string[] lines = frame.ToPlainText().Split('\n');
            Assert.StartsWith("╰", lines[19]);
        }

        [Fact]
        public void SizedWrapperSuppliesTheDefaultSize() {
            Columns columns = new Columns(new Sized(new TextBlock("a"), LayoutSize.Fixed(10)), new TextBlock("b"));
            CellBuffer buffer = FrameComposer.RenderBlock(columns, 40, Context(40));
            Assert.Equal('b', buffer.GetRowText(0)[11]);
        }

        [Fact]
        public void LanesKeepRunningLanesVisibleAndSummariseTheRest() {
            Lanes lanes = new Lanes();
            for (int i = 0; i < 10; i++) {
                lanes.Add("l" + i, "lane " + i, 5);
            }
            lanes.Apply(new global::ConsoleMonster.Live.LiveEvent("lanes", "state", "l9", state: "running", time: 1));
            CellBuffer frame = FrameComposer.Compose(lanes, 80, 4, Context());
            string text = frame.ToPlainText();
            Assert.Contains("lane 9", text);
            Assert.Contains("+7 more", text);
        }

        [Fact]
        public void SixteenColourThemeUsesExplicitFallbacks() {
            Style pass = Theme.Dark.Resolve(ThemeToken.Pass, ColorDepth.Ansi16);
            Assert.Equal(ColorKind.Ansi16, pass.Foreground.Kind);
            Assert.Equal(10, pass.Foreground.Index);
            Assert.True(Theme.Dark.Resolve(ThemeToken.Fail, ColorDepth.None).IsPlain);
        }

        [Fact]
        public void TrueColourDownsamplesToTheNearestPaletteEntries() {
            Color orange = Color.FromRgb(255, 135, 0);
            Assert.Equal(208, orange.Downsample(ColorDepth.Ansi256).Index);
            Assert.Equal(ColorKind.Ansi16, orange.Downsample(ColorDepth.Ansi16).Kind);
            Assert.True(orange.Downsample(ColorDepth.None).IsDefault);
        }
    }
}
