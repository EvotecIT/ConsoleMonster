using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestRuns {
    /// <summary>
    /// The text of a test run, shared by the live view, the summary and the stream lines, so a failure reads the same
    /// everywhere: the status symbol, the blocks and test name joined by the breadcrumb chevron, expected against actual
    /// (or the message), and the file and line as a link.
    /// </summary>
    public static class TestRunText {
        private static readonly Regex ExpectedButGot = new Regex(@"^Expected (?<e>.+?),? but (?:got|was) (?<a>.+?)\.?$", RegexOptions.CultureInvariant);

        /// <summary>The blocks and the test name joined by the breadcrumb separator (› or &gt;).</summary>
        /// <param name="blocks">The blocks.</param>
        /// <param name="name">The test name.</param>
        /// <param name="glyphs">The glyph set.</param>
        /// <returns>The path.</returns>
        public static string Path(IReadOnlyList<string> blocks, string name, IGlyphSet glyphs) {
            string chevron = " " + Interactive.InteractiveGlyphs.For(glyphs.Tier).Breadcrumb + " ";
            StringBuilder sb = new StringBuilder();
            foreach (string block in blocks) {
                sb.Append(block).Append(chevron);
            }
            return sb.Append(name).ToString();
        }

        /// <summary>The location as "file:line" (the file name only), or null when the file is unknown.</summary>
        /// <param name="record">The record.</param>
        /// <returns>The text.</returns>
        public static string? Location(TestRecord record) {
            if (string.IsNullOrEmpty(record.File)) {
                return null;
            }
            // Recorded runs may come from another system: the file name is taken from the text of the path.
            string name = Text.DisplayPath.FileName(record.File);
            return record.Line > 0 ? name + ":" + record.Line.ToString(CultureInfo.InvariantCulture) : name;
        }

        /// <summary>A file URI for the record's file, or null.</summary>
        /// <param name="record">The record.</param>
        /// <returns>The URI.</returns>
        public static string? Link(TestRecord record) {
            if (string.IsNullOrEmpty(record.File) || Text.DisplayPath.IsForeign(record.File)) {
                // No link to a path from another system: the host could not open it.
                return null;
            }
            try {
                return new Uri(System.IO.Path.GetFullPath(record.File!)).AbsoluteUri;
            } catch (Exception ex) when (ex is ArgumentException || ex is UriFormatException || ex is NotSupportedException || ex is System.IO.PathTooLongException) {
                return null;
            }
        }

        /// <summary>
        /// Reads expected and actual values from an assertion message ("Expected 4, but got 3.", or the "Expected:" and
        /// "But was:" lines of a string comparison). Quotes around both values are removed.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <returns>Whether both were found.</returns>
        public static bool TryParseExpectation(string? message, out string expected, out string actual) {
            expected = string.Empty;
            actual = string.Empty;
            if (string.IsNullOrEmpty(message)) {
                return false;
            }
            string[] lines = message!.Replace("\r\n", "\n").Split('\n');
            string? e = null;
            string? a = null;
            foreach (string raw in lines) {
                string line = raw.Trim();
                if (line.StartsWith("Expected:", StringComparison.Ordinal)) {
                    e = line.Substring(9).Trim();
                } else if (line.StartsWith("But was:", StringComparison.Ordinal)) {
                    a = line.Substring(8).Trim();
                }
            }
            if (e == null || a == null) {
                Match match = ExpectedButGot.Match(lines[0].Trim());
                if (!match.Success) {
                    return false;
                }
                e = match.Groups["e"].Value;
                a = match.Groups["a"].Value;
            }
            if (e.Length >= 2 && a.Length >= 2 && e[0] == '\'' && e[e.Length - 1] == '\'' && a[0] == '\'' && a[a.Length - 1] == '\'') {
                e = e.Substring(1, e.Length - 2);
                a = a.Substring(1, a.Length - 2);
            }
            expected = e;
            actual = a;
            return true;
        }

        /// <summary>
        /// The lines of one failure: the symbol and path, expected and actual (the characters of the actual value that
        /// differ are underlined) or up to <paramref name="messageLines"/> lines of the message, and the location as a link.
        /// </summary>
        /// <param name="record">The failure.</param>
        /// <param name="context">The render context.</param>
        /// <param name="messageLines">The most message lines to show.</param>
        /// <returns>The lines.</returns>
        public static List<StyledLine> FailureLines(TestRecord record, RenderContext context, int messageLines = 3) {
            Style muted = context.Style(ThemeToken.Muted);
            Style text = context.Style(ThemeToken.Text);
            List<StyledLine> lines = new List<StyledLine>();
            List<TextSpan> head = new List<TextSpan> { new TextSpan(context.Glyphs.StatusSymbol(StatusKind.Fail) + " ", context.Style(StatusKind.Fail)) };
            string chevron = " " + Interactive.InteractiveGlyphs.For(context).Breadcrumb + " ";
            foreach (string block in record.Blocks) {
                head.Add(new TextSpan(block + chevron, muted));
            }
            head.Add(new TextSpan(record.Name, text.With(TextDecoration.Bold)));
            lines.Add(new StyledLine(head));
            if (TryParseExpectation(record.Message, out string expected, out string actual)) {
                lines.Add(new StyledLine(new[] { new TextSpan("  " + Pad(context.Labels.TestsExpected, context.Labels.TestsActual) + " ", muted), new TextSpan(expected, context.Style(StatusKind.Pass)) }));
                List<TextSpan> got = new List<TextSpan> { new TextSpan("  " + Pad(context.Labels.TestsActual, context.Labels.TestsExpected) + " ", muted) };
                got.AddRange(Diff(expected, actual, context.Style(StatusKind.Fail)));
                lines.Add(new StyledLine(got));
            } else if (!string.IsNullOrEmpty(record.Message)) {
                string[] message = record.Message!.Replace("\r\n", "\n").Trim('\n').Split('\n');
                int shown = Math.Min(message.Length, Math.Max(1, messageLines));
                for (int i = 0; i < shown; i++) {
                    string line = message[i].TrimEnd();
                    if (i == shown - 1 && shown < message.Length) {
                        line += " " + context.Ellipsis;
                    }
                    if (i == 0 && record.ErrorType != null) {
                        // An error that is not an assertion names its exception type, as the exception view does.
                        lines.Add(new StyledLine(new[] { new TextSpan("  "), new TextSpan(record.ErrorType + ": ", context.Style(StatusKind.Fail).With(TextDecoration.Bold)), new TextSpan(line, text) }));
                        continue;
                    }
                    lines.Add(StyledLine.From("  " + line, text));
                }
            }
            string? location = Location(record);
            if (location != null) {
                string? link = Link(record);
                lines.Add(new StyledLine(new[] { new TextSpan("  "), new TextSpan(location, link == null ? muted : muted.With(TextDecoration.Underline).WithLink(link)) }));
            }
            return lines;
        }

        /// <summary>One line for a failure in stream output: symbol, path, the first message line or expected and actual, and the location.</summary>
        /// <param name="record">The failure.</param>
        /// <param name="glyphs">The glyph set.</param>
        /// <returns>The line (plain text).</returns>
        public static string FailureLine(TestRecord record, IGlyphSet glyphs) {
            StringBuilder sb = new StringBuilder();
            sb.Append(glyphs.StatusSymbol(StatusKind.Fail)).Append(' ').Append(Path(record.Blocks, record.Name, glyphs));
            if (TryParseExpectation(record.Message, out string expected, out string actual)) {
                sb.Append(": ").Append(Localization.WidgetLabels.Current.TestsExpected).Append(' ').Append(expected).Append(", ").Append(Localization.WidgetLabels.Current.TestsActual).Append(' ').Append(actual);
            } else if (!string.IsNullOrEmpty(record.Message)) {
                sb.Append(": ").Append(record.ErrorType == null ? string.Empty : record.ErrorType + ": ").Append(record.Message!.Replace("\r\n", "\n").Trim('\n').Split('\n')[0].Trim());
            }
            string? location = Location(record);
            if (location != null) {
                sb.Append(" (").Append(location).Append(')');
            }
            return sb.ToString();
        }

        /// <summary>The run totals: "3 failed · 3 passed · 1 skipped · 7 tests · 00:00:01" (separators per tier).</summary>
        /// <param name="snapshot">The snapshot.</param>
        /// <param name="glyphs">The glyph set.</param>
        /// <param name="clock">Whether to end with the run clock.</param>
        /// <returns>The facts.</returns>
        public static string Totals(TestRunSnapshot snapshot, IGlyphSet glyphs, bool clock = true) {
            string dot = " " + Interactive.InteractiveGlyphs.For(glyphs.Tier).Separator + " ";
            Localization.WidgetLabels labels = Localization.WidgetLabels.Current;
            List<string> facts = new List<string>();
            if (snapshot.Failed > 0 || snapshot.BlockFailures > 0) {
                facts.Add(Localization.WidgetLabels.Format(labels.FailedCount, Count(snapshot.Failed)));
            }
            facts.Add(Localization.WidgetLabels.Format(labels.TestsPassedCount, Count(snapshot.Passed)));
            if (snapshot.Skipped > 0) {
                facts.Add(Localization.WidgetLabels.Format(labels.SkippedCount, Count(snapshot.Skipped)));
            }
            if (snapshot.Inconclusive > 0) {
                facts.Add(Localization.WidgetLabels.Format(labels.TestsInconclusiveCount, Count(snapshot.Inconclusive)));
            }
            if (snapshot.NotRun > 0) {
                facts.Add(Localization.WidgetLabels.Format(labels.TestsNotRunCount, Count(snapshot.NotRun)));
            }
            if (snapshot.BlockFailures > 0) {
                facts.Add(Localization.WidgetLabels.Format(snapshot.BlockFailures == 1 ? labels.TestsOneBlockFailed : labels.TestsManyBlocksFailed, Count(snapshot.BlockFailures)));
            }
            facts.Add(snapshot.IsFinished || !snapshot.Total.HasValue
                ? Localization.WidgetLabels.Format(labels.TestsDoneCount, Count(snapshot.Done))
                : Localization.WidgetLabels.Format(labels.TestsDoneOfTotal, Count(snapshot.Done), Count(snapshot.Total.Value)));
            if (clock) {
                facts.Add(NumberFormat.Clock(snapshot.Elapsed));
            }
            return string.Join(dot, facts);
        }

        /// <summary>The closing line for stream output: the status symbol, the outcome and the totals.</summary>
        /// <param name="snapshot">The snapshot.</param>
        /// <param name="glyphs">The glyph set.</param>
        /// <returns>The line (plain text).</returns>
        public static string SummaryLine(TestRunSnapshot snapshot, IGlyphSet glyphs) {
            StatusKind status = snapshot.Status;
            return glyphs.StatusSymbol(status) + " " + Outcome(status) + ": " + Totals(snapshot, glyphs);
        }

        /// <summary>The outcome words for a run status (Tests failed, Tests inconclusive, Tests passed), translated.</summary>
        /// <param name="status">The run status.</param>
        /// <returns>The words.</returns>
        public static string Outcome(StatusKind status) {
            Localization.WidgetLabels labels = Localization.WidgetLabels.Current;
            return status == StatusKind.Fail ? labels.TestsFailed : status == StatusKind.Warn ? labels.TestsInconclusive : labels.TestsPassed;
        }

        // "expected" and "actual" padded to the same width, so the two values line up.
        private static string Pad(string word, string other) => word.PadRight(Math.Max(word.Length, other.Length));

        private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);

        // The actual value with the characters that differ from the expected value underlined.
        private static IEnumerable<TextSpan> Diff(string expected, string actual, Style style) {
            Style changed = style.With(TextDecoration.Underline | TextDecoration.Bold);
            StringBuilder run = new StringBuilder();
            bool runChanged = false;
            for (int i = 0; i < actual.Length; i++) {
                bool differs = i >= expected.Length || expected[i] != actual[i];
                if (run.Length > 0 && differs != runChanged) {
                    yield return new TextSpan(run.ToString(), runChanged ? changed : style);
                    run.Clear();
                }
                runChanged = differs;
                run.Append(actual[i]);
            }
            if (run.Length > 0) {
                yield return new TextSpan(run.ToString(), runChanged ? changed : style);
            }
        }
    }
}
