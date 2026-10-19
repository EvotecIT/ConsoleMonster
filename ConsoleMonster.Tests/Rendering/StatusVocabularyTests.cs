using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>One status vocabulary for statuses, logs and status columns, including PowerShell's own words.</summary>
    public sealed class StatusVocabularyTests {
        private enum JobState { Running, Stopped, Failed }

        [Theory]
        [InlineData("Success", StatusKind.Pass)]
        [InlineData("Ok", StatusKind.Pass)]
        [InlineData("Passed", StatusKind.Pass)]
        [InlineData("Information", StatusKind.Info)]
        [InlineData("Info", StatusKind.Info)]
        [InlineData("Verbose", StatusKind.Info)]
        [InlineData("Debug", StatusKind.Info)]
        [InlineData("Warning", StatusKind.Warn)]
        [InlineData("Warn", StatusKind.Warn)]
        [InlineData("Error", StatusKind.Fail)]
        [InlineData("Failed", StatusKind.Fail)]
        [InlineData("Failure", StatusKind.Fail)]
        [InlineData("Skipped", StatusKind.Skip)]
        public void TryParse_AcceptsPowerShellWords(string word, StatusKind expected) {
            Assert.True(StatusKinds.TryParse(word, out StatusKind status));
            Assert.Equal(expected, status);
        }

        [Theory]
        [InlineData("Information", "info")]
        [InlineData("Warning", "warn")]
        [InlineData("Error", "err")]
        [InlineData("err", "err")]
        [InlineData("Failed", "err")]
        [InlineData("Verbose", "verbose")]
        [InlineData("Debug", "debug")]
        [InlineData("Success", "pass")]
        [InlineData("Skipped", "skip")]
        public void LogLevels_NormalizeEveryWord(string word, string expected) {
            Assert.True(LogLevels.TryNormalize(word, out string level));
            Assert.Equal(expected, level);
        }

        [Fact]
        public void LogLevels_UnknownWord_IsRejected() {
            Assert.False(LogLevels.TryNormalize("loud", out _));
        }

        [Theory]
        [InlineData("Running")]
        [InlineData("Pending")]
        public void LogLevels_ProgressStatuses_AreInfo(string word) {
            Assert.True(LogLevels.TryNormalize(word, out string level));
            Assert.Equal("info", level);
        }

        [Fact]
        public void LogLevels_MarkErrorsWarningsPassesAndSkipsOnly() {
            Assert.Equal(StatusKind.Fail, LogLevels.ToStatus("Error"));
            Assert.Equal(StatusKind.Warn, LogLevels.ToStatus("warn"));
            Assert.Equal(StatusKind.Pass, LogLevels.ToStatus("Success"));
            Assert.Equal(StatusKind.Skip, LogLevels.ToStatus("Skipped"));
            Assert.Null(LogLevels.ToStatus("info"));
            Assert.Null(LogLevels.ToStatus("Verbose"));
        }

        [Theory]
        [InlineData("Status", "Success", true)]
        [InlineData("Result", "Failed", true)]
        [InlineData("Level", "Warning", true)]
        [InlineData("Status", "something else", false)]
        [InlineData("Name", "Success", false)]
        public void ForStatus_TextColumnsNamedLikeAStatus(string column, string value, bool expected) {
            Assert.Equal(expected, CellPresenters.ForStatus(column, value) != null);
        }

        [Fact]
        public void ForStatus_StatusLikeEnums_InAnyColumn() {
            Assert.NotNull(CellPresenters.ForStatus("Anything", JobState.Running));
            Assert.Null(CellPresenters.ForStatus("Day", System.DayOfWeek.Monday));
        }
    }
}
