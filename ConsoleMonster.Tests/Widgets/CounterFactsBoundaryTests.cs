using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Dates;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;
using Calendar = ConsoleMonster.Widgets.Calendar;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class CounterFactsBoundaryTests {
        private static RenderContext Context(int width, GlyphTier tier = GlyphTier.Full, string culture = "") =>
            Terminal.CreateContext(TestTerminal.Profile(width, glyphs: tier, colors: ColorDepth.TrueColor, culture: CultureInfo.GetCultureInfo(culture)));

        private static void HasTone(CellBuffer frame, string text, Style style, int skip = 0, bool bold = false) {
            string[] rows = frame.ToPlainText().Split('\n');
            bool found = false;
            for (int y = 0; y < rows.Length; y++) {
                int start = 0;
                while ((start = rows[y].IndexOf(text, start, StringComparison.Ordinal)) >= 0) {
                    found = true;
                    for (int x = start + skip; x < start + text.Length; x++) {
                        if (char.IsWhiteSpace(rows[y][x])) continue;
                        Assert.Equal(style.Color, frame[x, y].Style.Color);
                        if (bold) { Assert.True((frame[x, y].Style.Decoration & TextDecoration.Bold) != 0); }
                    }
                    start += text.Length;
                }
            }
            Assert.True(found, "Missing rendered fact: " + text);
        }

        [Theory]
        [InlineData(GlyphTier.Full, "✓ 1,203 passed · ✗ 0 failed · ▲ 2 warnings", "✗ 0 failed", "▲ 2 warnings")]
        [InlineData(GlyphTier.Basic, "√ 1,203 passed · × 0 failed · ▲ 2 warnings", "× 0 failed", "▲ 2 warnings")]
        [InlineData(GlyphTier.Ascii, "+ 1,203 passed | x 0 failed | ! 2 warnings", "x 0 failed", "! 2 warnings")]
        public void SummaryBar_CountersUseSemanticSeparatorsAndMuteZero(GlyphTier tier, string expected, string zero, string warning) {
            var bar = new SummaryBar().SetCounter("passed", 1203, StatusKind.Pass)
                .SetCounter("failed", 0, StatusKind.Fail).SetCounter("warnings", 2, StatusKind.Warn);
            var context = Context(100, tier);
            CellBuffer frame = FrameComposer.RenderBlock(bar, 100, context);
            Assert.Equal(expected, frame.ToPlainText().TrimStart());
            HasTone(frame, zero, context.Style(ThemeToken.Muted));
            HasTone(frame, warning, context.Style(ThemeToken.Warn), skip: 2);
            HasTone(frame, "1,203", context.Style(ThemeToken.Pass), bold: true);
        }

        [Theory]
        [InlineData(80, "✓ 1", "✗ 0")]
        [InlineData(120, "1 passed", "0 failed")]
        public void RunProgress_CounterFactsSeparateSymbolsAndMuteZero(int width, string passed, string failed) {
            var tracker = new RunTracker(1, () => TimeSpan.Zero);
            tracker.Finished(0, StatusKind.Pass, "Backup");
            var context = Context(width);
            CellBuffer frame = FrameComposer.RenderBlock(new RunProgress(tracker.Snapshot()), width, context);
            Assert.Contains(" · " + passed + " · " + failed + " · ", frame.ToPlainText());
            Assert.Contains("ETA", frame.ToPlainText());
            HasTone(frame, failed, context.Style(ThemeToken.Muted));
            HasTone(frame, passed, context.Style(ThemeToken.Pass), skip: width < 100 ? 2 : 0);
            if (width < 100) { HasTone(frame, "✗ 0", context.Style(ThemeToken.Muted), skip: 2, bold: true); }
        }

        [Theory]
        [InlineData(3)]
        [InlineData(20)]
        public void RunDashboard_HeaderAndGroupCountersMuteZeroAndSeparateSymbols(int height) {
            var tracker = new RunTracker(new[] { new RunItem("Backup", group: "Storage") }, () => TimeSpan.Zero);
            tracker.FinishItem(0, StatusKind.Pass);
            var context = Context(120);
            CellBuffer frame = FrameComposer.Compose(new RunDashboard(tracker), 120, height, context);
            string text = frame.ToPlainText();
            if (height == 3) {
                Assert.Contains("1/1 · 0 fail · 0 warn", text);
                HasTone(frame, "0 fail", context.Style(ThemeToken.Muted));
            } else {
                Assert.Contains("✓ 1 passed · ✗ 0 failed", text);
                Assert.Contains("1/1 · ✓ 1 · ✗ 0 · ▲ 0", text);
                HasTone(frame, "✗ 0", context.Style(ThemeToken.Muted));
                HasTone(frame, "▲ 0", context.Style(ThemeToken.Muted));
            }
        }

        [Theory]
        [InlineData(false, "en-US", "93,088")]
        [InlineData(true, "en-US", "93,088")]
        [InlineData(false, "pl-PL", "93 088")]
        [InlineData(true, "pl-PL", "93 088")]
        public void Lanes_ClassicAndProgressResultsSeparateSymbolsFromCounts(bool columns, string culture, string passed) {
            var lanes = new Lanes();
            lanes.Add(new Lane("worker", "Worker") { Total = 93090, Value = 93090, Passed = 93088, Failed = 0, Warned = 2, Status = StatusKind.Warn });
            if (columns) { lanes.Columns = new List<ProgressColumn> { ProgressColumns.Description(), ProgressColumns.Count(), ProgressColumns.Results() }; }
            var context = Context(100, culture: culture);
            CellBuffer frame = FrameComposer.RenderBlock(lanes, 100, context);
            string zero = columns ? "✗ 0" : "✗      0";
            string warning = columns ? "▲ 2" : "▲      2";
            Assert.Contains("✓ " + passed + " " + zero + " " + warning, frame.ToPlainText());
            HasTone(frame, zero, context.Style(ThemeToken.Muted));
            HasTone(frame, warning, context.Style(ThemeToken.Warn));
            if (columns) { Assert.Contains(culture == "pl-PL" ? "93 090/93 090" : "93,090/93,090", frame.ToPlainText()); }
        }

        [Theory]
        [InlineData(GlyphTier.Full, 40, "✗ Fail · ▲ Warn")]
        [InlineData(GlyphTier.Ascii, 40, "x Fail | ! Warn")]
        [InlineData(GlyphTier.Full, 12, " ✗ Fail\n ▲ Warn")]
        public void Calendar_LegendFlowsAtSemanticSeparatorsWithMutedTone(GlyphTier tier, int width, string expected) {
            var calendar = new Calendar(2026, 10, new[] {
                new CalendarMark(new DateTime(2026, 10, 2), StatusKind.Fail, "Backup"),
                new CalendarMark(new DateTime(2026, 10, 3), StatusKind.Warn, "Expiry")
            });
            var context = Context(width, tier);
            CellBuffer frame = FrameComposer.RenderBlock(calendar, width, context);
            Assert.EndsWith(expected, frame.ToPlainText());
            HasTone(frame, tier == GlyphTier.Ascii ? "x Fail" : "✗ Fail", context.Style(ThemeToken.Muted));
        }
    }
}
