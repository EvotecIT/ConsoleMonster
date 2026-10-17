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
    public sealed class WidgetLabels {
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

        /// <summary>The members of an inspected object: <c>{0} members</c>.</summary>
        public string Members { get; set; } = "{0} members";

        /// <summary>A collection cut short without a known count.</summary>
        public string MoreUnknown { get; set; } = "+ more";

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
