using System;
using System.Collections;
using System.Collections.Generic;
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
                    // A RowTypeName definition is shown through its rows: the source types expand, the row type owns the columns.
                    ScriptBlock? rows = data["Rows"] as ScriptBlock;
                    List<object> types = new List<object>();
                    foreach (object type in NativeFormatDefinition.Sequence(data["TypeName"])) { types.Add(type); }
                    if (data["RowTypeName"] is string rowType) { types.Add(rowType); }
                    foreach (object type in types) {
                        // Source types own no columns: an object whose rows cannot be read keeps PowerShell's own columns.
                        bool source = rows != null && !Equals(type, data["RowTypeName"]);
                        AddBuiltIn(ConsoleViewRegistration.Define((string)type, ConsoleViewKind.Table,
                            source ? null : (object[])data["Property"]!, data["GroupBy"], (string)data["Group"]!, source ? null : data["Title"] as ScriptBlock, true, true,
                            data["GroupHeadersAsRows"] is bool section && section,
                            data["GroupMinimumRows"] is int minimum ? minimum : 0,
                            data["GroupRepeatedOnly"] is bool repeated && repeated,
                            data["StructuredData"] is bool structured && structured,
                            source ? rows : null,
                            data["SingleRecord"] is bool record && record,
                            data["HideEmptyColumns"] is bool hide && hide));
                    }
                }
            }
        }

        /// <summary>Adds the trusted summary scripts (the facts line under a view) to the built-in views they name.</summary>
        private static void AttachEverydaySummaries() {
            using (Stream stream = typeof(ConsoleFormatting).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Formatting.EverydaySummaries.ps1")!)
            using (StreamReader reader = new StreamReader(stream)) {
                foreach (PSObject item in ScriptBlock.Create(reader.ReadToEnd()).Invoke()) {
                    IDictionary data = (IDictionary)item.BaseObject;
                    ScriptBlock summary = ScriptBlock.Create(((ScriptBlock)data["Summary"]!).ToString());
                    foreach (object type in NativeFormatDefinition.Sequence(data["TypeName"])) {
                        lock (Gate) {
                            if (BuiltIns.TryGetValue((string)type, out ConsoleViewRegistration? registration)) { registration.AttachSummary(summary); }
                        }
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
