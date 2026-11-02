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
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Live;
using ConsoleMonster.Widgets;
using Xunit;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.Tests.Rendering {
    public sealed partial class BehaviourContractTests {
        public static IEnumerable<object[]> ColourIndependentCases() {
            foreach (string scene in new[] { "header", "verdict", "gauge", "bar role", "bar expanded label", "calendar", "bar values", "bar without values", "lanes", "progress", "cells", "flow", "stacked", "line", "scatter" })
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
                var chart = new LineChart { Scatter = scene == "scatter" };
                chart.Add("Alpha", 10).Add("Alpha", 20).Add("Beta", 30).Add("Beta", 40);
                widget = chart;
            }
            string text = FrameComposer.RenderBlock(widget, 80, context).ToPlainText();
            if (scene == "bar expanded label") Assert.Contains(tier == GlyphTier.Ascii ? "ssabc " : "ßabc ", text);
            if (scene == "stacked") {
                string bar = text.Split('\n')[0];
                Assert.True(bar.Trim().Distinct().Count() >= 2, "Series fills must be distinguishable when styles are removed.");
                Assert.Contains("Alpha", text);
                Assert.Contains("Beta", text);
            } else if (scene == "line" || scene == "scatter") {
                Assert.Contains("1 Alpha", text);
                Assert.Contains("2 Beta", text);
                // The key must identify the traces too, not just two differently coloured identical legend symbols.
                string plot = text.Substring(0, text.LastIndexOf('\n'));
                Assert.Contains("1", plot);
                Assert.Contains("2", plot);
            } else {
                string marker = tier == GlyphTier.Full ? "✗" : tier == GlyphTier.Basic ? "×" : scene == "flow" || scene == "header" ? "[FAIL]" : "x";
                Assert.Contains(marker, text);
            }
        }

        public static IEnumerable<object[]> FocusCases() {
            foreach (string scene in new[] { "text input", "text area", "toggle", "select", "multiselect", "button", "date picker", "grid", "tree select", "log viewport", "tabs", "menu choices" })
                foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii })
                    yield return new object[] { scene, tier };
        }

        [Theory]
        [MemberData(nameof(FocusCases))]
        public void Focus_IsVisibleAfterRemovingAllStyles(string scene, GlyphTier tier) {
            var context = new RenderContext(TerminalProfile.Fixed(80, 24, ColorDepth.TrueColor, tier), Theme.Dark);
            IWidget widget = scene == "tabs" ? new Tabs().Add("Alpha", new TextBlock("State")).Add("Beta", new TextBlock("State"))
                : scene == "grid" ? new DataGrid("grid", new Grid("Rows").AddColumn("Value").AddRow("first", "alpha"))
                : scene == "menu choices" ? new ChoiceList(new[] { new Choice(1, "Alpha") }, false) { Focused = false, ShowInactiveFocus = true }
                : Scenes[scene]();
            try {
                if (widget is InputControl input) input.Focused = false;
                string before = FrameComposer.RenderBlock(widget, 80, context).ToPlainText();
                if (widget is InputControl control) control.Focus();
                else if (widget is Tabs tabs) tabs.SelectedIndex = 1;
                else ((ChoiceList)widget).Focused = true;
                string after = FrameComposer.RenderBlock(widget, 80, context).ToPlainText();
                Assert.NotEqual(before, after);
                if (scene == "tabs") Assert.Contains("[Beta]", after);
                else Assert.Contains(InteractiveGlyphs.For(context).Pointer, after);
                if (scene == "grid") Assert.Matches(System.Text.RegularExpressions.Regex.Escape(InteractiveGlyphs.For(context).Pointer) + @"\s+alpha", after);
                if (scene == "date picker") Assert.Contains("Cursor: 2026-11-02", after);
            } finally { (widget as IDisposable)?.Dispose(); }
        }

    }
}
