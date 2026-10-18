using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class TextWrapperTests {
        [Fact]
        public void Truncate_UsesExplicitTerminalWidthProfile() {
            TerminalWidthProfile narrowEmoji = TerminalWidthProfile.Standard.WithEmojiWidth(1);
            Assert.Equal("😀…", TextWrapper.Truncate("😀ab", 2, narrowEmoji));
            Assert.Equal("😀…", TextWrapper.TruncateMiddle("😀abc", 2, narrowEmoji));
            TerminalWidthProfile wideAmbiguous = TerminalWidthProfile.Standard.WithAmbiguousWidth(2);
            Assert.Equal("▲…", TextWrapper.Truncate("▲ab", 3, wideAmbiguous));
        }
        [Theory]
        [InlineData("one  two three", 8, "one  two|three")]
        [InlineData("one   two", 5, "one|two")]
        [InlineData("one two", 3, "one|two")]
        [InlineData("abcdefgh", 3, "abc|def|gh")]
        [InlineData("中文天地", 4, "中文|天地")]
        [InlineData("中文 a", 3, "中|文|a")]
        [InlineData("e\u0301e\u0301e\u0301", 2, "e\u0301e\u0301|e\u0301")]
        [InlineData("👩‍💻ab", 2, "👩‍💻|ab")]
        [InlineData("👍🏽a", 2, "👍🏽|a")]
        [InlineData("🇵🇱🇬🇧", 2, "🇵🇱|🇬🇧")]
        [InlineData("中文", 1, " | ")]
        [InlineData("abc", 0, "a|b|c")]
        [InlineData("", 5, "")]
        [InlineData("    ", 2, "")]
        public void WrapsWordsAndWholeGraphemesWithinBudget(string text, int width, string expected) {
            IReadOnlyList<StyledLine> lines = TextWrapper.Wrap(StyledLine.From(text), width);
            Assert.Equal(expected, string.Join("|", lines.Select(line => line.PlainText)));
            Assert.All(lines, line => Assert.InRange(line.Width, 0, Math.Max(1, width)));
        }

        [Fact]
        public void WrappingPreservesStylesAndInnerSpaces() {
            Style red = new Style(Color.FromAnsi16(9));
            Style blue = new Style(Color.FromAnsi16(12));
            StyledLine input = new StyledLine(new[] { new TextSpan("one  ", red), new TextSpan("two three", blue) });
            IReadOnlyList<StyledLine> lines = TextWrapper.Wrap(input, 8);
            Assert.Equal(new[] { "one  two", "three" }, lines.Select(line => line.PlainText));
            Assert.Equal(red, lines[0][0].Style);
            Assert.Equal("one  ", lines[0][0].Text);
            Assert.Equal(blue, lines[0][1].Style);
            Assert.Equal(blue, lines[1][0].Style);
        }

        [Fact]
        public void StyleBoundariesInsideGraphemesDoNotIntroduceBreaks() {
            Style red = new Style(Color.FromAnsi16(9));
            Style blue = new Style(Color.FromAnsi16(12));
            StyledLine input = new StyledLine(new[] {
                new TextSpan("👩", red), new TextSpan("‍💻a", blue), new TextSpan("\u0301b", red)
            });
            IReadOnlyList<StyledLine> lines = TextWrapper.Wrap(input, 2);
            Assert.Equal(new[] { "👩‍💻", "a\u0301b" }, lines.Select(line => line.PlainText));
            Assert.Equal(red, lines[0][0].Style);
            Assert.Equal(blue, lines[0][1].Style);
            Assert.Equal(blue, lines[1][0].Style);
            Assert.Equal(red, lines[1][1].Style);
            Assert.Equal("👩‍💻…", TextWrapper.Truncate(input, 3).PlainText);
        }

        [Theory]
        [InlineData("abcdef", 4, "…", "abc…")]
        [InlineData("abcdef", 1, "…", "…")]
        [InlineData("abcdef", 2, "...", "..")]
        [InlineData("abcdef", 0, "…", "")]
        [InlineData("abcdef", -1, "…", "")]
        [InlineData("abc", 3, "…", "abc")]
        [InlineData("中文天地", 4, "…", "中…")]
        [InlineData("中文", 3, "…", "中…")]
        [InlineData("e\u0301e\u0301e\u0301", 2, "…", "e\u0301…")]
        [InlineData("👩‍💻abc", 3, "…", "👩‍💻…")]
        [InlineData("abcdef", 4, "", "abcd")]
        [InlineData("abcdef", 1, "中", "a")]
        public void TruncatesEndWithoutSplittingGraphemes(string text, int width, string ellipsis, string expected) {
            string actual = TextWrapper.Truncate(text, width, ellipsis);
            Assert.Equal(expected, actual);
            Assert.True(DisplayWidth.Measure(actual) <= Math.Max(0, width));
        }

        [Fact]
        public void TruncationKeepsStylesAndUsesLastRetainedStyleForMarker() {
            Style red = new Style(Color.FromAnsi16(9));
            Style blue = new Style(Color.FromAnsi16(12));
            StyledLine input = new StyledLine(new[] { new TextSpan("ab", red), new TextSpan("cdef", blue) });
            StyledLine actual = TextWrapper.Truncate(input, 3);
            Assert.Equal("ab…", actual.PlainText);
            Assert.All(actual, span => Assert.Equal(red, span.Style));
        }

        [Theory]
        [InlineData("C:/projects/file.txt", 13, "…", "C:/pro…le.txt")]
        [InlineData("abcdef", 5, "…", "ab…ef")]
        [InlineData("abcdef", 4, "…", "ab…f")]
        [InlineData("abcdef", 1, "…", "…")]
        [InlineData("abcdef", 0, "…", "")]
        [InlineData("abc", 3, "…", "abc")]
        [InlineData("中文天地", 5, "…", "中…地")]
        [InlineData("👩‍💻abcdef👍🏽", 5, "…", "👩‍💻…👍🏽")]
        [InlineData("e\u0301abcde\u0301", 5, "…", "e\u0301a…de\u0301")]
        public void MiddleTruncationKeepsPathEndsAndWholeGraphemes(string text, int width, string ellipsis, string expected) {
            string actual = TextWrapper.TruncateMiddle(text, width, ellipsis);
            Assert.Equal(expected, actual);
            Assert.True(DisplayWidth.Measure(actual) <= Math.Max(0, width));
        }
    }
}
