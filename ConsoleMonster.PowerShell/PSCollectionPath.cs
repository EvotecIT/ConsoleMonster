using System.Collections.Generic;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    // Collection flattening is an input grammar, not object inspection. Reject cycles before recursion can exhaust the host stack.
    internal static class PSCollectionPath {
        internal static void Enter(ref List<object>? path, object collection) {
            path ??= new List<object>();
            if (path.Count >= 128 || path.Exists(item => object.ReferenceEquals(item, collection))) {
                throw new PSArgumentException("A recursive or excessively nested collection cannot be flattened into widget content. Use Show-ConsoleObject to inspect an object graph.");
            }
            path.Add(collection);
        }
    }
}
