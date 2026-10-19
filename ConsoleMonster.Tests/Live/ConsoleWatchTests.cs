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

        [Fact]
        public void WatchView_Differences_HighlightsOnlyChangedCells() {
            WatchView view = new WatchView { Differences = true };
            RenderContext context = new RenderContext(TerminalProfile.Fixed(30, 5, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            view.Update(new TextBlock("count 41 of 99"), 1);
            CellBuffer first = new CellBuffer(30, 2);
            view.Render(new Region(first), context);
            view.Update(new TextBlock("count 42 of 9"), 2);
            CellBuffer second = new CellBuffer(30, 2);
            view.Render(new Region(second), context);
            // Row 1 holds the content: "count 4" is unchanged, the "2" changed.
            Assert.Equal(TextDecoration.None, second[0, 1].Style.Decoration & TextDecoration.Reverse);
            Assert.Equal(TextDecoration.Reverse, second[7, 1].Style.Decoration & TextDecoration.Reverse);
            Assert.Equal(TextDecoration.None, second[9, 1].Style.Decoration & TextDecoration.Reverse);
            // The last "9" disappeared: the blank cell is marked too.
            Assert.Equal(TextDecoration.Reverse, second[13, 1].Style.Decoration & TextDecoration.Reverse);
        }
    }
}
