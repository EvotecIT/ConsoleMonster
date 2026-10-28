using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets;

public sealed class ConsoleViewIntegrationTests {
    [Theory]
    [InlineData(StatusKind.Pass, "healthy")]
    [InlineData(StatusKind.Fail, "")]
    [InlineData(StatusKind.Warn, "[literal]")]
    public void DisplayText_OrdinaryAsciiStatus_PreservesLiteralMarkerAndLabel(StatusKind status, string label) {
        RenderContext context = Context();
        string markup = DisplayText.Status(status, label, context);
        Assert.True(Markup.Validate(markup, out string? error), error);
        string text = TestTerminal.RenderText(new TextBlock(markup), 80, glyphs: GlyphTier.Ascii);
        Assert.Contains(context.Glyphs.StatusMarker(status), text);
        Assert.Contains(label, text);
    }

    [Fact]
    public void Table_VisibilityBeforeFirstRender_PreparesInferredColumns() {
        Table table = Table.For(new[] { new { Name = "host-one", Evidence = "confirmed" } });
        Assert.True(table.DisplaysColumn("Evidence", 80, Context()));
        Assert.Contains("confirmed", TestTerminal.RenderText(table, 80));
        Assert.False(table.DisplaysColumn("Absent", 80, Context()));
    }

    [Fact]
    public void Table_VisibilityInColumnBands_ReportsRenderedEvidence() {
        Table table = new Table { KeepAllColumns = true, RecordViewRows = 0, CardsBelow = 0 };
        table.AddColumn(new TableColumn("Name") { IsKey = true, MinWidth = 30 });
        table.AddColumn(new TableColumn("Evidence") { MinWidth = 30 });
        table.AddValues("host-one", "banded-evidence");
        Assert.Contains("banded-evidence", TestTerminal.RenderText(table, 40));
        Assert.True(table.DisplaysColumn("Name", 40, Context(40)));
        Assert.True(table.DisplaysColumn("Evidence", 40, Context(40)));
    }

    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    public void Tree_AfterLabel_PreservesStatusDetailAndScalarEvidence(int width) {
        Tree tree = AfterLabelTree();
        TreeNode node = tree.Add("Service", StatusKind.Fail, "query unavailable");
        node.Value = TableCell.FromValue(42);
        node.Values["Type"] = TableCell.FromValue("service");
        string text = TestTerminal.RenderText(tree, width, glyphs: GlyphTier.Ascii);
        Assert.Contains("x", text);
        Assert.Contains("query unavailable", text);
        Assert.Contains("42", text);
        Assert.Contains("Service", text);
        Assert.Contains("service", text);
    }

    [Theory]
    [InlineData(TreeReferenceKind.Cycle, "cycle")]
    [InlineData(TreeReferenceKind.Repeat, "shown above")]
    [InlineData(TreeReferenceKind.Incomplete, "incomplete")]
    public void Tree_AfterLabel_ExplainsReferenceWithoutAStateColumn(TreeReferenceKind reference, string expected) {
        Tree tree = AfterLabelTree();
        TreeNode node = tree.Add("Reference");
        node.Reference = reference;
        node.ReferenceSummary = "source evidence";
        node.Values["Type"] = TableCell.FromValue("group");
        string text = TestTerminal.RenderText(tree, 80, glyphs: GlyphTier.Ascii);
        Assert.Contains(expected, text);
        Assert.Contains("source evidence", text);
        Assert.Contains("Reference", text);
    }

    [Theory]
    [InlineData(TableBorder.None)]
    [InlineData(TableBorder.Rounded)]
    public void Table_BelowRowFooter_PreservesItsEvidence(TableBorder border) {
        Table table = new Table { Border = border, RecordViewRows = 0, CardsBelow = 0 };
        table.AddColumn(new TableColumn("Name") { IsKey = true });
        table.AddColumn(new TableColumn("Evidence") { Placement = ColumnPlacement.Below, BelowWidth = 100 });
        table.AddValues("host-one", "row evidence");
        table.SetFooter(TableCell.FromValue("Total"), TableCell.FromValue("final evidence"));
        string text = TestTerminal.RenderText(table, 80);
        Assert.Contains("row evidence", text);
        Assert.Contains("final evidence", text);
        Assert.True(text.IndexOf("final evidence", System.StringComparison.Ordinal) > text.IndexOf("row evidence", System.StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Table_DimRepeats_UsesMutedForegroundInBufferedChunkAndWindowRendering(bool numeric) {
        Table table = new Table { ShowHeader = false, Border = TableBorder.None, RecordViewRows = 0, CardsBelow = 0 };
        table.AddColumn(new TableColumn("Key") { DimRepeats = true });
        object value = numeric ? (object)42 : "host-one";
        table.AddValues(value); table.AddValues(value);
        RenderContext context = Context();
        CellBuffer buffered = FrameComposer.RenderBlock(table, 80, context);
        Assert.Equal(context.Style(ThemeToken.Text).Color, buffered[0, 0].Style.Color);
        Assert.Equal(context.Style(ThemeToken.Muted).Color, buffered[0, 1].Style.Color);
        CellBuffer[] chunks = table.RenderChunks(80, context, 1).ToArray();
        Assert.Equal(context.Style(ThemeToken.Muted).Color, chunks.Last()[0, 0].Style.Color);
        CellBuffer window = table.RenderWindow(80, 1, 1, context, out _, out List<int> _, out _);
        Assert.Equal(context.Style(ThemeToken.Muted).Color, window[0, 0].Style.Color);
    }

    private static Tree AfterLabelTree() {
        Tree tree = new Tree { ValuePlacement = TreeValuePlacement.AfterLabel };
        tree.ValueColumns.Add(new TableColumn("Type") { Priority = 10 });
        return tree;
    }

    private static RenderContext Context(int width = 80) => Terminal.CreateContext(TestTerminal.Profile(width, glyphs: GlyphTier.Ascii, colors: ColorDepth.TrueColor));
}
