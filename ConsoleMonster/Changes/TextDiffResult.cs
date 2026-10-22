using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ConsoleMonster.Changes {
    /// <summary>An immutable line comparison that can be rendered repeatedly or exported as a unified diff.</summary>
    public sealed class TextDiffResult {
        internal TextDiffResult(IReadOnlyList<TextDiffLine> lines, TextDiffOptions options) {
            Lines = lines;
            Context = options.Context;
            ReferenceLabel = CleanLabel(options.ReferenceLabel, "Before");
            DifferenceLabel = CleanLabel(options.DifferenceLabel, "After");
            Added = lines.Count(line => line.Kind == ChangeKind.Added);
            Removed = lines.Count(line => line.Kind == ChangeKind.Removed);
        }

        /// <summary>All lines, including unchanged regions, in edit order.</summary>
        public IReadOnlyList<TextDiffLine> Lines { get; }

        /// <summary>The unchanged context retained around each change.</summary>
        public int Context { get; }

        /// <summary>The original-text label, with control characters replaced by spaces.</summary>
        public string ReferenceLabel { get; }

        /// <summary>The new-text label, with control characters replaced by spaces.</summary>
        public string DifferenceLabel { get; }

        /// <summary>The number of added lines.</summary>
        public int Added { get; }

        /// <summary>The number of removed lines.</summary>
        public int Removed { get; }

        /// <summary>Whether any content or final-newline changes were found.</summary>
        public bool HasChanges => Added != 0 || Removed != 0;

        /// <summary>
        /// Produces plain unified diff text with LF separators, source labels, hunk ranges and missing-final-newline
        /// markers. Identical inputs produce an empty string. No terminal markup or escape sequences are added.
        /// </summary>
        /// <returns>The unified diff, ending in LF when nonempty.</returns>
        public string ToUnifiedText() {
            if (!HasChanges) { return string.Empty; }
            StringBuilder output = new StringBuilder();
            output.Append("--- ").Append(ReferenceLabel).Append('\n');
            output.Append("+++ ").Append(DifferenceLabel).Append('\n');
            int reference = 0;
            int difference = 0;
            int index = 0;
            foreach ((int Start, int End) range in Ranges()) {
                while (index < range.Start) {
                    Advance(Lines[index++], ref reference, ref difference);
                }
                int oldCount = 0;
                int newCount = 0;
                for (int i = range.Start; i < range.End; i++) {
                    Advance(Lines[i], ref oldCount, ref newCount);
                }
                output.Append("@@ -").Append(Range(reference, oldCount)).Append(" +")
                    .Append(Range(difference, newCount)).Append(" @@\n");
                while (index < range.End) {
                    TextDiffLine line = Lines[index++];
                    output.Append(line.Kind == ChangeKind.Added ? '+' : line.Kind == ChangeKind.Removed ? '-' : ' ')
                        .Append(line.Text).Append('\n');
                    if (!line.HasLineEnding) { output.Append("\\ No newline at end of file\n"); }
                    Advance(line, ref reference, ref difference);
                }
            }
            return output.ToString();
        }

        internal List<(int Start, int End)> Ranges() {
            List<(int Start, int End)> ranges = new List<(int Start, int End)>();
            for (int i = 0; i < Lines.Count; i++) {
                if (Lines[i].Kind == ChangeKind.Unchanged) { continue; }
                int start = Math.Max(0, i - Context);
                int end = (int)Math.Min(Lines.Count, (long)i + Context + 1);
                if (ranges.Count > 0 && start <= ranges[ranges.Count - 1].End) {
                    start = ranges[ranges.Count - 1].Start;
                    ranges[ranges.Count - 1] = (start, end);
                } else {
                    ranges.Add((start, end));
                }
            }
            return ranges;
        }

        private static void Advance(TextDiffLine line, ref int reference, ref int difference) {
            if (line.Kind != ChangeKind.Added) { reference++; }
            if (line.Kind != ChangeKind.Removed) { difference++; }
        }

        private static string Range(int preceding, int count) =>
            (count == 0 ? preceding : preceding + 1).ToString(CultureInfo.InvariantCulture) + "," + count.ToString(CultureInfo.InvariantCulture);

        private static string CleanLabel(string? text, string fallback) =>
            string.IsNullOrEmpty(text) ? fallback : new string(text!.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
    }
}
