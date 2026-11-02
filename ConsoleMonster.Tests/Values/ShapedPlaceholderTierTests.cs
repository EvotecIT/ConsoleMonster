using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Values {
    public sealed class ShapedPlaceholderTierTests {
        private static string Render(Table table, GlyphTier tier) =>
            Terminal.RenderText(table, TerminalProfile.Fixed(90, 10, ColorDepth.None, tier), Theme.Dark);

        [Fact]
        public void PlaceholdersAndCallerTextFollowTheDestinationTier() {
            var table = new Table { RecordViewRows = 0 };
            table.AddColumn("Note");
            table.AddColumn("Tags");
            table.AddColumn("Members");
            table.AddValues("café — ✓ …", ValueShaper.Cell(new string[0]), ValueShaper.Cell(Enumerable.Range(1, 9).Select(i => "m" + i).ToList()));

            string ascii = Render(table, GlyphTier.Ascii);
            Assert.Contains("caf? - + ...", ascii);
            Assert.Contains("m5 ... +4", ascii);
            Assert.Contains("(empty)", ascii);
            Assert.All(ascii, c => Assert.InRange((int)c, 0, 127));

            string full = Render(table, GlyphTier.Full);
            Assert.Contains("m5 … +4", full);
            Assert.Contains("(empty)", full);
            // The public text keeps the Unicode placeholders for logs, exports and string consumers.
            Assert.Equal("(empty)", ValueShaper.Text(new string[0]));
        }
    }
}
