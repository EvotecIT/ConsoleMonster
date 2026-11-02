using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConsoleMonster.Changes {
    public static partial class ObjectDiff {
        /// <summary>Compares two or more objects as property rows against a reference column.</summary>
        /// <param name="objects">Objects in column order; null represents an object with no properties.</param>
        /// <param name="options">Shared path, depth, normalization and equality options.</param>
        /// <param name="names">Explicit column names, or null to use the key selector and default key properties.</param>
        /// <param name="baselineIndex">The zero-based reference column, default zero.</param>
        /// <param name="baselineName">A unique column name instead of an index, using the configured case rule.</param>
        /// <returns>The structured comparison for <see cref="BaselineView"/>.</returns>
        public static BaselineComparison CompareBaseline(IEnumerable<object?> objects, DiffOptions? options = null, IReadOnlyList<string>? names = null, int baselineIndex = 0, string? baselineName = null) {
            if (objects == null) { throw new ArgumentNullException(nameof(objects)); }
            object?[] items = objects.ToArray();
            if (items.Length < 2) { throw new ArgumentException("A baseline comparison needs at least two objects.", nameof(objects)); }
            if (baselineIndex < 0 || baselineIndex >= items.Length) { throw new ArgumentOutOfRangeException(nameof(baselineIndex)); }
            if (names != null && (names.Count != items.Length || names.Any(string.IsNullOrWhiteSpace))) {
                throw new ArgumentException("Supply one nonempty name per object.", nameof(names));
            }
            options ??= new DiffOptions();
            options.Budget = new Values.ShapeBudget(options.TimeBudget, int.MaxValue);
            options.Shape = null;
            options.NotCompared.Clear();
            string[] headings = names?.ToArray() ?? items.Select((item, i) => item == null ? "#" + (i + 1).ToString(CultureInfo.InvariantCulture) : KeyOf(item, options, i)).ToArray();
            if (baselineName != null) {
                int[] matches = headings.Select((name, i) => new { name, i })
                    .Where(pair => string.Equals(pair.name, baselineName, options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.i).ToArray();
                if (matches.Length != 1) { throw new ArgumentException("Baseline must name exactly one column; use an index for duplicate names.", nameof(baselineName)); }
                baselineIndex = matches[0];
            }
            var rows = new List<BaselineRow>();
            BaselineInto(rows, string.Empty, items, items.Select(i => i != null).ToArray(), options, baselineIndex, 0);
            BaselineRow[] shown = options.IncludeUnchanged ? rows.ToArray() : rows.Where(row => row.HasDifferences || row.HasCaseDifferences).ToArray();
            return new BaselineComparison(headings, baselineIndex, shown, options.NotCompared.ToArray(), rows.Count - shown.Length);
        }

        private static void BaselineInto(List<BaselineRow> rows, string path, object?[] values, bool[] present, DiffOptions options, int baseline, int depth) {
            for (int i = 0; i < values.Length; i++) {
                if (values[i] != null) { values[i] = options.Unwrap?.Invoke(values[i]!) ?? values[i]; }
            }
            if (values.Any(v => v is Values.BudgetExceeded)) {
                string skipped = path.Length == 0 ? "(value)" : path;
                if (!options.NotCompared.Contains(skipped)) { options.NotCompared.Add(skipped); }
                return;
            }
            bool terminal = depth >= options.Depth || values.All(IsLeaf) || values.Any(IsCollection) ||
                (path.Length > 0 && options.Normalize.ContainsKey(path));
            Dictionary<string, object?>[]? members = terminal ? null : values.Select(v => Members(v, options)).ToArray();
            // An empty record has no child paths, but its presence must still be compared.
            terminal = terminal || members!.All(m => m.Count == 0);
            if (terminal) {
                var cells = new List<BaselineCell>();
                for (int i = 0; i < values.Length; i++) {
                    var differences = new List<PropertyChange>();
                    if (i != baseline && (present[baseline] || present[i])) {
                        CompareInto(differences, path, values[baseline], values[i], options, depth, present[baseline], present[i]);
                    }
                    ChangeKind kind = differences.Any(p => !p.IsCaseOnly)
                        ? !present[i] ? ChangeKind.Removed : !present[baseline] ? ChangeKind.Added : ChangeKind.Changed
                        : ChangeKind.Unchanged;
                    cells.Add(new BaselineCell(values[i], present[i], kind, differences.Any(p => p.IsCaseOnly)));
                }
                var row = new BaselineRow(path.Length == 0 ? "(value)" : path, cells);
                rows.Add(row);
                return;
            }
            Dictionary<string, object?>[] fields = members!;
            IEnumerable<string> names = fields.SelectMany(m => m.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (string name in names) {
                string child = path.Length == 0 ? name : path + "." + name;
                if (depth == 0 && options.Properties.Count > 0 && !options.Properties.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase))) { continue; }
                if (Ignored(child, name, options)) { continue; }
                var children = new object?[values.Length];
                var exists = new bool[values.Length];
                for (int i = 0; i < values.Length; i++) { exists[i] = fields[i].TryGetValue(name, out children[i]); }
                BaselineInto(rows, child, children, exists, options, baseline, depth + 1);
            }
        }
    }
}
