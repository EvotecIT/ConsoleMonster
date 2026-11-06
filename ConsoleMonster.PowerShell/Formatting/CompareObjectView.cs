using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Changes;
using ConsoleMonster.PowerShell.Binding;

namespace ConsoleMonster.PowerShell {
    // Compare-Object results (InputObject or the -Property columns, then SideIndicator) drawn as Compare-ConsoleObject's
    // ChangeView: "<=" is only in the reference set (removed), "=>" only in the difference set (added), "==" in both.
    internal static class CompareObjectView {
        private const string Indicator = "SideIndicator";

        /// <summary>The batching shape of a Compare-Object result (its property names), or null for any other object.</summary>
        internal static string? Shape(PSObject item) {
            if (!(item.BaseObject is PSCustomObject)) { return null; }
            List<string> names = new List<string>();
            PSPropertyInfo? last = null;
            foreach (PSPropertyInfo property in item.Properties) {
                if (!(property is PSNoteProperty)) { return null; }
                names.Add(property.Name);
                last = property;
            }
            if (names.Count < 2 || last == null || !string.Equals(last.Name, Indicator, StringComparison.OrdinalIgnoreCase) || Kind(last.Value) == null) {
                return null;
            }
            return string.Join("\n", names);
        }

        /// <summary>The comparison view of one run of Compare-Object results sharing a shape.</summary>
        internal static ChangeView Build(IReadOnlyList<PSObject> items) {
            List<string> names = items[0].Properties.Select(p => p.Name).ToList();
            bool wrapped = names.Count == 2 && string.Equals(names[0], "InputObject", StringComparison.OrdinalIgnoreCase);
            // -Property results: the first requested property names the item, the others describe it.
            DiffOptions options = PSChanges.Options(wrapped ? null : new[] { names[0] }, null, new[] { Indicator }, 3, true, true, null);
            IEnumerable<KeyValuePair<ChangeKind, object>> sides = items.Select(item => new KeyValuePair<ChangeKind, object>(
                Kind(item.Properties[Indicator].Value) ?? ChangeKind.Unchanged,
                wrapped ? PSObjectMemberSource.UnwrapPSObject(item.Properties["InputObject"].Value) ?? string.Empty : item));
            return new ChangeView(ObjectDiff.FromSides(sides, options));
        }

        private static ChangeKind? Kind(object? indicator) {
            switch (indicator as string) {
                case "<=": return ChangeKind.Removed;
                case "=>": return ChangeKind.Added;
                case "==": return ChangeKind.Unchanged;
                default: return null;
            }
        }
    }
}
