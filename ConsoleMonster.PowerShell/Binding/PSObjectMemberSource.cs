using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Data;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>Exposes PowerShell note and adapted properties to the shared object inspector.</summary>
    public sealed class PSObjectMemberSource : IMemberSource {
        /// <summary>
        /// Removes a PowerShell wrapper unless it represents a PSCustomObject whose note properties must remain visible.
        /// </summary>
        /// <param name="value">The value that may be wrapped in a PSObject.</param>
        /// <returns>The base object for wrapped values, or the PSObject for a PSCustomObject.</returns>
        public static object UnwrapPSObject(object value) {
            if (value is PSObject item && !(item.BaseObject is PSCustomObject))
                return item.BaseObject ?? value;
            return value;
        }

        /// <inheritdoc />
        public bool CanRead(object value) {
            if (!(value is PSObject item))
                return false;
            return item.BaseObject is PSCustomObject
                || PropertyBinder.GetDefaultDisplayForSource(item) != null;
        }

        /// <inheritdoc />
        public string GetTypeName(object value) {
            PSObject item = AsPSObject(value);
            string typeName =
                item.TypeNames.Count > 0
                    ? item.TypeNames[0]
                    : item.BaseObject.GetType().FullName ?? item.BaseObject.GetType().Name;
            if (
                string.Equals(
                    typeName,
                    "System.Management.Automation.PSCustomObject",
                    StringComparison.Ordinal
                )
            )
                return "PSCustomObject";
            return typeName.StartsWith("System.", StringComparison.Ordinal)
                ? typeName.Substring("System.".Length)
                : typeName;
        }

        /// <inheritdoc />
        public IEnumerable<InspectMember> GetMembers(object value) {
            PSObject item = AsPSObject(value);
            PSPropertySet? display = PropertyBinder.GetDefaultDisplayForSource(item);
            HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (display != null) {
                foreach (string name in display.ReferencedPropertyNames) {
                    PSPropertyInfo? property = item.Properties[name];
                    if (property != null && property.IsGettable && added.Add(property.Name))
                        yield return new InspectMember(property.Name, () => property.Value);
                }
            }
            foreach (PSPropertyInfo property in item.Properties) {
                if (
                    !property.IsGettable
                    || string.Equals(
                        property.Name,
                        "PSStandardMembers",
                        StringComparison.OrdinalIgnoreCase
                    )
                    || !added.Add(property.Name)
                )
                    continue;
                PSPropertyInfo captured = property;
                yield return new InspectMember(captured.Name, () => captured.Value);
            }
        }

        private static PSObject AsPSObject(object value) =>
            value as PSObject ?? PSObject.AsPSObject(value);
    }
}
