using System;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Frames;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Frames;

public sealed class DashboardFrameTests {
    private static string Render(DashboardFrame frame, int width, int height = 40, GlyphTier glyphs = GlyphTier.Full, ConsoleViewLength length = ConsoleViewLength.Standard) {
        frame.Scale = FrameScale.Of(width, height, length);
        return TestTerminal.RenderText(frame, width, 0, glyphs, CultureInfo.InvariantCulture);
    }

    private static string[] Lines(string text) => text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

    [Theory]
    [InlineData(5, 120, GlyphTier.Full)]
    [InlineData(5, 100, GlyphTier.Full)]
    [InlineData(5, 80, GlyphTier.Full)]
    [InlineData(5, 60, GlyphTier.Full)]
    [InlineData(5, 120, GlyphTier.Basic)]
    [InlineData(5, 120, GlyphTier.Ascii)]
    [InlineData(5, 220, GlyphTier.Full)]
    [InlineData(50, 120, GlyphTier.Full)]
    [InlineData(50, 220, GlyphTier.Full)]
    [InlineData(100, 120, GlyphTier.Full)]
    [InlineData(100, 220, GlyphTier.Full)]
    public void Replication_ScaleSet_MatchesGoldenAndFitsWidth(int dcs, int width, GlyphTier glyphs) {
        string text = Render(ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(dcs)), width, width >= 220 ? 60 : 40, glyphs);
        Assert.All(Lines(text), line => Assert.InRange(DisplayWidth.Measure(line), 0, width - 1));
        if (glyphs == GlyphTier.Ascii) Assert.All(text, c => Assert.True(c == '\n' || c == '\r' || (c >= ' ' && c < 127), "Non-ASCII character U+" + ((int)c).ToString("X4")));
        Golden.Match("frames/replication-" + dcs + "dc-" + width + "." + glyphs.ToString().ToLowerInvariant() + ".txt", text);
    }

    [Fact]
    public void Replication_EveryCountInTheFrame_ComesFromTheRows() {
        ReplicationFrameFixture.Estate estate = ReplicationFrameFixture.Create(50);
        ReplicationFrameFixture.Partnership[] partnerships = ReplicationFrameFixture.Partnerships(estate);
        string text = Render(ReplicationFrameFixture.Build(estate), 120);
        int failing = partnerships.Count(p => p.Failing);
        Assert.Contains("× " + failing + " failing", text);
        Assert.Contains(partnerships.Count(p => p.State == HealthState.Healthy) + "/" + partnerships.Length, text);
        Assert.Contains(estate.Links.Count + " naming-context links", text);
        foreach (ReplicationFrameFixture.Partnership p in partnerships.Where(p => p.Failing)) Assert.Contains(p.Destination + " ← " + p.Source, text);
    }

    [Fact]
    public void Packing_WideTerminal_PutsAttentionMatrixAndDistributionInOneRow() {
        string[] lines = Lines(Render(ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(5)), 220, 60));
        Assert.Contains(lines, line => line.Contains("Needs attention") && line.Contains("Matrix") && line.Contains("Last success"));
    }

    [Fact]
    public void Packing_SectionThatMissesTheRow_FlowsUnderTheShortestColumn() {
        static FrameSection Block(string title, int lines, int width) =>
            new FrameSection(title, context => TextBlock.Plain(string.Join("\n", Enumerable.Range(1, lines).Select(i => title.ToLowerInvariant() + " " + i)))) { MinWidth = width, MaxWidth = width };
        DashboardFrame frame = new DashboardFrame("Masonry");
        frame.Sections.Add(Block("Short", 3, 120));
        frame.Sections.Add(Block("Tall", 14, 90));
        frame.Sections.Add(Block("Later", 2, 44));
        string[] lines = Lines(Render(frame, 220, 60));
        int shortEnd = Array.FindIndex(lines, line => line.Contains("short 3"));
        int later = Array.FindIndex(lines, line => line.Contains("Later"));
        int tallEnd = Array.FindIndex(lines, line => line.Contains("tall 14"));
        Assert.Equal(shortEnd + 2, later);
        Assert.True(later < tallEnd, "The later section waited below the tallest column.");
        Assert.Equal(lines[shortEnd].IndexOf("short 3", StringComparison.Ordinal), lines[later].IndexOf("Later", StringComparison.Ordinal));
    }

    [Fact]
    public void Packing_Laptop_GivesAttentionItsOwnRowWhenTheMatrixDoesNotFitBesideIt() {
        string[] lines = Lines(Render(ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(5)), 120));
        string attention = Assert.Single(lines, line => line.Contains("Needs attention"));
        Assert.DoesNotContain("Matrix", attention);
        Assert.Contains(lines, line => line.Contains("Matrix") && line.Contains("Last success"));
    }

    [Fact]
    public void Narrow_BelowCompactWidth_PrintsTheCompactViewAndMeterCounts() {
        string text = Render(ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(50)), 60);
        Assert.StartsWith(" compact replication view", text);
        Assert.Contains("DCs reached 49/50", text);
        Assert.DoesNotContain("Inbound by DC", text);
    }

    [Fact]
    public void Summary_OmitsDetailSections() {
        DashboardFrame frame = ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(50));
        Assert.Contains("Inbound by DC", Render(frame, 120));
        Assert.DoesNotContain("Inbound by DC", Render(frame, 120, length: ConsoleViewLength.Summary));
    }

    [Fact]
    public void Detail_LargeSection_CollapsesHealthyRowsInStandardButNotFull() {
        DashboardFrame frame = ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(100));
        string standard = Render(frame, 120);
        Assert.Contains("current DCs not listed", standard);
        Assert.DoesNotContain("AMS-DC03", standard);
        Assert.Contains("MEX-DC02", standard);
        string full = Render(frame, 120, length: ConsoleViewLength.Full);
        Assert.DoesNotContain("not listed", full);
        Assert.Contains("AMS-DC03", full);
    }

    [Fact]
    public void Attention_Standard_ListsEveryItemWorstFirst() {
        DashboardFrame frame = new DashboardFrame("Disks");
        for (int i = 0; i < 30; i++) frame.Attention.Items.Add(new AttentionItem(HealthState.Warning, "SRV" + i.ToString("00"), "volume over 90% " + i));
        frame.Attention.Items.Add(new AttentionItem(HealthState.Critical, "SRV99", "volume full"));
        frame.Attention.Items.Add(new AttentionItem(HealthState.Healthy, "SRV98", "fine"));
        string[] lines = Lines(Render(frame, 120));
        Assert.Equal(30, lines.Count(line => line.Contains("volume over 90%")));
        Assert.DoesNotContain(lines, line => line.Contains("SRV98"));
        Assert.True(Array.FindIndex(lines, line => line.Contains("SRV99")) < Array.FindIndex(lines, line => line.Contains("SRV00")));
    }

    [Fact]
    public void Attention_Summary_CapsToTheFirstScreenWithMore() {
        DashboardFrame frame = new DashboardFrame("Disks");
        for (int i = 0; i < 40; i++) frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "SRV" + i.ToString("00"), "volume full " + i));
        string text = Render(frame, 120, 30, length: ConsoleViewLength.Summary);
        Assert.Equal(16, Lines(text).Count(line => line.Contains("volume full")));
        Assert.Contains("+24 more", text);
    }

    [Fact]
    public void Attention_IdenticalReasons_GroupAndStillNameEveryTarget() {
        DashboardFrame frame = new DashboardFrame("Servers");
        frame.Attention.Noun = "server";
        foreach (string name in new[] { "WEB01", "WEB02", "WEB03", "WEB04" }) frame.Attention.Items.Add(new AttentionItem(HealthState.Warning, name, "reboot pending"));
        frame.Attention.Items.Add(new AttentionItem(HealthState.Warning, "SQL01", "volume over 90%"));
        string text = Render(frame, 120);
        Assert.Equal(1, Lines(text).Count(line => line.Contains("reboot pending")));
        Assert.Contains("4 servers", text);
        Assert.Contains("WEB01, WEB02, WEB03, WEB04", text);
        Assert.Contains("SQL01", text);
    }

    [Fact]
    public void Meter_NoColour_ProblemShareKeepsItsGlyphAndOneCell() {
        DashboardFrame frame = new DashboardFrame("Volumes");
        frame.Meters.Add(Meter.Composition("Volumes healthy", 999, failing: 1));
        frame.Meters.Add(Meter.Missing("Free space"));
        string text = Render(frame, 120);
        Assert.Contains("999/1,000", text);
        Assert.Contains("×", Lines(text)[2]);
        Assert.Contains("◌ unknown", text);
        Assert.DoesNotContain("Free space 0", text);
    }

    [Fact]
    public void Header_ZeroCountsOnly_ShowsOneHealthyVerdict() {
        DashboardFrame frame = new DashboardFrame("Replication");
        frame.Header.States.Add(FrameState.Of(HealthState.Failing, 0, "failing"));
        string first = Lines(Render(frame, 100))[0];
        Assert.EndsWith("✓ all healthy", first.TrimEnd());
        Assert.DoesNotContain("0 failing", first);
    }

    [Fact]
    public void Header_ObservedToday_ShowsTimeOnly() {
        TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 28, 12, 0, 0));
        DashboardFrame frame = new DashboardFrame("Replication") { Now = new DateTimeOffset(2026, 10, 28, 12, 0, 0, localOffset) };
        frame.Header.ObservedAt = new DateTimeOffset(2026, 10, 28, 10, 42, 0, localOffset);
        frame.Header.States.Add(FrameState.Of(HealthState.Failing, 2, "failing"));
        string first = Lines(Render(frame, 100))[0];
        Assert.Contains("× 2 failing · " + frame.Header.ObservedAt.Value.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture), first);
    }

    [Fact]
    public void Footer_AlwaysEndsWithTheHint() {
        DashboardFrame frame = new DashboardFrame("Health");
        frame.Footer.Counts.Add("6 DCs");
        frame.Footer.Hint = FrameHint.EveryProperty;
        Assert.EndsWith("6 DCs · pipe to Format-List for every property", Lines(Render(frame, 100)).Last().TrimEnd());
    }

    [Fact]
    public void GroupedRows_SingleColumn_RepeatsTheHeaderAndCapsWithMore() {
        string[] names = Enumerable.Range(1, 260).Select(i => "HOST" + i.ToString("000")).ToArray();
        GroupedRows<string> rows = new GroupedRows<string>(names, name => "Site " + (name.GetHashCode() % 1 + 1), FrameScale.Of(100, 30));
        rows.Columns.Add(new GroupedColumn<string>("Host", (name, _) => name));
        string text = TestTerminal.RenderText(rows, 100, 0, GlyphTier.Full, CultureInfo.InvariantCulture);
        Assert.Contains("+60 more · pipe to Format-Table for every row", text);
        Assert.DoesNotContain("HOST201", text);
        Assert.True(Lines(text).Count(line => line.TrimStart().StartsWith("Host")) >= 8);
    }

    [Fact]
    public void GroupedRows_WideTerminal_FlowsGroupsWithoutSplittingThem() {
        string[] names = Enumerable.Range(1, 40).Select(i => "HOST" + i.ToString("00")).ToArray();
        GroupedRows<string> rows = new GroupedRows<string>(names, name => "Site " + ((int.Parse(name.Substring(4)) - 1) / 10 + 1), FrameScale.Of(220, 60));
        rows.Columns.Add(new GroupedColumn<string>("Host", (name, _) => name) { Width = 40 });
        rows.Columns.Add(new GroupedColumn<string>("Note", (name, _) => "ok") { Width = 40 });
        string[] lines = Lines(TestTerminal.RenderText(rows, 218, 0, GlyphTier.Full, CultureInfo.InvariantCulture));
        Assert.Equal(2, lines[0].Split(new[] { "Host" }, StringSplitOptions.None).Length - 1);
        foreach (string site in new[] { "Site 1", "Site 2", "Site 3", "Site 4" }) Assert.Single(lines, line => line.Contains(site));
    }

    [Fact]
    public void Ascii_FoldsTypographicLiteralsInAuthorText() {
        DashboardFrame frame = new DashboardFrame("Replication");
        frame.Header.Facts.Add("a · b");
        frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "DC01 ← DC02", "15m–1h … gone"));
        string text = Render(frame, 100, glyphs: GlyphTier.Ascii);
        Assert.Contains("DC01 <- DC02", text);
        Assert.Contains("15m-1h ... gone", text);
        Assert.All(text, c => Assert.True(c < 127));
    }

    [Theory]
    [InlineData(120, 40, 4, 1)]
    [InlineData(220, 60, 7, 2)]
    [InlineData(80, 24, 2, 1)]
    public void Scale_FollowsTheTerminalSize(int width, int height, int tiles, int columns) {
        FrameScale scale = FrameScale.Of(width, height);
        Assert.Equal(tiles, scale.MeterTiles);
        Assert.Equal(columns, scale.DetailColumns(96));
        Assert.Equal(Math.Max(20, height - 6), scale.HeaderRepeat);
        Assert.Null(scale.AttentionCap);
        Assert.Equal(200, scale.DetailCap);
    }

    [Fact]
    public void Scale_UnknownSize_Uses120By50() {

        FrameScale redirected = FrameScale.Of(0, 0);
        Assert.Equal(120, redirected.Width);
        Assert.Equal(50, redirected.Height);
        Assert.False(redirected.IsSizeKnown);
    }

    [Theory]
    [InlineData("summary", true, ConsoleViewLength.Summary)]
    [InlineData(" Full ", true, ConsoleViewLength.Full)]
    [InlineData("long", false, ConsoleViewLength.Standard)]
    [InlineData(null, false, ConsoleViewLength.Standard)]
    public void Length_ParsesNamesOnly(string? text, bool parsed, ConsoleViewLength expected) {
        Assert.Equal(parsed, FrameScale.TryParseLength(text, out ConsoleViewLength length));
        Assert.Equal(expected, length);
    }

    [Fact]
    public void Bars_LineAndRange_StayExactlyTheirWidthInEveryTier() {
        foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii }) {
            Canvas.RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24, ColorDepth.None, tier));
            Assert.Equal(20, context.Markup(FrameBars.Line(0.333, 20, null, context)).Lines[0].Width);
            Assert.Equal(20, context.Markup(FrameBars.Range(0.1, 0.7, 20, HealthState.Failing, 0.5, context)).Lines[0].Width);
        }
        Canvas.RenderContext full = Terminal.CreateContext(TerminalProfile.Fixed(80, 24, ColorDepth.None, GlyphTier.Full));
        Assert.Equal("╸─────────", Markup.Strip(FrameBars.Line(0.001, 10, null, full)));
    }

    [Fact]
    public void Attention_SummaryCap_CountsHiddenItemsInsideGroups() {
        DashboardFrame frame = new DashboardFrame("Servers");
        frame.Attention.Noun = "server";
        for (int i = 0; i < 20; i++) frame.Attention.Items.Add(new AttentionItem(HealthState.Warning, "WEB" + i.ToString("00"), "reason " + i));
        for (int i = 0; i < 40; i++) frame.Attention.Items.Add(new AttentionItem(HealthState.Unknown, "APP" + i.ToString("00"), "no answer"));
        string text = Render(frame, 120, 20, length: ConsoleViewLength.Summary);
        Assert.Contains("+54 more", text);
    }

    [Fact]
    public void Header_ZeroStatesWithAttentionItems_DoesNotClaimAllHealthy() {
        DashboardFrame frame = new DashboardFrame("Disks");
        frame.Header.States.Add(FrameState.Of(HealthState.Failing, 0, "failing"));
        frame.Attention.Items.Add(new AttentionItem(HealthState.Warning, "SRV01", "volume over 90%"));
        Assert.DoesNotContain("all healthy", Render(frame, 100));
    }

    [Fact]
    public void Length_HostScope_ReachesFramesWithoutAnExplicitScale() {
        DashboardFrame frame = ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(50));
        using (FrameScale.UseLength(ConsoleViewLength.Summary)) {
            Assert.DoesNotContain("Inbound by DC", TestTerminal.RenderText(frame, 120, 0, GlyphTier.Full, CultureInfo.InvariantCulture));
        }
        Assert.Contains("Inbound by DC", TestTerminal.RenderText(frame, 120, 0, GlyphTier.Full, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Narrow_CompactView_StillEndsWithTheHint() {
        Assert.EndsWith("pipe to Format-Table for every row", Lines(Render(ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(5)), 60)).Last().TrimEnd());
    }

    [Theory]
    [InlineData("de-CH")]
    [InlineData("pl-PL")]
    public void Ascii_GroupSeparatorsOfAnyCulture_StayAscii(string name) {
        DashboardFrame frame = ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(100));
        frame.Scale = FrameScale.Of(220, 60);
        string text = TestTerminal.RenderText(frame, 220, 0, GlyphTier.Ascii, CultureInfo.GetCultureInfo(name));
        Assert.All(text, c => Assert.True(c == '\n' || c == '\r' || (c >= ' ' && c < 127), "Non-ASCII character U+" + ((int)c).ToString("X4")));
    }

    [Fact]
    public void Meter_ManyEqualShares_KeepEveryShareInsideTheBar() {
        DashboardFrame frame = new DashboardFrame("Targets");
        frame.Meters.Add(Meter.Composition("Targets", 1, warning: 1, failing: 1, unknown: 1, notEvaluated: 1));
        string bar = Lines(Render(frame, 120))[2];
        Assert.Contains("×", bar); Assert.Contains("▴", bar); Assert.Contains("?", bar);
        Assert.DoesNotContain("…", bar);
    }

    [Fact]
    public void NoColour_MarkedBucketsAndRanges_KeepTheirStateGlyph() {
        Canvas.RenderContext context = ConsoleViewStyle.Apply(Terminal.CreateContext(TerminalProfile.Fixed(80, 24, ColorDepth.None, GlyphTier.Full)));
        Assert.Contains("×", Markup.Strip(FrameBars.Range(0.1, 0.8, 20, HealthState.Failing, null, context)));
        string text = TestTerminal.RenderText(new Distribution().Add("<1h", 10).Add(">1d", 3, HealthState.Warning), 60, 0, GlyphTier.Full, CultureInfo.InvariantCulture);
        Assert.Contains("▴ 3", text);
    }

    [Fact]
    public void Frame_RenderedTwice_DrawsTheSameCells() {
        DashboardFrame frame = ReplicationFrameFixture.Build(ReplicationFrameFixture.Create(50));
        frame.Scale = FrameScale.Of(120, 40);
        Assert.Equal(TestTerminal.RenderText(frame, 120, 0, GlyphTier.Full, CultureInfo.InvariantCulture), TestTerminal.RenderText(frame, 120, 0, GlyphTier.Full, CultureInfo.InvariantCulture));
        string narrow = TestTerminal.RenderText(frame, 100, 0, GlyphTier.Full, CultureInfo.InvariantCulture);
        Assert.Equal(TestTerminal.RenderText(frame, 120, 0, GlyphTier.Full, CultureInfo.InvariantCulture), TestTerminal.RenderText(frame, 120, 0, GlyphTier.Full, CultureInfo.InvariantCulture));
        Assert.NotEmpty(narrow);
    }

    [Fact]
    public void Ratio_InvalidInputs_AreRejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Meter.Ratio("Reached", 11, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => Meter.Ratio("Reached", 3, 10, HealthState.Healthy));
        Assert.Throws<ArgumentOutOfRangeException>(() => Meter.Composition("Bad", -1));
    }

    [Fact]
    public void Attention_MixedOrderKeyTypes_DoNotFailTheFrame() {
        DashboardFrame frame = new DashboardFrame("Mixed");
        frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "A", "x", 5));
        frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "B", "y", "text"));
        Assert.Contains("B", Render(frame, 100));
    }

    [Fact]
    public void Attention_GroupedMixedOrderKeys_RetainsEveryIdentity() {
        DashboardFrame frame = new DashboardFrame("Mixed group");
        frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "SRV01", "no answer", 5));
        frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "SRV02", "no answer", "text"));
        frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "SRV03", "no answer", 2));
        string text = Render(frame, 100);
        Assert.Contains("SRV01, SRV02, SRV03", text);
        Assert.Single(Lines(text), line => line.Contains("no answer"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Attention_SummaryBudget_CountsRenderedLinesAndOmittedTargets(bool grouped) {
        DashboardFrame frame = new DashboardFrame("Servers");
        for (int i = 0; i < 200; i++) {
            string reason = grouped ? "query timed out" : "query timed out while collecting every pending volume and service for the current server state " + i;
            frame.Attention.Items.Add(new AttentionItem(HealthState.Failing, "SRV" + i.ToString("000"), reason));
        }
        string text = Render(frame, 80, 20, length: ConsoleViewLength.Summary);
        string[] lines = Lines(text);
        int start = Array.FindIndex(lines, line => line.Contains("Needs attention")) + 1;
        int more = Array.FindIndex(lines, line => line.Contains(" more "));
        Assert.InRange(more - start, 1, 6);
        int named = System.Text.RegularExpressions.Regex.Matches(text, @"SRV\d{3}").Count;
        Assert.InRange(named, 1, 199);
        Assert.Contains("+" + (200 - named) + " more", text);
        Assert.Contains("SRV199", Render(frame, 80, 20, length: ConsoleViewLength.Standard));
        Assert.Contains("SRV199", Render(frame, 80, 20, length: ConsoleViewLength.Full));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupedRows_ShortCells_PreserveDistinctHeadingsAndStates(bool collapsed) {
        string[] groups = Enumerable.Range(0, collapsed ? 60 : 2).Select(i => i % 2 == 0 ? "LON-HQ" : "LON-DR").ToArray();
        GroupedRows<string> rows = new GroupedRows<string>(groups, value => value, FrameScale.Of(120, 40));
        rows.Columns.Add(new GroupedColumn<string>("DC", (_, _) => "A"));
        rows.IsHealthy = _ => collapsed;
        rows.Heading = (_, _) => {
            GroupHeading heading = new GroupHeading();
            heading.States.Add(FrameState.Of(HealthState.Warning, 1, "pending"));
            return heading;
        };
        string text = TestTerminal.RenderText(rows, 118, 0, GlyphTier.Full, CultureInfo.InvariantCulture);
        Assert.Contains("LON-HQ  ▴ 1 pending", text);
        Assert.Contains("LON-DR  ▴ 1 pending", text);
        if (collapsed) Assert.Contains("60 healthy rows not listed", text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Packing_CappedGrow_ReassignsSpaceRegardlessOfSectionOrder(bool cappedFirst) {
        int growingWidth = 0, cappedWidth = 0;
        FrameSection growing = new FrameSection("Growing", context => {
            growingWidth = context.Width;
            return TextBlock.Plain("growing");
        }) { MinWidth = 20, Grow = 1 };
        FrameSection capped = new FrameSection("Capped", context => {
            cappedWidth = context.Width;
            return TextBlock.Plain("capped");
        }) { MinWidth = 20, MaxWidth = 30, Grow = 1 };
        DashboardFrame frame = new DashboardFrame("Allocation");
        frame.Sections.Add(cappedFirst ? capped : growing);
        frame.Sections.Add(cappedFirst ? growing : capped);
        Render(frame, 120);
        Assert.Equal(30, cappedWidth);
        Assert.Equal(86, growingWidth);
    }
}
