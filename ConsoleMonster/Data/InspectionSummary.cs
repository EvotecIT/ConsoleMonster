using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Data {
    /// <summary>Prepares bounded inspection results for concise object views.</summary>
    public static class InspectionSummary {
        /// <summary>Removes empty members and repeated root getter errors, retaining their counts.</summary>
        /// <param name="roots">Already inspected roots.</param>
        /// <param name="hideEmpty">Whether to omit empty members.</param>
        /// <param name="unavailable">Receives getter names unavailable on every root.</param>
        /// <returns>Independent display nodes; the original inspection is unchanged.</returns>
        public static IReadOnlyList<InspectNode> Prepare(IReadOnlyList<InspectNode> roots, bool hideEmpty, out IReadOnlyList<string> unavailable) {
            var repeated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (roots.Count > 1 && roots.All(root => root is InspectObject)) {
                foreach (InspectError error in ((InspectObject)roots[0]).Members.OfType<InspectError>()) {
                    if (error.Name != null && roots.All(root => ((InspectObject)root).Members.OfType<InspectError>().Any(other => string.Equals(other.Name, error.Name, StringComparison.OrdinalIgnoreCase)))) {
                        repeated.Add(error.Name);
                    }
                }
            }
            unavailable = repeated.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
            return roots.Select(root => Normalize(root, hideEmpty, repeated, true)).ToArray();
        }

        private static InspectNode Normalize(InspectNode node, bool hideEmpty, ISet<string> repeated, bool root) {
            if (node is InspectError error) {
                bool denied = error.Message.IndexOf("access is denied", StringComparison.OrdinalIgnoreCase) >= 0
                    || error.Message.IndexOf("access denied", StringComparison.OrdinalIgnoreCase) >= 0
                    || error.Message.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0;
                return new InspectScalar(error.Name, "String", error.Path, denied ? "⊘ access denied" : "⊘ unavailable");
            }
            if (node is InspectCollection collection) {
                return new InspectCollection(collection.Name, collection.TypeName, collection.Path,
                    collection.Items.Select(item => Normalize(item, hideEmpty, repeated, false)).ToArray(),
                    collection.Count, collection.Omitted, collection.Columns, collection.HiddenColumns);
            }
            if (!(node is InspectObject record)) { return node; }
            var members = new List<InspectNode>();
            int empty = 0;
            foreach (InspectNode member in record.Members) {
                if (root && member is InspectError && member.Name != null && repeated.Contains(member.Name)) { continue; }
                if (hideEmpty && IsEmpty(member)) { empty++; continue; }
                members.Add(Normalize(member, hideEmpty, repeated, false));
            }
            if (empty > 0) { members.Add(new InspectScalar("+" + empty + " empty", "String", record.Path, string.Empty)); }
            return new InspectObject(record.Name, record.TypeName, record.Path, members);
        }

        private static bool IsEmpty(InspectNode node) => node is InspectScalar scalar && (scalar.Value == null || scalar.Value is string text && text.Length == 0)
            || node is InspectCollection collection && collection.Count == 0;
    }
}
