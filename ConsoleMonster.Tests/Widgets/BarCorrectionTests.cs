using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Frames;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets;

public sealed class BarCorrectionTests {
    private static RenderContext Context(GlyphTier tier = GlyphTier.Full) =>
        Terminal.CreateContext(TerminalProfile.Fixed(80, 24, ColorDepth.TrueColor, tier), Theme.ColorBlindSafe);

    [Theory]
    [InlineData(GlyphTier.Full, "▆")]
    [InlineData(GlyphTier.Basic, "▄")]
    [InlineData(GlyphTier.Ascii, "#")]
    public void Distribution_UsesComparisonBlocksAndBlankRemainder(GlyphTier tier, string glyph) {
        RenderContext context = Context(tier);
        var buffer = FrameComposer.RenderBlock(new Distribution { BarWidth = 8 }.Add("a", 8).Add("b", 4, HealthState.Failing), 30, context);
        Assert.Equal(glyph, buffer[3, 0].Glyph);
        Assert.Equal(glyph, buffer[6, 1].Glyph);
        Assert.Equal(" ", buffer[7, 1].Glyph);
        Assert.Equal(context.Style(ThemeToken.Fail).Color, buffer[3, 1].Style.Color);
        Assert.Equal(TextDecoration.None, buffer[3, 1].Style.Decoration);
    }

    [Theory]
    [InlineData(StatusKind.Fail, ThemeToken.Fail)]
    [InlineData(StatusKind.Warn, ThemeToken.Warn)]
    [InlineData(StatusKind.Pass, ThemeToken.Muted)]
    public void StackedBar_NeutralRoleUsesStatusColorForFillAndSwatch(StatusKind status, ThemeToken expected) {
        RenderContext context = Context();
        var bar = new StackedBar().Add("jobs", 1);
        bar.Segments[0].Status = status;
        var buffer = FrameComposer.RenderBlock(bar, 40, context);
        foreach (int y in new[] { 0, 1 }) {
            Assert.Equal(context.Style(expected).Color, buffer[0, y].Style.Color);
            Assert.Equal(TextDecoration.None, buffer[0, y].Style.Decoration);
        }
        bar.ShowLegend = false;
        Assert.Equal(context.Glyphs.StatusSymbol(status), bar.GetFillMarker(0, context));
    }

    [Fact]
    public void StackedBar_LaterProblemSegmentsKeepOpaqueStatusFillsInColor() {
        var bar = new StackedBar().Add("Compliant", 7).Add("Pending reboot", 2, ThemeToken.Warn)
            .Add("Missing updates", 2, ThemeToken.Warn).Add("Failed", 1, ThemeToken.Fail);
        RenderContext context = Context();
        Assert.Equal("█", bar.GetFillMarker(3, context));
        CellBuffer frame = FrameComposer.RenderBlock(bar, 60, context);
        Assert.Equal("█", frame[55, 0].Glyph);
        Assert.Equal(context.Style(ThemeToken.Fail).Color, frame[55, 0].Style.Color);
        Assert.Equal(TextDecoration.None, frame[55, 0].Style.Decoration);
    }

    [Theory]
    [InlineData(0.025, "    ")]
    [InlineData(0.125, "╸   ")]
    [InlineData(0.625, "━━╸ ")]
    public void LineFill_RetainsMidpointToEvenRoundingAndHalfCap(double fraction, string expected) {
        var buffer = new CellBuffer(4, 1);
        RenderContext context = Context();
        BarDrawer.Draw(new Region(buffer), 0, 0, 4, fraction, context.Style(ThemeToken.Fail), context, false, BarShape.Line);
        Assert.Equal(expected, buffer.GetRowText(0));
        Assert.Equal(TextDecoration.None, buffer[0, 0].Style.Decoration);
    }

    [Theory]
    [InlineData(GlyphTier.Basic, 0.66, 22, 15)]
    [InlineData(GlyphTier.Ascii, 0.66, 22, 14)]
    [InlineData(GlyphTier.Basic, 0.001, 10, 1)]
    [InlineData(GlyphTier.Ascii, 0.001, 10, 0)]
    [InlineData(GlyphTier.Basic, 0.25, 10, 2)]
    [InlineData(GlyphTier.Ascii, 0.25, 10, 2)]
    [InlineData(GlyphTier.Basic, 0.35, 10, 4)]
    [InlineData(GlyphTier.Ascii, 0.35, 10, 3)]
    public void LineFill_BasicAndAsciiRetainTheirPreviousRounding(GlyphTier tier, double fraction, int width, int filled) {
        RenderContext context = Context(tier);
        var buffer = new CellBuffer(width, 1);
        BarDrawer.Draw(new Region(buffer), 0, 0, width, fraction, context.Style(ThemeToken.Fail), context, true, BarShape.Line);
        char fill = tier == GlyphTier.Basic ? '═' : '#';
        char track = tier == GlyphTier.Basic ? '─' : '-';
        Assert.Equal(new string(fill, filled) + new string(track, width - filled), buffer.ToPlainText());
    }

    [Fact]
    public void MarkupBars_PreserveProblemColorWithoutTextDecorations() {
        RenderContext context = Context();
        StyledLine line = context.Markup(BarDrawer.Markup(4, 0.5, "fail", context, true, BarShape.Line)).Lines[0];
        var buffer = new CellBuffer(4, 1);
        new Region(buffer).Write(0, 0, line);
        Assert.Equal(context.Style(ThemeToken.Fail).Color, buffer[0, 0].Style.Color);
        for (int x = 0; x < 4; x++) Assert.Equal(TextDecoration.None, buffer[x, 0].Style.Decoration);
        StyledLine range = context.Markup(FrameBars.Range(0, 1, 4, HealthState.Failing, null, context)).Lines[0];
        new Region(buffer).Write(0, 0, range);
        Assert.Equal(context.Style(ThemeToken.Fail).Color, buffer[0, 0].Style.Color);
        for (int x = 0; x < 4; x++) Assert.Equal(TextDecoration.None, buffer[x, 0].Style.Decoration);
    }
}
