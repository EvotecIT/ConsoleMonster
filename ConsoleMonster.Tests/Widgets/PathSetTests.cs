using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class PathSetTests {
        [Theory]
        [InlineData(120, GlyphTier.Full, "→")]
        [InlineData(28, GlyphTier.Ascii, "->")]
        public void Render_MultiplePaths_WrapsAtArrowsRetainingOriginChipsAndDestinations(int width, GlyphTier tier, string arrow) {
            PathHop shared = new PathHop("shared", "shared-hop");
            shared.Chips.Add("category");
            PathSet paths = new PathSet(new PathHop("origin", "start")) { HighlightSharedHops = true };
            paths.Add(shared, new PathHop("one", "end-one"));
            paths.Add(shared, new PathHop("two", "end-two"));
            string text = TestTerminal.RenderText(paths, width, glyphs: tier);
            Assert.Contains("end-one", text);
            Assert.Contains("end-two", text);
            Assert.Contains("[category]", text);
            Assert.Contains("shared-hop*", text);
            Assert.Contains("* shared hop", text);
            Assert.Equal(2, text.Split('\n').Count(row => row.Contains("start")));
            Assert.Equal(4, text.Split(new[] { arrow }, System.StringSplitOptions.None).Length - 1);
            Assert.All(text.Split('\n'), row => Assert.True(ConsoleMonster.Text.DisplayWidth.Measure(row) <= width));
        }

        [Fact]
        public void Render_RepeatedHopInOneChain_DoesNotMarkItAsSharedAcrossChains() {
            PathSet paths = new PathSet(new PathHop("origin", "start")) { HighlightSharedHops = true };
            PathHop hop = new PathHop("same", "twice");
            paths.Add(hop, hop);
            string text = TestTerminal.RenderText(paths, 120);
            Assert.DoesNotContain("twice*", text);
            Assert.DoesNotContain("shared hop", text);
        }

        [Fact]
        public void Render_PathLimit_NamesOmittedChains() {
            PathSet paths = new PathSet(new PathHop("origin", "start")) { MaxPaths = 1 };
            paths.Add(new PathHop("one", "end-one"));
            paths.Add(new PathHop("two", "end-two"));
            string text = TestTerminal.RenderText(paths, 80);
            Assert.Contains("+1 more paths", text);
            Assert.DoesNotContain("end-two", text);
        }
    }
}
