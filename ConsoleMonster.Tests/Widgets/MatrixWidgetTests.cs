using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Frames;
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Render_HealthyMatrixInFrameSection_KeepsTheGridItMeasured(bool inlineAxes) {
            MatrixLabel[] dcs = { new MatrixLabel("AD1", "Warsaw"), new MatrixLabel("AD2", "Warsaw") };
            Matrix matrix = new Matrix(dcs, dcs) { InlineAxes = inlineAxes, RowAxisCaption = "destination", ColumnAxisCaption = "source" }
                .SetCell(0, 0, null, diagonal: true).SetCell(1, 1, null, diagonal: true)
                .SetCell(0, 1, StatusKind.Pass, "3 m").SetCell(1, 0, StatusKind.Pass, "13 m");
            // The frame gives a section exactly its measured width; the matrix must not change layout at that width.
            string text = TestTerminal.RenderText(Ui.DashboardFrame("Replication").Section("Who replicates from whom", c => c.Add(matrix)), 150);
            Assert.DoesNotContain("non-healthy", text);
            Assert.Contains("13 m", text);
            Assert.Contains("AD2", text);
        }

        [Theory]
        [InlineData(120, "per column")]
        [InlineData(120, "per column, full glyphs")]
        [InlineData(30, "per band")]
        [InlineData(12, "list")]
        public void Render_DenseOverflow_ShowsEveryColumnThenEveryBandThenTheList(int width, string expected) {
            // 4 sites × 10 DCs: 40 columns exceed MaxGridSize; one failure in site C, everything else passes.
            MatrixLabel[] dcs = new MatrixLabel[40];
            for (int i = 0; i < dcs.Length; i++) dcs[i] = new MatrixLabel("DC" + i, "site-" + (char)('A' + i / 10));
            Matrix matrix = new Matrix(new[] { new MatrixLabel("Replication") }, dcs) { Overflow = MatrixOverflow.Dense, ShowLegend = false };
            for (int c = 0; c < dcs.Length; c++) matrix.SetCell(0, c, c == 25 ? StatusKind.Fail : StatusKind.Pass);
            bool full = expected.EndsWith("full glyphs", StringComparison.Ordinal);
            matrix.ShowLegend = full;
            string text = TestTerminal.RenderText(matrix, width, glyphs: full ? GlyphTier.Full : GlyphTier.Ascii);
            string row = Array.Find(text.Split('\n'), line => line.StartsWith("Repli", StringComparison.Ordinal)) ?? string.Empty;
            switch (expected) {
                case "per column":
                    Assert.Contains("site-A", text);
                    Assert.Contains(new string('+', 25) + "x" + new string('+', 14), row.Replace(" ", string.Empty));
                    break;
                case "per column, full glyphs":
                    // Healthy cells form a solid field; the failure keeps its own symbol, so it reads without colour.
                    Assert.Contains(new string('■', 25), row);
                    Assert.DoesNotContain("■■■■■■■■■■■■■■■■■■■■■■■■■■", row.Substring(0, row.IndexOf("  ", 20, StringComparison.Ordinal)));
                    Assert.Contains("■ pass", text);
                    break;
                case "per band":
                    Assert.Contains("++x+", row.Replace(" ", string.Empty));
                    Assert.Contains("worst", text);
                    break;
                default:
                    Assert.Contains("DC25", text);
                    Assert.DoesNotContain("++", text);
                    break;
            }
        }

        [Fact]
        public void Render_DenseOverflow_KeepsItsFormWhenFailuresArrive() {
            // A live check map: all passing at first, then failures land. The dense form chosen for a width must not flip.
            MatrixLabel[] dcs = new MatrixLabel[40];
            for (int i = 0; i < dcs.Length; i++) dcs[i] = new MatrixLabel("DC" + i, "site-" + (char)('A' + i / 10));
            Matrix matrix = new Matrix(new[] { new MatrixLabel("Replication") }, dcs) { Overflow = MatrixOverflow.Dense, ShowLegend = false };
            for (int c = 0; c < dcs.Length; c++) matrix.SetCell(0, c, StatusKind.Pass);
            int width = 58; // label 11 + gap + 40 cells + gap fits "x10 !10" only if the summary was reserved up front
            string before = TestTerminal.RenderText(matrix, width, glyphs: GlyphTier.Ascii);
            for (int c = 0; c < 20; c++) matrix.SetCell(0, c * 2, c % 2 == 0 ? StatusKind.Fail : StatusKind.Warn);
            string after = TestTerminal.RenderText(matrix, width, glyphs: GlyphTier.Ascii);
            string Form(string text) => Array.Find(text.Split('\n'), line => line.StartsWith("one cell per", StringComparison.Ordinal)) ?? string.Empty;
            Assert.NotEqual(string.Empty, Form(before));
            Assert.Equal(Form(before), Form(after));
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
