using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class MarkdownViewTests {
        private static CellBuffer Render(IWidget view, int width = 60, bool links = false, GlyphTier tier = GlyphTier.Full) =>
            FrameComposer.RenderBlock(view, width, new RenderContext(TerminalProfile.Fixed(width, 30, ColorDepth.TrueColor, tier).WithHyperlinks(links), Theme.Dark));

        private static MarkdownView Sample() => new MarkdownView()
            .AddHeading(RichText.Plain("Deployment [literal]"))
            .AddParagraph(new RichTextBuilder().Append("Read ").AppendLink("the guide", "https://example.org/guide").Append(" before continuing.").Build())
            .AddQuote(new MarkdownView().AddParagraph(RichText.Plain("Keep the rollback plan close.")))
            .AddList(new[] {
                new MarkdownView().AddParagraph(RichText.Plain("Verify the target.")),
                new MarkdownView().AddParagraph(RichText.Plain("Deploy the approved build."))
            }, 3)
            .AddCode("Get-Service | Select-Object Name", "powershell")
            .AddTable(new[] { RichText.Plain("Host"), RichText.Plain("State") }, new[] {
                new[] { RichText.Plain("LAB01"), RichText.Plain("Ready") },
                new[] { RichText.Plain("LAB02"), RichText.Plain("Waiting") }
            });

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void SemanticBlocks_MatchGolden(GlyphTier tier) {
            CellBuffer buffer = Render(Sample(), tier: tier);
            Golden.Match("markdown/document." + tier.ToString().ToLowerInvariant() + ".txt", buffer.ToPlainText().TrimEnd() + "\n");
            Assert.Contains("Deployment [literal]", buffer.ToPlainText());
            Assert.Equal(Theme.Dark.Resolve(ThemeToken.Accent, ColorDepth.TrueColor).Color, buffer[0, 0].Style.Color);
            Assert.True((buffer[0, 0].Style.Decoration & TextDecoration.Bold) != 0);
        }

        [Fact]
        public void HeadingLevels_KeepHierarchyWithoutColour() {
            foreach (var profile in new[] {
                TerminalProfile.Fixed(28, 30, ColorDepth.TrueColor, GlyphTier.Full),
                TerminalProfile.Fixed(28, 30, ColorDepth.Ansi16, GlyphTier.Basic),
                TerminalProfile.Fixed(28, 30, ColorDepth.None, GlyphTier.Ascii),
                TerminalProfile.Fixed(28, 30, ColorDepth.None, GlyphTier.Full)
            }) {
                RenderContext context = new RenderContext(profile, Theme.Dark);
                for (int level = 1; level <= 6; level++) {
                    CellBuffer frame = FrameComposer.RenderBlock(
                        new MarkdownView().AddHeading(RichText.Plain("Section"), level), 28, context);
                    string[] lines = frame.ToPlainText().Split('\n');
                    Assert.Equal(level <= 2 ? 2 : 1, lines.Length);
                    Assert.Equal(level <= 2 ? "Section" : new string('#', level) + " Section", lines[0]);
                    if (profile.ColorDepth != ColorDepth.None) {
                        Assert.True((frame[0, 0].Style.Decoration & TextDecoration.Bold) != 0);
                    }
                    ThemeToken token = level <= 2 ? ThemeToken.Accent : level == 3 ? ThemeToken.Text : ThemeToken.Muted;
                    Assert.Equal(Theme.Dark.Resolve(token, profile.ColorDepth).Color, frame[0, 0].Style.Color);
                    if (level <= 2) {
                        string expected = level == 1
                            ? (profile.Glyphs == GlyphTier.Ascii ? "=" : "═")
                            : (profile.Glyphs == GlyphTier.Ascii ? "-" : "─");
                        Assert.Equal(string.Concat(Enumerable.Repeat(expected, 28)), lines[1]);
                    }
                }
            }
        }

        [Fact]
        public void InlineLinks_KeepTargetsOrShowMutedUrl() {
            RichText label = new RichTextBuilder().AppendLink("Read ", "https://example.org")
                .AppendLink("more", "https://example.org", new RichStyle(decoration: TextDecoration.Bold)).Build();
            MarkdownView view = new MarkdownView().AddParagraph(label);
            CellBuffer linked = Render(view, links: true);
            Assert.Equal("Read more", linked.ToPlainText());
            Assert.Equal("https://example.org", linked[0, 0].Style.Link);
            TerminalProfile profile = TerminalProfile.Fixed(60, 30).WithHyperlinks(true);
            Assert.Contains("\u001b]8;;https://example.org\u001b\\", Terminal.RenderEncoded(view, profile, Theme.Dark));
            CellBuffer fallback = Render(view);
            Assert.Equal("Read more (https://example.org)", fallback.ToPlainText());
            Assert.Null(fallback[0, 0].Style.Link);
            Assert.Equal(Theme.Dark.Resolve(ThemeToken.Muted, ColorDepth.TrueColor).Color, fallback[10, 0].Style.Color);
            string wrapped = Render(view, 12).ToPlainText().Replace("\n", string.Empty).Replace(" ", string.Empty);
            Assert.Equal("Readmore(https://example.org)", wrapped);
            RichText address = new RichTextBuilder().AppendLink("https://example.org", "https://example.org").Build();
            Assert.Equal("https://example.org", Render(new MarkdownView().AddParagraph(address)).ToPlainText());
            RichText unsupported = new RichTextBuilder().AppendLink("note", "notes:topic").Build();
            Assert.Equal("note (notes:topic)", Render(new MarkdownView().AddParagraph(unsupported), links: true).ToPlainText());
        }

        [Fact]
        public void ListsAndQuotes_ReflowWithHangingIndent() {
            MarkdownView view = new MarkdownView().AddQuote(new MarkdownView().AddList(new[] {
                new MarkdownView().AddParagraph(RichText.Plain("alpha beta gamma delta"))
            }, 9));
            string[] lines = Render(view, 16).ToPlainText().TrimEnd('\n').Split('\n');
            Assert.Equal(new[] { "│ 9. alpha beta", "│    gamma delta" }, lines);
            Assert.All(lines, line => Assert.True(DisplayWidth.Measure(line) <= 16));
        }

        [Fact]
        public void Code_UsesRegisteredHighlighterAndUnknownLanguageFallsBack() {
            MarkdownView view = new MarkdownView().AddCode("{\"enabled\": true}", "json");
            CellBuffer buffer = Render(view);
            Assert.Contains("json", buffer.ToPlainText());
            Assert.Contains("\"enabled\": true", buffer.ToPlainText());
            HashSet<Style> styles = new HashSet<Style>();
            for (int x = 0; x < buffer.Width; x++) {
                if (buffer[x, 1].Glyph == "t" || buffer[x, 1].Glyph == "e") {
                    styles.Add(buffer[x, 1].Style);
                }
            }
            Assert.True(styles.Count > 1, "Code tokens should carry the registered highlighter's distinct styles.");
            Assert.Contains("unregistered-language", Render(new MarkdownView().AddCode("[fail]literal[/]", "unregistered-language")).ToPlainText());
            Assert.Contains("[fail]literal[/]", Render(new MarkdownView().AddCode("[fail]literal[/]", "unregistered-language")).ToPlainText());
        }

        [Fact]
        public void Tables_UseNativeLayoutAndKeepCells() {
            MarkdownView table = new MarkdownView().AddTable(new[] { RichText.Plain("Name"), RichText.Plain("Notes") }, new[] {
                new[] { RichText.Plain("alpha"), RichText.Plain("one two three four five six seven eight nine ten eleven twelve") },
                new[] { RichText.Plain("beta"), new RichTextBuilder().AppendLink("guide", "https://example.org").Build() }
            }, new HorizontalAlignment?[] { HorizontalAlignment.Left, HorizontalAlignment.Right });
            string text = Render(table, 40).ToPlainText();
            Assert.Contains("alpha", text);
            Assert.Contains("beta", text);
            Assert.Contains("twelve", text);
            Assert.Contains("https://example.org", text);
            Assert.DoesNotContain("…", text);
        }

        [Fact]
        public void SingleRowTable_PreservesHeaderGridAndAlignment() {
            MarkdownView view = new MarkdownView().AddTable(
                new[] { RichText.Plain("Name"), RichText.Plain("Count"), RichText.Plain("State") },
                new[] { new[] { RichText.Plain("alpha"), RichText.Plain("12"), RichText.Plain("Ready") } },
                new HorizontalAlignment?[] { HorizontalAlignment.Left, HorizontalAlignment.Right, HorizontalAlignment.Left });
            string[] lines = Render(view).ToPlainText().Split('\n');
            string header = Assert.Single(lines, line => line.Contains("Name"));
            string row = Assert.Single(lines, line => line.Contains("alpha"));
            Assert.Contains("Count", header);
            Assert.Contains("State", header);
            Assert.Contains("Ready", row);
            Assert.Equal(header.LastIndexOf('│'), row.LastIndexOf('│'));
            Assert.Equal(header.IndexOf("Count", StringComparison.Ordinal) + 4, row.IndexOf("12", StringComparison.Ordinal) + 1);
        }

        [Fact]
        public void WideTables_PreserveEveryFieldAndLongLinkedKeys() {
            string[] names = { "Identifier", "Environment", "Verification", "Maintenance", "Evidence" };
            RichText[] headers = names.Select(name => RichText.Plain(name)).ToArray();
            RichText linked = new RichTextBuilder().AppendLink(
                "START first second third fourth fifth KEY_END", "https://example.org/key").Build();
            MarkdownView view = new MarkdownView().AddTable(headers, new[] {
                new[] { linked, RichText.Plain("staging"), RichText.Plain("passed"), RichText.Plain("window"), RichText.Plain("ONLY_EVIDENCE") }
            });
            foreach (int width in new[] { 60, 40 }) {
                CellBuffer frame = Render(view, width, links: true);
                string text = frame.ToPlainText();
                foreach (string name in names) { Assert.Contains(name, text); }
                foreach (string value in new[] { "KEY_END", "staging", "passed", "window", "ONLY_EVIDENCE" }) {
                    Assert.Contains(value, text);
                }
                string[] lines = text.Split('\n');
                int row = Array.FindIndex(lines, line => line.Contains("KEY_END"));
                Assert.Equal("https://example.org/key", frame[lines[row].IndexOf("KEY_END", StringComparison.Ordinal), row].Style.Link);
                Assert.DoesNotContain("…", text);
            }
        }

        [Fact]
        public void LongTableHeaders_WrapWithoutLosingTheirSuffix() {
            MarkdownView view = new MarkdownView().AddTable(new[] {
                RichText.Plain("Host"), RichText.Plain("Required approval before deployment ONLY_HEADER_SUFFIX")
            }, new[] { new[] { RichText.Plain("web-01"), RichText.Plain("ready") } });
            string text = Render(view, 40).ToPlainText();
            Assert.Contains("ONLY_HEADER_SUFFIX", text);
            Assert.Contains("web-01", text);
            Assert.DoesNotContain("…", text);
        }

        [Fact]
        public void InvalidStructure_RejectsMalformedInput() {
            MarkdownView view = new MarkdownView();
            Assert.Throws<ArgumentOutOfRangeException>(() => view.AddHeading(RichText.Plain("bad"), 0));
            for (int level = 1; level <= 6; level++) {
                Assert.Throws<ArgumentNullException>(() => view.AddHeading(null!, level));
            }
            Assert.Throws<ArgumentException>(() => view.AddTable(new[] { RichText.Plain("Header") }, new[] { Array.Empty<RichText>() }));
            Assert.Throws<ArgumentException>(() => view.AddBlock(view));
            Assert.Throws<ArgumentException>(() => view.AddQuote(view));
            Assert.Throws<ArgumentException>(() => view.AddList(new[] { view }));
            Assert.Equal(0, Render(view).Height);
        }
    }
}
