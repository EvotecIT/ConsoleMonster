namespace ConsoleMonster.Styling {
    /// <summary>Helpers for <see cref="StatusKind"/>: theme token mapping and lenient parsing.</summary>
    public static class StatusKinds {
        /// <summary>Returns the theme token that colours a status.</summary>
        /// <param name="status">The status.</param>
        /// <returns>The token.</returns>
        public static ThemeToken ToToken(this StatusKind status) {
            switch (status) {
                case StatusKind.Running:
                    return ThemeToken.Running;
                case StatusKind.Pass:
                    return ThemeToken.Pass;
                case StatusKind.Fail:
                    return ThemeToken.Fail;
                case StatusKind.Warn:
                    return ThemeToken.Warn;
                case StatusKind.Skip:
                    return ThemeToken.Skip;
                case StatusKind.Info:
                    return ThemeToken.Info;
                default:
                    return ThemeToken.Pending;
            }
        }

        /// <summary>
        /// Parses a status word leniently, including PowerShell's own words: pass/passed/ok/success/done/completed,
        /// fail/failed/failure/error, warn/warning, skip/skipped/na, run/running/active/busy, pending/queued/waiting,
        /// info/information/verbose/debug.
        /// </summary>
        /// <param name="text">The word.</param>
        /// <param name="status">The status.</param>
        /// <returns>Whether the word was recognised.</returns>
        public static bool TryParse(string? text, out StatusKind status) {
            switch ((text ?? string.Empty).Trim().ToLowerInvariant()) {
                case "pass":
                case "passed":
                case "ok":
                case "success":
                case "succeeded":
                case "done":
                case "complete":
                case "completed":
                    status = StatusKind.Pass;
                    return true;
                case "fail":
                case "failed":
                case "failure":
                case "error":
                case "errors":
                case "failures":
                    status = StatusKind.Fail;
                    return true;
                case "warn":
                case "warning":
                case "warnings":
                    status = StatusKind.Warn;
                    return true;
                case "skip":
                case "skipped":
                case "skips":
                case "na":
                case "n/a":
                    status = StatusKind.Skip;
                    return true;
                case "run":
                case "running":
                case "active":
                case "busy":
                case "inprogress":
                    status = StatusKind.Running;
                    return true;
                case "pending":
                case "queued":
                case "waiting":
                case "notstarted":
                    status = StatusKind.Pending;
                    return true;
                case "info":
                case "information":
                case "informational":
                case "verbose":
                case "debug":
                    status = StatusKind.Info;
                    return true;
                default:
                    status = StatusKind.Pending;
                    return false;
            }
        }

        /// <summary>The words <see cref="TryParse"/> accepts, for argument completion: the canonical names first, then PowerShell's words.</summary>
        public static System.Collections.Generic.IReadOnlyList<string> Words { get; } = new[] {
            "Pass", "Fail", "Warn", "Skip", "Info", "Running", "Pending",
            "Success", "Ok", "Passed", "Error", "Failed", "Failure", "Warning", "Skipped", "Information", "Verbose", "Debug"
        };

        /// <summary>A short upper-case word for a status (PASS, FAIL, WARN, SKIP, RUN, WAIT, INFO in English), in the language of <see cref="Localization.WidgetLabels.Current"/>.</summary>
        /// <param name="status">The status.</param>
        /// <returns>The word.</returns>
        public static string ToWord(this StatusKind status) => Localization.WidgetLabels.Current.StatusWord(status);

        /// <summary>The English status word (PASS, FAIL...), for logs and machine-read output.</summary>
        /// <param name="status">The status.</param>
        /// <returns>The word.</returns>
        public static string ToEnglishWord(this StatusKind status) {
            switch (status) {
                case StatusKind.Running:
                    return "RUN";
                case StatusKind.Pass:
                    return "PASS";
                case StatusKind.Fail:
                    return "FAIL";
                case StatusKind.Warn:
                    return "WARN";
                case StatusKind.Skip:
                    return "SKIP";
                case StatusKind.Info:
                    return "INFO";
                default:
                    return "WAIT";
            }
        }
    }
}
