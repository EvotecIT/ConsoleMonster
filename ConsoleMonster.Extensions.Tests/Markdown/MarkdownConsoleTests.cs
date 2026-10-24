using System;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Markdown;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Markdown {
    public sealed class MarkdownConsoleTests {
        private static CellBuffer Render(string source, int width = 60, bool links = false, GlyphTier tier = GlyphTier.Full) =>
            FrameComposer.RenderBlock(MarkdownConsole.Parse(source), width,
                new RenderContext(TerminalProfile.Fixed(width, 30, ColorDepth.TrueColor, tier).WithHyperlinks(links), Theme.Dark));

        [Theory]
        [InlineData(120, GlyphTier.Full)]
        [InlineData(60, GlyphTier.Full)]
        [InlineData(60, GlyphTier.Ascii)]
        public void CompleteDocument_ReflowsWithoutLosingContent(int width, GlyphTier tier) {
            string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "everyday.md"));
            string text = Render(source, width, tier: tier).ToPlainText();
            Golden.Match("markdown/document." + width + "." + tier.ToString().ToLowerInvariant() + ".txt", text.TrimEnd() + "\n");
            Assert.Contains("Deployment notebook", text);
            Assert.Contains("Compact terminal reading", text);
            Assert.Contains("5. Continue only after verification.", text);
            Assert.Contains("incident review", text);
            Assert.Contains("deployment-notes", text);
            Assert.Contains("検証", text);
            Assert.All(text.Split('\n'), line => Assert.True(DisplayWidth.Measure(line) <= width));
        }

        [Fact]
        public void CommonMark_NestingEscapesAndEntitiesRetainStyles() {
            CellBuffer frame = Render("**bold *both*** &amp; `[fail]literal[/]` \\*plain\\*");
            Assert.Equal("bold both & [fail]literal[/] *plain*", frame.ToPlainText());
            Assert.True((frame[0, 0].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.Equal(TextDecoration.Bold | TextDecoration.Italic,
                frame[5, 0].Style.Decoration & (TextDecoration.Bold | TextDecoration.Italic));
        }

        [Fact]
        public void CommonMark_OrderedStartNestedQuotesAndCodeUseNativeWidgets() {
            MarkdownView view = MarkdownConsole.Parse("# Title\n\n3. first\n4. second\n\n> quoted\n>\n> - nested\n\n```csharp\nint count = 42;\n```");
            string text = FrameComposer.RenderBlock(view, 60, new RenderContext(TerminalProfile.Fixed(60, 30), Theme.Dark)).ToPlainText();
            Assert.Contains("3. first", text);
            Assert.Contains("4. second", text);
            Assert.Contains("│ quoted", text);
            Assert.Contains("│ - nested", text);
            Assert.Single(view.Children.OfType<CodeView>());
            Assert.Contains("csharp", text);
            Assert.Contains("int count = 42;", text);
        }

        [Fact]
        public void PipeTable_KeepsInlineStylesAndAlignment() {
            CellBuffer frame = Render("| Label | Value |\n| :--- | ---: |\n| **first** | 12 |\n| second | 3 |", 60);
            string[] rows = frame.ToPlainText().Split('\n');
            string first = Assert.Single(rows, row => row.Contains("first"));
            string second = Assert.Single(rows, row => row.Contains("second"));
            Assert.Equal(first.IndexOf("12", StringComparison.Ordinal) + 1, second.IndexOf('3'));
            int y = Array.FindIndex(rows, row => row.Contains("first"));
            Assert.True((frame[first.IndexOf("first", StringComparison.Ordinal), y].Style.Decoration & TextDecoration.Bold) != 0);
        }

        [Fact]
        public void ReferenceLinks_UseCapabilityAndAppendOneMutedDestination() {
            const string source = "[**Read** more][guide]\n\n[guide]: https://example.org/guide";
            CellBuffer plain = Render(source);
            Assert.Equal("Read more (https://example.org/guide)", plain.ToPlainText());
            Assert.Equal(Theme.Dark.Resolve(ThemeToken.Muted, ColorDepth.TrueColor).Foreground, plain[10, 0].Style.Foreground);
            CellBuffer linked = Render(source, links: true);
            Assert.Equal("Read more", linked.ToPlainText());
            Assert.Equal("https://example.org/guide", linked[0, 0].Style.Link);
            Assert.Equal("https://example.org/guide", linked[5, 0].Style.Link);
        }

        [Fact]
        public void UnsafeLinks_StayVisibleWithoutControlSequences() {
            foreach (string address in new[] { "javascript:alert(1)", "data:text/html,hello" }) {
                CellBuffer frame = Render("[open](" + address + ")", links: true);
                Assert.Contains("open", frame.ToPlainText());
                for (int y = 0; y < frame.Height; y++) {
                    for (int x = 0; x < frame.Width; x++) { Assert.Null(frame[x, y].Style.Link); }
                }
                Assert.DoesNotContain("\u001b", frame.ToPlainText());
            }
        }

        [Fact]
        public void HtmlAndImages_RemainLiteralWithoutFetchingOrMarkupInterpretation() {
            string text = Render("before <script>alert(1)</script> after\n\n<div>[fail]literal[/]</div>\n\n![diagram](https://example.invalid/image.png)").ToPlainText();
            Assert.Contains("<script>alert(1)</script>", text);
            Assert.Contains("<div>[fail]literal[/]</div>", text);
            Assert.Contains("[Image: diagram]", text);
            Assert.Contains("https://example.invalid/image.png", text);
        }

        [Fact]
        public void LinkedImages_UseTheOuterDestinationAndLiteralLabel() {
            foreach (string source in new[] {
                "[![Build](https://example.org/badge.svg)](https://example.org/build)",
                "[Build ![badge](https://example.org/badge.svg) result](https://example.org/build)"
            }) {
                CellBuffer frame = Render(source, 120, links: true);
                Assert.Contains("[Image:", frame.ToPlainText());
                for (int y = 0; y < frame.Height; y++) {
                    for (int x = 0; x < frame.Width; x++) {
                        if (frame[x, y].Style.Link != null) {
                            Assert.Equal("https://example.org/build", frame[x, y].Style.Link);
                        }
                    }
                }
                Assert.Contains("https://example.org/build", Render(source, 120).ToPlainText());
            }
        }

        [Theory]
        [InlineData("u")]
        [InlineData("sup")]
        [InlineData("sub")]
        [InlineData("ins")]
        [InlineData("q")]
        public void HtmlWrappers_StayLiteralInsideEmphasis(string tag) {
            string source = "**Read <" + tag + ">this note</" + tag + "> before continuing.**";
            CellBuffer frame = Render(source, 120);
            Assert.Equal("Read <" + tag + ">this note</" + tag + "> before continuing.", frame.ToPlainText());
            Assert.True((frame[0, 0].Style.Decoration & TextDecoration.Bold) != 0);
        }

        [Fact]
        public void LineBreaks_DistinguishHardAndSoftSourceBreaks() =>
            Assert.Equal("soft continuation\n\nhard\nbreak", Render("soft\ncontinuation\n\nhard\\\nbreak").ToPlainText());

        [Fact]
        public void EmptyAndNull_AreExplicitContracts() {
            Assert.Empty(MarkdownConsole.Parse(string.Empty).Children);
            Assert.Throws<ArgumentNullException>(() => MarkdownConsole.Parse(null!));
        }

        [Fact]
        public void CancelledShow_DoesNotWriteOrAcquireInput() {
            using CancellationTokenSource stop = new CancellationTokenSource();
            stop.Cancel();
            Assert.Throws<OperationCanceledException>(() => MarkdownConsole.Show("# stopped", cancellationToken: stop.Token));
        }
    }
}
