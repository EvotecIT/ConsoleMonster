using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConsoleMonster.Data {
    /// <summary>Reads public instance properties without invoking them until each member is read.</summary>
    internal sealed class ReflectionMemberSource : IMemberSource {
        public bool CanRead(object value) => value != null;

        public string GetTypeName(object value) =>
            ObjectInspector.FriendlyTypeName(value.GetType());

        public IEnumerable<InspectMember> GetMembers(object value) {
            PropertyInfo[] properties = value
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetIndexParameters().Length == 0 && p.GetGetMethod(false) != null)
                .OrderBy(p => p.MetadataToken)
                .ToArray();
            foreach (PropertyInfo property in properties) {
                yield return new InspectMember(
                    property.Name,
                    () => {
                        try {
                            return property.GetValue(value, null);
                        }
                        catch (TargetInvocationException ex) {
                            throw ex.InnerException ?? ex;
                        }
                    }
                );
            }
        }
    }
}
