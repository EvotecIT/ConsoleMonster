using System;
using System.Globalization;
using ConsoleMonster.Live.Tails;
using Xunit;

namespace ConsoleMonster.Tests.Live.Tails {
    public sealed class LogLineParserTests {
        [Theory]
        [InlineData("2026-10-22T09:31:09Z ERROR database timeout", "database timeout")]
        [InlineData(" [2026-10-22T09:31:09+02:00] | [WARN]: [red]database[/]", "[red]database[/]")]
        [InlineData("[ERROR] failed request", "failed request")]
        [InlineData("INFO | message", "message")]
        [InlineData("2026-10-22T09:31:09Z message containing ERROR", "message containing ERROR")]
        [InlineData("message containing ERROR", "message containing ERROR")]
        [InlineData("  ordinary text", "  ordinary text")]
        [InlineData("[12:34:56] [ERROR] incomplete date stays literal", "[12:34:56] [ERROR] incomplete date stays literal")]
        public void DisplayMessageRemovesOnlyRecognizedLeadingFieldsAndPreservesOriginal(string original, string message) {
            TailedLogEntry entry = LogLineParser.Parse(original);
            Assert.Equal(original, entry.Text);
            Assert.Equal(message, entry.Message);
        }

        [Theory]
        [InlineData("[ERROR] failed request", "err")]
        [InlineData("warn: retry", "warn")]
        [InlineData("INFO | message", "info")]
        [InlineData("[Debug] detail", "debug")]
        [InlineData("[Verbose] trace", "verbose")]
        [InlineData("[Success] done", "pass")]
        [InlineData("[Skipped] disabled", "skip")]
        [InlineData("[WARNING] message", "warn")]
        [InlineData("[Failure] message", "err")]
        [InlineData("[WARN] then [ERROR]", "warn")]
        public void WholeLevelWordsUseExistingCanonicalVocabulary(string line, string expected) {
            TailedLogEntry entry = LogLineParser.Parse(line);
            Assert.Equal(expected, entry.Level);
            Assert.Equal(line, entry.Text);
            Assert.Null(entry.Timestamp);
        }

        [Theory]
        [InlineData("terror errorless warningly informationally debugged passing")]
        [InlineData("prefix_ERROR_suffix WARN2 error42")]
        [InlineData("plain message")]
        [InlineData("")]
        public void SubstringWordsAreNotLevelMarkers(string line) {
            TailedLogEntry entry = LogLineParser.Parse(line);
            Assert.Equal("info", entry.Level);
            Assert.Equal(line, entry.Text);
            Assert.Null(entry.Timestamp);
        }

        [Theory]
        [InlineData("[2026-10-22T12:34:56.123+02:00] [ERROR] message", "2026-10-22T12:34:56.123+02:00")]
        [InlineData("2026-10-22T12:34:56Z INFO message", "2026-10-22T12:34:56+00:00")]
        [InlineData("2026-10-22 12:34:56 +02:00 [WARN] message", "2026-10-22T12:34:56+02:00")]
        [InlineData("[2026-10-22 12:34:56] message", "2026-10-22T12:34:56+00:00")]
        public void ExplicitLeadingTimestampsPreserveTheirValueAndLiteralText(string line, string expected) {
            TailedLogEntry entry = LogLineParser.Parse(line);
            Assert.Equal(DateTimeOffset.Parse(expected, CultureInfo.InvariantCulture), entry.Timestamp);
            Assert.Equal(line, entry.Text);
        }

        [Theory]
        [InlineData("[12:34:56] [ERROR] message")]
        [InlineData("[10-22 12:34] message")]
        [InlineData("[2026-99-99T12:34:56Z] message")]
        [InlineData("[INFO] timestamp appears later 2026-10-22T12:34:56Z")]
        public void MissingInvalidOrIncompleteTimestampNeverInventsCurrentTime(string line) {
            Assert.Null(LogLineParser.Parse(line).Timestamp);
        }

        [Fact]
        public void MarkupAndAnsiLikeLogTextRemainLiteral() {
            const string line = "[ERROR] [red]literal[/] <b>&text</b>\u001b[31m";
            TailedLogEntry entry = LogLineParser.Parse(line);
            Assert.Equal(line, entry.Text);
            Assert.Equal("err", entry.Level);
            Assert.Null(entry.Timestamp);
        }

        [Fact]
        public void EntryNormalizesStatusWithoutSupplyingTimestamp() {
            TailedLogEntry entry = new TailedLogEntry("literal [red]", "Failed");
            Assert.Equal("err", entry.Level);
            Assert.Equal("literal [red]", entry.Text);
            Assert.Null(entry.Timestamp);
            Assert.Equal("info", new TailedLogEntry("text", "unrecognised").Level);
        }
    }
}
