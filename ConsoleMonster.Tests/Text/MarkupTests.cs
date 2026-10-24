using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class MarkupTests {
        [Fact]
        public void NestedStylesInheritAndRestoreTheirParent() {
            StyledLine line = Markup.Parse("[red]a[bold on blue]b[/]c[/]d").Lines[0];
            Assert.Equal("abcd", line.PlainText);
            Assert.Equal(4, line.Count);
            Assert.Equal(Color.FromAnsi16(9), line[0].Style.Color);
            Assert.Equal(line[0].Style.Color, line[1].Style.Color);
            Assert.Equal(Color.FromAnsi16(12), line[1].Style.BackgroundColor);
            Assert.Equal(TextDecoration.Bold, line[1].Style.Decoration);
            Assert.Equal(line[0].Style, line[2].Style);
            Assert.True(line[3].Style.IsPlain);
        }

        [Theory]
        [InlineData("bold", TextDecoration.Bold)]
        [InlineData("dim", TextDecoration.Dim)]
        [InlineData("italic", TextDecoration.Italic)]
        [InlineData("underline", TextDecoration.Underline)]
        [InlineData("strikethrough", TextDecoration.Strikethrough)]
        [InlineData("reverse", TextDecoration.Reverse)]
        [InlineData("invert", TextDecoration.Reverse)]
        [InlineData("blink", TextDecoration.None)]
        public void RecognizesDecorations(string word, TextDecoration expected) {
            Assert.True(Markup.TryParseStyle(word, null, ColorDepth.TrueColor, out Style style));
            Assert.Equal(expected, style.Decoration);
        }

        [Theory]
        [InlineData("#123456", 0x12, 0x34, 0x56)]
        [InlineData("#abc", 0xAA, 0xBB, 0xCC)]
        [InlineData("rgb(1, 2, 3)", 1, 2, 3)]
        public void ParsesRgbForegroundAndBackground(string specification, int r, int g, int b) {
            Assert.True(Markup.TryParseStyle(specification + " on " + specification, null, ColorDepth.TrueColor, out Style style));
            Color expected = Color.FromRgb((byte)r, (byte)g, (byte)b);
            Assert.Equal(expected, style.Color);
            Assert.Equal(expected, style.BackgroundColor);
        }

        [Theory]
        [InlineData("208", 208)]
        [InlineData("0", 0)]
        [InlineData("255", 255)]
        [InlineData("color(108)", 108)]
        public void ParsesIndexedColors(string specification, int index) {
            Assert.True(Markup.TryParseStyle(specification, null, ColorDepth.TrueColor, out Style style));
            Assert.Equal(Color.FromAnsi256(index), style.Color);
        }

        [Fact]
        public void ResolvesThemeTokensAtRequestedDepth() {
            StyledText text = Markup.Parse("[pass]OK[/] [muted]quiet[/]", Theme.Dark, ColorDepth.Ansi16);
            Assert.Equal("OK quiet", text.Lines[0].PlainText);
            Assert.True(Theme.Dark.TryResolve("pass", ColorDepth.Ansi16, out Style pass));
            Assert.True(Theme.Dark.TryResolve("muted", ColorDepth.Ansi16, out Style muted));
            Assert.Equal(pass, text.Lines[0][0].Style);
            Assert.Equal(muted, text.Lines[0][2].Style);
        }

        [Fact]
        public void UsesCustomThemeForParsingAndStrictValidation() {
            ITheme theme = new NamedTheme();
            TextSpan span = Assert.Single(Markup.Parse("[brand]value[/]", theme, ColorDepth.TrueColor).Lines[0]);
            Assert.Equal("value", span.Text);
            Assert.Equal(new Style(Color.FromRgb(12, 34, 56), decoration: TextDecoration.Italic), span.Style);
            Assert.True(Markup.Validate("[brand]value[/]", theme, out string? error));
            Assert.Null(error);
            Assert.False(Markup.Validate("[brand]value[/]", out error));
            Assert.NotNull(error);
        }

        [Fact]
        public void ExplicitDefaultResetsInheritedColorOnlyInItsScope() {
            StyledLine line = Markup.Parse("[bold red on blue]a[default on default]b[/]c[/]").Lines[0];
            Assert.True(line[1].Style.Color.IsDefault);
            Assert.True(line[1].Style.BackgroundColor.IsDefault);
            Assert.Equal(TextDecoration.Bold, line[1].Style.Decoration);
            Assert.Equal(line[0].Style, line[2].Style);
        }

        [Fact]
        public void BareLinkTag_UsesItsTextAsTheTarget() {
            TextSpan span = Assert.Single(Markup.Parse("[link]click[/]").Lines[0]);
            Assert.Equal("click", span.Text);
            Assert.Equal("click", span.Style.Link);
        }

        [Fact]
        public void LinkTarget_SetsTheStyleLinkAndSurvivesNestedStyles() {
            StyledLine line = Markup.Parse("[link=https://example.com/a(1)]click [bold]here[/][/] after").Lines[0];
            List<TextSpan> spans = new List<TextSpan>(line);
            Assert.Equal("https://example.com/a(1)", spans[0].Style.Link);
            Assert.Equal("https://example.com/a(1)", spans[1].Style.Link);
            Assert.True((spans[1].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.Null(spans[2].Style.Link);
        }

        [Fact]
        public void RoleBackground_UsesTheRoleColour() {
            TextSpan span = Assert.Single(Markup.Parse("[white on fail]x[/]", Theme.Dark).Lines[0]);
            Assert.Equal(Theme.Dark.Resolve(ThemeToken.Fail, ColorDepth.TrueColor).Color, span.Style.BackgroundColor);
        }

        [Theory]
        [InlineData("[unknown]x[/]", "x")]
        [InlineData("[red on]x[/]", "x")]
        [InlineData("[rgb(999,2,3)]x[/]", "x")]
        [InlineData("[link=]x[/]", "x")]
        [InlineData("[unknown]x", "[unknown]x")]
        [InlineData("[bold", "[bold")]
        [InlineData("[red[blue]x", "[red[blue]x")]
        [InlineData("[/]text", "text")]
        [InlineData("a]b", "a]b")]
        [InlineData("[[red]]", "[red]")]
        [InlineData("[[[red]x[/]]]", "[x]")]
        public void LenientParsingDropsClosedUnknownTagsAndKeepsEscapes(string markup, string expected) {
            Assert.Equal(expected, Markup.Strip(markup));
        }

        [Fact]
        public void UnclosedTagsEndAtEofAndStyleContinuesAcrossLines() {
            StyledText text = Markup.Parse("[bold red]a\r\n\tb\rc\n");
            Assert.Equal(new[] { "a", "    b", "c", "" }, text.Lines.Select(line => line.PlainText));
            Assert.Equal(TextDecoration.Bold, text.Lines[1][0].Style.Decoration);
            Assert.Equal(text.Lines[0][0].Style, text.Lines[2][0].Style);
            Assert.Equal("a\n    b\nc\n", Markup.Strip("[bold red]a\r\n\tb\rc\n"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("plain")]
        [InlineData("\n\n")]
        [InlineData("[red] literal ] [bold]text")]
        public void EscapeRoundTripsLiteralText(string plain) {
            Assert.Equal(plain, Markup.Strip(Markup.Escape(plain)));
        }

        [Theory]
        [InlineData("[bold red]a[/]", true)]
        [InlineData("[pass]OK[/]", true)]
        [InlineData("[[a]]", true)]
        [InlineData("[link=https://example.com]x[/]", true)]
        [InlineData("[red]a", false)]
        [InlineData("[/]a", false)]
        [InlineData("[red on]a[/]", false)]
        [InlineData("[unknown]a[/]", false)]
        [InlineData("a]", false)]
        [InlineData("[bold", false)]
        public void StrictValidationRejectsMalformedOrUnbalancedMarkup(string markup, bool expected) {
            Assert.Equal(expected, Markup.Validate(markup, out string? error));
            Assert.Equal(expected, error == null);
        }

        [Theory]
        [InlineData(ColorDepth.None)]
        [InlineData(ColorDepth.Ansi16)]
        [InlineData(ColorDepth.Ansi256)]
        public void DownsamplesLiteralColorsAndDecorations(ColorDepth depth) {
            Style actual = Markup.Parse("[bold #123456 on #fedcba]x[/]", null, depth).Lines[0][0].Style;
            Assert.Equal(new Style(Color.FromHex("#123456"), Color.FromHex("#fedcba"), TextDecoration.Bold).Downsample(depth), actual);
        }
    }
}
