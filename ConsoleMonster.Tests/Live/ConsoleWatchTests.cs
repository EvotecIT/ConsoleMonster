using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Watching a command: runs, printed output without a console, errors that do not stop the watch, changed cells.</summary>
    public sealed class ConsoleWatchTests {
        private static string Text(IWidget widget, int width = 60) {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(width, 20, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Size size = widget.Measure(new Constraints(width, 20), context);
            CellBuffer buffer = new CellBuffer(width, Math.Max(1, size.Height));
            widget.Render(new Region(buffer), context);
            return buffer.ToPlainText();
        }

        [Fact]
        public void Run_WithoutAConsole_PrintsEveryRunUpToTheCount() {
            List<string> printed = new List<string>();
            int runs = ConsoleWatch.Run(i => new TextBlock("value " + i), new WatchOptions {
                Count = 3,
                Interactive = false,
                Title = "demo",
                Print = widget => printed.Add(Text(widget)),
                Delay = (interval, token) => { }
            });
            Assert.Equal(3, runs);
            Assert.Equal(3, printed.Count);
            Assert.Contains("#2", printed[1]);
            Assert.Contains("demo", printed[1]);
            Assert.Contains("value 3", printed[2]);
        }

        [Fact]
        public void Run_AFailedRun_ShowsItsErrorAndTheWatchGoesOn() {
            List<string> printed = new List<string>();
            ConsoleWatch.Run(i => i == 1 ? throw new InvalidOperationException("not yet") : new TextBlock("ok now"), new WatchOptions {
                Count = 2,
                Interactive = false,
                Print = widget => printed.Add(Text(widget)),
                Delay = (interval, token) => { }
            });
            Assert.Contains("not yet", printed[0]);
            Assert.Contains("InvalidOperationException", printed[0]);
            Assert.Contains("ok now", printed[1]);
        }

        [Fact]
        public void Run_Cancelled_StopsBetweenRuns() {
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                int runs = ConsoleWatch.Run(i => {
                    if (i == 2) {
                        stop.Cancel();
                    }
                    return new TextBlock("run " + i);
                }, new WatchOptions { Interactive = false, Print = widget => { }, Delay = (interval, token) => { } }, stop.Token);
                Assert.Equal(2, runs);
            }
        }

        [Fact]
        public void Options_IntervalIsAtLeastFiftyMilliseconds() {
            Assert.Equal(WatchOptions.MinimumInterval, new WatchOptions { Interval = TimeSpan.Zero }.Interval);
            Assert.Equal(WatchOptions.MinimumInterval, new WatchOptions { Interval = TimeSpan.FromSeconds(-1) }.Interval);
        }

        [Fact]
        public void Run_AFrameWithAStatus_ShowsItInTheHeading() {
            List<string> printed = new List<string>();
            ConsoleWatch.Run(i => new WatchFrame(new TextBlock("partial"), "2 errors"), new WatchOptions { Count = 1, Interactive = false, Print = widget => printed.Add(Text(widget)) });
            Assert.Contains("2 errors", printed[0]);
            Assert.Contains("partial", printed[0]);
        }

        private static CellBuffer RenderTwice(WatchView view, RenderContext context, string before, string after) {
            view.Update(new TextBlock(before), 1);
            view.Render(new Region(new CellBuffer(30, 2)), context);
            view.Update(new TextBlock(after), 2);
            CellBuffer second = new CellBuffer(30, 2);
            view.Render(new Region(second), context);
            return second;
        }

        [Fact]
        public void WatchView_Differences_TonesWholeChangedValuesOnly() {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(30, 5, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            Color tone = context.Style(ThemeToken.Surface).Background;
            WatchView view = new WatchView { Differences = true };
            // Columns two spaces apart: "count" is unchanged; "42" changed as a whole, not only its "2".
            CellBuffer second = RenderTwice(view, context, "count  41  of  99", "count  42  of  9");
            Assert.NotEqual(tone, second[0, 1].Style.Background);
            Assert.Equal(tone, second[7, 1].Style.Background);
            Assert.Equal(tone, second[8, 1].Style.Background);
            Assert.True((second[7, 1].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.NotEqual(tone, second[11, 1].Style.Background);
            // "99" became "9": the value is toned, the padding it left is not.
            Assert.Equal(tone, second[15, 1].Style.Background);
            Assert.NotEqual(tone, second[16, 1].Style.Background);
            // A changed line of text is one continuous span, spaces and unchanged words included, padding excluded.
            CellBuffer line = RenderTwice(new WatchView { Differences = true }, context, "queue-c is slow", "queue-b is draining");
            for (int x = 0; x < 19; x++) {
                Assert.Equal(tone, line[x, 1].Style.Background);
            }
            Assert.NotEqual(tone, line[19, 1].Style.Background);
            // The next run with the same values clears the tone.
            view.Update(new TextBlock("count  42  of  9"), 3);
            CellBuffer third = new CellBuffer(30, 2);
            view.Render(new Region(third), context);
            Assert.NotEqual(tone, third[7, 1].Style.Background);
        }

        [Fact]
        public void WatchView_Differences_WideCharactersBorderRowsAndCultureDecimals() {
            RenderContext colour = new RenderContext(TerminalProfile.Fixed(30, 5, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            // Wide characters before a space do not break the value scan.
            CellBuffer wide = RenderTwice(new WatchView { Differences = true }, colour, "名前 Alice 1", "名前 Alice 2");
            Assert.Contains("Alice", wide.GetRowText(1));
            // A border row that changes width is layout, not a value.
            RenderContext plain = new RenderContext(TerminalProfile.Fixed(30, 5, ColorDepth.None, GlyphTier.Ascii).WithCulture(System.Globalization.CultureInfo.GetCultureInfo("pl-PL")), Theme.Dark);
            CellBuffer border = RenderTwice(new WatchView { Differences = true }, plain, "+------+", "+--------+");
            Assert.DoesNotContain("*", border.GetRowText(1));
            // pl-PL decimals: 1,5 down to 1,25 is a drop.
            CellBuffer decimals = RenderTwice(new WatchView { Differences = true }, plain, "load  1,5", "load  1,25");
            Assert.Contains("v1,25", decimals.GetRowText(1));
            // A value at the start of the line gets its marker after it.
            CellBuffer first = RenderTwice(new WatchView { Differences = true }, plain, "41  waiting", "48  waiting");
            Assert.StartsWith("48^ waiting", first.GetRowText(1));
            // A line that went away is marked at its start.
            CellBuffer gone = RenderTwice(new WatchView { Differences = true }, plain, "queue-c is slow", string.Empty);
            Assert.StartsWith("*", gone.GetRowText(1));
            // Two numbers one space apart are not one number: no arrow.
            CellBuffer merged = RenderTwice(new WatchView { Differences = true }, plain, "x  12,34 5678", "x  12,30 5679");
            Assert.Contains("*12,30", merged.GetRowText(1));
            // Digit groups are one number: 1 204 up to 1 305.
            CellBuffer groups = RenderTwice(new WatchView { Differences = true }, plain, "x  1 204", "x  1 305");
            Assert.Contains("^1 305", groups.GetRowText(1));
        }

        [Fact]
        public void WatchView_Differences_WithoutColour_MarksValuesBeforeThem() {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(30, 5, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            CellBuffer second = RenderTwice(new WatchView { Differences = true }, context, "left  41  queue  a  40", "left  48  queue  b   4");
            string row = second.GetRowText(1);
            Assert.Contains("↑48", row);
            // "40" became "4": the whole previous value is compared, so it went down.
            Assert.Contains("↓4", row);
            Assert.Contains("*b", row.Replace(" ", string.Empty));
            Assert.StartsWith("left", row);
        }
    }
}
