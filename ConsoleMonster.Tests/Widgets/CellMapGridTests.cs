using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    /// <summary>
    /// A cell map as a labelled grid: a fixed number of cells per row, a label before each row, and one legend for the
    /// whole grid that wraps onto a second line when the map is narrow.
    /// </summary>
    public sealed class CellMapGridTests {
        // Three servers by four checks.
        private static CellMap Grid() {
            CellMap map = new CellMap { Columns = 4, LegendLines = 2 };
            foreach (string server in new[] { "APP01", "APP02", "DB01" }) {
                map.RowLabels.Add(server);
            }
            StatusKind[] states = {
                StatusKind.Pass, StatusKind.Pass, StatusKind.Fail, StatusKind.Pending,
                StatusKind.Pass, StatusKind.Warn, StatusKind.Pass, StatusKind.Pass,
                StatusKind.Pass, StatusKind.Pass, StatusKind.Pass, StatusKind.Pending
            };
            for (int i = 0; i < states.Length; i++) {
                map.Set(i, states[i]);
            }
            return map;
        }

        private static string[] Lines(CellMap map, int width) => TestTerminal.RenderText(map, width, glyphs: GlyphTier.Ascii).TrimEnd('\n').Split('\n').Select(l => l.TrimEnd()).ToArray();

        [Fact]
        public void Render_RowLabelsAndColumns_DrawOneLabelledRowPerServer() {
            string[] lines = Lines(Grid(), 70);
            Assert.Equal("APP01 + + x .", lines[0]);
            Assert.Equal("APP02 + ! + +", lines[1]);
            Assert.Equal("DB01  + + + .", lines[2]);
            Assert.Equal(4, lines.Length);
        }

        [Fact]
        public void Render_NarrowMap_WrapsTheSharedLegendOntoASecondLine() {
            CellMap map = Grid();
            RenderContext context = new RenderContext(TerminalProfile.Fixed(80, 10, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            string legend = map.Legend(context).PlainText;
            string[] lines = Lines(map, 24);
            Assert.Equal(5, lines.Length);
            Assert.True(lines[3].Length <= 24 && lines[4].Length <= 24, string.Join("\n", lines));
            Assert.Equal(legend, lines[3] + "  " + lines[4]);
        }

        [Fact]
        public void Render_ColumnsWiderThanTheMap_CutsCellsOffInsteadOfWrapping() {
            CellMap map = new CellMap { Columns = 10, ShowLegend = false };
            map.RowLabels.Add("A");
            map.RowLabels.Add("B");
            map.Resize(20);
            map.Set(10, StatusKind.Pass);
            string[] lines = Lines(map, 12);
            Assert.Equal(new[] { "A . . . . .", "B + . . . ." }, lines);
        }

        [Fact]
        public void Render_Defaults_KeepTheFlowingMapAndOneLegendLine() {
            CellMap map = new CellMap();
            map.Resize(30);
            map.Set(0, StatusKind.Pass);
            map.Set(1, StatusKind.Fail);
            string[] lines = Lines(map, 20);
            Assert.Equal(4, lines.Length);
            Assert.StartsWith("+ x . . .", lines[0]);
        }
    }
}
