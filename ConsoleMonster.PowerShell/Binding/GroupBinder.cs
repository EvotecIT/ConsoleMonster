using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Values;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>Groups PowerShell objects by a property name or script block.</summary>
    internal static class GroupBinder {
        private const string NoKey = "(none)";

        /// <summary>Groups objects in the order their keys first appear (case-insensitive keys).</summary>
        public static IReadOnlyList<KeyValuePair<string, IReadOnlyList<PSObject>>> Group(
            IReadOnlyList<PSObject> items,
            object groupBy
        ) => Group(items, groupBy, false, null);

        /// <summary>
        /// Groups objects by a property name or script block. Keys compare case-insensitively; with <paramref name="sort"/>
        /// the groups are sorted by key (numerically when every key is a number, "(none)" last) and rows keep their order
        /// inside a group, otherwise groups keep the order their keys first appear.
        /// </summary>
        /// <param name="items">The objects.</param>
        /// <param name="groupBy">A property name or a script block.</param>
        /// <param name="sort">Whether to sort the groups by key.</param>
        /// <param name="options">Shape options, or null for PowerShell defaults.</param>
        /// <returns>The groups.</returns>
        internal static IReadOnlyList<KeyValuePair<string, IReadOnlyList<PSObject>>> Group(
            IReadOnlyList<PSObject> items,
            object groupBy,
            bool sort,
            ShapeOptions? options
        ) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (groupBy == null)
                throw new ArgumentNullException(nameof(groupBy));
            ShapeOptions shape = options ?? PSShaping.Options();
            List<string> keys = new List<string>();
            Dictionary<string, List<PSObject>> groups = new Dictionary<string, List<PSObject>>(StringComparer.OrdinalIgnoreCase);
            foreach (PSObject item in items) {
                object? raw;
                try {
                    raw = ValueShaper.Prepare(PropertyBinder.Evaluate(item, groupBy, shape), shape);
                } catch (Exception ex) when (!(ex is PipelineStoppedException) && !(ex is OperationCanceledException)) {
                    raw = null;
                }
                string key = raw == null || raw is FieldError
                    ? NoKey
                    : raw is DateTime date ? date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    : ValueShaper.IsScalar(raw) ? Convert.ToString(raw, CultureInfo.InvariantCulture) ?? NoKey : ValueShaper.Text(raw, shape);
                if (key.Length == 0) {
                    key = NoKey;
                }
                if (!groups.TryGetValue(key, out List<PSObject>? group)) {
                    group = new List<PSObject>();
                    groups.Add(key, group);
                    keys.Add(key);
                }
                group.Add(item);
            }
            if (sort) {
                keys = Sorted(keys);
            }
            List<KeyValuePair<string, IReadOnlyList<PSObject>>> result = new List<KeyValuePair<string, IReadOnlyList<PSObject>>>();
            foreach (string key in keys)
                result.Add(new KeyValuePair<string, IReadOnlyList<PSObject>>(key, groups[key].AsReadOnly()));
            return result.AsReadOnly();
        }

        private static List<string> Sorted(List<string> keys) {
            List<string> named = keys.Where(key => !string.Equals(key, NoKey, StringComparison.Ordinal)).ToList();
            bool numeric = named.Count > 0 && named.All(key => double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out _));
            List<string> sorted = numeric
                ? named.Select((key, index) => new { key, index }).OrderBy(x => double.Parse(x.key, NumberStyles.Float, CultureInfo.InvariantCulture)).ThenBy(x => x.index).Select(x => x.key).ToList()
                : named.Select((key, index) => new { key, index }).OrderBy(x => x.key, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.index).Select(x => x.key).ToList();
            if (keys.Contains(NoKey)) {
                sorted.Add(NoKey);
            }
            return sorted;
        }
    }
}
