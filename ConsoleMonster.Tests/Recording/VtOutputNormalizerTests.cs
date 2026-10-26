using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class VtOutputNormalizerTests {
        [Theory]
        [InlineData("A\r\n\u001b[38;5;8mB\u001b[m", "A\u001b[m\u001b[38;5;8m\r\nB")]
        [InlineData("A\u001b[?25h\u001b[?9001l\u001b[?1004l\u001b[m", "A\u001b[m\u001b[?1004l\u001b[?25h\u001b[?9001l")]
        [InlineData("help paragraph      \r\nnext", "help paragraph\r\nnext")]
        [InlineData("A\u001b]0;window title\a\r\nB", "A\r\n\u001b]2;other title\aB")]
        public void IncidentalHostOrderingHasOneEncoding(string left, string right) {
            Assert.Equal(new VtOutputNormalizer(60, 30).Normalize(left), new VtOutputNormalizer(60, 30).Normalize(right));
        }

        [Theory]
        [InlineData("\u001b[31mred\u001b[0m \u001b[44m \r\n\u001b[K\u001b[0m界\u001b[4m \u001b[24m!")]
        [InlineData("\u001b[?1049h\u001b[?2026h\u001b[2J\u001b[H╭──╮\r\n│界│\r\n╰──╯\u001b[?2026l\u001b[?1049lDone")]
        [InlineData("\u001b]8;;https://example.com\u001b\\link \u001b]8;;\u001b\\\r\nnext")]
        [InlineData("\u001b[31m \u0301!")]
        [InlineData("\u001b[31m \r\n\u001b[1;2H\u0301")]
        [InlineData("\u001b[3;1H\u001b[41m\v")]
        [InlineData("\u001b[3;1H\u001b[41m\f")]
        [InlineData("\u001b[31m\u001b7\u001b[32mX\u001b8\u001b[32mY")]
        [InlineData("\u001b[31m\u001b[s\u001b[32mX\u001b[uY")]
        [InlineData("\u001b[31m\u001b[?1048h\u001b[32mX\u001b[?1048lY")]
        [InlineData("\u001b[31m\u001b[?1049h\u001b[32mX\u001b[?1049lY")]
        [InlineData("HELLO\r     \r\nX")]
        [InlineData("12345678901       \r\nX")]
        [InlineData("\u001b[41mA\r\nB\u001b[0m\r\nC\r\nD\u001b[2K")]
        public void VisibleCellsBackgroundsDecorationsLinksAndScrollbackSurvive(string original) {
            string normalized = new VtOutputNormalizer(12, 3).Normalize(original);
            Assert.Equal(normalized, new VtOutputNormalizer(12, 3).Normalize(normalized));
            var before = new VtScreen(12, 3); before.Feed(original);
            var after = new VtScreen(12, 3); after.Feed(normalized);
            Same(before.Snapshot(), after.Snapshot()); Same(before.Transcript(), after.Transcript());
        }

        [Fact]
        public void TextNewlinesAreOptInAndStylesPersistBetweenCompleteSegments() {
            var normalizer = new VtOutputNormalizer(12, 3);
            string text = normalizer.Normalize("\u001b[31mA\n", true) + normalizer.Normalize("B\u001b[0m\n", true);
            Assert.Equal(new VtOutputNormalizer(12, 3).Normalize("\u001b[31mA\r\nB\u001b[0m\r\n"), text);
            Assert.Equal("A\nB", new VtOutputNormalizer(12, 3).Normalize("A\nB"));
            Assert.Equal("A\r\nB", new VtOutputNormalizer(12, 3).Normalize("A\nB", true));
        }

        [Theory]
        [InlineData("\u001b7", "\u001b8")]
        [InlineData("\u001b[s", "\u001b[u")]
        [InlineData("\u001b[?1048h", "\u001b[?1048l")]
        [InlineData("\u001b[?1049h", "\u001b[?1049l")]
        public void RestoredRenditionAndExplicitFollowingSgrBothTakeEffect(string save, string restore) {
            string source = "\u001b[31m" + save + "\u001b[32mX" + restore + "Y\u001b[32mZ";
            var screen = new VtScreen(12, 3);
            screen.Feed(new VtOutputNormalizer(12, 3).Normalize(source));
            Assert.Equal("YZ", screen.Snapshot().GetRowText(0).TrimEnd());
            Assert.Equal(Color.FromAnsi16(1), screen.Snapshot()[0, 0].Style.Color);
            Assert.Equal(Color.FromAnsi16(2), screen.Snapshot()[1, 0].Style.Color);
        }

        [Fact]
        public void CursorRevisitsPreserveBlankBaseStylesAcrossSegments() {
            var normalizer = new VtOutputNormalizer(12, 3);
            string text = normalizer.Normalize("\u001b[31m \r\n") + normalizer.Normalize("\u001b[1;2H\u0301");
            var screen = new VtScreen(12, 3); screen.Feed(text);
            Assert.Equal(" \u0301", screen.Snapshot()[0, 0].Glyph);
            Assert.Equal(Color.FromAnsi16(1), screen.Snapshot()[0, 0].Style.Color);
        }

        [Theory]
        [InlineData("\u001b[31")]
        [InlineData("\u001b[5mblinking")]
        public void UnsupportedOrTruncatedOutputCannotBeSilentlyNormalized(string text) {
            Assert.Throws<FormatException>(() => new VtOutputNormalizer(12, 3).Normalize(text));
        }

        private static void Same(CellBuffer a, CellBuffer b) {
            Assert.Equal(a.Width, b.Width); Assert.Equal(a.Height, b.Height);
            for (int y = 0; y < a.Height; y++) {
                for (int x = 0; x < a.Width; x++) {
                    Assert.Equal(a[x, y].Glyph, b[x, y].Glyph);
                    Assert.Equal(a[x, y].Width, b[x, y].Width);
                    var left = a[x, y].Style; var right = b[x, y].Style;
                    // Foreground on an undecorated blank has no visible effect.
                    if (a[x, y].Glyph == " " && left.Decoration == 0 && right.Decoration == 0) {
                        left = left.WithColor(default); right = right.WithColor(default);
                    }
                    Assert.Equal(left, right);
                }
            }
        }
    }
}
