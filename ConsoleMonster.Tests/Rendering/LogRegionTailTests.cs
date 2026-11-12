using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using ConsoleMonster.Input;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    public sealed class LogRegionTailTests {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ScrollOffsetSelectsEarlierEntriesAndClampsAtOldest(bool wrap) {
            LogRegion log = new LogRegion { ShowElapsed = false, Wrap = wrap };
            for (int i = 1; i <= 5; i++) { log.Write("L" + i); }
            log.ScrollOffset = 1;
            Assert.Equal("L3\nL4", Render(log).ToPlainText());
            log.ScrollOffset = int.MaxValue;
            Assert.Equal("L1\nL2", Render(log).ToPlainText());
            log.ScrollOffset = -1;
            Assert.Equal("L4\nL5", Render(log).ToPlainText());
        }

        // Rich lines a write pushes straight out of Capacity were still received, so a paused text filter counts them.
        [Fact]
        public void PausedTextFilterCountsRichLinesPushedOutOfCapacity() {
            LogRegion log = new LogRegion { Capacity = 1 };
            LogTail tail = new LogTail(log) { Filter = new LogFilter(text: "match") };
            tail.Pause();
            log.Write(RichText.FromMarkup("[bold]match 1\nmatch 2\nother[/]"));
            Assert.Equal(2, tail.NewLines);
        }

        [Fact]
        public void SourceTimestampAndLevelWidthsStayAlignedWhenScrollingToUnknownInfoEntries() {
            LogRegion log = new LogRegion { ShowTimestamp = true };
            DateTimeOffset timestamp = new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
            log.WriteTimestamped("first", "warn", timestamp);
            log.WriteTimestamped("second", "info", (DateTimeOffset?)null);
            CellBuffer first = Render(log);
            int column = first.GetRowText(0).IndexOf("first", StringComparison.Ordinal);
            Assert.Equal(column, first.GetRowText(1).IndexOf("second", StringComparison.Ordinal));
            log.WriteTimestamped("third", "info", (DateTimeOffset?)null);
            CellBuffer second = Render(log);
            Assert.Equal(column, second.GetRowText(0).IndexOf("second", StringComparison.Ordinal));
            Assert.Equal(column, second.GetRowText(1).IndexOf("third", StringComparison.Ordinal));
            Assert.StartsWith(new string(' ', column), second.GetRowText(0));
        }

        [Fact]
        public void TimestampedLiteralEntriesUseExistingCapacityAndMultilineNormalization() {
            LogRegion log = new LogRegion { Capacity = 2, ShowTimestamp = true };
            log.WriteTimestamped("discard\n[red]literal[/]\r\nnext", "error", (DateTimeOffset?)null);
            Assert.Equal(3, log.TotalLines);
            Assert.Equal(2, log.Entries.Count);
            string text = Render(log).ToPlainText();
            Assert.Contains("[red]literal[/]", text);
            Assert.DoesNotContain("00:00", text);
        }

        /// <summary>Transferring a source entry retains both clocks while expanding its physical lines.</summary>
        [Fact]
        public void EntryTransferPreservesSourceTimestampAndSessionTime() {
            DateTimeOffset timestamp = new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
            TimeSpan elapsed = TimeSpan.FromSeconds(7);
            LogRegion log = new LogRegion { ShowTimestamp = true };
            log.Write(new LogEntry(elapsed, "warning", "first\r\nsecond", timestamp));

            Assert.Equal(2, log.TotalLines);
            Assert.Collection(log.Entries,
                entry => Assert.Equal("first", entry.Text),
                entry => Assert.Equal("second", entry.Text));
            Assert.All(log.Entries, entry => {
                Assert.Equal(timestamp, entry.Timestamp);
                Assert.Equal(elapsed, entry.Time);
                Assert.Equal("warn", entry.Level);
            });
        }

        /// <summary>Tail highlighting leaves each styled source slice intact in both log layouts.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void HighlightingPreservesRichMultilineSlices(bool wrap) {
            LogRegion log = new LogRegion { ShowElapsed = false, Wrap = wrap, HighlightText = "first" };
            log.Write(RichText.FromMarkup("[pass]first\nsecond[/]"));
            RenderContext context = new RenderContext(TerminalProfile.Fixed(120, 2, ColorDepth.TrueColor, GlyphTier.Ascii), Theme.Dark);
            CellBuffer frame = new CellBuffer(120, 2);
            log.Render(new Region(frame), context);

            Assert.Equal("first\nsecond", frame.ToPlainText());
            Assert.Equal(context.Style(ThemeToken.Pass).Color, frame[0, 0].Style.Color);
            Assert.Equal(context.Style(ThemeToken.Pass).Color, frame[0, 1].Style.Color);
        }

        [Fact]
        public void WrappedOldestPageKeepsSeverityAndSourceColumns() {
            LogRegion log = new LogRegion { ShowElapsed = false, Wrap = true, ScrollOffset = int.MaxValue };
            log.Write("L1");
            log.WriteLiteral("L2", "warn", TimeSpan.Zero, null, "db");
            for (int i = 3; i <= 5; i++) { log.Write("L" + i); }
            string text = Render(log).ToPlainText();
            Assert.Contains("WARN", text);
            Assert.Contains("db", text);
            Assert.Contains("L1", text);
            Assert.Contains("L2", text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FindStylesSourceTextBeforeAsciiFallback(bool wrap) {
            LogRegion log = new LogRegion { ShowElapsed = false, Wrap = wrap };
            log.Write(RichText.FromMarkup("[pass]✅ needle[/]"));
            LogFind find = new LogFind(new LogTail(log) { VisibleRows = 2 });
            RenderContext context = new RenderContext(TerminalProfile.Fixed(30, 2, ColorDepth.TrueColor, GlyphTier.Ascii), Theme.Dark);
            CellBuffer frame = new CellBuffer(30, 2);
            find.Render(new Region(frame), context);
            find.Handle(InputEvent.Character('/'));
            foreach (char key in "needle") { find.Handle(InputEvent.Character(key)); }
            find.Render(new Region(frame), context);
            Assert.StartsWith("[OK] needle", frame.GetRowText(0));
            for (int x = 5; x < 11; x++) { Assert.True((frame[x, 0].Style.Decoration & TextDecoration.Reverse) != 0); }
            Assert.False((frame[0, 0].Style.Decoration & TextDecoration.Reverse) != 0);
        }

        [Fact]
        public void WrappedFindUsesTheLastRequestedMatchBeforeAnotherRender() {
            LogRegion log = new LogRegion { ShowElapsed = false, Wrap = true };
            for (int i = 0; i < 5; i++) { log.Write((i == 1 || i == 4 ? "needle " : "entry ") + i + " with enough words to wrap twice"); }
            LogFind find = new LogFind(new LogTail(log) { VisibleRows = 4 });
            RenderContext context = new RenderContext(TerminalProfile.Fixed(30, 4, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            CellBuffer frame = new CellBuffer(30, 4);
            find.Render(new Region(frame), context);
            find.Handle(InputEvent.Character('/'));
            foreach (char key in "needle") { find.Handle(InputEvent.Character(key)); }
            find.Handle(InputEvent.Press(ConsoleKey.Enter));
            find.Render(new Region(frame), context);
            find.Handle(InputEvent.Press(ConsoleKey.Enter));
            find.Handle(InputEvent.Press(ConsoleKey.Enter));
            frame.Clear();
            find.Render(new Region(frame), context);
            Assert.Contains("needle 4", frame.ToPlainText());
            Assert.DoesNotContain("needle 1", frame.ToPlainText());
        }

        private static CellBuffer Render(LogRegion log) {
            CellBuffer frame = new CellBuffer(120, 2);
            log.Render(new Region(frame), new RenderContext(TerminalProfile.Fixed(120, 2, ColorDepth.None, GlyphTier.Ascii), Theme.Dark));
            return frame;
        }
    }
}
