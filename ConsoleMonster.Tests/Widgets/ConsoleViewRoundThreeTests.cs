using System;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets;

public sealed class ConsoleViewRoundThreeTests {
    [Theory]
    [InlineData(GlyphTier.Full)]
    [InlineData(GlyphTier.Basic)]
    [InlineData(GlyphTier.Ascii)]
    public void ConsoleView_HealthyWord_UsesTextWhileMarkerKeepsPass(GlyphTier tier) {
        Table table = new Table { ShowHeader = false, RecordViewRows = 0, Border = TableBorder.None };
        table.AddColumn("State");
        table.AddRow(TableCell.ForStatus(StatusKind.Pass, "running"));
        Review184Capture capture = new Review184Capture(ConsoleViewStyle.Wrap(table), 60, tier, ColorDepth.TrueColor);
        Assert.Equal(ConsoleViewStyle.Apply(capture.Context).Style(ThemeToken.Pass).Color, capture.Raw[0, 0].Style.Color);
        Assert.Equal(ConsoleViewStyle.Apply(capture.Context).Style(ThemeToken.Text).Color, capture.Raw[2, 0].Style.Color);
        Assert.Contains("running", capture.Raw.ToPlainText());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConsoleView_StatusPresenter_HealthySeverityAndStatusUseTextWords(bool severity) {
        var context = ConsoleViewStyle.Apply(Terminal.CreateContext(TestTerminal.Profile(60, colors: ColorDepth.TrueColor)));
        var value = new StatusPresenter().Present(severity ? (object)Severity.Passed : StatusKind.Pass, new PresenterContext(context));
        Assert.Equal(context.Style(ThemeToken.Pass).Color, value.Line.First().Style.Color);
        Assert.Equal(context.Style(ThemeToken.Text).Color, value.Line.Last().Style.Color);
    }

    [Fact]
    public void ViewHint_UnnamedHiddenColumn_DoesNotPrintEmptyColumnList() {
        ViewHint hint = new ViewHint(); hint.Columns.Add(""); hint.Columns.Add("   ");
        string text = TestTerminal.RenderText(hint, 60);
        Assert.DoesNotContain("column", text);
        hint.MoreRows = 3;
        Assert.Contains("+3 more", TestTerminal.RenderText(hint, 60));
    }

    [Theory]
    [InlineData(60, false)]
    [InlineData(120, true)]
    public void Table_ColumnVisibility_UsesTheSameLayoutAsTheBody(int width, bool expected) {
        Table table = new Table { Border = TableBorder.Rule, RecordViewRows = 0, CardsBelow = 0 };
        table.AddColumn(new TableColumn("Name") { IsKey = true, MinWidth = 30 });
        table.AddColumn(new TableColumn("Settings") { MinWidth = 35, Priority = -1 });
        table.AddRow("Default Domain Policy", "computer and user settings");
        var context = Terminal.CreateContext(TestTerminal.Profile(width));
        Assert.Equal(expected, table.DisplaysColumn("Settings", width, context));
        Assert.Equal(expected, TestTerminal.RenderText(table, width).Contains("computer and user settings"));
    }

    [Theory]
    [InlineData(GlyphTier.Full, '█', '░')]
    [InlineData(GlyphTier.Ascii, '#', '.')]
    public void ScoreGauge_FractionalScore_HasNoUnpaintedPartialCell(GlyphTier tier, char fill, char track) {
        Review184Capture capture = new Review184Capture(new ScoreGauge(72), 120, tier, ColorDepth.TrueColor);
        string bar = capture.Raw.ToPlainText().Substring(8);
        Assert.Equal(24, bar.Length);
        Assert.Equal(new string(fill, 17) + new string(track, 7), bar);
        Assert.Equal(capture.Context.Style(ThemeToken.Subtle).Color, capture.Raw[25, 0].Style.Color);
    }

    [Fact]
    public void DisplayText_PolishCulture_FormatsFractionalDurationAndHistoricalDateConsistently() {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            Assert.Equal("3,1s", DisplayText.Duration(TimeSpan.FromSeconds(3.1)));
            Assert.Equal("27 paź 10:42", DisplayText.Time(new DateTimeOffset(2026, 10, 27, 10, 42, 0, DateTimeOffset.Now.Offset), new DateTimeOffset(2026, 10, 28, 10, 42, 0, DateTimeOffset.Now.Offset)));
            Assert.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, DisplayText.Count(1203, "day"));
        } finally { CultureInfo.CurrentCulture = previous; }
    }
}
