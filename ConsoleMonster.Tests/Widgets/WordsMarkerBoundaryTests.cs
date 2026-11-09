using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Dates;
using ConsoleMonster.Live;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class WordsMarkerBoundaryTests {
        private static string Render(IWidget widget, int width, GlyphTier tier) {
            var profile = TestTerminal.Profile(width, glyphs: tier).WithStatusMarkers(StatusMarkerStyle.Words);
            return FrameComposer.RenderBlock(widget, width, Terminal.CreateContext(profile)).ToPlainText();
        }

        [Theory]
        [InlineData(GlyphTier.Full, false, "✓ Worker")]
        [InlineData(GlyphTier.Basic, false, "√ Worker")]
        [InlineData(GlyphTier.Ascii, false, "+ Worker")]
        [InlineData(GlyphTier.Full, true, "✓ 1 done")]
        public void Lanes_WordsMarkers_FixedSymbolSlotPreservesFollowingText(GlyphTier tier, bool collapsed, string prefix) {
            var lanes = new Lanes { CollapseWhenFinished = collapsed };
            lanes.Apply(new LiveEvent("lanes", "add", "worker", "Worker", total: 1));
            lanes.Apply(new LiveEvent("lanes", "complete", "worker", status: "pass"));
            Assert.StartsWith(prefix, Render(lanes, 60, tier));
        }

        [Theory]
        [InlineData(GlyphTier.Full, "02✗ 03  04", "✗ Fail")]
        [InlineData(GlyphTier.Basic, "02× 03  04", "× Fail")]
        [InlineData(GlyphTier.Ascii, "02x 03  04", "x Fail")]
        public void Calendar_WordsMarkers_FixedDaySlotPreservesAdjacentDates(GlyphTier tier, string dates, string legend) {
            var calendar = new Calendar(2026, 10, new[] {
                new CalendarMark(new DateTime(2026, 10, 2), StatusKind.Fail, "Backup")
            }) { FirstDayOfWeek = DayOfWeek.Monday };
            string text = Render(calendar, 40, tier);
            Assert.Contains(dates, text);
            Assert.Contains(legend, text);
        }

        [Theory]
        [InlineData(StatusKind.Pass, false)]
        [InlineData(StatusKind.Fail, false)]
        [InlineData(StatusKind.Pass, true)]
        [InlineData(StatusKind.Fail, true)]
        public void StatusCells_WordsMarkers_NameStatusOnceAndPreserveDistinctText(StatusKind status, bool consoleView) {
            var table = new Table { ShowHeader = false, RecordViewRows = 0, Border = TableBorder.None };
            table.AddColumn(new TableColumn("State") { Presenter = new StatusPresenter() });
            table.AddRow(TableCell.ForStatus(status));
            table.AddRow(TableCell.FromValue(status));
            table.AddRow(TableCell.ForStatus(status, "verification failed"));
            IWidget widget = consoleView ? ConsoleViewStyle.Wrap(table) : table;
            string[] rows = Render(widget, 60, GlyphTier.Full).Split('\n');
            string marker = status == StatusKind.Pass ? "[OK]" : "[FAIL]";
            Assert.Equal(marker, rows[0].Trim());
            Assert.Equal(marker, rows[1].Trim());
            Assert.Equal(marker + " verification failed", rows[2].Trim());
        }
    }
}
