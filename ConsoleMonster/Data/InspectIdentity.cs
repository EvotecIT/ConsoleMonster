using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Values;

namespace ConsoleMonster.Data {
    /// <summary>How an inspected nested object is labelled: its identity, or its field count instead of a generic type name.</summary>
    internal static class InspectIdentity {
        private static readonly HashSet<string> GenericNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "PSCustomObject", "Hashtable", "OrderedDictionary", "ExpandoObject", "Object", "JsonObject", "JsonElement"
        };

        /// <summary>The label of a nested object.</summary>
        /// <param name="record">The inspected object.</param>
        /// <param name="fieldCount">The text of a field count (<c>3 fields</c>) when the object has no identity and a generic type.</param>
        /// <returns>The identity (<c>daxian-dbw (127450)</c>), null with <paramref name="fieldCount"/> set, or the type name.</returns>
        internal static string? Label(InspectObject record, out string? fieldCount) {
            fieldCount = null;
            // The shared title rule over the object's scalar members, as a record of them.
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (InspectNode member in record.Members) {
                if (member is InspectScalar scalar && scalar.Name != null && scalar.TypeName != "InspectionEmptySummary" && !values.ContainsKey(scalar.Name)) {
                    values[scalar.Name] = scalar.Value;
                }
            }
            var identity = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string title = values.Count == 0 ? string.Empty : ValueShaper.Title(values, null, null, true, identity);
            if (IsIdentity(identity) && title.Length > 0) {
                return title;
            }
            if (!IsGeneric(record.TypeName)) {
                return record.TypeName;
            }
            int count = 0;
            foreach (InspectNode member in record.Members) {
                if (member.TypeName != "InspectionEmptySummary") { count++; }
            }
            fieldCount = count.ToString("N0", CultureInfo.InvariantCulture) + (count == 1 ? " field" : " fields");
            return null;
        }

        /// <summary>
        /// Whether the fields behind a title identify a nested object. A Key alone names a dictionary entry, not the
        /// object (a certificate's Key is its algorithm), so a nested object titled only by it reads by its fields instead.
        /// </summary>
        /// <param name="identity">The fields the shared title rule used.</param>
        /// <returns>True for an identity.</returns>
        internal static bool IsIdentity(ICollection<string> identity) =>
            identity.Count > 0 && !(identity.Count == 1 && identity.Contains("Key"));

        private static bool IsGeneric(string typeName) {
            string name = typeName ?? string.Empty;
            if (name.Length == 0) { return true; }
            int dot = name.LastIndexOf('.', name.IndexOf('<') < 0 ? name.Length - 1 : name.IndexOf('<'));
            if (dot >= 0) { name = name.Substring(dot + 1); }
            return GenericNames.Contains(name)
                || name.StartsWith("Dictionary<", StringComparison.Ordinal) || name.StartsWith("SortedDictionary<", StringComparison.Ordinal)
                || name.StartsWith("ConcurrentDictionary<", StringComparison.Ordinal) || name.StartsWith("IDictionary", StringComparison.Ordinal);
        }
    }
}
