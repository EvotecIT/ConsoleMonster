using System;
using System.Reflection;

namespace ConsoleMonster.Data {
    /// <summary>Uses declaration order where metadata tokens exist, with stable names for Native AOT.</summary>
    internal static class PropertyOrder {
        internal static PropertyInfo[] Sort(PropertyInfo[] properties) {
            try {
                // Check the entire array first so a tokenless runtime never receives a mixed comparer.
                foreach (PropertyInfo property in properties)
                    _ = property.MetadataToken;
                Array.Sort(properties, (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
            }
            catch (InvalidOperationException) {
                Array.Sort(properties, (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            }
            catch (NotSupportedException) {
                Array.Sort(properties, (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            }
            return properties;
        }
    }
}
