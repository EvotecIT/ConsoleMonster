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

#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
            "Trimming", "IL2075",
            Justification = "This fallback is selected only when UseReflection is true. Values callers must preserve reflected members when trimming, or use explicit adapters with UseReflection=false; ObjectInspector entry points also carry RequiresUnreferencedCode."
        )]
#endif
        public IEnumerable<InspectMember> GetMembers(object value) {
            PropertyInfo[] properties = PropertyOrder.Sort(value
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetIndexParameters().Length == 0 && p.GetGetMethod(false) != null)
                .ToArray());
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
