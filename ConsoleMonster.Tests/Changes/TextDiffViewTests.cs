using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Changes;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Changes {
    public sealed class TextDiffViewTests {
        [Theory]
        [InlineData(120, false)]
        [InlineData(60, false)]
        [InlineData(120, true)]
        [InlineData(60, true)]
        public void View_UsesDefaultOrCustomLiteralSideLabels(int width, bool custom) {
            TextDiffOptions options = new TextDiffOptions();
            if (custom) {
                options.ReferenceLabel = "[old] config";
                options.DifferenceLabel = "[new] config";
            }
            string text = TestTerminal.RenderText(new TextDiffView(TextDiff.Compare("old\n", "new\n", options)), width);
            string heading = text.Split('\n')[0];
            Assert.Contains(custom ? "[old] config" : "Before", heading);
            Assert.Contains(custom ? "[new] config" : "After", heading);
            Assert.Contains(width >= 120 ? "|" : " -> ", heading);
            Assert.Contains("- old", text);
            Assert.Contains("+ new", text);
        }

        [Theory]
        [InlineData(120)]
        [InlineData(60)]
        public void View_MatchesGoldenAtWideAndInlineWidths(int width) {
            TextDiffView view = new TextDiffView(TextDiff.Compare(
                "# settings\nserver=web01\nport=80\nmode=legacy\nkeep=one\nkeep=two\nkeep=three\nkeep=four\nend=yes\n",
                "# settings\nserver=web01\nport=443\nmode=secure\nkeep=one\nkeep=two\nkeep=three\nkeep=four\nend=yes\n",
                new TextDiffOptions { Context = 1 }));
            string text = TestTerminal.RenderText(view, width);
            Assert.Contains("-", text);
            Assert.Contains("+", text);
            Assert.Contains("4 unchanged lines (6-9 / 6-9)", text);
            Golden.Match("changes/text." + width + ".txt", text);
        }

        [Theory]
        [InlineData(119, false)]
        [InlineData(120, true)]
        public void View_ChangesLayoutAtExactly120Columns(int width, bool sideBySide) {
            string text = TestTerminal.RenderText(new TextDiffView(TextDiff.Compare("old\n", "new\n")), width);
            Assert.Equal(sideBySide, text.Split('\n').Any(line => line.Contains("- old") && line.Contains("+ new")));
            Assert.Equal(sideBySide, text.Contains("|"));
        }

        [Fact]
        public void View_HighlightsChangedWordsWithoutTintingTheirSharedPrefix() {
            TextDiffView view = new TextDiffView(TextDiff.Compare("server old value\n", "server new value\n"));
            foreach (ITheme theme in new ITheme[] { Theme.Dark, Theme.Light }) {
                CellBuffer frame = ConsoleExporter.Render(view, new ExportOptions { Width = 60, ColorDepth = ColorDepth.TrueColor }, theme);
                string[] lines = frame.ToPlainText().Split('\n');
                int removedRow = System.Array.FindIndex(lines, line => line.Contains("- server old value"));
                int addedRow = System.Array.FindIndex(lines, line => line.Contains("+ server new value"));
                Assert.True(removedRow >= 0 && addedRow >= 0);
                int oldColumn = lines[removedRow].IndexOf("old", System.StringComparison.Ordinal);
                int newColumn = lines[addedRow].IndexOf("new", System.StringComparison.Ordinal);
                Assert.NotEqual(frame[oldColumn - 2, removedRow].Style.Color, frame[oldColumn, removedRow].Style.Color);
                Assert.NotEqual(frame[oldColumn, removedRow].Style.Color, frame[newColumn, addedRow].Style.Color);
                Assert.Equal(TextDecoration.Bold, frame[oldColumn, removedRow].Style.Decoration & TextDecoration.Bold);
            }
        }

        [Theory]
        [InlineData("surrogate-pair", 120)]
        [InlineData("surrogate-pair", 60)]
        [InlineData("combining-mark", 120)]
        [InlineData("combining-mark", 60)]
        public void View_HighlightsCompleteChangedGraphemesInRenderedCells(string scenario, int width) {
            string before = scenario == "surrogate-pair" ? "\U0001F600" : "e\u0301";
            string after = scenario == "surrogate-pair" ? "\U0001F601" : "e\u0300";
            TextDiffView view = new TextDiffView(TextDiff.Compare("value " + before + " tail\n", "value " + after + " tail\n"));
            foreach (ITheme theme in new ITheme[] { Theme.Dark, Theme.Light }) {
                CellBuffer frame = ConsoleExporter.Render(view,
                    new ExportOptions { Width = width, ColorDepth = ColorDepth.TrueColor }, theme);
                string text = frame.ToPlainText();
                Assert.Contains("- value " + before + " tail", text);
                Assert.Contains("+ value " + after + " tail", text);
                Cell removed = FindGlyph(frame, before);
                Cell added = FindGlyph(frame, after);
                Assert.Equal(theme.Resolve(ThemeToken.Fail, ColorDepth.TrueColor).Color, removed.Style.Color);
                Assert.Equal(theme.Resolve(ThemeToken.Pass, ColorDepth.TrueColor).Color, added.Style.Color);
                Assert.NotEqual(removed.Style.Color, added.Style.Color);
                Assert.Equal(TextDecoration.Bold, removed.Style.Decoration & TextDecoration.Bold);
                Assert.Equal(TextDecoration.Bold, added.Style.Decoration & TextDecoration.Bold);
            }
            CellBuffer plain = ConsoleExporter.Render(view,
                new ExportOptions { Width = width, ColorDepth = ColorDepth.None }, Theme.Dark);
            Assert.Equal(before, FindGlyph(plain, before).Glyph);
            Assert.Equal(after, FindGlyph(plain, after).Glyph);
            Golden.Match("changes/text.unicode." + scenario + "." + width + ".txt", plain.ToPlainText());
        }

        private static Cell FindGlyph(CellBuffer frame, string glyph) => Assert.Single(
            Enumerable.Range(0, frame.Height).SelectMany(y => Enumerable.Range(0, frame.Width).Select(x => frame[x, y])),
            cell => cell.Glyph == glyph);

        [Fact]
        public void View_PreservesLiteralMarkupAndWrapsUnicodeWithoutLosingContent() {
            string source = "[red] literal 界 " + new string('x', 100) + "\n";
            string text = TestTerminal.RenderText(new TextDiffView(TextDiff.Compare("", source)), 60);
            Assert.Contains("[red] literal 界", text);
            Assert.Equal(100, text.Count(c => c == 'x'));
            Assert.DoesNotContain("[pass", text);
        }
    }
}
