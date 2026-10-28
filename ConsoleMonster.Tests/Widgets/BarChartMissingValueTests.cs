using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class BarChartMissingValueTests {
        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Render_MissingObservation_ShowsExplanationStatusAndSharedAxisWithoutABar(GlyphTier tier) {
            BarChart chart = new BarChart { Unit = "s", AxisLabel = "elapsed since start" };
            chart.Add("arrived", 12);
            chart.AddMissing("waiting", "not arrived", StatusKind.Warn);
            string[] lines = TestTerminal.RenderText(chart, 80, glyphs: tier).Split('\n');
            Assert.Equal(3, lines.Length);
            Assert.Contains("12s", lines[0]);
            Assert.Contains(GlyphSet.For(tier).StatusSymbol(StatusKind.Warn) + " not arrived", lines[1]);
            Assert.DoesNotContain("NaN", lines[1]);
            Assert.DoesNotContain("▆", lines[1]);
            Assert.Equal("elapsed since start", lines[2]);
        }

        [Fact]
        public void Render_ExplicitStatus_RemainsVisibleWithColor() {
            BarChart chart = new BarChart().AddMissing("waiting", "not arrived", StatusKind.Fail);
            var context = Terminal.CreateContext(TestTerminal.Profile(80, colors: ColorDepth.Ansi16), Theme.Dark);
            string text = FrameComposer.RenderBlock(chart, 80, context).ToPlainText();
            Assert.Contains(context.Glyphs.StatusSymbol(StatusKind.Fail) + " not arrived", text);
        }

        [Fact]
        public void Add_AfterMissing_RestoresANumericValueInsteadOfTheMissingExplanation() {
            BarChart chart = new BarChart { MissingText = "no measurement" };
            chart.AddMissing("sample");
            Assert.Contains("no measurement", TestTerminal.RenderText(chart, 80));
            chart.Add("sample", 7);
            string text = TestTerminal.RenderText(chart, 80);
            Assert.Contains("7", text);
            Assert.DoesNotContain("no measurement", text);
        }
    }
}
