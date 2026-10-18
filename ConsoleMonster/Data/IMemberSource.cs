using System.Collections.Generic;

namespace ConsoleMonster.Data {
    /// <summary>Supplies named members for values understood by an object inspector.</summary>
    public interface IMemberSource {
        /// <summary>Returns whether this source can inspect <paramref name="value"/>.</summary>
        bool CanRead(object value);

        /// <summary>Gets a short friendly type name.</summary>
        string GetTypeName(object value);

        /// <summary>Gets members in their preferred display order.</summary>
        IEnumerable<InspectMember> GetMembers(object value);
    }
}
