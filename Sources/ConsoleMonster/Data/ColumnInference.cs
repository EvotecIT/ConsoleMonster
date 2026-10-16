using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ConsoleMonster.Data {
    /// <summary>Infers whether a sequence has record-like items and which columns to display.</summary>
    public static class ColumnInference {
#if NET8_0_OR_GREATER
        /// <summary>Returns the ordered union of member names for tabular items, or an empty list for non-tabular items.</summary>
        /// <remarks>Reflection based inference is not trim safe. Trimmed or AOT applications should provide an <see cref="IMemberSource"/> or disable reflection.</remarks>
        [RequiresUnreferencedCode(
            "Column inference may inspect public properties. Supply an IMemberSource or set UseReflection=false for trimmed or AOT applications."
        )]
#else
        /// <summary>Returns the ordered union of member names for tabular items, or an empty list for non-tabular items.</summary>
#endif
        public static IReadOnlyList<string> Infer(
            IEnumerable items,
            InspectorOptions options,
            out int hiddenColumns
        ) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            hiddenColumns = 0;
            List<string> names = new List<string>();
            IEnumerator iterator = items.GetEnumerator();
            int inspected = 0;
            try {
                while (inspected < Math.Max(0, options.MaxItems) && iterator.MoveNext()) {
                    inspected++;
                    object? item = ObjectInspector.UnwrapValue(iterator.Current, options);
                    if (item == null)
                        continue;
                    if (IsScalar(item))
                        return Array.Empty<string>();
                    IMemberSource? source = ObjectInspector.FindSource(item, options);
                    if (source == null)
                        return Array.Empty<string>();
                    foreach (InspectMember member in source.GetMembers(item)) {
                        if (!ObjectInspector.MemberIncluded(member.Name, options))
                            continue;
                        if (
                            !names.Any(name =>
                                string.Equals(name, member.Name, StringComparison.OrdinalIgnoreCase)
                            )
                        )
                            names.Add(member.Name);
                    }
                }
            }
            finally {
                (iterator as IDisposable)?.Dispose();
            }
            if (names.Count == 0)
                return Array.Empty<string>();
            ObjectInspector.ApplyOrder(names, options.Order);
            hiddenColumns = Math.Max(0, names.Count - Math.Max(0, options.MaxColumns));
            if (names.Count > options.MaxColumns)
                names.RemoveRange(
                    Math.Max(0, options.MaxColumns),
                    names.Count - Math.Max(0, options.MaxColumns)
                );
            return names.AsReadOnly();
        }

        /// <summary>Returns whether a value is one of the inspector's supported scalar types.</summary>
        public static bool IsScalar(object? value) => ObjectInspector.IsScalarValue(value);
    }
}
