using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// Golden snapshots of rich text in every widget type (cells, headers, cards, key/value, record view, tree, rule,
    /// panel, logo, status list, lanes, log, charts), per glyph tier as plain text and per colour depth as VT, including
    /// wrapping and truncation that keep styles.
    /// </summary>
    public sealed class RichTextGoldenTests {
        private static readonly string Esc = ((char)27).ToString();

        private static RichText Server(string number) => new RichTextBuilder().Append("SRV", ConsoleColor.Cyan).Append(number, ConsoleColor.Yellow, TextDecoration.Bold).Build();

        private static RichText Pass() => new RichTextBuilder().Append("√ ", ThemeToken.Pass).Append("PASS", ThemeToken.Pass, TextDecoration.Bold).Build();

        private static RichText Fail() => new RichTextBuilder().Append("× ", ThemeToken.Fail).Append("FAIL", ThemeToken.Fail, TextDecoration.Bold).Build();

        internal static IWidget Gallery() {
            Table table = new Table { Title = new RichTextBuilder().Append("Checks", ThemeToken.Accent, TextDecoration.Bold).Append(" · rich cells", ThemeToken.Muted).Build().ToMarkup() };
            table.AddColumn(new TableColumn(new RichTextBuilder().Append("Ser", ConsoleColor.Cyan).Append("ver", ConsoleColor.Magenta).Build()) { IsKey = true });
            table.AddColumn(new TableColumn("Result"));
            table.AddColumn(new TableColumn("Path") { Overflow = ColumnOverflow.TruncateMiddle, MaxWidth = 18 });
            table.AddColumn(new TableColumn("Note") { Overflow = ColumnOverflow.Wrap, MaxWidth = 14 });
            table.AddValues(Server("01"), Pass(), new RichTextBuilder().Append(@"C:\Support\GitHub\", ThemeToken.Muted).Append("ConsoleMonster", ConsoleColor.Green).Build(), RichText.Plain("replication healthy across sites", new RichStyle(ThemeToken.Info)));
            table.AddValues(Server("02"), Fail(), RichText.Plain(@"D:\Data\Reports\Weekly.xlsx"), RichText.Plain("[red]literal[/] data"));

            Table cards = new Table { CardsBelow = 200, RecordViewRows = 0 };
            cards.AddColumn(new TableColumn("Server") { IsKey = true });
            cards.AddColumn(new TableColumn("Result"));
            cards.AddValues(Server("03"), Pass());

            KeyValueList keys = new KeyValueList();
            keys.AddValue("Owner", new RichTextBuilder().Append("Przemysław ", ConsoleColor.White).Append("Kłys", ConsoleColor.Cyan, TextDecoration.Italic).Build());
            keys.AddValue("Status", Fail());

            RecordView record = new RecordView("Certificate");
            record.AddField("Expires", RichText.Plain("in 22 d", new RichStyle(ThemeToken.Warn, default, TextDecoration.Bold)));

            Tree tree = new Tree(new RichTextBuilder().Append("ad.", ThemeToken.Muted).Append("evotec.xyz", ThemeToken.Accent).Build().ToMarkup());
            TreeNode site = tree.Add(new RichTextBuilder().Append("Site ", ThemeToken.Muted).Append("Warsaw", ConsoleColor.Green).Build(), StatusKind.Pass);
            site.Add(Server("01"), StatusKind.Warn, "2 warnings");

            StatusList status = new StatusList();
            status.Add(new RichTextBuilder().Append("DNS ", ConsoleColor.Gray).Append("scavenging", ConsoleColor.Yellow).Build(), StatusKind.Warn, "7 days");

            Lanes lanes = new Lanes("lanes");
            Lane lane = lanes.Add("srv01", Server("01"), 4);
            lane.Status = StatusKind.Running;
            lane.Done = 2;
            lane.DetailText = new RichTextBuilder().Append("copying ", ThemeToken.Muted).Append("Weekly.xlsx", ConsoleColor.Green).Build();

            // A log region takes its preferred height when the height is bounded (stable in live dashboards); match the content here.
            LogRegion log = new LogRegion { ShowTime = false, Wrap = true, PreferredHeight = 2 };
            log.Write(new RichTextBuilder().Append("DC03 ", ConsoleColor.Cyan).Append("replication failed", ThemeToken.Fail).Append(" - no inbound replication for 4h from DC02", ThemeToken.Muted).Build(), "err");

            BarChart bars = new BarChart();
            bars.Add(Server("01"), 12);
            bars.Add(Server("02"), 7);
            StackedBar stacked = new StackedBar();
            stacked.Add(Pass(), 40);
            stacked.Add(Fail(), 4);
            LineChart line = new LineChart { Height = 3 };
            line.AddSeries(new RichTextBuilder().Append("rules", ConsoleColor.Cyan).Append("/s", ThemeToken.Muted).Build());
            line.AddSeries(RichText.Plain("failed", new RichStyle(ThemeToken.Fail)));
            foreach (double v in new[] { 1.0, 3, 2, 5, 4 }) {
                line.Add("rules/s", v);
                line.Add("failed", v / 4);
            }

            return new Rows(
                new Logo("CM", new RichTextBuilder().Append("rich ", ThemeToken.Accent).Append("text", ConsoleColor.Magenta, TextDecoration.Underline).Build().ToMarkup()),
                new Rule(new RichTextBuilder().Append("Results ", ThemeToken.Text).Append("2026", ThemeToken.Muted).Build()),
                new Panel(table, new RichTextBuilder().Append("Panel ", ThemeToken.Accent).Append("title", ConsoleColor.Green).Build()),
                cards, keys, record, tree, status, lanes, log, bars, stacked, line,
                RichText.Gradient("gradient across the line", RichColor.FromRgb(0, 198, 255), RichColor.FromRgb(255, 0, 170))
            ) { Gap = 1 };
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void Gallery_PlainText_MatchesGoldenPerTier(GlyphTier tier) {
            string text = TableShowcaseGoldenTests.WithThreadCulture("en-US", () => Terminal.RenderText(Gallery(), TerminalProfile.Fixed(64, 200, ColorDepth.None, tier), Theme.Dark));
            Golden.Match("richtext." + tier.ToString().ToLowerInvariant() + ".txt", text.TrimEnd('\n') + "\n");
        }

        [Theory]
        [InlineData(ColorDepth.Ansi16)]
        [InlineData(ColorDepth.TrueColor)]
        public void Gallery_Vt_MatchesGoldenPerColorDepth(ColorDepth depth) {
            string encoded = TableShowcaseGoldenTests.WithThreadCulture("en-US", () => Terminal.RenderEncoded(Gallery(), TerminalProfile.Fixed(64, 200, depth, GlyphTier.Full), Theme.Dark));
            Golden.Match("richtext.vt." + depth.ToString().ToLowerInvariant() + ".txt", encoded.Replace(Esc, "\\e").Replace("\r\n", "\n").TrimEnd('\n') + "\n");
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Cards_RichCells_MatchGoldenPerTier(GlyphTier tier) {
            Table cards = new Table { Title = "Cards" };
            cards.AddColumn(new TableColumn(new RichTextBuilder().Append("Ser", ConsoleColor.Cyan).Append("ver", ConsoleColor.Magenta).Build()) { IsKey = true });
            cards.AddColumn(new TableColumn("Result") { MinWidth = 10 });
            cards.AddColumn(new TableColumn("Owner") { MinWidth = 16 });
            cards.AddColumn(new TableColumn("Note") { MinWidth = 16 });
            cards.AddValues(Server("03"), Pass(), RichText.Plain("Przemysław Kłys", new RichStyle(ConsoleColor.Cyan)), RichText.Plain("[red]literal[/] data"));
            cards.AddValues(Server("04"), Fail(), RichText.Plain("Ada", new RichStyle(ThemeToken.Muted)), RichText.Plain("needs a reboot", new RichStyle(ThemeToken.Warn)));
            string text = Terminal.RenderText(cards, TerminalProfile.Fixed(36, 40, ColorDepth.None, tier), Theme.Dark);
            Assert.Contains("[red]literal[/] data", text);
            Golden.Match("richtext.cards." + tier.ToString().ToLowerInvariant() + ".txt", text.TrimEnd('\n') + "\n");
        }

        [Fact]
        public void Gallery_LightTheme_UsesTheLightRoleColours() {
            string dark = Terminal.RenderEncoded(Pass(), TerminalProfile.Fixed(20, 3, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            string light = Terminal.RenderEncoded(Pass(), TerminalProfile.Fixed(20, 3, ColorDepth.TrueColor, GlyphTier.Full), Theme.Light);
            Assert.NotEqual(dark, light);
            Color lightPass = Theme.Light.Resolve(ThemeToken.Pass, ColorDepth.TrueColor).Foreground;
            Assert.Contains(";38;2;" + lightPass.R + ";" + lightPass.G + ";" + lightPass.B, light);
        }
    }
}
