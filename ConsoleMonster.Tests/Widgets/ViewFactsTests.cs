using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    /// <summary>The facts line under a default view: presented values, breaks only between facts, inline sparklines.</summary>
    public sealed class ViewFactsTests {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        [Fact]
        public void Facts_PresentValuesAndBreakOnlyBetweenFacts() {
            ViewFacts facts = new ViewFacts(new[] {
                new ViewFact(22, "folders"),
                new ViewFact(10, "files"),
                new ViewFact(120_832L, presenter: CellPresenters.Bytes),
                new ViewFact("CHANGELOG.MD", caption: "largest")
            });
            Assert.Equal(" 22 folders · 10 files · 118 KB · largest CHANGELOG.MD", TestTerminal.RenderText(facts, 80, culture: English).TrimEnd());

            string[] narrow = TestTerminal.RenderText(facts, 30, culture: English).Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToArray();
            Assert.Equal(new[] { " 22 folders · 10 files", " 118 KB · largest CHANGELOG.MD" }, narrow);
        }

        [Fact]
        public void NewLine_StartsEachGroupOnItsOwnLine() {
            ViewFacts facts = new ViewFacts(new[] {
                new ViewFact("6/6", "replies", "example.com"),
                new ViewFact(26, "ms", "avg"),
                new ViewFact("5/6", "replies", "evotec.xyz", role: ThemeToken.Warn, newLine: true),
                new ViewFact(24, "ms", "avg")
            });
            string[] lines = TestTerminal.RenderText(facts, 120, culture: English).Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToArray();
            Assert.Equal(new[] { " example.com 6/6 replies · avg 26 ms", " evotec.xyz 5/6 replies · avg 24 ms" }, lines);
        }

        [Fact]
        public void Sparkline_IsDrawnInlineWithoutItsLastValue_AndRolesColourTheValue() {
            ViewFacts facts = new ViewFacts(new[] {
                new ViewFact(3, "errors", role: ThemeToken.Fail),
                new ViewFact(new[] { 1.0, 4.0, 2.0, 8.0 }, caption: "per hour", presenter: CellPresenters.Sparkline)
            });
            Assert.Equal(" 3 errors · per hour ▁▄▂█", TestTerminal.RenderText(facts, 80, culture: English).TrimEnd());
            Assert.All(TestTerminal.RenderText(facts, 80, glyphs: GlyphTier.Ascii, culture: English), c => Assert.True(c < 128));

            RenderContext context = Terminal.CreateContext(TestTerminal.Profile(80, colors: ColorDepth.TrueColor));
            StyledLine line = facts.Lines(80, context)[0];
            TextSpan count = line.First(span => span.Text == "3");
            Assert.Equal(context.Style(ThemeToken.Fail).Color, count.Style.Color);
            Assert.NotEqual(context.Style(ThemeToken.Text).Color, count.Style.Color);
        }
    }
}
