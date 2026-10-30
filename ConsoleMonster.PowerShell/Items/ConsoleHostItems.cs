using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Adapts PowerShell records, typed items and builder script blocks into the core widgets' existing Add/Set operations.</summary>
    public static class ConsoleHostItems {
        /// <summary>Adds collected items using the parent widget record schemas and validation.</summary>
        /// <typeparam name="T">The widget or tree-node type.</typeparam>
        /// <param name="inputObject">The widget to extend.</param>
        /// <param name="item">One item, an item sequence, or an owning-runspace script block.</param>
        /// <param name="keyProperty">Optional stable-key selector for ordinary live-table rows.</param>
        /// <returns>The same widget.</returns>
        public static T Add<T>(T inputObject, object? item, string? keyProperty = null) where T : class {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            foreach (object value in PSItems.Items(item)) {
                if (!PSItems.TryAdd(inputObject, value, keyProperty, out string? problem)) {
                    throw new PSArgumentException(problem ?? ("A " + value.GetType().Name + " cannot be added to a " + inputObject.GetType().Name + ". It takes " + PSItems.Accepted(inputObject) + "."));
                }
            }
            return inputObject;
        }
    }
}
