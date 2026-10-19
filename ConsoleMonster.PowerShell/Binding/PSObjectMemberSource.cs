using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Values;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>
    /// Reads PowerShell objects as records: their PowerShell properties (note, script, alias and code properties, CIM and
    /// adapted properties), the default display property set first. Nested objects that have a default display property set
    /// show only that set; the root object shows all its properties. Plumbing members are left out by the shared value rules.
    /// </summary>
    public sealed class PSObjectMemberSource : IMemberSource {
        /// <summary>The object an inspection starts from (shows all members), or null when every object shows all members.</summary>
        public object? Root { get; set; }

        /// <summary>
        /// The value to read for a PowerShell object: the wrapped object when it is a scalar, a collection or a data value
        /// (dictionary, data row, XML, JSON, file or folder), so the shared adapters read it; the PowerShell object itself
        /// otherwise, so its PowerShell properties are read.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The value to read.</returns>
        public static object UnwrapPSObject(object value) {
            if (!(value is PSObject item)) {
                return value;
            }
            object? baseObject = item.BaseObject;
            if (baseObject == null || baseObject is PSCustomObject) {
                return value;
            }
            if (ValueShaper.IsScalar(baseObject) || ValueShaper.IsStructural(baseObject) || ValueShaper.TryGetItems(baseObject, out _)) {
                return baseObject;
            }
            return value;
        }

        /// <summary>The identity of a value for cycle detection: the object a PowerShell object wraps.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The identity.</returns>
        public static object Identity(object value) => value is PSObject item && item.BaseObject != null && !(item.BaseObject is PSCustomObject) ? item.BaseObject : value;

        /// <inheritdoc />
        public bool CanRead(object value) => value is PSObject;

        /// <inheritdoc />
        public string GetTypeName(object value) {
            PSObject item = AsPSObject(value);
            string typeName =
                item.TypeNames.Count > 0
                    ? item.TypeNames[0]
                    : item.BaseObject.GetType().FullName ?? item.BaseObject.GetType().Name;
            if (string.Equals(typeName, "System.Management.Automation.PSCustomObject", StringComparison.Ordinal)) {
                return "PSCustomObject";
            }
            int hash = typeName.LastIndexOf('#');
            if (hash >= 0 && hash < typeName.Length - 1) {
                // CIM classes: Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetIPAddress -> MSFT_NetIPAddress
                string cim = typeName.Substring(hash + 1);
                int slash = cim.LastIndexOf('/');
                return slash >= 0 ? cim.Substring(slash + 1) : cim;
            }
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
                    if (property != null && property.IsGettable && added.Add(property.Name)) {
                        PSPropertyInfo captured = property;
                        yield return new InspectMember(captured.Name, () => Nested(captured.Value));
                    }
                }
                if (Root != null && !ReferenceEquals(Identity(item), Identity(Root))) {
                    yield break;
                }
            }
            foreach (PSPropertyInfo property in item.Properties) {
                if (
                    !property.IsGettable
                    || string.Equals(property.Name, "PSStandardMembers", StringComparison.OrdinalIgnoreCase)
                    || !added.Add(property.Name)
                ) {
                    continue;
                }
                PSPropertyInfo captured = property;
                yield return new InspectMember(captured.Name, () => Nested(captured.Value));
            }
        }

        // Nested plain .NET objects are read as PowerShell objects too, so they get their PowerShell properties and the
        // default-display-set rule above (Process.Parent shows Id, Handles, CPU, Name rather than every member).
        private object? Nested(object? value) {
            // Only in an inspection (Root set); tables, lists and comparisons keep the values as they are.
            if (Root == null || value == null || value is PSObject || ValueShaper.IsScalar(value) || ValueShaper.IsStructural(value) || ValueShaper.TryGetItems(value, out _)) {
                return value;
            }
            return PSObject.AsPSObject(value);
        }

        private static PSObject AsPSObject(object value) =>
            value as PSObject ?? PSObject.AsPSObject(value);
    }
}
