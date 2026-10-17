using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Helpers shared by the menu cmdlets.</summary>
    internal static class PSMenus {
        public static List<MenuItem> CollectItems(ScriptBlock? content) {
            List<MenuItem> items = new List<MenuItem>();
            if (content == null) {
                return items;
            }
            foreach (PSObject output in content.Invoke()) {
                if (output?.BaseObject is MenuItem item) {
                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>Runs an action scriptblock with $MenuContext (and $_) set; extra variables are added as given.</summary>
        public static void Invoke(ScriptBlock script, MenuContext context, params KeyValuePair<string, object?>[] variables) {
            List<KeyValuePair<string, object?>> all = new List<KeyValuePair<string, object?>> { new KeyValuePair<string, object?>("MenuContext", context) };
            all.AddRange(variables);
            PSInteractive.InvokeWith(script, context, all.ToArray());
        }
    }
}
