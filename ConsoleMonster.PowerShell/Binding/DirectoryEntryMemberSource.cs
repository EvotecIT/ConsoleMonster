using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Data;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>
    /// ADSI directory entries ([adsi], System.DirectoryServices.DirectoryEntry): a few fields read one by one, instead of
    /// PowerShell's adapter, whose property listing binds to the directory first (minutes when the directory cannot be
    /// reached). Each read is guarded by the shared stall budget like any other getter.
    /// </summary>
    public sealed class DirectoryEntryMemberSource : IMemberSource {
        private const string TypeName = "System.DirectoryServices.DirectoryEntry";
        private static readonly string[] Names = { "Path", "Name", "SchemaClassName", "Guid" };

        /// <inheritdoc />
        public bool CanRead(object value) {
            object? raw = value is PSObject item ? item.BaseObject : value;
            return raw != null && string.Equals(raw.GetType().FullName, TypeName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public string GetTypeName(object value) => "DirectoryEntry";

        /// <inheritdoc />
        public IEnumerable<InspectMember> GetMembers(object value) {
            PSObject item = value as PSObject ?? PSObject.AsPSObject(value);
            object raw = item.BaseObject;
            foreach (string name in Names) {
                string captured = name;
                // The .NET property itself (not PowerShell's adapted LDAP attribute of the same name).
                yield return new InspectMember(captured, () => raw.GetType().GetProperty(captured)?.GetValue(raw, null));
            }
        }
    }
}
