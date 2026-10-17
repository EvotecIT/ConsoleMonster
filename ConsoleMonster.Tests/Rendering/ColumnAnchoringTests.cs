using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// VT output re-anchors the cursor after glyphs whose width terminals disagree on, so a terminal that draws an emoji,
    /// a flag or an ambiguous symbol wider or narrower than measured cannot shift the rest of the row.
    /// </summary>
    public sealed class ColumnAnchoringTests {
        private const string Family = "\U0001F468‍\U0001F469‍\U0001F467";
        private const string Flag = "\U0001F1F5\U0001F1F1";

        // A conhost-like screen with a deliberately different width table: ZWJ families split into three 2-cell emoji,
        // flags draw as two 1-cell letters, ambiguous ▲ is 2 cells wide. The library still measures 2, 2 and 1.
        private static int WrongWidths(string glyph) {
            if (glyph == Family) {
                return 6;
            }
            if (glyph == Flag) {
                return 1;
            }
            if (glyph == "▲") {
                return 2;
            }
            return DisplayWidth.Measure(glyph);
        }

        private static CellBuffer Table() {
            Table table = new Table { Border = TableBorder.Rounded };
            table.AddColumn("Owner").AddColumn("Site").AddColumn("Status").AddColumn("Count", HorizontalAlignment.Right);
            table.AddPlainRow(Family + " family", Flag + " Warsaw", "▲ warn", "42");
            table.AddPlainRow("张伟", "Tokyo", "ok", "7");
            table.AddPlainRow("Ada", "London", "ok", "1234");
            RenderContext context = new RenderContext(TerminalProfile.Fixed(60, 10, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            return FrameComposer.RenderBlock(table, 60, context);
        }

        private static string Encode(CellBuffer buffer, bool anchor) {
            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < buffer.Height; y++) {
                Style? current = null;
                CellEncoder.AppendRun(sb, buffer, y, 0, buffer.Width, ColorDepth.None, ref current, anchor);
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        [Fact]
        public void Anchoring_WrongTerminalWidths_TrustedCellsKeepTheirColumns() {
            CellBuffer buffer = Table();
            StringTerminalWriter writer = new StringTerminalWriter();
            using (VtOutput output = new VtOutput(writer, TerminalProfile.Fixed(60, 10, ColorDepth.None, GlyphTier.Full), false)) {
                output.WriteBlock(buffer);
            }
            FakeVtScreen screen = new FakeVtScreen(60, buffer.Height + 1, WrongWidths);
            screen.Feed(writer.ToString());
            Assert.Equal(0, Misplaced(buffer, screen));
        }

        [Fact]
        public void Anchoring_Disabled_WrongTerminalWidthsShiftTheRow() {
            CellBuffer buffer = Table();
            FakeVtScreen screen = new FakeVtScreen(64, buffer.Height + 1, WrongWidths);
            screen.Feed(Encode(buffer, false));
            Assert.True(Misplaced(buffer, screen) > 10);
        }

        // Counts ASCII and box-drawing cells that do not appear at their buffer column on the screen. Cells holding the
        // mis-measured glyphs themselves (and their continuation) may be damaged; nothing else may move.
        private static int Misplaced(CellBuffer buffer, FakeVtScreen screen) {
            int misplaced = 0;
            for (int y = 0; y < buffer.Height; y++) {
                for (int x = 0; x < buffer.Width; x++) {
                    Cell cell = buffer[x, y];
                    if (cell.IsContinuation || cell.Glyph.Length != 1 || !(cell.Glyph[0] < 0x7F || (cell.Glyph[0] >= 0x2500 && cell.Glyph[0] <= 0x257F))) {
                        continue;
                    }
                    if (x > 0 && buffer[x - 1, y].IsContinuation == false && buffer[x - 1, y].Width == 2) {
                        continue;
                    }
                    if (screen.CellAt(x, y) != cell.Glyph) {
                        misplaced++;
                    }
                }
            }
            return misplaced;
        }
        [Fact]
        public void AnchoringGolden_WrongWidthTable_WithAndWithoutAnchors() {
            CellBuffer buffer = Table();
            StringBuilder text = new StringBuilder();
            foreach (bool anchor in new[] { false, true }) {
                FakeVtScreen screen = new FakeVtScreen(64, buffer.Height + 1, WrongWidths);
                screen.Feed(Encode(buffer, anchor));
                text.Append(anchor ? "== anchored (columns hold)\n" : "== unanchored (rows drift)\n");
                for (int y = 0; y < buffer.Height; y++) {
                    text.Append(screen.Grid(y)).Append('\n');
                }
            }
            Golden.Match("anchoring.wrong-widths.txt", text.ToString());
        }
    }
}
