using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class LogRegionMarkupTests {
        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("\r")]
        public void DirectWrite_SpanningMarkupKeepsLinesStylesAndRenderTimeTheme(string newline) {
            string markup = "[bold]first[[literal]]" + newline + newline + "second[/] third" + newline;
            LogRegion log = new LogRegion { ShowTime = false };
            TimeSpan time = TimeSpan.FromSeconds(3);
            log.Write(RichText.FromMarkup(markup, new RichStyle(RichColor.FromRole(ThemeToken.Pass))), "warning", time);

            Assert.Equal(new[] { "first[literal]", "", "second third", "" }, log.Entries.Select(e => e.Text));
            Assert.Equal(4, log.TotalLines);
            Assert.All(log.Entries, e => { Assert.Equal("warn", e.Level); Assert.Equal(time, e.Time); });
            foreach (ITheme theme in new ITheme[] { Theme.Dark, Theme.Light }) {
                foreach (ColorDepth depth in new[] { ColorDepth.None, ColorDepth.Ansi16, ColorDepth.TrueColor }) {
                    RenderContext context = new RenderContext(TerminalProfile.Fixed(60, 10, depth), theme);
                    StyledText expected = context.Markup("[pass]" + markup + "[/]");
                    LogEntry[] entries = log.Entries.ToArray();
                    for (int i = 0; i < entries.Length; i++) {
                        StyledLine actual = entries[i].RichText!.ToStyledText(context).Lines[entries[i].LineIndex];
                        Assert.Equal(expected.Lines[i].ToArray(), actual.ToArray());
                    }
                    CellBuffer frame = new CellBuffer(60, 4);
                    log.Render(new Region(frame), context);
                    Assert.Equal(context.Style(ThemeToken.Pass).Foreground, frame[10, 0].Style.Foreground);
                    Assert.Equal(context.Style(ThemeToken.Pass).Foreground, frame[10, 2].Style.Foreground);
                    Assert.Equal(depth != ColorDepth.None, (frame[10, 0].Style.Decoration & TextDecoration.Bold) != 0);
                    Assert.Equal(depth != ColorDepth.None, (frame[10, 2].Style.Decoration & TextDecoration.Bold) != 0);
                }
            }
        }

        [Fact]
        public void DirectWrite_NestedLinksAndLiteralSegmentsKeepTheirMeaning() {
            LogRegion log = new LogRegion { ShowTime = false };
            log.Write(RichText.Concat(RichText.Plain("[literal] "),
                RichText.FromMarkup("[link=https://example.com/report][pass]first\nsecond[/][/]")));
            Assert.Equal(new[] { "[literal] first", "second" }, log.Entries.Select(e => e.Text));
            RenderContext context = new RenderContext(TerminalProfile.Fixed(60, 10), Theme.Dark);
            foreach (LogEntry entry in log.Entries) {
                Assert.Contains(entry.RichText!.ToStyledText(context).Lines[entry.LineIndex], span =>
                    span.Style.Link == "https://example.com/report");
            }
        }

        [Fact]
        public void EntryTransfer_KeepsSelectedLineAndExpandsCallerCreatedMessages() {
            LogRegion source = new LogRegion { ShowTime = false };
            source.Write(RichText.FromMarkup("[pass]first\nsecond[/]"), "warn", TimeSpan.FromSeconds(3));
            LogRegion all = new LogRegion { ShowTime = false };
            foreach (LogEntry entry in source.Entries) { all.Write(entry); }
            Assert.Equal(new[] { "first", "second" }, all.Entries.Select(entry => entry.Text));
            LogRegion selected = new LogRegion { ShowTime = false };
            selected.Write(source.Entries.Last());
            Assert.Equal("second", Assert.Single(selected.Entries).Text);
            Assert.Equal(1, selected.Entries.Single().LineIndex);
            Assert.Equal(TimeSpan.FromSeconds(3), selected.Entries.Single().Time);
            Assert.Equal("warn", selected.Entries.Single().Level);
            string rendered = Terminal.RenderText(selected, TerminalProfile.Fixed(60, 6, ColorDepth.None), Theme.Dark);
            Assert.Contains("second", rendered);
            Assert.DoesNotContain("first", rendered);
            all.Write(new LogEntry(TimeSpan.Zero, "info", RichText.FromMarkup("[pass]third\nfourth[/]")));
            Assert.Equal(new[] { "first", "second", "third", "fourth" }, all.Entries.Select(entry => entry.Text));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BareLinkAcrossLines_KeepsWholeSourceTargetWithoutInventingClickableLinks(bool throughStream) {
            string markup = "[link]https://example.com/a\nhttps://example.com/b[/]";
            RichText source = RichText.FromMarkup(markup);
            LogRegion log = new LogRegion { ShowTime = false };
            if (throughStream) {
                using (LiveSession session = new LiveSession(log, LiveSessionTests.Options(new FakeOutput(false)))) {
                    session.Start();
                    session.Post(new LiveEvent("log", "write", text: markup, isMarkup: true));
                    session.Stop();
                    Assert.Equal(0, session.ApplyErrors);
                }
            } else { log.Write(source); }
            TerminalProfile profile = TerminalProfile.Fixed(60, 6).WithHyperlinks(true);
            RenderContext context = new RenderContext(profile, Theme.Dark);
            StyledText expected = source.ToStyledText(context);
            Assert.Equal(new[] { 0, 1 }, log.Entries.Select(entry => entry.LineIndex));
            foreach (LogEntry entry in log.Entries) {
                Assert.Equal(expected.Lines[entry.LineIndex].ToArray(),
                    entry.RichText!.ToStyledText(context).Lines[entry.LineIndex].ToArray());
            }
            Assert.DoesNotContain("\u001b]8;", Terminal.RenderEncoded(source, profile, Theme.Dark));
            Assert.DoesNotContain("\u001b]8;", Terminal.RenderEncoded(log, profile, Theme.Dark));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DirectWrite_InvalidRichMarkupIsLiteralAndLaterWritesContinue(bool strict) {
            bool previous = Markup.Strict;
            try {
                Markup.Strict = strict;
                LogRegion log = new LogRegion { ShowTime = false };
                log.Write(new RichText(new[] { new RichSegment("[unknown]first\nsecond[/]", isMarkup: true) }));
                log.Write("later");
                Assert.Equal(new[] { "[unknown]first", "second[/]", "later" }, log.Entries.Select(e => e.Text));
                string rendered = Terminal.RenderText(log, TerminalProfile.Fixed(60, 6, ColorDepth.None), Theme.Dark);
                Assert.Contains("[unknown]first", rendered);
                Assert.Contains("second[/]", rendered);
                Assert.Contains("later", rendered);
            } finally { Markup.Strict = previous; }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void StreamPath_SpanningMarkupAndMalformedMarkupKeepExactLines(bool strict) {
            bool previous = Markup.Strict;
            try {
                Markup.Strict = strict;
                LogRegion log = new LogRegion { ShowTime = false };
                FakeOutput output = new FakeOutput(false);
                using (LiveSession session = new LiveSession(log, LiveSessionTests.Options(output))) {
                    session.Start();
                    session.Post(new LiveEvent("log", "write", text: "[pass]first\nsecond[/]", isMarkup: true));
                    session.Post(new LiveEvent("log", "write", text: "[unknown]bad\nraw[/]", isMarkup: true));
                    session.Post(new LiveEvent("log", "write", text: "[pass]unclosed\nraw", isMarkup: true));
                    log.Handle(session).Write("later");
                    session.Stop();
                    string[] expected = { "first", "second", "[unknown]bad", "raw[/]", "[pass]unclosed", "raw", "later" };
                    Assert.Equal(expected, log.Entries.Select(e => e.Text));
                    Assert.Equal(expected, output.Lines);
                    Assert.Equal(4, session.EventsApplied);
                    Assert.Equal(0, session.EventsDropped);
                    Assert.Equal(0, session.ApplyErrors);
                    Assert.Null(session.LastError);
                }
            } finally { Markup.Strict = previous; }
        }
    }
}
