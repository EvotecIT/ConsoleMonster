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
        // Capacity bounds the number of entries; one runaway line (a minified file, a dumped object) must not hold
        // megabytes in every retained entry.
        [Fact]
        public void LiteralEntries_KeepAtMostMaxLineLengthCharacters() {
            LogRegion log = new LogRegion();
            log.Write(new string('x', LogRegion.MaxLineLength * 4) + "\nshort");
            Assert.Equal(new[] { LogRegion.MaxLineLength, 5 }, log.Entries.Select(e => e.Text.Length));
            Assert.EndsWith("\u2026", log.Entries.First().Text);
            Assert.Equal("short", log.Entries.Last().Text);
        }

        // Rich entries share one source: neither a runaway line nor lines pushed straight out of Capacity (their text, line
        // breaks or tags) may stay in it.
        [Fact]
        public void RichEntries_RetainABoundedSourceAndKeepTheirStyles() {
            const int pushedOut = 100000;
            LogRegion log = new LogRegion { Capacity = 2, ShowElapsed = false };
            string many = string.Concat(Enumerable.Repeat("[pass]dropped[/]\n", pushedOut));
            log.Write(RichText.FromMarkup("[bold]" + many + "dropped " + new string('d', LogRegion.MaxLineLength * 4) + "\n[pass]" + new string('x', LogRegion.MaxLineLength * 4) + "[/]\nshort[/]"));

            Assert.Equal(new[] { LogRegion.MaxLineLength, 5 }, log.Entries.Select(e => e.Text.Length));
            Assert.EndsWith("…", log.Entries.First().Text);
            Assert.True(log.Entries.First().RichText!.ToMarkup().Length < LogRegion.MaxLineLength + 100);
            Assert.Equal(new[] { 0, 1 }, log.Entries.Select(e => e.LineIndex));
            Assert.Equal(pushedOut + 3, log.TotalLines);
            RenderContext context = new RenderContext(TerminalProfile.Fixed(40, 2, ColorDepth.TrueColor), Theme.Dark);
            CellBuffer frame = new CellBuffer(40, 2);
            log.Render(new Region(frame), context);
            Assert.Equal(context.Style(ThemeToken.Pass).Color, frame[0, 0].Style.Color);
            Assert.True((frame[0, 0].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.True((frame[0, 1].Style.Decoration & TextDecoration.Bold) != 0);
            Assert.Equal("short", frame.GetRowText(1).Trim());
        }

        // Lines a later write evicts leave the source their write shared, so a kept entry no longer exposes them.
        [Fact]
        public void RichEntries_DropLinesEvictedByLaterWritesFromTheirSource() {
            LogRegion log = new LogRegion { Capacity = 2, ShowElapsed = false };
            log.Write(RichText.FromMarkup("[pass]secret\nkept[/]"));
            log.Write("new");

            LogEntry kept = log.Entries.First();
            Assert.DoesNotContain("secret", kept.RichText!.ToMarkup());
            Assert.Equal(0, kept.LineIndex);
            RenderContext context = new RenderContext(TerminalProfile.Fixed(40, 2, ColorDepth.TrueColor), Theme.Dark);
            CellBuffer frame = new CellBuffer(40, 2);
            log.Render(new Region(frame), context);
            Assert.Equal("kept", frame.GetRowText(0).Trim());
            Assert.Equal(context.Style(ThemeToken.Pass).Color, frame[0, 0].Style.Color);
        }

        // A bare [link] takes its target from all the text it encloses; cutting lines from it must not turn the kept text
        // into a new target.
        [Fact]
        public void RichEntries_CutThroughABareLinkKeepItsOriginalTarget() {
            LogRegion log = new LogRegion { Capacity = 1, ShowElapsed = false };
            log.Write(RichText.FromMarkup("[bold link]https://example.com/a\nhttps://example.com/b[/]"));

            RenderContext context = new RenderContext(TerminalProfile.Fixed(40, 1, ColorDepth.TrueColor), Theme.Dark);
            CellBuffer frame = new CellBuffer(40, 1);
            log.Render(new Region(frame), context);
            Assert.Equal("https://example.com/b", frame.GetRowText(0).Trim());
            Assert.Null(frame[0, 0].Style.Link);
            Assert.True((frame[0, 0].Style.Decoration & TextDecoration.Bold) != 0);
        }
        [Theory]
        [InlineData("warn", "[WARN]", "WARN")]
        [InlineData("err", "[FAIL]", "ERR")]
        [InlineData("pass", "[OK]", "OK")]
        [InlineData("skip", "[SKIP]", "SKIP")]
        public void WordsMarkersReplaceTheFollowingLogLevel(string level, string marker, string plain) {
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Basic, GlyphTier.Ascii }) {
                var log = new LogRegion { ShowElapsed = false };
                log.Write("record", level);
                var profile = TerminalProfile.Fixed(40, 4, ColorDepth.None, tier).WithStatusMarkers(StatusMarkerStyle.Words);
                string text = Terminal.RenderText(log, profile, Theme.Dark).Trim();
                Assert.Equal((tier == GlyphTier.Ascii ? plain : marker) + " record", text);
            }
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("\r")]
        public void DirectWrite_SpanningMarkupKeepsLinesStylesAndRenderTimeTheme(string newline) {
            string markup = "[bold]first[[literal]]" + newline + newline + "second[/] third" + newline;
            LogRegion log = new LogRegion { ShowElapsed = false };
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
                    Assert.Equal(context.Style(ThemeToken.Pass).Color, frame[10, 0].Style.Color);
                    Assert.Equal(context.Style(ThemeToken.Pass).Color, frame[10, 2].Style.Color);
                    Assert.Equal(depth != ColorDepth.None, (frame[10, 0].Style.Decoration & TextDecoration.Bold) != 0);
                    Assert.Equal(depth != ColorDepth.None, (frame[10, 2].Style.Decoration & TextDecoration.Bold) != 0);
                }
            }
        }

        [Fact]
        public void DirectWrite_NestedLinksAndLiteralSegmentsKeepTheirMeaning() {
            LogRegion log = new LogRegion { ShowElapsed = false };
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
            LogRegion source = new LogRegion { ShowElapsed = false };
            source.Write(RichText.FromMarkup("[pass]first\nsecond[/]"), "warn", TimeSpan.FromSeconds(3));
            LogRegion all = new LogRegion { ShowElapsed = false };
            foreach (LogEntry entry in source.Entries) { all.Write(entry); }
            Assert.Equal(new[] { "first", "second" }, all.Entries.Select(entry => entry.Text));
            LogRegion selected = new LogRegion { ShowElapsed = false };
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
            LogRegion log = new LogRegion { ShowElapsed = false };
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
                LogRegion log = new LogRegion { ShowElapsed = false };
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
                LogRegion log = new LogRegion { ShowElapsed = false };
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
