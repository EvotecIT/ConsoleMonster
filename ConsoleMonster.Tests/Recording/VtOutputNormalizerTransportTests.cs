using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class VtOutputNormalizerTransportTests {
        // Genuine strict-repeat failures from two sealed dd742c1f native Windows runs.
        // The original failed report stays failed; these fixtures protect the canonical encoder only.
        [Theory]
        [InlineData("help-ps5-120-dark", 120, 18)]
        [InlineData("help-ps5-60-dark", 60, 18)]
        [InlineData("help-ps7-120-dark", 120, 18)]
        [InlineData("help-ps7-120-light", 120, 18)]
        [InlineData("help-ps7-60-dark", 60, 18)]
        [InlineData("help-ps7-60-light", 60, 18)]
        [InlineData("native-ps5-120-dark", 120, 32)]
        [InlineData("native-ps5-120-light", 120, 32)]
        public void NativeHelpTransportAlternativesHaveOneCanonicalEncoding(string name, int width, int height) {
            string left = Read("capture1", name), right = Read("capture2", name);
            string canonical = new VtOutputNormalizer(width, height).Normalize(left);
            Assert.Equal(canonical, new VtOutputNormalizer(width, height).Normalize(right));
            Assert.Equal(canonical, new VtOutputNormalizer(width, height).Normalize(canonical));
            foreach (string original in new[] { left, right }) {
                var source = new VtScreen(width, height); source.Feed(original);
                var output = new VtScreen(width, height); output.Feed(canonical);
                VtOutputNormalizerTests.Same(source.Snapshot(), output.Snapshot());
                VtOutputNormalizerTests.Same(source.Transcript(), output.Transcript());
                Assert.Equal(source.Cursor.X, output.Cursor.X);
                Assert.Equal(source.Cursor.Y, output.Cursor.Y);
                Assert.Equal(source.Cursor.PendingWrap, output.Cursor.PendingWrap);
                Assert.Equal(source.Cursor.OriginTop, output.Cursor.OriginTop);
                Assert.Equal(source.Cursor.Bottom, output.Cursor.Bottom);
                if (name.StartsWith("help-")) {
                    List<CellBuffer> pages = Pages(original, width, height);
                    List<CellBuffer> encodedPages = Pages(canonical, width, height);
                    Assert.Equal(3, pages.Count);
                    Assert.Equal(pages.Count, encodedPages.Count);
                    for (int i = 0; i < pages.Count; i++) { VtOutputNormalizerTests.Same(pages[i], encodedPages[i]); }
                }
            }
        }

        private static List<CellBuffer> Pages(string text, int width, int height) {
            var screen = new VtScreen(width, height);
            var pages = new List<CellBuffer>();
            string previous = string.Empty;
            foreach (char character in text) {
                screen.Feed(character.ToString());
                if (!char.IsDigit(character) && character != 't') { continue; }
                CellBuffer viewport = screen.Snapshot();
                for (int row = height - 1; row >= 0; row--) {
                    string footer = viewport.GetRowText(row);
                    Match match = Regex.Match(footer, @"Page ([0-9]+) of ([0-9]+)");
                    if (!match.Success || !footer.Contains("q quit")) { continue; }
                    if (match.Groups[1].Value != previous) {
                        previous = match.Groups[1].Value;
                        Assert.Equal(pages.Count == 1 ? "2" : "1", previous);
                        pages.Add(viewport);
                    }
                    break;
                }
            }
            return pages;
        }

        private static string Read(string run, string name) {
            using Stream stream = typeof(VtOutputNormalizerTransportTests).Assembly.GetManifestResourceStream(
                "ConsoleMonster.Tests.Recording.Fixtures.help_repeat." + run + "." + name + ".ansi")!;
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
