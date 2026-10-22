using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class ShellPromptTests {
        private static RenderContext Context(ColorDepth depth = ColorDepth.None, GlyphTier glyphs = GlyphTier.Ascii) =>
            new RenderContext(new TerminalProfile(depth, glyphs, OutputMode.Interactive), Theme.Dark);

        [Theory]
        [InlineData(1.999, false)]
        [InlineData(2, false)]
        [InlineData(2.001, true)]
        public void DurationUsesCompletedHistoryThreshold(double seconds, bool shown) {
            string text = new ShellPrompt { Path = "/work/project", Duration = TimeSpan.FromSeconds(seconds) }.ToAnsi(Context(), 120);
            Assert.Equal(shown, text.Contains("2.0s"));
            Assert.EndsWith("[PASS] > ", text, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("/home/alex/project", "/home/alex", "~/project")]
        [InlineData("/home/alexander/project", "/home/alex", "/home/alexander/project")]
        [InlineData("C:\\Users\\Alex\\project", "c:\\users\\alex", "~\\project")]
        [InlineData("/HOME/alex/project", "/home/alex", "/HOME/alex/project")]
        public void HomeAbbreviationRespectsProviderPathBoundaries(string path, string home, string expected) {
            string text = new ShellPrompt { Path = path, Home = home }.ToAnsi(Context(), 120);
            Assert.StartsWith(expected + " ", text, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        [InlineData(20)]
        public void LongUnicodePathsKeepStatusAndCursorWithinWidth(int width) {
            string text = new ShellPrompt { Path = "/work/開発/👩‍💻/a-very-long-project-name/component", Branch = "feature/a-long-branch", Dirty = true,
                Succeeded = false, Duration = TimeSpan.FromSeconds(3.4) }.ToAnsi(Context(), width);
            Assert.True(DisplayWidth.Measure(text) <= width);
            Assert.EndsWith("[FAIL] > ", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\u001b", text, StringComparison.Ordinal);
        }

        [Fact]
        public void BranchAndPathAreLiteralDataAndControlsCannotEscape() {
            string text = new ShellPrompt { Path = "/work/[fail]literal\u001b\n", Branch = "branch\u001b[2J", Dirty = true }.ToAnsi(Context(), 120);
            Assert.Contains("[fail]literal", text);
            Assert.Contains("(branch[2J*)", text);
            Assert.DoesNotContain("\u001b", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\n", text, StringComparison.Ordinal);
        }

        [Fact]
        public void NarrowPromptElidesWholeFoldersAcrossCommandStates() {
            foreach (bool succeeded in new[] { true, false }) {
                ShellPrompt prompt = new ShellPrompt {
                    Path = @"C:\Users\Alex\src\Operations\Deployment\Scripts", Home = @"C:\Users\Alex",
                    Branch = "feature/themed-prompt", Dirty = true, Succeeded = succeeded,
                    Duration = succeeded ? TimeSpan.Zero : TimeSpan.FromSeconds(3.4)
                };
                string text = prompt.ToAnsi(Context(ColorDepth.None, GlyphTier.Full), 60);
                Assert.StartsWith(succeeded ? @"~\src\…\Deployment\Scripts " : @"~\src\…\Scripts ", text, StringComparison.Ordinal);
                Assert.True(DisplayWidth.Measure(text) <= 60);
            }
        }

        [Fact]
        public void ActiveThemeAndGlyphsStyleTheSamePromptWithoutChangingTerminalState() {
            ShellPrompt prompt = new ShellPrompt { Path = "~/src", Branch = "main", Dirty = true, Succeeded = false };
            RenderContext context = Context(ColorDepth.TrueColor, GlyphTier.Full);
            string result = prompt.ToAnsi(context, 60);
            Assert.Contains(SgrEncoder.Encode(context.Style(ThemeToken.Accent), context.Depth), result);
            Assert.Contains(SgrEncoder.Encode(context.Style(ThemeToken.Fail), context.Depth), result);
            Assert.Contains(context.Glyphs.StatusMarker(StatusKind.Fail), result);
            Assert.EndsWith(SgrEncoder.Reset, result, StringComparison.Ordinal);
        }
    }
}
