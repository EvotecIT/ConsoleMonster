using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Short values never break; free text gives way first and wraps onto at most three lines unless asked.</summary>
    public sealed class ShortValuesTests {
        private const string Long = "A long free-text description that keeps going well past the width of any reasonable column, so it has to wrap onto many lines when the table is narrow.";

        private static string[] Render(Table table, int width) =>
            TableShowcaseGoldenTests.WithInvariantCulture(() => Terminal.RenderText(table, TerminalProfile.Fixed(width, 60, ColorDepth.None, GlyphTier.Full), Theme.Dark))
                .Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

        private static Table Sample() {
            Table table = new Table { Border = TableBorder.Rounded, RecordViewRows = 0, CardsBelow = 0 };
            table.AddColumn(new TableColumn("Name"));
            table.AddColumn(new TableColumn("Enabled"));
            table.AddColumn(new TableColumn("Changed"));
            table.AddColumn(new TableColumn("Kind"));
            table.AddColumn(new TableColumn("Notes"));
            table.AddValues("alpha", true, new DateTime(2026, 10, 19, 14, 5, 0), DayOfWeek.Wednesday, Long);
            return table;
        }

        [Fact]
        public void NarrowTable_BooleansDatesAndEnumsStayOnOneLine() {
            string[] lines = Render(Sample(), 70);
            string first = lines.First(line => line.Contains("alpha"));
            Assert.Contains("yes", first);
            Assert.Contains("2026-10-19 14:05", first);
            Assert.Contains("Wednesday", first);
        }

        [Fact]
        public void LongText_WrapsOntoThreeLinesWithAnEllipsis() {
            string[] lines = Render(Sample(), 70);
            int body = lines.Count(line => line.StartsWith("│", StringComparison.Ordinal) && !line.Contains("Name"));
            Assert.Equal(3, body);
            Assert.Contains(lines, line => line.Contains("…"));
        }

        [Fact]
        public void WrapLinesZero_ShowsEveryLine() {
            Table table = Sample();
            table.WrapLines = 0;
            string[] lines = Render(table, 70);
            int body = lines.Count(line => line.StartsWith("│", StringComparison.Ordinal) && !line.Contains("Name"));
            Assert.True(body > 3);
            Assert.DoesNotContain(lines, line => line.Contains("…"));
        }

        [Fact]
        public void StatusWordsAsText_StayOnOneLine() {
            Table table = new Table { Border = TableBorder.Rounded, RecordViewRows = 0, CardsBelow = 0 };
            table.AddColumn(new TableColumn("Name"));
            table.AddColumn(new TableColumn("State") { Presenter = ConsoleMonster.Presenters.CellPresenters.StatusWord });
            table.AddColumn(new TableColumn("Notes"));
            table.AddValues("alpha", "Warning", Long);
            string[] lines = Render(table, 50);
            Assert.Contains(lines, line => line.Contains("alpha") && line.Contains("Warning"));
        }

        [Fact]
        public void ColumnMaxLinesUnlimited_ShowsEveryLineInsideATableThatKeepsThree() {
            Table table = Sample();
            table.Columns[4].MaxLines = TableColumn.Unlimited;
            string[] lines = Render(table, 70);
            int body = lines.Count(line => line.StartsWith("│", StringComparison.Ordinal) && !line.Contains("Name"));
            Assert.True(body > 3);
        }

        [Fact]
        public void ColumnMaxLines_OverridesTheTableDefault() {
            Table table = Sample();
            table.Columns[4].MaxLines = 1;
            string[] lines = Render(table, 70);
            int body = lines.Count(line => line.StartsWith("│", StringComparison.Ordinal) && !line.Contains("Name"));
            Assert.Equal(1, body);
        }
    }
}
