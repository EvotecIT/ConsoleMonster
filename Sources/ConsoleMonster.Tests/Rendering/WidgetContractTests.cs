using System;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Widget and container contracts: bar shapes, status tones, histogram labels, tables, spinner, Align and Stack.</summary>
    public sealed class WidgetContractTests {
        private static RenderContext Context(int width = 80, GlyphTier tier = GlyphTier.Full, long tick = 0, TimeSpan elapsed = default) {
            return new RenderContext(TerminalProfile.Fixed(width, 30, ColorDepth.None, tier), Theme.Dark, tick: tick, elapsed: elapsed);
        }

        private static string[] Render(IWidget widget, int width = 40, GlyphTier tier = GlyphTier.Full, long tick = 0, TimeSpan elapsed = default) {
            return FrameComposer.RenderBlock(widget, width, Context(width, tier, tick, elapsed)).ToPlainText().Split('\n');
        }

        [Fact]
        public void BarShapes_Defaults_ProgressAndLanesUseLineChartsUseBlock() {
            Assert.Equal(BarShape.Line, new ProgressBar().Shape);
            Assert.Equal(BarShape.Line, new Lanes().Shape);
            Assert.Equal(BarShape.Block, new BarChart().Shape);
            string bars = Render(new BarChart().Add("alpha", 7.5).Add("beta", 10))[0];
            Assert.Contains("█", bars);
        }

        [Fact]
        public void LineBar_FullTier_RemainingTrackDiffersFromCompletedBar() {
            string half = Render(new ProgressBar("x") { Value = 50, ShowCount = false })[0];
            string done = Render(new ProgressBar("x") { Value = 100, ShowCount = false })[0];
            Assert.Contains("━", half);
            Assert.Contains("─", half);
            Assert.Contains("━", done);
            Assert.DoesNotContain("─", done);
        }

        [Fact]
        public void LineChart_StatusNamedSeries_UsesStatusTones() {
            LineChart chart = new LineChart();
            Assert.Equal(ThemeToken.Fail, chart.AddSeries("failed").Token);
            Assert.Equal(ThemeToken.Pass, chart.AddSeries("passed").Token);
            Assert.Equal(ThemeToken.Series3, chart.AddSeries("latency").Token);
        }

        [Theory]
        [InlineData(24)]
        [InlineData(40)]
        [InlineData(80)]
        public void Histogram_EdgeLabels_KeepEndsAndNeverCollide(int width) {
            double[] edges = Enumerable.Range(0, 11).Select(i => i * 100.0).ToArray();
            Histogram histogram = new Histogram(edges) { Height = 4 };
            foreach (double value in new[] { 50.0, 150, 150, 420, 610, 990 }) {
                histogram.Add(value);
            }
            string[] rows = Render(histogram, width);
            string labels = rows.Last(r => r.Trim().Length > 0);
            string[] tokens = labels.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("0", tokens.First());
            Assert.Equal("1k", tokens.Last());
            double previous = double.MinValue;
            foreach (string token in tokens) {
                double value = token.EndsWith("k", StringComparison.Ordinal)
                    ? double.Parse(token.TrimEnd('k'), CultureInfo.InvariantCulture) * 1000
                    : double.Parse(token, CultureInfo.InvariantCulture);
                Assert.True(value > previous, "labels must ascend without merging: " + labels);
                previous = value;
            }
        }

        [Fact]
        public void Table_WithoutHeaderSeparator_FirstRowFollowsHeader() {
            Table table = new Table { Border = TableBorder.None, HeaderSeparator = false };
            table.AddColumn("Name").AddColumn("Count");
            table.AddPlainRow("alpha", "7");
            string[] rows = Render(table);
            Assert.Contains("Name", rows[0]);
            Assert.Contains("alpha", rows[1]);
        }

        [Fact]
        public void Spinner_Running_AnimatesWithTheFrameCounter() {
            Spinner spinner = new Spinner("Resolving DNS");
            RenderContext context = Context();
            string first = Render(spinner, tick: 0)[0];
            string second = Render(spinner, tick: 1)[0];
            Assert.StartsWith(context.Glyphs.SpinnerFrames[0], first);
            Assert.StartsWith(context.Glyphs.SpinnerFrames[1], second);
            Assert.Contains("Resolving DNS", first);
        }

        [Fact]
        public void Spinner_Finished_ShowsStatusMarkerDurationAndNarration() {
            Spinner spinner = new Spinner("Resolving DNS");
            spinner.Apply(new LiveEvent("spinner", "start", time: 1000));
            LiveEvent done = new LiveEvent("spinner", "state", text: "42 zones", state: "fail", time: 4500);
            spinner.Apply(done);
            string row = Render(spinner, elapsed: TimeSpan.FromSeconds(30))[0];
            Assert.StartsWith(Context().Glyphs.StatusMarker(StatusKind.Fail), row);
            Assert.Contains("42 zones", row);
            Assert.EndsWith("3.5s", row.TrimEnd());
            Assert.Equal("Resolving DNS FAIL: 42 zones in 3.5s", spinner.Describe(done));
        }

        [Fact]
        public void Spinner_AsciiTier_UsesAsciiFramesAndWords() {
            Spinner spinner = new Spinner("Sync");
            string running = Render(spinner, tier: GlyphTier.Ascii, tick: 1)[0];
            Assert.StartsWith("/", running);
            spinner.Apply(new LiveEvent("spinner", "state", state: "pass", time: 10));
            string finished = Render(spinner, tier: GlyphTier.Ascii)[0];
            Assert.StartsWith("[PASS]", finished);
            Assert.True(finished.All(c => c < 127));
        }

        [Fact]
        public void Align_RightAndCentre_PlaceContentAtItsMeasuredWidth() {
            Assert.Equal("right", Render(new Align(new TextBlock("right"), HorizontalAlignment.Right), 20)[0].Substring(15));
            Assert.Equal("        mid", Render(new Align(new TextBlock("mid")), 20)[0].TrimEnd());
            Assert.Equal(3, new Align(new TextBlock("mid"), HorizontalAlignment.Left).Measure(new Constraints(20), Context(20)).Width);
        }

        [Fact]
        public void Align_Middle_CentresVerticallyInAFixedHeight() {
            Rows rows = new Rows().Add(new Align(new TextBlock("x"), HorizontalAlignment.Left, VerticalAlignment.Middle), LayoutSize.Fixed(5));
            string[] lines = FrameComposer.Compose(rows, 10, 5, Context(10)).ToPlainText().Split('\n');
            Assert.Equal("x", lines[2].Trim());
            Assert.Equal(string.Empty, lines[1].Trim());
        }

        [Fact]
        public void Stack_LaterLayersPaintOverEarlierOnesOnlyWhereTheyWrite() {
            Stack stack = new Stack(new TextBlock("aaaaaaaaaa"), new Align(new TextBlock("B"), HorizontalAlignment.Right));
            Assert.Equal(new Size(10, 1), stack.Measure(new Constraints(10), Context(10)));
            Assert.Equal("aaaaaaaaaB", Render(stack, 10)[0]);
            Assert.Equal(2, stack.Children.Count);
        }
    }
}
