using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>Groups PowerShell items by a property or calculated expression.</summary>
    public static class GroupBinder {
        /// <summary>Returns groups in the order their keys first appear.</summary>
        public static IReadOnlyList<KeyValuePair<string, IReadOnlyList<PSObject>>> Group(
            IReadOnlyList<PSObject> items,
            object groupBy
        ) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (groupBy == null)
                throw new ArgumentNullException(nameof(groupBy));
            List<string> keys = new List<string>();
            Dictionary<string, List<PSObject>> groups = new Dictionary<string, List<PSObject>>(
                StringComparer.OrdinalIgnoreCase
            );
            foreach (PSObject item in items) {
                object? raw = PropertyBinder.Evaluate(item, groupBy);
                string key =
                    raw == null
                        ? "(none)"
                        : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "(none)";
                if (!groups.TryGetValue(key, out List<PSObject>? group)) {
                    group = new List<PSObject>();
                    groups.Add(key, group);
                    keys.Add(key);
                }
                group.Add(item);
            }
            List<KeyValuePair<string, IReadOnlyList<PSObject>>> result =
                new List<KeyValuePair<string, IReadOnlyList<PSObject>>>();
            foreach (string key in keys)
                result.Add(
                    new KeyValuePair<string, IReadOnlyList<PSObject>>(key, groups[key].AsReadOnly())
                );
            return result.AsReadOnly();
        }
    }
}
