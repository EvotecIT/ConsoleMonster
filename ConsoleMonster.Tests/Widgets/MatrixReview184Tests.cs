using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class MatrixReview184Tests {
        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.TrueColor, 120, false)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16, 60, false)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 60, false)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 120, true)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 60, true)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 39, true)]
        public void Render_CellAndLegendMarkerOverrides_KeepOriginalStatusesTonesAndCounts(GlyphTier tier, ColorDepth depth, int width, bool list) {
            Matrix matrix = new Matrix(new[] { new MatrixLabel("row"), new MatrixLabel("row2") }, new[] { new MatrixLabel("q"), new MatrixLabel("u") }) {
                NonHealthyOnly = list, ListDirection = "<-"
            };
            matrix.SetCell(0, 0, StatusKind.Warn, "query failed", marker: "?");
            matrix.SetCell(0, 1, StatusKind.Info, "unknown", marker: "?");
            matrix.SetCell(1, 0, StatusKind.Warn, "stale");
            matrix.SetCell(1, 1, StatusKind.Warn, "ignored", marker: "?", diagonal: true);
            matrix.LegendLabels[StatusKind.Warn] = "attention";
            matrix.LegendLabels[StatusKind.Info] = "unknown";
            matrix.LegendMarkers[StatusKind.Warn] = "?";
            matrix.LegendMarkers[StatusKind.Info] = "?";
            Review184Capture capture = new Review184Capture(matrix, width, tier, depth);
            string raw = capture.Raw.ToPlainText();
            string compact = System.Text.RegularExpressions.Regex.Replace(capture.Decoded, @"\s+", " ");
            Assert.Equal(StatusKind.Warn, matrix.GetCell(0, 0)!.Status);
            Assert.Equal(StatusKind.Info, matrix.GetCell(0, 1)!.Status);
            Assert.Equal("?", matrix.GetCell(0, 0)!.Marker);
            Assert.Null(matrix.GetCell(1, 0)!.Marker);
            Assert.Contains("? attention 2", compact);
            Assert.Contains("? unknown 1", compact);
            Assert.Contains("self 1", compact);
            Assert.DoesNotContain("! ?", raw);
            Assert.DoesNotContain("i unknown", raw);
            string warn = capture.Context.Glyphs.StatusSymbol(StatusKind.Warn);
            string query = list ? "? row <- q  query failed" : "? query failed";
            string unknown = list ? "? row <- u  unknown" : "? unknown";
            string stale = list ? warn + " row2 <- q  stale" : warn + " stale";
            Assert.Contains(query, raw);
            Assert.Contains(unknown, capture.Decoded);
            Assert.Contains(stale, capture.Decoded);
            string[] lines = raw.Split('\n');
            int row = System.Array.FindIndex(lines, line => line.Contains(query));
            int x = lines[row].IndexOf(query, System.StringComparison.Ordinal);
            Assert.Equal(capture.Context.Style(StatusKind.Warn).Color, capture.Raw[x, row].Style.Color);
        }

        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.TrueColor, 120, false)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16, 60, false)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None, 60, false)]
        [InlineData(GlyphTier.Basic, ColorDepth.None, 120, true)]
        [InlineData(GlyphTier.Ascii, ColorDepth.TrueColor, 60, true)]
        [InlineData(GlyphTier.Full, ColorDepth.None, 39, true)]
        public void Render_OptInAxesAndLegendLabels_KeepDirectionAndLiteralDomainWordsInEveryLayout(GlyphTier tier, ColorDepth depth, int width, bool list) {
            MatrixLabel[] labels = { new MatrixLabel("row-a"), new MatrixLabel("row-b") };
            Matrix matrix = new Matrix(labels, labels) {
                RowAxisCaption = "destination", ColumnAxisCaption = "source", ListDirection = "←", AsciiListDirection = "<-",
                SelfLegendLabel = "self[[literal]]", MissingLegendLabel = "no partnership", NonHealthyOnly = list
            };
            matrix.LegendLabels[StatusKind.Pass] = "healthy";
            matrix.LegendLabels[StatusKind.Fail] = "failing";
            matrix.SetCell(0, 0, null, diagonal: true).SetCell(0, 1, StatusKind.Pass, "5m").SetCell(1, 0, StatusKind.Fail, "3d");
            Review184Capture capture = new Review184Capture(matrix, width, tier, depth);
            string raw = capture.Raw.ToPlainText();
            string legendText = System.Text.RegularExpressions.Regex.Replace(raw, @"\s+", " ");
            Assert.Contains("destination", raw);
            Assert.Contains("source", raw);
            Assert.Contains("healthy 1", legendText);
            Assert.Contains("failing 1", legendText);
            Assert.Contains("no partnership 1", legendText);
            Assert.Contains("self[[literal]] 1", legendText);
            Assert.Contains("healthy 1", capture.Decoded);
            if (list) {
                Assert.Contains(tier == GlyphTier.Ascii ? "<-" : "←", raw);
                Assert.Contains(capture.Context.Glyphs.StatusSymbol(StatusKind.Fail) + " destination", raw);
            } else Assert.Contains(capture.Context.Glyphs.StatusSymbol(StatusKind.Pass) + " 5m", raw);
            Assert.DoesNotContain("+5m", raw);
        }

        [Theory]
        [InlineData(GlyphTier.Basic, ColorDepth.TrueColor)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None)]
        public void Render_StructuredStatusCellAndMatrixValue_SeparateGlyphsWithoutChangingLiteralData(GlyphTier tier, ColorDepth depth) {
            Matrix matrix = new Matrix(new[] { new MatrixLabel("row") }, new[] { new MatrixLabel("column") });
            matrix.SetCell(0, 0, StatusKind.Info, "Unknown");
            Review184Capture capture = new Review184Capture(matrix, 120, tier, depth);
            Assert.Contains(capture.Context.Glyphs.StatusSymbol(StatusKind.Info) + " Unknown", capture.Decoded);
            Assert.DoesNotContain("iUnknown", capture.Decoded);
            Table table = new Table { Border = TableBorder.None, RecordViewRows = 0, ShowHeader = false };
            table.AddColumn("Literal").AddColumn("State");
            table.AddRow("iUnknown", TableCell.ForStatus(StatusKind.Info, "Unknown"));
            Review184Capture cells = new Review184Capture(table, 60, tier, depth);
            Assert.Contains("iUnknown", cells.Decoded);
            Assert.Contains(cells.Context.Glyphs.StatusMarker(StatusKind.Info) + " Unknown", cells.Decoded);
        }
    }
}
