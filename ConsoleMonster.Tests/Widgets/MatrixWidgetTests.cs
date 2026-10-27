using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class MatrixWidgetTests {
        private static Matrix Grid() {
            MatrixLabel[] rows = { new MatrixLabel("alpha", "North"), new MatrixLabel("beta", "South") };
            MatrixLabel[] columns = { new MatrixLabel("check-one", "Coverage"), new MatrixLabel("check-two", "Coverage") };
            return new Matrix(rows, columns).SetCell(0, 0, null, diagonal: true)
                .SetCell(0, 1, StatusKind.Pass, "3m").SetCell(1, 0, StatusKind.Fail, "2d");
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Render_Grid_ShowsBothAxesBandsCellTextAndCountedLegend(GlyphTier tier) {
            string text = TestTerminal.RenderText(Grid(), 120, glyphs: tier);
            Assert.Contains("alpha", text);
            Assert.Contains("beta", text);
            Assert.Contains("check-one", text);
            Assert.Contains("check-two", text);
            Assert.Contains("North", text);
            Assert.Contains("South", text);
            Assert.Contains("Coverage", text);
            Assert.Contains(GlyphSet.For(tier).StatusSymbol(StatusKind.Pass) + " 3m", text);
            Assert.Contains(GlyphSet.For(tier).StatusSymbol(StatusKind.Fail) + " 2d", text);
            Assert.Contains("self 1", text);
            Assert.Contains("none 1", text);
            Assert.Contains(StatusKind.Pass.ToWord() + " 1", text);
            Assert.Contains(StatusKind.Fail.ToWord() + " 1", text);
        }

        [Fact]
        public void Render_NumberedHeaders_KeepsFullColumnKeyAndExplicitSelfCells() {
            Matrix matrix = Grid();
            matrix.ShowLegend = false;
            string text = TestTerminal.RenderText(matrix, 60);
            Assert.Contains("1 alpha", text);
            Assert.Contains("2 beta", text);
            Assert.Contains("1 check-one", text);
            Assert.Contains("2 check-two", text);
            Assert.Contains("—", text);
        }

        [Fact]
        public void Render_NarrowList_OrdersFailuresBeforeWarningsAndNamesOmissions() {
            Matrix matrix = Grid().SetCell(1, 1, StatusKind.Warn, "waiting");
            matrix.MaxListRows = 1;
            matrix.ShowLegend = false;
            string text = TestTerminal.RenderText(matrix, 45, glyphs: GlyphTier.Ascii);
            Assert.Contains("beta / check-one", text);
            Assert.Contains("2d", text);
            Assert.DoesNotContain("3m", text);
            Assert.DoesNotContain("waiting", text);
            Assert.Contains("+1 more", text);
        }

        [Fact]
        public void Render_CustomHealth_DoesNotInventAClassification() {
            Matrix matrix = Grid();
            matrix.NonHealthyOnly = true;
            matrix.IsHealthy = status => status != StatusKind.Pass;
            matrix.ShowLegend = false;
            string text = TestTerminal.RenderText(matrix, 120);
            Assert.Contains("alpha / check-two", text);
            Assert.DoesNotContain("2d", text);
        }

        [Fact]
        public void SetCell_RectangularAxes_DoesNotInferADiagonalFromMatchingIndexes() {
            Matrix matrix = new Matrix(new[] { new MatrixLabel("row[[literal]]") }, new[] { new MatrixLabel("column") });
            matrix.SetCell(0, 0, StatusKind.Warn, "late");
            Assert.False(matrix.GetCell(0, 0)!.Diagonal);
            Assert.Contains("row[[literal]]", TestTerminal.RenderText(matrix, 120));
            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.SetCell(1, 0, StatusKind.Pass));
        }
    }
}
