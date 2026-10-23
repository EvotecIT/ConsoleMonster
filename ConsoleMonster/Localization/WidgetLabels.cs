using System;
using System.Globalization;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Localization {
    /// <summary>
    /// The built-in words that widgets render: status badges, "+3 more", "Showing 5 of 20", "done in 12s", "window too
    /// small" and the like. Each render context resolves them for its language (<see cref="Capabilities.TerminalProfile.Language"/>,
    /// <c>CONSOLEMONSTER_LANGUAGE</c> or the current UI culture); English, Polish, German, Spanish, French and Italian are
    /// built in. Assign <see cref="Override"/> to replace them everywhere, or change a property on a copy from
    /// <see cref="For"/>. Format strings take <c>{0}</c>, <c>{1}</c> placeholders.
    /// </summary>
    public sealed partial class WidgetLabels {
        /// <summary>The compact error label.</summary>
        public string ErrorLabel { get; set; } = "Error";

        /// <summary>A source location without a script file.</summary>
        public string CommandLine { get; set; } = "command line";

        /// <summary>The folded inner-exception count.</summary>
        public string InnerErrorCount { get; set; } = "+{0} inner exceptions";

        /// <summary>The folded count for one inner exception.</summary>
        public string OneInnerError { get; set; } = "+1 inner exception";

        /// <summary>A PowerShell 7 next-step hint below compact errors.</summary>
        public string GetErrorDetails { get; set; } = "Get-Error for details";

        /// <summary>Prefix for the innermost distinct message in a compact error.</summary>
        public string ErrorCause { get; set; } = "cause:";

        /// <summary>Number of runtime stack lines omitted from an exception message.</summary>
        public string HiddenStackLines { get; set; } = "{0} stack lines hidden";

        /// <summary>One runtime stack line omitted from an exception message.</summary>
        public string HiddenStackLine { get; set; } = "1 stack line hidden";
        private static readonly System.Reflection.PropertyInfo[] LabelProperties = typeof(WidgetLabels).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        [ThreadStatic]
        private static WidgetLabels? _scoped;

        /// <summary>Labels used everywhere instead of the translated ones, or null (default) to follow the language.</summary>
        public static WidgetLabels? Override { get; set; }

        /// <summary>
        /// The labels for code that renders without a context (stream lines of live widgets): the ones a live session set
        /// for its render thread, otherwise <see cref="For"/> the default language.
        /// </summary>
        public static WidgetLabels Current => Override ?? _scoped ?? For(LabelLanguage.Default);

        // A live session owns narration labels even when its target differs from the ambient terminal target.
        internal static WidgetLabels? Scoped => Override ?? _scoped;

        /// <summary>The labels for a language (cached; unknown languages get English).</summary>
        /// <param name="language">The language, or null for <see cref="LabelLanguage.Default"/>.</param>
        /// <returns>The labels; don't modify the cached instance, use <see cref="Clone"/>.</returns>
        public static WidgetLabels For(CultureInfo? language) => Override ?? LabelCatalog.Get(language ?? LabelLanguage.Default, "Widget.", () => new WidgetLabels(), LabelProperties);

        internal static IDisposable Scope(WidgetLabels labels) {
            WidgetLabels? previous = _scoped;
            _scoped = labels;
            return new Restore(previous);
        }

        /// <summary>A copy to modify.</summary>
        /// <returns>The copy.</returns>
        public WidgetLabels Clone() => (WidgetLabels)MemberwiseClone();

        /// <summary>The status badge for running work.</summary>
        public string StatusRun { get; set; } = "RUN";

        /// <summary>The status badge for a pass.</summary>
        public string StatusPass { get; set; } = "PASS";

        /// <summary>The status badge for a failure.</summary>
        public string StatusFail { get; set; } = "FAIL";

        /// <summary>The status badge for a warning.</summary>
        public string StatusWarn { get; set; } = "WARN";

        /// <summary>The status badge for a skip.</summary>
        public string StatusSkip { get; set; } = "SKIP";

        /// <summary>The status badge for information.</summary>
        public string StatusInfo { get; set; } = "INFO";

        /// <summary>The status badge for pending work.</summary>
        public string StatusWait { get; set; } = "WAIT";

        /// <summary>Lanes before the first lane arrives.</summary>
        public string WaitingForWork { get; set; } = "waiting for work";

        /// <summary>Rows that did not fit: <c>+{0} more</c>.</summary>
        public string MoreCount { get; set; } = "+{0} more";

        /// <summary>Finished rows in a summary: <c>{0} done</c>.</summary>
        public string DoneCount { get; set; } = "{0} done";

        /// <summary>Waiting rows in a summary: <c>{0} queued</c>.</summary>
        public string QueuedCount { get; set; } = "{0} queued";

        /// <summary>Running rows in a summary: <c>{0} running</c>.</summary>
        public string RunningCount { get; set; } = "{0} running";

        /// <summary>Failed rows in a summary: <c>{0} failed</c>.</summary>
        public string FailedCount { get; set; } = "{0} failed";

        /// <summary>A finished row.</summary>
        public string Done { get; set; } = "done";

        /// <summary>A finished row with its duration: <c>done in {0}</c>.</summary>
        public string DoneIn { get; set; } = "done in {0}";

        /// <summary>A waiting row.</summary>
        public string Queued { get; set; } = "queued";

        /// <summary>Everything finished: <c>{0} done in {1}</c>.</summary>
        public string AllDoneIn { get; set; } = "{0} done in {1}";

        /// <summary>A started task in stream output.</summary>
        public string Started { get; set; } = "started";

        /// <summary>A duration in stream output: <c>in {0}</c>.</summary>
        public string TookDuration { get; set; } = "in {0}";

        /// <summary>Step counts in stream output: <c>{0} passed, {1} failed, {2} warnings</c>.</summary>
        public string StepCounts { get; set; } = "{0} passed, {1} failed, {2} warnings";

        /// <summary>Skipped steps in stream output: <c>{0} skipped</c>.</summary>
        public string SkippedCount { get; set; } = "{0} skipped";

        /// <summary>A phase in stream output: <c>phase {0}</c>.</summary>
        public string Phase { get; set; } = "phase {0}";

        /// <summary>Estimated time left: <c>{0} left</c>.</summary>
        public string TimeLeft { get; set; } = "{0} left";

        /// <summary>A rate: <c>{0}/s</c>.</summary>
        public string PerSecond { get; set; } = "{0}/s";

        /// <summary>A table cut short: <c>Showing {0} of {1}</c>.</summary>
        public string Showing { get; set; } = "Showing {0} of {1}";

        /// <summary>A group heading whose rows are cut short: <c>{0} of {1}</c> (rows shown of the group's rows).</summary>
        public string GroupOf { get; set; } = "{0} of {1}";

        /// <summary>The groups of a table cut short, after "Showing": <c>{0} of {1} groups</c>.</summary>
        public string GroupsShowing { get; set; } = "{0} of {1} groups";

        /// <summary>One group, after "Showing": <c>{0} group</c>.</summary>
        public string OneGroup { get; set; } = "{0} group";

        /// <summary>Several groups, after "Showing": <c>{0} groups</c>.</summary>
        public string ManyGroups { get; set; } = "{0} groups";

        /// <summary>One record (cards).</summary>
        public string OneRecord { get; set; } = "{0} record";

        /// <summary>Several records (cards).</summary>
        public string ManyRecords { get; set; } = "{0} records";

        /// <summary>One hidden column: <c>+{0} column ({1})</c>.</summary>
        public string OneHiddenColumn { get; set; } = "+{0} column ({1})";

        /// <summary>Several hidden columns: <c>+{0} columns ({1})</c>.</summary>
        public string ManyHiddenColumns { get; set; } = "+{0} columns ({1})";

        /// <summary>The totals row of a table.</summary>
        public string Total { get; set; } = "Total";

        /// <summary>A group's subtotal row.</summary>
        public string Subtotal { get; set; } = "Subtotal";

        /// <summary>A date that never happened (DateTime.MinValue/MaxValue).</summary>
        public string Never { get; set; } = "never";

        /// <summary>A window below the layout's minimum: <c>window too small: {0}, needs {1}</c>.</summary>
        public string WindowTooSmall { get; set; } = "window too small: {0}, needs {1}";

        /// <summary>The minimum size alone: <c>needs {0}</c>.</summary>
        public string Needs { get; set; } = "needs {0}";

        /// <summary>The dashboard footer hint for Ctrl+C.</summary>
        public string Stop { get; set; } = "stop";

        /// <summary>An inner exception heading.</summary>
        public string CausedBy { get; set; } = "Caused by";

        /// <summary>One hidden stack frame: <c>+ {0} more frame</c>.</summary>
        public string OneMoreFrame { get; set; } = "+ {0} more frame";

        /// <summary>Several hidden stack frames: <c>+ {0} more frames</c>.</summary>
        public string ManyMoreFrames { get; set; } = "+ {0} more frames";

        /// <summary>One hidden framework frame: <c>+ {0} framework frame</c>.</summary>
        public string OneFrameworkFrame { get; set; } = "+ {0} framework frame";

        /// <summary>Several hidden framework frames: <c>+ {0} framework frames</c>.</summary>
        public string ManyFrameworkFrames { get; set; } = "+ {0} framework frames";

        /// <summary>One more line of code: <c>+ {0} more line</c>.</summary>
        public string OneMoreLine { get; set; } = "+ {0} more line";

        /// <summary>More lines of code: <c>+ {0} more lines</c>.</summary>
        public string ManyMoreLines { get; set; } = "+ {0} more lines";

        /// <summary>One item in a collection.</summary>
        public string OneItem { get; set; } = "{0} item";

        /// <summary>Several items in a collection.</summary>
        public string ManyItems { get; set; } = "{0} items";

        /// <summary>The test-run dashboard title.</summary>
        public string TestsTitle { get; set; } = "Tests";

        /// <summary>A finished test run.</summary>
        public string TestsFinished { get; set; } = "Finished";

        /// <summary>A test run that finished with failures.</summary>
        public string TestsFinishedWithFailures { get; set; } = "Finished with failures";

        /// <summary>A test run before the first file.</summary>
        public string TestsDiscovering { get; set; } = "Discovering tests";

        /// <summary>A test run between files.</summary>
        public string TestsStarting { get; set; } = "Starting";

        /// <summary>The failure list of a clean run.</summary>
        public string TestsNoFailures { get; set; } = "No failures";

        /// <summary>One failure not shown: <c>+ {0} more failure</c>.</summary>
        public string TestsOneMoreFailure { get; set; } = "+ {0} more failure";

        /// <summary>Failures not shown: <c>+ {0} more failures</c>.</summary>
        public string TestsManyMoreFailures { get; set; } = "+ {0} more failures";

        /// <summary>The slowest list before any test finished.</summary>
        public string TestsNoneFinished { get; set; } = "No tests finished yet";

        /// <summary>The title of the per-file summary table.</summary>
        public string TestsResultsByFile { get; set; } = "Results by file";

        /// <summary>The file column.</summary>
        public string TestsFile { get; set; } = "File";

        /// <summary>The test count column.</summary>
        public string TestsCount { get; set; } = "Tests";

        /// <summary>The passed column.</summary>
        public string TestsPassedColumn { get; set; } = "Passed";

        /// <summary>The failed column.</summary>
        public string TestsFailedColumn { get; set; } = "Failed";

        /// <summary>The skipped column.</summary>
        public string TestsSkippedColumn { get; set; } = "Skipped";

        /// <summary>The time column.</summary>
        public string TestsTimeColumn { get; set; } = "Time";

        /// <summary>The failures heading.</summary>
        public string TestsFailures { get; set; } = "Failures";

        /// <summary>The outcome of a failed run.</summary>
        public string TestsFailed { get; set; } = "Tests failed";

        /// <summary>The outcome of an inconclusive run.</summary>
        public string TestsInconclusive { get; set; } = "Tests inconclusive";

        /// <summary>The outcome of a passed run.</summary>
        public string TestsPassed { get; set; } = "Tests passed";

        /// <summary>The expected value of a failed assertion.</summary>
        public string TestsExpected { get; set; } = "expected";

        /// <summary>The actual value of a failed assertion.</summary>
        public string TestsActual { get; set; } = "actual";

        /// <summary>Passed tests: <c>{0} passed</c>.</summary>
        public string TestsPassedCount { get; set; } = "{0} passed";

        /// <summary>Inconclusive tests: <c>{0} inconclusive</c>.</summary>
        public string TestsInconclusiveCount { get; set; } = "{0} inconclusive";

        /// <summary>Tests not run: <c>{0} not run</c>.</summary>
        public string TestsNotRunCount { get; set; } = "{0} not run";

        /// <summary>One failed block: <c>{0} block failed</c>.</summary>
        public string TestsOneBlockFailed { get; set; } = "{0} block failed";

        /// <summary>Failed blocks: <c>{0} blocks failed</c>.</summary>
        public string TestsManyBlocksFailed { get; set; } = "{0} blocks failed";

        /// <summary>Tests run: <c>{0} tests</c>.</summary>
        public string TestsDoneCount { get; set; } = "{0} tests";

        /// <summary>Tests run of the total: <c>{0} of {1}</c>.</summary>
        public string TestsDoneOfTotal { get; set; } = "{0} of {1}";

        /// <summary>The slowest tests panel.</summary>
        public string TestsSlowest { get; set; } = "Slowest";

        /// <summary>The durations panel.</summary>
        public string TestsDurations { get; set; } = "Durations";

        /// <summary>The files panel.</summary>
        public string TestsFiles { get; set; } = "Files";

        /// <summary>Files done: <c>{0} of {1} done</c>.</summary>
        public string TestsFilesDone { get; set; } = "{0} of {1} done";

        /// <summary>The dashboard status while tests run.</summary>
        public string TestsStatusRunning { get; set; } = "running";

        /// <summary>The dashboard status after failures.</summary>
        public string TestsStatusFailed { get; set; } = "failed";

        /// <summary>The dashboard status of an inconclusive run.</summary>
        public string TestsStatusInconclusive { get; set; } = "inconclusive";

        /// <summary>The dashboard status of a passed run.</summary>
        public string TestsStatusPassed { get; set; } = "passed";

        /// <summary>A frame that failed to render: <c>render error at {0}: {1}</c>.</summary>
        public string RenderError { get; set; } = "render error at {0}: {1}";

        /// <summary>Added objects: <c>+{0} added</c>.</summary>
        public string ChangesAdded { get; set; } = "+{0} added";

        /// <summary>Removed objects: <c>-{0} removed</c>.</summary>
        public string ChangesRemoved { get; set; } = "-{0} removed";

        /// <summary>Changed objects: <c>~{0} changed</c>.</summary>
        public string ChangesChanged { get; set; } = "~{0} changed";

        /// <summary>Unchanged objects: <c>{0} unchanged</c>.</summary>
        public string ChangesUnchanged { get; set; } = "{0} unchanged";

        /// <summary>Items before a window: <c>+{0} earlier</c>.</summary>
        public string EarlierCount { get; set; } = "+{0} earlier";

        /// <summary>Items with a warning: <c>{0} warnings</c>.</summary>
        public string WarningCount { get; set; } = "{0} warnings";

        /// <summary>The warning counter when exactly one warning is present.</summary>
        public string OneWarningCount { get; set; } = "{0} warning";

        /// <summary>Selects the warning label for a count.</summary>
        /// <param name="count">The warning count.</param>
        /// <returns>The localized format string.</returns>
        public string Warnings(int count) => count == 1 ? OneWarningCount : WarningCount;

        /// <summary>The heading of a watched command: <c>Every {0}:</c> with the interval.</summary>
        public string WatchEvery { get; set; } = "Every {0}:";

        /// <summary>Properties left out of a comparison because reading them stalled: <c>{0} not compared</c>.</summary>
        public string ChangesNotCompared { get; set; } = "{0} not compared";

        /// <summary>The item column of a change table.</summary>
        public string ChangesItem { get; set; } = "Item";

        /// <summary>The property column of a change table.</summary>
        public string ChangesProperty { get; set; } = "Property";

        /// <summary>The old value column.</summary>
        public string ChangesBefore { get; set; } = "Before";

        /// <summary>The new value column.</summary>
        public string ChangesAfter { get; set; } = "After";

        /// <summary>A long value shown as a line diff below the table.</summary>
        public string ChangesSeeDiff { get; set; } = "(see diff)";

        /// <summary>A comparison without changes.</summary>
        public string ChangesNone { get; set; } = "No changes";

        /// <summary>The question before applying changes.</summary>
        public string ChangesConfirm { get; set; } = "Apply these changes?";

        /// <summary>The members of an inspected object: <c>{0} members</c>.</summary>
        public string Members { get; set; } = "{0} members";

        /// <summary>A collection cut short without a known count.</summary>
        public string MoreUnknown { get; set; } = "+ more";

        /// <summary>A tree node that repeats an item already on its path: <c>(cycle)</c>.</summary>
        public string TreeCycle { get; set; } = "(cycle)";

        /// <summary>Children not followed past a tree's depth limit: <c>… (depth limit)</c>.</summary>
        public string TreeDepthLimit { get; set; } = "… (depth limit)";

        /// <summary>The end of a tree that reached its node limit: <c>… (node limit reached)</c>.</summary>
        public string TreeNodeLimit { get; set; } = "… (node limit reached)";

        /// <summary>Hidden columns of an inspected collection: <c>+{0} more columns</c>.</summary>
        public string MoreColumns { get; set; } = "+{0} more columns";

        /// <summary>The badge word for a status.</summary>
        /// <param name="status">The status.</param>
        /// <returns>The word.</returns>
        public string StatusWord(StatusKind status) {
            switch (status) {
                case StatusKind.Running:
                    return StatusRun;
                case StatusKind.Pass:
                    return StatusPass;
                case StatusKind.Fail:
                    return StatusFail;
                case StatusKind.Warn:
                    return StatusWarn;
                case StatusKind.Skip:
                    return StatusSkip;
                case StatusKind.Info:
                    return StatusInfo;
                default:
                    return StatusWait;
            }
        }

        /// <summary>Formats a label with the invariant digits the widgets already formatted.</summary>
        /// <param name="format">The label.</param>
        /// <param name="args">The values.</param>
        /// <returns>The text.</returns>
        public static string Format(string format, params object?[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        private sealed class Restore : IDisposable {
            private readonly WidgetLabels? _previous;

            internal Restore(WidgetLabels? previous) {
                _previous = previous;
            }

            public void Dispose() => _scoped = _previous;
        }
    }
}
