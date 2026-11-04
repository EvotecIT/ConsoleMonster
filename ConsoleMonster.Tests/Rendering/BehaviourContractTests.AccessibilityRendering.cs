using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Interactive;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Live;
using ConsoleMonster.Widgets;
using Xunit;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.Tests.Rendering {
    public sealed partial class BehaviourContractTests {
        public static IEnumerable<object[]> ColourIndependentCases() {
            foreach (string scene in new[] { "header", "verdict", "gauge", "bar role", "bar expanded label", "data bar", "data bar word", "calendar", "bar values", "bar without values", "lanes", "progress", "cells", "flow", "stacked", "line", "scatter", "line status", "scatter status", "line converging", "scatter converging", "line quantized", "scatter quantized" })
                foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii })
                    foreach (ColorDepth color in new[] { ColorDepth.None, ColorDepth.TrueColor })
                        yield return new object[] { scene, tier, color };
        }

        [Theory]
        [MemberData(nameof(ColourIndependentCases))]
        public void StatusAndSeries_KeepMeaningWithoutColour(string scene, GlyphTier tier, ColorDepth color) {
            var context = new RenderContext(TerminalProfile.Fixed(80, 24, color, tier), Theme.Dark);
            IWidget widget;
            if (scene == "header") widget = new HeaderBar("Result") { Status = StatusKind.Fail };
            else if (scene == "verdict") widget = new Panel(new TextBlock("Result")) { Verdict = true, Status = StatusKind.Fail };
            else if (scene == "gauge") widget = new ScoreGauge(90, role: ThemeToken.Fail);
            else if (scene.StartsWith("data bar", StringComparison.Ordinal)) widget = new Table().AddColumn(new TableColumn("Disk") { Presenter = new DataBarPresenter(100, new NumberPresenter { NonZeroRole = ThemeToken.Fail }) { ShowRoleMarker = scene != "data bar word" } }).AddValues(90);
            else if (scene == "bar expanded label") widget = new BarChart().Add("ßabc", 90, ThemeToken.Fail);
            else if (scene == "bar role") widget = new BarChart().Add("Disk", 90, ThemeToken.Fail);
            else if (scene == "calendar") widget = new Calendar(2026, 11, new[] { new Dates.CalendarMark(new DateTime(2026, 11, 2), StatusKind.Fail) }) { ShowLegend = false };
            else if (scene.StartsWith("bar", StringComparison.Ordinal)) widget = new BarChart { Critical = 80, ShowValues = scene == "bar values" }.Add("Disk", 90);
            else if (scene == "lanes") {
                var lanes = new Lanes { Columns = new[] { ProgressColumns.Description() } };
                lanes.Add("Disk").Status = StatusKind.Fail;
                widget = lanes;
            } else if (scene == "progress") widget = new ProgressBar("Disk") { Status = StatusKind.Fail, Columns = new[] { ProgressColumns.Description() } };
            else if (scene == "cells") widget = new CellMap { ShowLegend = false }.Resize(1).Set(0, StatusKind.Fail);
            else if (scene == "flow") widget = new Flow().Node("disk", "Disk", StatusKind.Fail);
            else if (scene == "stacked") widget = new StackedBar().Add("Alpha", 1).Add("Beta", 1);
            else {
                var chart = new LineChart { Scatter = scene.StartsWith("scatter", StringComparison.Ordinal) };
                if (scene.EndsWith("status", StringComparison.Ordinal)) { var series = chart.AddSeries("Disk", ThemeToken.Fail); series.Add(10, 512); series.Add(20, 512); }
                else chart.Add("Alpha", 10).Add("Alpha", 20).Add("Beta", 30).Add("Beta", scene.EndsWith("converging", StringComparison.Ordinal) ? 20 : scene.EndsWith("quantized", StringComparison.Ordinal) ? 20.1 : 40);
                if (scene == "line" || scene == "scatter") chart.Add("Gamma", 35).Add("Gamma", 5).Add("Delta", 5).Add("Delta", 35);
                widget = chart;
            }
            string text = FrameComposer.RenderBlock(widget, 80, context).ToPlainText();
            if (scene == "bar expanded label") Assert.Contains(tier == GlyphTier.Ascii ? "ssabc " : "ßabc ", text);
            if (scene == "stacked") {
                string bar = text.Split('\n')[0];
                Assert.True(bar.Trim().Distinct().Count() >= 2, "Series fills must be distinguishable when styles are removed.");
                Assert.Contains("Alpha", text);
                Assert.Contains("Beta", text);
            } else if ((scene.StartsWith("line", StringComparison.Ordinal) || scene.StartsWith("scatter", StringComparison.Ordinal)) && !scene.EndsWith("status", StringComparison.Ordinal)) {
                Assert.Contains(color == ColorDepth.None ? "1 * Alpha" : "1 Alpha", text);
                Assert.Contains(color == ColorDepth.None ? "2 + Beta" : "2 Beta", text);
                // The key must identify the traces too, not just two differently coloured identical legend symbols.
                string plot = text.Substring(0, text.LastIndexOf('\n'));
                Assert.Contains("1", plot);
                Assert.Contains("2", plot);
                if (color == ColorDepth.None) {
                    Assert.Contains("*", plot);
                    Assert.Contains("+", plot);
                    if (scene == "line" || scene == "scatter") {
                        Assert.Contains("3 o Gamma", text);
                        Assert.Contains("4 x Delta", text);
                        Assert.Contains("o", plot);
                        Assert.Contains("x", plot);
                        // At the left edge, 10/30/35/5 sit on four distinct rows of the 0..40 scale.
                        string[] rows = plot.Split('\n');
                        Assert.Equal('*', rows[4][3]);
                        Assert.Equal('+', rows[2][3]);
                        Assert.Equal('o', rows[1][3]);
                        Assert.Equal('x', rows[5][3]);
                    }
                }
            } else {
                string marker = scene == "data bar word" ? context.Labels.StatusWord(StatusKind.Fail) : tier == GlyphTier.Full ? "✗" : tier == GlyphTier.Basic ? "×" : scene == "flow" || scene == "header" ? "[FAIL]" : "x";
                Assert.Contains(marker, text);
            }
        }

        public static IEnumerable<object[]> FocusCases() {
            foreach (string scene in new[] { "text input", "text area", "toggle", "select", "multiselect", "button", "date picker", "grid", "tree select", "log viewport", "tabs", "menu choices" })
                foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii })
                    yield return new object[] { scene, tier };
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii })
                yield return new object[] { "compact date picker", tier };
        }

        [Theory]
        [MemberData(nameof(FocusCases))]
        public void Focus_IsVisibleAfterRemovingAllStyles(string scene, GlyphTier tier) {
            var context = new RenderContext(TerminalProfile.Fixed(80, 24, scene == "grid" ? ColorDepth.None : ColorDepth.TrueColor, tier), Theme.Dark);
            int height = scene == "compact date picker" ? 12 : 24;
            IWidget widget = scene == "tabs" ? new Tabs().Add("Alpha", new TextBlock("State")).Add("Beta", new TextBlock("State"))
                : scene == "grid" ? new DataGrid("grid", new Grid("Rows").AddColumn("Value").AddRow("first", "alpha")) { StatusPresenters = true }
                : scene == "menu choices" ? new ChoiceList(new[] { new Choice(1, "Alpha") }, false) { Focused = false, ShowInactiveFocus = true }
                : Scenes[scene == "compact date picker" ? "date picker" : scene]();
            try {
                if (widget is InputControl input) input.Focused = false;
                if (scene == "compact date picker") ((DatePicker)widget).FirstDayOfWeek = DayOfWeek.Monday;
                string before = FrameComposer.Compose(widget, 80, height, context).ToPlainText();
                if (scene == "compact date picker") {
                    var picker = (DatePicker)widget;
                    picker.Range = true;
                    picker.Value = new Dates.DateRange(new DateTime(2026, 11, 2), new DateTime(2026, 11, 5));
                }
                if (widget is InputControl control) control.Focus();
                else if (widget is Tabs tabs) tabs.SelectedIndex = 1;
                else ((ChoiceList)widget).Focused = true;
                string after = FrameComposer.Compose(widget, 80, height, context).ToPlainText();
                Assert.NotEqual(before, after);
                if (scene == "tabs") Assert.Contains("[Beta]", after);
                else Assert.Contains(InteractiveGlyphs.For(context).Pointer, after);
                if (scene == "grid") {
                    Assert.Matches(System.Text.RegularExpressions.Regex.Escape(InteractiveGlyphs.For(context).Pointer) + @"\s+alpha", after);
                    string[] rows = after.Split('\n');
                    int headerRow = Array.FindIndex(rows, row => row.Contains("Value"));
                    Assert.Contains(context.Glyphs.Get(Glyph.Check), rows[headerRow]);
                    int valueColumn = rows[headerRow].IndexOf("Value", StringComparison.Ordinal);
                    string underline = rows[headerRow + 1];
                    Assert.Equal(new string(' ', valueColumn - 1), underline.Substring(1, valueColumn - 1));
                    Assert.Contains(context.Glyphs.Get(Glyph.RuleLine), underline.Substring(valueColumn));
                }
                if (scene.Contains("date picker")) Assert.Contains("Cursor: 2026-11-02", after);
                if (scene == "compact date picker") {
                    Assert.Contains("Selected: 2026-11-02..2026-11-05", after);
                    var picker = (DatePicker)widget;
                    object? selected = picker.Value;
                    Assert.False(picker.HandleInput(InputEvent.Mouse(10, 9), context));
                    Assert.False(picker.HandleInput(InputEvent.Pointer(10, 9, MouseAction.Up, 1), context));
                    Assert.Same(selected, picker.Value);
                    Assert.Equal(new DateTime(2026, 11, 2), picker.Cursor);
                }
            } finally { (widget as IDisposable)?.Dispose(); }
        }

    }
}
