using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    public sealed class EverydayPresenterTests {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Chip_PreservesLiteralCategoryWithAndWithoutColour(bool colour) {
            var profile = TerminalProfile.Fixed(60, 20, colour ? ColorDepth.TrueColor : ColorDepth.None, GlyphTier.Ascii);
            var context = new PresenterContext(new RenderContext(profile, Theme.Dark));
            Assert.Equal("[Function]", CellPresenters.FromName("Chip").Present("Function", context).Text);
            Assert.Equal("[[fail]literal[/]]", CellPresenters.Chip.Present("[fail]literal[/]", context).Text);
            Assert.True(CellPresenters.Chip.Present(null, context).IsEmpty);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MemberSignature_PreservesOverloadsAndMutesTypesInBothThemes(bool light) {
            const string signature = "string Substring(int startIndex), string Substring(int startIndex, int length)";
            var profile = TerminalProfile.Fixed(120, 20, ColorDepth.TrueColor, GlyphTier.Full);
            var render = new RenderContext(profile, light ? Theme.Light : Theme.Dark);
            var value = CellPresenters.FromName("MemberSignature").Present(signature, new PresenterContext(render));
            Assert.Equal(signature, value.Text);
            Assert.Contains(value.Line, span => span.Text == "string" && span.Style.Equals(render.Style(ThemeToken.Muted)));
            Assert.Contains(value.Line, span => span.Text == "Substring" && span.Style.Equals(render.Style(ThemeToken.Series6)));
            var tokens = new MemberSignatureHighlighter().Tokenize("System.Collections.Generic.List[string] Items {get;set;}");
            Assert.Equal(HighlightRole.Type, tokens.First().Role);
        }
    }
}
