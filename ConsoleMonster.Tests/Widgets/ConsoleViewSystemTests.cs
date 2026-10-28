using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets;

public sealed class ConsoleViewSystemTests {
    [Theory]
    [InlineData(GlyphTier.Full, "✗ FAIL", "× FAIL")]
    [InlineData(GlyphTier.Basic, "× FAIL", "× FAIL")]
    [InlineData(GlyphTier.Ascii, "[FAIL]", "x FAIL")]
    public void ConsoleViewStyle_OptIn_DoesNotChangeOrdinaryRendering(GlyphTier tier, string ordinaryStatus, string viewStatus) {
        Table table = new Table { Border = TableBorder.None, RecordViewRows = 0, ShowHeader = false };
        table.AddColumn(new TableColumn("State") { Presenter = new ConsoleMonster.Presenters.StatusPresenter() });
        table.AddColumn(new TableColumn("Used") { Presenter = new ConsoleMonster.Presenters.NumberPresenter("%", 0) });
        table.AddValues(StatusKind.Fail, 68);
        string before = TestTerminal.RenderText(table, 80, glyphs: tier);
        string styled = TestTerminal.RenderText(ConsoleViewStyle.Wrap(table), 80, glyphs: tier);
        string after = TestTerminal.RenderText(table, 80, glyphs: tier);
        Assert.Equal(before, after);
        Assert.Contains(ordinaryStatus, before);
        Assert.Contains("68 %", before);
        Assert.Contains(viewStatus, styled);
        Assert.Contains("68%", styled);
        Assert.DoesNotContain("68 %", styled);
    }

    [Theory]
    [InlineData(120, GlyphTier.Full)]
    [InlineData(60, GlyphTier.Full)]
    [InlineData(120, GlyphTier.Ascii)]
    [InlineData(60, GlyphTier.Ascii)]
    public void Frame_TitleStateAndGutter_ContentWidthWithoutFilledBand(int width, GlyphTier tier) {
        Table table = new Table { RecordViewRows = 0 };
        table.AddColumn(new TableColumn("Computer") { MinWidth = 12, IsKey = true });
        table.AddColumn("State"); table.AddRow("CORP-DC01", "running");
        ConsoleViewFrame frame = new ConsoleViewFrame(new ViewHeader("Services", "1 stopped", "corp.contoso.com", "18 services"), table);
        string text = TestTerminal.RenderText(frame, width, glyphs: tier);
        string[] lines = text.Split('\n');
        Assert.StartsWith(" Services", lines[0]);
        Assert.Contains("1 stopped", lines[0]);
        Assert.DoesNotContain("╭", text);
        Assert.All(lines.Where(line => line.Length > 0), line => Assert.StartsWith(" ", line));
        Assert.All(lines, line => Assert.True(line.Length <= width - 1, line));
        Assert.DoesNotContain("\n\n\n", text);
    }

    [Fact]
    public void RuleTable_LongHeadersAndLowPriorityDetail_KeepIdentityMinimumAndValueWidth() {
        Table table = new Table { Border = TableBorder.Rule, CardsBelow = 0, RecordViewRows = 0 };
        table.AddColumn(new TableColumn("A deliberately long identity header") { IsKey = true, MinWidth = 24, Overflow = ColumnOverflow.Truncate });
        table.AddColumn(new TableColumn("A very long count header") { ShortHeader = "N", Priority = 5 });
        table.AddColumn(new TableColumn("Detail") { Priority = -1, MinWidth = 35 });
        table.AddRow("CORP-DC01.corp.contoso.com", TableCell.FromValue(1), "extra evidence");
        string text = TestTerminal.RenderText(table, 40);
        Assert.Contains("CORP-DC01.corp.contoso.com", text);
        Assert.Contains("N", text.Split('\n')[0]);
        Assert.DoesNotContain("extra evidence", text);
        Assert.All(text.Split('\n'), line => Assert.True(line.Length <= 40));
    }

    [Theory]
    [InlineData(GlyphTier.Full)]
    [InlineData(GlyphTier.Ascii)]
    public void BelowColumn_NarrowTable_KeepsEvidenceOnItsOwnContinuationRow(GlyphTier tier) {
        Table table = new Table { Border = TableBorder.Rule, CardsBelow = 0, RecordViewRows = 0 };
        table.AddColumn(new TableColumn("Severity") { MinWidth = 10 });
        table.AddColumn(new TableColumn("Rule") { IsKey = true, MinWidth = 20 });
        table.AddColumn(new TableColumn("Detail") { Placement = ColumnPlacement.Below });
        table.AddRow(TableCell.ForSeverity(Severity.Critical), "krbtgt not rotated", "Kerberos: last set 412 days ago");
        table.AddRow(TableCell.ForSeverity(Severity.High), "Time source local", "Clock: +412 ms from the PDC");
        string text = TestTerminal.RenderText(table, 58, glyphs: tier);
        string[] lines = text.Split('\n');
        int first = Array.FindIndex(lines, line => line.Contains("krbtgt not rotated"));
        int second = Array.FindIndex(lines, line => line.Contains("Time source local"));
        Assert.True(first >= 0 && second > first);
        Assert.StartsWith(new string(' ', 12), lines[first + 1]);
        Assert.Contains("Kerberos: last set 412 days ago", lines[first + 1]);
        Assert.Contains("Clock: +412 ms from the PDC", lines[second + 1]);
        Assert.DoesNotContain("+1 column", text);
    }

    [Theory]
    [InlineData(GlyphTier.Full, "◆", "▵")]
    [InlineData(GlyphTier.Basic, "♦", "◊")]
    [InlineData(GlyphTier.Ascii, "X", "~")]
    public void SeverityScale_EveryTier_DistinguishesCriticalLowAndEvidenceGaps(GlyphTier tier, string critical, string low) {
        Assert.Equal(critical, StatusScale.Marker(Severity.Critical, tier));
        Assert.Equal(low, StatusScale.Marker(Severity.Low, tier));
        Assert.Equal(ThemeToken.Gap, StatusScale.Role(Severity.CouldNotEvaluate));
        Assert.NotEqual(StatusScale.Marker(Severity.Unknown, tier), StatusScale.Marker(Severity.CouldNotEvaluate, tier));
        Assert.Equal(9, Enum.GetValues(typeof(Severity)).Cast<Severity>().Select(value => StatusScale.Marker(value, tier)).Distinct().Count());
    }

    [Theory]
    [InlineData(120, 24, GlyphTier.Full)]
    [InlineData(60, 20, GlyphTier.Full)]
    [InlineData(60, 20, GlyphTier.Ascii)]
    public void ScoreGauge_SuppliedScore_UsesRequestedScaleAndFindingRole(int width, int cells, GlyphTier tier) {
        ScoreGauge gauge = new ScoreGauge(72, 100, ThemeToken.Critical);
        Review184Capture capture = new Review184Capture(gauge, width, tier, ColorDepth.TrueColor);
        string text = capture.Raw.ToPlainText();
        Assert.StartsWith("72/100  ", text);
        string bar = text.Substring(8);
        Assert.Equal(cells, bar.Length);
        Assert.Equal(capture.Context.Style(ThemeToken.Critical).Color, capture.Raw[8, 0].Style.Color);
        Assert.DoesNotContain("security", text);
    }

    [Fact]
    public void AfterLabelTree_BareRoot_DoesNotPushValuesToRightEdge() {
        Tree tree = new Tree { CompactGuides = true, ValuePlacement = TreeValuePlacement.AfterLabel, ShowValueHeaders = false };
        tree.ValueColumns.Add(new TableColumn("Type") { Priority = 1 });
        tree.ValueColumns.Add(new TableColumn("State") { Priority = 8 });
        TreeNode root = tree.Add("A long bare site name which must not widen every child") ; root.Bare = true;
        TreeNode user = root.Add("adm.jsmith"); user.Values["Type"] = "user"; user.Values["State"] = TableCell.ForStatus(StatusKind.Pass, "enabled");
        string[] lines = TestTerminal.RenderText(tree, 118).Split('\n');
        string row = Assert.Single(lines, line => line.Contains("adm.jsmith"));
        Assert.Contains("├", row.Replace("└", "├"));
        Assert.True(row.IndexOf("user", StringComparison.Ordinal) < 30, row);
        Assert.Contains(root.Label, lines[0]);
    }

    [Fact]
    public void DarkThemes_Console16Roles_AvoidPowerShellMagentaYellowSlots() {
        foreach (Theme theme in new[] { Theme.Dark, Theme.HighContrast, Theme.ColorBlindSafe }) {
            foreach (ThemeToken token in Enum.GetValues(typeof(ThemeToken))) {
                Color color = theme.Resolve(token, ColorDepth.Ansi16).Color;
                if (color.IsDefault) { continue; }
                Assert.DoesNotContain(color.Index, new byte[] { 3, 5 });
            }
        }
    }
}
