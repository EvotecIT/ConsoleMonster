using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Progress {
    /// <summary>Progress columns on lanes and progress bars: layout, rates, remaining time and the display modes.</summary>
    public sealed class ProgressColumnTests {
        private static LiveEvent Event(string operation, string key, double value = 0, double total = 0, long ms = 0, string? state = null, string? text = null) {
            return new LiveEvent("lanes", operation, key, text, state, value, total, ms);
        }

        // srv01: started at 0 s, 30 of 60 after 10 s (3/s); srv02: done in 8 s; srv03: queued.
        private static Lanes Sample() {
            Lanes lanes = new Lanes();
            lanes.Apply(Event("add", "srv01", total: 60, text: "SRV01"));
            lanes.Apply(Event("add", "srv02", total: 10, text: "SRV02"));
            lanes.Apply(Event("add", "srv03", total: 10, text: "SRV03"));
            lanes.Apply(Event("state", "srv01", ms: 0, state: "running"));
            lanes.Apply(Event("state", "srv02", ms: 0, state: "running"));
            for (int s = 1; s <= 10; s++) {
                lanes.Apply(Event("advance", "srv01", 3, ms: s * 1000, state: "pass", text: "copying files"));
            }
            lanes.Apply(Event("advance", "srv02", 10, ms: 8000, state: "pass"));
            lanes.Apply(Event("complete", "srv02", ms: 8000));
            return lanes;
        }

        private static string Render(IWidget widget, int width, GlyphTier tier, double seconds = 10) {
            RenderContext context = new RenderContext(TestTerminal.Profile(width, 10, tier), Theme.Dark, null, 0, TimeSpan.FromSeconds(seconds));
            return FrameComposer.RenderBlock(widget, width, context).ToPlainText();
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void Lanes_StandardColumnsWithRate_RenderPerTier(GlyphTier tier) {
            Lanes lanes = Sample();
            lanes.Columns = ProgressColumns.Standard();
            lanes.Columns.Insert(7, ProgressColumns.Rate(unit: "files"));
            Golden.Match("progress.columns." + tier.ToString().ToLowerInvariant() + ".txt", Render(lanes, 110, tier));
        }

        [Fact]
        public void Lanes_RateAndRemaining_UseTheMovingWindow() {
            Lanes lanes = Sample();
            lanes.Columns = new List<ProgressColumn> { ProgressColumns.Description(), ProgressColumns.Rate(), ProgressColumns.Remaining() };
            string text = Render(lanes, 60, GlyphTier.Ascii);
            Assert.Contains("SRV01  3/s  10s left", text);
            // Stalled for 20 s: the window holds no progress, so the rate and the estimate disappear.
            Assert.DoesNotContain("/s", Render(lanes, 60, GlyphTier.Ascii, 30));
        }

        [Fact]
        public void Lanes_NarrowRegion_DropsLowPriorityColumnsFirst() {
            Lanes lanes = Sample();
            lanes.Columns = ProgressColumns.Standard();
            string narrow = Render(lanes, 40, GlyphTier.Ascii);
            Assert.Contains("SRV01", narrow);
            Assert.Contains("50%", narrow);
            Assert.DoesNotContain("copying", narrow);
            Assert.DoesNotContain("8.0s", narrow);
            Assert.DoesNotContain("10s left", Render(lanes, 30, GlyphTier.Ascii));
        }

        [Fact]
        public void Lanes_CustomColumn_RendersFromTheRow() {
            Lanes lanes = Sample();
            lanes.Columns = new List<ProgressColumn> {
                ProgressColumns.Description(),
                ProgressColumns.Custom("Phase", row => row.IsFinished ? "verified" : row.Status == StatusKind.Running ? "copy" : "wait")
            };
            string[] lines = Render(lanes, 40, GlyphTier.Ascii).Split('\n');
            Assert.Equal("SRV01  copy", lines[0].TrimEnd());
            Assert.Equal("SRV02  verified", lines[1].TrimEnd());
            Assert.Equal("SRV03  wait", lines[2].TrimEnd());
        }

        [Fact]
        public void Lanes_HideCompleted_CountsFinishedLanesInTheSummaryLine() {
            Lanes lanes = Sample();
            lanes.HideCompleted = true;
            lanes.Columns = new List<ProgressColumn> { ProgressColumns.Description(), ProgressColumns.Percent() };
            string text = Render(lanes, 40, GlyphTier.Ascii);
            Assert.DoesNotContain("SRV02", text);
            Assert.Contains("1 done", text);
            Assert.Equal(3, text.Split('\n').Length);
        }

        [Fact]
        public void Lanes_CollapseWhenFinished_AndTransient() {
            Lanes lanes = Sample();
            lanes.Apply(Event("complete", "srv01", ms: 12000));
            lanes.Apply(Event("complete", "srv03", ms: 12000, state: "fail"));
            lanes.CollapseWhenFinished = true;
            Assert.Equal("x 3 done in 12s - 1 failed", Render(lanes, 60, GlyphTier.Ascii).TrimEnd());
            lanes.Transient = true;
            Assert.Equal(0, lanes.Measure(new Constraints(60), Terminal.CreateContext(TestTerminal.Profile(60))).Height);
        }

        [Fact]
        public void Lanes_StreamSummary_UsesTheColumns() {
            Lanes lanes = Sample();
            lanes.Columns = new List<ProgressColumn> { ProgressColumns.Description(), ProgressColumns.Percent(), ProgressColumns.Count(), ProgressColumns.Remaining() };
            Assert.Equal("lanes: 1 running, 1 done, 1 queued; SRV01 50% 30/60 10s left", lanes.GetSummaryLine());
        }

        [Fact]
        public void ProgressBar_Columns_ShowRateAndRemaining() {
            ProgressBar bar = new ProgressBar("Copy") { Total = 100, Columns = new List<ProgressColumn> { ProgressColumns.Description(), ProgressColumns.Bar(10), ProgressColumns.Percent(), ProgressColumns.Rate(true), ProgressColumns.Remaining() } };
            bar.Apply(new LiveEvent("progress", "set", value: 0, total: 100, time: 0));
            bar.Apply(new LiveEvent("progress", "set", value: 40, total: 100, time: 4000));
            string text = Render(bar, 60, GlyphTier.Ascii, 4);
            Assert.StartsWith("Copy  ", text);
            Assert.Contains("40%  10 B/s  6s left", text);
            Assert.Equal("Copy 40% 10 B/s 6s left", bar.GetSummaryLine());
        }

        [Fact]
        public void Lanes_ClassicLayoutIsUnchangedWithoutColumns() {
            Lanes lanes = Sample();
            string text = Render(lanes, 100, GlyphTier.Ascii);
            Assert.Contains("copying files", text);
            Assert.Contains("done in 8.0s", text);
            Assert.Contains("queued", text);
        }
    }
}
