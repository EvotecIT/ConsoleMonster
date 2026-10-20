using System.Collections.Generic;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Log levels for log regions and Write-ConsoleLog: info, warn, err, verbose, debug, pass and skip. PowerShell's own words
    /// (Information, Warning, Error, Verbose, Debug, Success...) and the status words map onto them, so one vocabulary works
    /// for statuses and logs.
    /// </summary>
    public static class LogLevels {
        /// <summary>The canonical level names.</summary>
        public static IReadOnlyList<string> Names { get; } = new[] { "info", "warn", "err", "verbose", "debug", "pass", "skip" };

        /// <summary>The words <see cref="TryNormalize"/> accepts, for argument completion.</summary>
        public static IReadOnlyList<string> Words { get; } = new[] {
            "info", "warn", "err", "verbose", "debug", "pass", "skip",
            "Information", "Warning", "Error", "Verbose", "Debug", "Success", "Ok", "Passed", "Failed", "Failure", "Skipped"
        };

        /// <summary>Maps a level or status word to a canonical level name.</summary>
        /// <param name="text">The word (case does not matter).</param>
        /// <param name="level">The canonical level.</param>
        /// <returns>Whether the word was recognised.</returns>
        public static bool TryNormalize(string? text, out string level) {
            string word = (text ?? string.Empty).Trim().ToLowerInvariant();
            switch (word) {
                case "verbose":
                case "verb":
                    level = "verbose";
                    return true;
                case "debug":
                case "dbg":
                    level = "debug";
                    return true;
                case "err":
                    level = "err";
                    return true;
            }
            if (StatusKinds.TryParse(word, out StatusKind status)) {
                switch (status) {
                    case StatusKind.Fail:
                        level = "err";
                        return true;
                    case StatusKind.Warn:
                        level = "warn";
                        return true;
                    case StatusKind.Pass:
                        level = "pass";
                        return true;
                    case StatusKind.Skip:
                        level = "skip";
                        return true;
                    case StatusKind.Info:
                    case StatusKind.Running:
                    case StatusKind.Pending:
                        level = "info";
                        return true;
                }
            }
            level = "info";
            return false;
        }

        /// <summary>The status a level is marked with (err → Fail, warn → Warn, pass → Pass, skip → Skip), or null for info, verbose and debug.</summary>
        /// <param name="level">A level or status word.</param>
        /// <returns>The status, or null.</returns>
        public static StatusKind? ToStatus(string? level) {
            TryNormalize(level, out string normalized);
            switch (normalized) {
                case "err":
                    return StatusKind.Fail;
                case "warn":
                    return StatusKind.Warn;
                case "pass":
                    return StatusKind.Pass;
                case "skip":
                    return StatusKind.Skip;
                default:
                    return null;
            }
        }
    }
}
