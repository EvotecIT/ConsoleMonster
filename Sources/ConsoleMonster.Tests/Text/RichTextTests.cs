using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    /// <summary>Rich text: building, markup round trips, the exact PowerShell colour mapping, safety and styled layout.</summary>
    public sealed class RichTextTests {
        private static readonly string Esc = ((char)27).ToString();

        private static string Encode(RichText text, ColorDepth depth = ColorDepth.TrueColor) {
            return Terminal.RenderEncoded(text, TerminalProfile.Fixed(40, 5, depth, GlyphTier.Full), Theme.Dark);
        }

        [Theory]
        [InlineData(ConsoleColor.Black, "30")]
        [InlineData(ConsoleColor.DarkRed, "31")]
        [InlineData(ConsoleColor.DarkGreen, "32")]
        [InlineData(ConsoleColor.DarkYellow, "33")]
        [InlineData(ConsoleColor.DarkBlue, "34")]
        [InlineData(ConsoleColor.DarkMagenta, "35")]
        [InlineData(ConsoleColor.DarkCyan, "36")]
        [InlineData(ConsoleColor.Gray, "37")]
        [InlineData(ConsoleColor.DarkGray, "90")]
        [InlineData(ConsoleColor.Red, "91")]
        [InlineData(ConsoleColor.Green, "92")]
        [InlineData(ConsoleColor.Yellow, "93")]
        [InlineData(ConsoleColor.Blue, "94")]
        [InlineData(ConsoleColor.Magenta, "95")]
        [InlineData(ConsoleColor.Cyan, "96")]
        [InlineData(ConsoleColor.White, "97")]
        public void ConsoleColor_RendersWithTheCodeWriteHostUses(ConsoleColor color, string code) {
            // PowerShell's DarkRed is ANSI red (31) and Red is bright red (91): the terminal palette shows them like Write-Host.
            foreach (ColorDepth depth in new[] { ColorDepth.TrueColor, ColorDepth.Ansi256, ColorDepth.Ansi16 }) {
                Assert.Contains(Esc + "[0;" + code + "m", Encode(RichText.Plain("x", new RichStyle(color)), depth));
            }
        }

        [Fact]
        public void ConsoleColor_Background_UsesTheBackgroundCode() {
            Assert.Contains(";101m", Encode(RichText.Plain("x", new RichStyle(ConsoleColor.Black, ConsoleColor.Red))));
            Assert.Contains(";41m", Encode(RichText.Plain("x", new RichStyle(ConsoleColor.Black, ConsoleColor.DarkRed))));
        }

        [Theory]
        [InlineData("Red", "red")]
        [InlineData("darkred", "maroon")]
        [InlineData("Green", "lime")]
        [InlineData("Gray", "silver")]
        [InlineData("DarkGray", "grey")]
        [InlineData("pass", "pass")]
        [InlineData("Muted", "muted")]
        [InlineData("#FF8800", "#ff8800")]
        [InlineData("rgb(1,2,3)", "#010203")]
        [InlineData("10, 20, 30", "#0a141e")]
        [InlineData("orange1", "color(214)")]
        [InlineData("214", "color(214)")]
        public void RichColor_Parse_UsesThePowerShellNamesFirst(string text, string markup) {
            Assert.True(RichColor.TryParse(text, out RichColor color));
            Assert.Equal(markup, color.ToMarkup());
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-colour")]
        [InlineData("300,1,1")]
        public void RichColor_Parse_RejectsUnknownText(string text) {
            Assert.False(RichColor.TryParse(text, out _));
            Assert.Throws<FormatException>(() => RichColor.Parse(text));
        }

        [Fact]
        public void Plain_DataWithBracketsAndEscapes_StaysLiteral() {
            RichText text = RichText.Plain("[red]alert[/] " + Esc + "]0;PWNED" + ((char)7).ToString() + " done");
            string rendered = Terminal.RenderText(text, TerminalProfile.Fixed(60, 5, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Assert.StartsWith("[red]alert[/]", rendered);
            Assert.True(rendered.IndexOf((char)27) < 0);
            Assert.True(rendered.IndexOf((char)7) < 0);
            // Ordinal: culture-aware comparison ignores ESC and would match "]0;" alone.
            Assert.True(Encode(text).IndexOf(Esc + "]0;", StringComparison.Ordinal) < 0);
        }

        [Fact]
        public void ToMarkup_EscapesLiteralTextAndRoundTripsThroughTheParser() {
            RichText text = new RichTextBuilder()
                .Append("SPF ", ConsoleColor.Gray)
                .Append("PASS", ThemeToken.Pass, TextDecoration.Bold)
                .Append(" [x] ")
                .AppendLink("docs", "https://evotec.xyz/a b")
                .Build();
            Assert.Equal("[silver]SPF [/][bold pass]PASS[/] [[x]] [link=https://evotec.xyz/a%20b]docs[/]", text.ToMarkup());
            Assert.Equal("SPF PASS [x] docs", text.PlainText);
            Assert.Equal("SPF PASS [x] docs", text.ToString());
            Assert.Equal("SPF PASS [x] docs", Markup.Strip(text.ToMarkup()));
        }

        [Fact]
        public void FromPieces_FollowsWriteColor_ExtraPiecesTakeTheFirstColour() {
            RichText text = RichText.FromPieces(new[] { "a", "b", "c" }, new RichColor[] { ConsoleColor.Yellow, ConsoleColor.Green }, new RichColor[] { ConsoleColor.Blue, ConsoleColor.Red });
            Assert.Equal(new RichColor[] { ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Yellow }, text.Segments.Select(s => s.Style.Foreground).ToArray());
            Assert.Equal(new RichColor[] { ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.Blue }, text.Segments.Select(s => s.Style.Background).ToArray());
        }

        [Fact]
        public void Gradient_InterpolatesPerGraphemeAndStepsOnSixteenColours() {
            RichText text = RichText.Gradient("ABCDE", RichColor.FromRgb(255, 0, 0), RichColor.FromRgb(0, 0, 255));
            Assert.Equal(5, text.Segments.Count);
            Assert.Equal("#ff0000", text.Segments[0].Style.Foreground.ToMarkup());
            Assert.Equal("#0000ff", text.Segments[4].Style.Foreground.ToMarkup());
            Assert.Equal("#800080", text.Segments[2].Style.Foreground.ToMarkup());
            Assert.Contains(";38;2;255;0;0m", Encode(text));
            string sixteen = Encode(text, ColorDepth.Ansi16);
            Assert.DoesNotContain(";38;2;", sixteen);
            Assert.Equal("ABCDE", Terminal.RenderText(text, TerminalProfile.Fixed(20, 3, ColorDepth.None, GlyphTier.Full), Theme.Dark));
        }

        [Fact]
        public void Gradient_KeepsEmojiAndCjkGraphemesWhole() {
            RichText text = RichText.Gradient("中文👍🏽!", ConsoleColor.Red, ConsoleColor.Blue);
            Assert.Equal(new[] { "中", "文", "👍🏽", "!" }, text.Segments.Select(s => s.Text).ToArray());
        }

        [Fact]
        public void ToMarkup_ExpandsTabsToEightCellStops() {
            Assert.Equal("ab      c", RichText.Plain("ab\tc").ToMarkup());
        }

        [Fact]
        public void TruncateMiddle_KeepsBothEndsStyles() {
            StyledLine line = new RichTextBuilder().Append("aaaaaaaa", ConsoleColor.Red).Append("bbbbbbbb", ConsoleColor.Green).Build().ToStyledText(new Canvas.RenderContext(TerminalProfile.Fixed(40, 5), Theme.Dark)).Lines[0];
            StyledLine cut = TextWrapper.TruncateMiddle(line, 9, "…");
            List<TextSpan> spans = new List<TextSpan>(cut);
            Assert.Equal("aaaa…bbbb", cut.PlainText);
            Assert.Equal(Color.FromAnsi16(9), spans[0].Style.Foreground);
            Assert.Equal(Color.FromAnsi16(10), spans[spans.Count - 1].Style.Foreground);
        }

        [Fact]
        public void Wrap_CarriesStylesOntoTheNextLine() {
            RichText text = RichText.Plain("alpha beta gamma delta", new RichStyle(ThemeToken.Fail));
            string encoded = Terminal.RenderEncoded(new RichText(text.Segments) { Overflow = TextOverflow.Wrap }, TerminalProfile.Fixed(12, 5, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            string fail = Theme.Dark.Resolve(ThemeToken.Fail, ColorDepth.TrueColor).Foreground.R.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(encoded.Split(new[] { "38;2;" + fail + ";" }, StringSplitOptions.None).Length - 1 >= 2, "every wrapped line keeps the colour");
        }

        [Fact]
        public void TableCell_RichTextValue_KeepsItsColoursAndIgnoresPresenters() {
            Table table = new Table();
            table.AddColumn(new TableColumn(new RichTextBuilder().Append("Re", ConsoleColor.Cyan).Append("sult", ConsoleColor.Magenta).Build()));
            table.AddColumn(new TableColumn("Data") { Presenter = CellPresenters.Bytes });
            table.AddValues(new RichTextBuilder().Append("√ ", ThemeToken.Pass).Append("PASS", ThemeToken.Pass, TextDecoration.Bold).Build(), RichText.Plain("[red]literal[/]"));
            string plain = Terminal.RenderText(table, TerminalProfile.Fixed(40, 8, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Assert.Contains("Result", plain);
            Assert.Contains("√ PASS", plain);
            Assert.Contains("[red]literal[/]", plain);
            string encoded = Terminal.RenderEncoded(table, TerminalProfile.Fixed(40, 8, ColorDepth.Ansi16, GlyphTier.Full), Theme.Dark);
            Assert.Contains(";96mRe", encoded);
            Assert.Contains(";95msult", encoded);
        }

        [Fact]
        public void MarkupPresenter_InterpretsOnlyWhenTheColumnOptsIn() {
            Table table = new Table();
            table.AddColumn(new TableColumn("Literal"));
            table.AddColumn(new TableColumn("Markup") { Presenter = CellPresenters.Markup });
            table.AddValues("[bold]x[/]", "[bold]y[/] [broken");
            string plain = Terminal.RenderText(table, TerminalProfile.Fixed(50, 8, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Assert.Contains("[bold]x[/]", plain);
            Assert.Contains("[bold]y[/] [broken", plain);
            Table valid = new Table();
            valid.AddColumn(new TableColumn("Markup") { Presenter = CellPresenters.FromName("Markup") });
            valid.AddValues("[bold]y[/] ok");
            Assert.Contains("y ok", Terminal.RenderText(valid, TerminalProfile.Fixed(50, 8, ColorDepth.None, GlyphTier.Full), Theme.Dark));
        }

        [Fact]
        public void Widgets_AcceptRichLabels_InEveryTier() {
            RichText label = new RichTextBuilder().Append("SRV", ConsoleColor.Cyan).Append("01", ConsoleColor.Yellow, TextDecoration.Bold).Build();
            Lanes lanes = new Lanes();
            lanes.Add("srv01", label, 4);
            lanes.Add("srv02", "SRV02 [literal]");
            BarChart bars = new BarChart();
            bars.Add(label, 5);
            StackedBar stacked = new StackedBar();
            stacked.Add(label, 3);
            LogRegion log = new LogRegion { ShowTime = false };
            log.Write(label, "warn");
            Tree tree = new Tree(label.ToMarkup());
            tree.Add(label, StatusKind.Pass);
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii }) {
                foreach (Layout.IWidget widget in new Layout.IWidget[] { lanes, bars, stacked, log, tree, new Rule(label), new Layout.Panel(new TextBlock("x"), label) }) {
                    string text = Terminal.RenderText(widget, TerminalProfile.Fixed(60, 10, ColorDepth.None, tier), Theme.Dark);
                    Assert.Contains("SRV01", text);
                }
            }
            Assert.Contains("SRV02 [literal]", Terminal.RenderText(lanes, TerminalProfile.Fixed(60, 10, ColorDepth.None, GlyphTier.Full), Theme.Dark));
            string encoded = Terminal.RenderEncoded(lanes, TerminalProfile.Fixed(60, 10, ColorDepth.Ansi16, GlyphTier.Full), Theme.Dark);
            Assert.Contains(";96mSRV", encoded);
        }

        [Fact]
        public void LiveEvent_MarkupText_GivesLanesAndLogRichText() {
            Lanes lanes = new Lanes();
            lanes.Apply(new ConsoleMonster.Live.LiveEvent("lanes", "add", "a", "[lime]SRV1[/]", isMarkup: true));
            lanes.Apply(new ConsoleMonster.Live.LiveEvent("lanes", "add", "b", "[lime]SRV2[/]"));
            Assert.Equal("SRV1", lanes.Items.First(l => l.Key == "a").Label);
            Assert.NotNull(lanes.Items.First(l => l.Key == "a").LabelText);
            Assert.Equal("[lime]SRV2[/]", lanes.Items.First(l => l.Key == "b").Label);
            LogRegion log = new LogRegion();
            log.Apply(new ConsoleMonster.Live.LiveEvent("log", "write", text: "[fail]boom[/]", state: "err", isMarkup: true));
            Assert.Equal("boom", log.Entries.Single().Text);
        }
    }
}
