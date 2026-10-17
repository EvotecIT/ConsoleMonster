using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.TestRuns;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    /// <summary>
    /// The core APIs behind Write-ConsoleText, Write-ConsoleStatus, host colour mapping, Format-ConsoleCode and
    /// Invoke-ConsolePester, so C# hosts get the same behaviour as the cmdlets.
    /// </summary>
    public sealed class ConsoleTextCoreTests {
        private static RenderContext Context(int width = 60) => Terminal.CreateContext(TerminalProfile.Fixed(width, 20, ColorDepth.TrueColor), Theme.Dark);

        private static string[] Plain(System.Collections.Generic.IReadOnlyList<StyledLine> lines) => lines.Select(l => l.PlainText).ToArray();

        [Fact]
        public void Layout_PrefixTimeSplitAndPadding_FollowWriteColor() {
            ConsoleTextLayout layout = new ConsoleTextLayout {
                StartTab = 1,
                Time = new DateTime(2026, 10, 17, 14, 3, 0),
                TimeFormat = "HH:mm",
                SplitLines = "|",
                LinesBefore = 1
            };
            string[] lines = Plain(layout.Lines(RichText.Plain("[info] first|second"), Context(), 60));
            Assert.Equal(new[] { string.Empty, "        [14:03] [info] first", "                second" }, lines);
            ConsoleTextLayout padded = new ConsoleTextLayout { PadCenter = 9, PadCharacter = '.' };
            Assert.Equal("...abc...", Plain(padded.Lines(RichText.Plain("abc"), Context(), 60))[0]);
        }

        [Fact]
        public void Layout_WrapContinuesUnderTheTextAndCentringUsesCells() {
            ConsoleTextLayout layout = new ConsoleTextLayout { StartSpaces = 4, Wrap = true };
            string[] lines = Plain(layout.Lines(RichText.Plain("alpha beta gamma delta"), Context(), 16));
            Assert.Equal("    alpha beta", lines[0].TrimEnd());
            Assert.StartsWith("    gamma", lines[1]);
            ConsoleTextLayout centred = new ConsoleTextLayout { HorizontalCenter = true };
            Assert.Equal("  中文", Plain(centred.Lines(RichText.Plain("中文"), Context(), 8))[0]);
        }

        [Fact]
        public void RichText_FromObjectPieces_CyclesColoursAndKeepsRichPieces() {
            RichText rich = RichText.Plain("own", new RichStyle(RichColor.FromRole(ThemeToken.Fail), RichColor.Default));
            RichText text = RichText.FromPieces(new object?[] { "a", rich, 3 }, new[] { RichColor.FromRole(ThemeToken.Pass) }, null, default);
            Assert.Equal("aown3", text.PlainText);
            Assert.Equal(RichColor.FromRole(ThemeToken.Pass), text.Segments[0].Style.Foreground);
            Assert.Equal(RichColor.FromRole(ThemeToken.Fail), text.Segments[1].Style.Foreground);
            Assert.Equal(RichColor.FromRole(ThemeToken.Pass), text.Segments[2].Style.Foreground);
            Assert.Single(text.Split("q"));
            Assert.Equal(new[] { "one", "two" }, RichText.Plain("one, two").Split(", ").Select(p => p.PlainText).ToArray());
        }

        [Fact]
        public void LogFile_FormatsTextCsvAndJsonLines_AndAppends() {
            ConsoleLogRecord record = new ConsoleLogRecord(new DateTime(2026, 10, 17, 14, 3, 0), "first\nsecond, \"quoted\"", "Info", "Deploy");
            ConsoleLogFile text = new ConsoleLogFile("x.log");
            Assert.Equal("[2026-10-17 14:03:00] [Info] [Deploy] first" + Environment.NewLine + "                                      second, \"quoted\"" + Environment.NewLine, text.FormatRecord(record, true));
            ConsoleLogFile csv = new ConsoleLogFile("x.csv") { Format = ConsoleLogFormat.Csv, IncludeTime = false };
            Assert.Equal("Time,Level,Category,Message" + Environment.NewLine + ",Info,Deploy,\"first\nsecond, \"\"quoted\"\"\"" + Environment.NewLine, csv.FormatRecord(record, true));
            ConsoleLogFile json = new ConsoleLogFile("x.jsonl") { Format = ConsoleLogFormat.JsonLines };
            Assert.Equal("{\"time\":\"2026-10-17 14:03:00\",\"level\":\"Info\",\"category\":\"Deploy\",\"message\":\"first\\nsecond, \\\"quoted\\\"\"}" + Environment.NewLine, json.FormatRecord(record, false));
            string path = Path.Combine(Path.GetTempPath(), "cm-log-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".log");
            try {
                ConsoleLogFile file = new ConsoleLogFile(path) { Encoding = ConsoleLogFile.ResolveEncoding("utf8BOM") };
                Assert.True(file.Append(new ConsoleLogRecord(DateTime.Now, "one")));
                Assert.True(file.Append(new ConsoleLogRecord(DateTime.Now, "two")));
                byte[] bytes = File.ReadAllBytes(path);
                Assert.Equal(0xEF, bytes[0]);
                Assert.Equal(1, Encoding.UTF8.GetString(bytes).Split('﻿').Length - 1);
                Assert.Equal(2, File.ReadAllLines(path).Length);
            } finally {
                File.Delete(path);
            }
        }

        [Fact]
        public void LogFile_ReportsEveryFailedAttempt() {
            string folder = Path.Combine(Path.GetTempPath(), "cm-missing-" + Guid.NewGuid().ToString("N"), "deeper", "x.log");
            System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
            ConsoleLogFile file = new ConsoleLogFile(folder) { Retries = 2, Warning = warnings.Add };
            Assert.False(file.Append(new ConsoleLogRecord(DateTime.Now, "lost")));
            Assert.Equal(2, warnings.Count);
            Assert.Contains("Retrying... (1/2)", warnings[0]);
            Assert.Contains("Tried (2/2)", warnings[1]);
        }

        [Fact]
        public void StatusLine_ComposesMarkerClockAndElapsed() {
            DateTime started = DateTime.Now.AddSeconds(-75);
            StyledLine line = StatusLine.Compose(Context(), RichText.Plain("copying"), StatusKind.Pass, false, false, true, "HH:mm:ss", started, 0, true);
            Assert.StartsWith(Context().Glyphs.StatusMarker(StatusKind.Pass), line.PlainText);
            Assert.Contains("copying", line.PlainText);
        }

        [Fact]
        public void ConsoleColorMap_MapsRolesByMeaningAndOthersToTheNearest() {
            RenderContext context = Context();
            ConsoleColorMap map = new ConsoleColorMap(context);
            Assert.Equal(ConsoleColor.Green, map.Foreground(context.Style(ThemeToken.Pass).Foreground));
            Assert.Equal(ConsoleColor.Red, map.Foreground(context.Style(ThemeToken.Fail).Foreground));
            Assert.Null(map.Foreground(context.Style(ThemeToken.Text).Foreground));
            Assert.Contains(map.Foreground(Color.FromRgb(250, 10, 10)), new ConsoleColor?[] { ConsoleColor.Red, ConsoleColor.DarkRed });
            Assert.Null(new ConsoleColorMap(context, false).Foreground(Color.FromRgb(250, 10, 10)));
        }

        [Theory]
        [InlineData("Passed", StatusKind.Pass)]
        [InlineData("failed", StatusKind.Fail)]
        [InlineData("Skipped", StatusKind.Skip)]
        [InlineData("Inconclusive", StatusKind.Warn)]
        [InlineData("NotRun", StatusKind.Pending)]
        [InlineData(null, StatusKind.Pending)]
        public void TestRecord_ParseStatus_ReadsResultWords(string? word, StatusKind expected) {
            Assert.Equal(expected, TestRecord.ParseStatus(word));
        }

        [Fact]
        public void CodeView_ForAndLineRanges_ResolveLanguagesAndRefuseUnknownOnes() {
            Assert.Equal("json", CodeView.For("{ }", "auto").Highlighter?.Name);
            Assert.Null(CodeView.For("{ }", "Text").Highlighter);
            Assert.Equal("powershell", CodeView.For("x", "ps1").Highlighter?.Name);
            ArgumentException unknown = Assert.Throws<ArgumentException>(() => CodeView.For("x", "Klingon"));
            Assert.Contains("Unknown language 'Klingon'", unknown.Message);
            CodeView view = CodeView.For("a\nb\nc\nd", "text");
            view.HighlightLines("2-3").HighlightLines("4");
            string text = ConsoleMonster.Testing.TestTerminal.RenderText(view, 20);
            Assert.Contains("▌b", text.Replace(" ", string.Empty));
            Assert.Throws<ArgumentException>(() => view.HighlightLines("two"));
        }
    }
}
