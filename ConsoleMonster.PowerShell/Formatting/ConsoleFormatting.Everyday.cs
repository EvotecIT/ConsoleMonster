using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    internal static partial class ConsoleFormatting {
        /// <summary>Loads the trusted built-in view definitions from their native PowerShell source.</summary>
        private static void RegisterEverydayBuiltIns() {
            using (Stream stream = typeof(ConsoleFormatting).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Formatting.EverydayBuiltInViews.ps1")!)
            using (StreamReader reader = new StreamReader(stream)) {
                foreach (PSObject item in ScriptBlock.Create(reader.ReadToEnd()).Invoke()) {
                    IDictionary data = (IDictionary)DetachBuiltInValue(item.BaseObject)!;
                    foreach (object type in NativeFormatDefinition.Sequence(data["TypeName"])) {
                        AddBuiltIn(new ConsoleViewRegistration((string)type, ConsoleViewKind.Table,
                            (object[])data["Property"]!, data["GroupBy"], (string)data["Group"]!, data["Title"] as ScriptBlock, true, true,
                            data["GroupHeadersAsRows"] is bool section && section,
                            data["GroupMinimumRows"] is int minimum ? minimum : 0,
                            data["GroupRepeatedOnly"] is bool repeated && repeated,
                            data["StructuredData"] is bool structured && structured));
                    }
                }
            }
        }

        /// <summary>Detaches trusted callbacks from the initializing runspace before sharing built-in definitions.</summary>
        /// <param name="value">A value from the embedded built-in definition.</param>
        /// <returns>The same literal value or a copy containing only unbound scriptblocks.</returns>
        private static object? DetachBuiltInValue(object? value) {
            if (value is ScriptBlock script) { return ScriptBlock.Create(script.ToString()); }
            if (value is IDictionary dictionary) {
                Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry entry in dictionary) { result[entry.Key] = DetachBuiltInValue(entry.Value); }
                return result;
            }
            if (value is object[] items) {
                object?[] result = new object?[items.Length];
                for (int i = 0; i < items.Length; i++) { result[i] = DetachBuiltInValue(items[i]); }
                return result;
            }
            return value;
        }
    }
}
