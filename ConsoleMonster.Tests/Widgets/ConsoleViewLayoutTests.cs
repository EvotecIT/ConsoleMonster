using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets;

public sealed class ConsoleViewLayoutTests {
    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    public void StreamingHeader_NaturalFrameWidth_IncludesTheStreamingFact(int width) {
        ViewHeader header = new ViewHeader("Users", null, "corp.contoso.com") { Streaming = true };
        string text = TestTerminal.RenderText(new ConsoleViewFrame(header), width);
        Assert.Single(text.Split('\n'), line => !string.IsNullOrWhiteSpace(line));
        Assert.Contains("Users · corp.contoso.com · streaming", text);
    }

    [Fact]
    public void Tree_EightyColumnsWithWideValues_KeepsIdentityAndContinuesRoles() {
        Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
        tree.ValueColumns.Add(new TableColumn("Roles") { Priority = 3, MaxWidth = 54 });
        tree.ValueColumns.Add(new TableColumn("State") { Priority = 9, MaxWidth = 36 });
        TreeNode node = tree.Add("CORP-DC01"); node.Values["Roles"] = "GC PDC RID infrastructure schema naming administrator";
        node.Values["State"] = "1 failing partnership";
        string text = TestTerminal.RenderText(new ConsoleViewFrame(new ViewHeader("Forest"), tree), 80);
        Assert.Contains("CORP-DC01", text); Assert.Contains("1 failing partnership", text); Assert.Contains("administrator", text);
        Assert.All(text.Split('\n'), line => Assert.InRange(ConsoleMonster.Text.DisplayWidth.Measure(line), 0, 80));
    }

    [Fact]
    public void Matrix_NumberedRectangularAxes_KeysBothAxesAndKeepsLiteralMarkup() {
        Matrix matrix = new Matrix(new[] { new MatrixLabel("HQ [bold]West[/]"), new MatrixLabel("Branch") },
            new[] { new MatrixLabel("_ldap"), new MatrixLabel("_kerberos"), new MatrixLabel("_gc"), new MatrixLabel("_kpasswd") }) { InlineAxes = true, MinCellWidth = 22, ShowLegend = false };
        matrix.SetCell(0, 0, StatusKind.Pass, "registered");
        string text = TestTerminal.RenderText(matrix, 60);
        Assert.Contains("1 HQ [bold]West[/]", text); Assert.Contains("2 Branch", text);
        foreach (string service in new[] { "1 _ldap", "2 _kerberos", "3 _gc", "4 _kpasswd" }) Assert.Contains(service, text);
        Assert.Contains("Rows", text); Assert.Contains("Columns", text);
        Assert.All(text.Split('\n'), line => Assert.InRange(ConsoleMonster.Text.DisplayWidth.Measure(line), 0, 60));
    }

    [Fact]
    public void FactFlow_OverlongEvidence_WrapsWithoutDiscardingTheTail() {
        string evidence = "query unavailable; missing/stale conclusions withheld; observed 2026-01-15 12:00";
        string text = TestTerminal.RenderText(new FactFlow(evidence), 39);
        Assert.Equal(evidence.Replace(" ", ""), string.Concat(text.Where(character => !char.IsWhiteSpace(character))));
        Assert.All(text.Split('\n'), line => Assert.InRange(ConsoleMonster.Text.DisplayWidth.Measure(line), 0, 39));
    }

    [Fact]
    public void Tree_NaturalWidthBelowTierBoundary_KeepsWideProfileValueColumns() {
        Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
        tree.ValueColumns.Add(new TableColumn("Type") { Priority = 1 });
        tree.ValueColumns.Add(new TableColumn("State") { Priority = 8 });
        TreeNode node = tree.Add("Domain Administrators"); node.Values["Type"] = "group"; node.Values["State"] = "cycle";
        string text = TestTerminal.RenderText(new ConsoleViewFrame(new ViewHeader("Root"), tree), 120);
        Assert.Contains("Domain Administrators", text); Assert.Contains("group", text); Assert.Contains("cycle", text);
    }

    [Fact]
    public void GroupedStream_NarrowBelowColumn_KeepsEveryRowsEvidenceAndOneHeader() {
        TableColumn[] columns = {
            new TableColumn("Name") { IsKey = true, MinWidth = 16 },
            new TableColumn("State") { MinWidth = 10 },
            new TableColumn("Evidence") { Placement = ColumnPlacement.Below }
        };
        GroupedStreamingTable<string, int> stream = new GroupedStreamingTable<string, int>(columns, _ => "",
            name => new TableCell[] { name, "failed", "evidence for " + name }, _ => 0, (count, _) => count + 1,
            (_, count, _) => TextBlock.Plain(count + " complete")) { Border = TableBorder.Rule, RepeatHeaderPerGroup = false };
        var context = Terminal.CreateContext(TerminalProfile.Fixed(60, 24, ColorDepth.None, GlyphTier.Full));
        string text = string.Join("\n", stream.RenderChunks(new[] { "first", "second" }, 58, context).Select(block => block.ToPlainText()));
        Assert.Contains("evidence for first", text);
        Assert.Contains("evidence for second", text);
        Assert.Contains("2 complete", text);
        Assert.Equal(1, text.Split('\n').Count(line => line.Contains("Name")));
        Assert.DoesNotContain("+1 column", text);
    }

    [Fact]
    public void NaturalFrame_AtSixtyColumns_KeepsTheGridTier() {
        Table table = new Table { Border = TableBorder.Rule, RecordViewRows = 0, CardsBelow = 60 };
        table.AddColumn(new TableColumn("Name") { IsKey = true, MinWidth = 20 });
        table.AddColumn(new TableColumn("State") { MinWidth = 20 });
        table.AddColumn(new TableColumn("Detail") { MinWidth = 30, Priority = -1 });
        table.AddRow("CORP-DC01", "failed", "extra evidence");
        string text = TestTerminal.RenderText(new ConsoleViewFrame(new ViewHeader("Services"), table), 60);
        Assert.Contains("Name", text.Split('\n')[2]);
        Assert.Contains("State", text.Split('\n')[2]);
        Assert.Contains("CORP-DC01", text);
        Assert.DoesNotContain("Name:", text);
    }

    [Fact]
    public void RuleTable_MarkerGapAndGroups_RuleMatchesBodyAndOneBlankSeparatesGroups() {
        Table table = new Table { Border = TableBorder.Rule, RecordViewRows = 0, CardsBelow = 0, GroupHeadersAsRows = true, ShowGroupCounts = false, GroupGap = 1 };
        table.AddColumn(new TableColumn("") { Width = 1 });
        table.AddColumn(new TableColumn("Name") { Gap = 1, IsKey = true });
        table.AddColumn("State");
        table.AddGroup("CORP-DC01"); table.AddRow("!", "W32Time", "stopped");
        table.AddGroup("CORP-DC02"); table.AddRow("", "W32Time", "running");
        string text = TestTerminal.RenderText(table, 60);
        Assert.Contains("! W32Time  stopped", text);
        Assert.Contains("stopped\n\nCORP-DC02", text);
        Assert.StartsWith("  ", text.Split('\n')[1]);
        Assert.DoesNotContain("CORP-DC01 · 1", text);
    }

    [Theory]
    [InlineData(120, GlyphTier.Full)]
    [InlineData(60, GlyphTier.Full)]
    [InlineData(60, GlyphTier.Ascii)]
    public void InlineMatrix_DirectionalCells_PreserveMissingSelfAndQueryGapsWithinWidth(int width, GlyphTier tier) {
        MatrixLabel[] labels = Enumerable.Range(1, 6).Select(i => new MatrixLabel("CORP-DC0" + i + ".corp.contoso.com", i <= 2 ? "LON-HQ" : i <= 4 ? "AMS-DC" : "NYC-BR") { ShortText = "CORP-DC0" + i }).ToArray();
        Matrix matrix = new Matrix(labels, labels) { InlineAxes = true, MinCellWidth = 11, RowAxisCaption = "destination", ColumnAxisCaption = "source", ShortRowAxisCaption = "dest", ShortColumnAxisCaption = "src" };
        for (int i = 0; i < labels.Length; i++) matrix.SetCell(i, i, null, diagonal: true);
        matrix.SetCell(0, 1, StatusKind.Pass, "3m"); matrix.SetCell(2, 4, StatusKind.Fail, "2d 4h");
        matrix.SetCell(5, 2, StatusKind.Info, "query failed", marker: "?");
        matrix.GetCell(5, 2)!.Tone = ThemeToken.Gap;
        matrix.LegendLabels[StatusKind.Info] = "query failed"; matrix.LegendMarkers[StatusKind.Info] = "?";
        string text = TestTerminal.RenderText(ConsoleViewStyle.Wrap(matrix), width, glyphs: tier);
        Assert.Contains("query failed", text);
        Assert.All(text.Split('\n'), line => Assert.True(line.Length <= width, line));
        Assert.Contains(tier == GlyphTier.Ascii ? "\\" : "—", text);
        Assert.Contains(tier == GlyphTier.Ascii ? "." : "·", text);
        string self = tier == GlyphTier.Ascii ? "\\" : "—";
        string destination = text.Split('\n').Single(line => line.Contains("CORP-DC03") && line.Contains(self) && (line.StartsWith("AMS-DC") || line.StartsWith("3 ")));
        Assert.Contains(tier == GlyphTier.Ascii ? "x" : "×", destination);
        Assert.DoesNotContain(tier == GlyphTier.Ascii ? "x" : "×", text.Split('\n').Single(line => line.Contains("CORP-DC05") && line.Contains(self) && (line.StartsWith("NYC-BR") || line.StartsWith("5 "))));
    }

    [Theory]
    [InlineData(GlyphTier.Full)]
    [InlineData(GlyphTier.Ascii)]
    public void CompactPaths_CommonOriginAppearsOnce_AndLongChainsHangUnderTheBranch(GlyphTier tier) {
        PathSet paths = new PathSet(new PathHop("u", "adm.mkowalski")) { CompactBranches = true };
        paths.Add(new PathHop("a", "T0-Admins-London"), new PathHop("b", "Tier 0 Operators"), new PathHop("c", "Domain Admins") { Role = ThemeToken.Fail, Bold = true });
        paths.Add(new PathHop("d", "Helpdesk-Escalation"), new PathHop("b", "Tier 0 Operators"), new PathHop("c", "Domain Admins"));
        string text = TestTerminal.RenderText(paths, 50, glyphs: tier);
        Assert.Equal(1, text.Split('\n').Count(line => line.Contains("adm.mkowalski")));
        Assert.Contains(tier == GlyphTier.Ascii ? "|-> " : "├─→ ", text);
        Assert.Contains(tier == GlyphTier.Ascii ? "`-> " : "└─→ ", text);
        Assert.Contains("\n    ", text);
        Assert.All(text.Split('\n'), line => Assert.True(line.Length <= 50));
    }
}
