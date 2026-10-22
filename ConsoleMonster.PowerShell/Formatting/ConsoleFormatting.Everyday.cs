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
                    IDictionary data = (IDictionary)item.BaseObject;
                    foreach (object type in NativeFormatDefinition.Sequence(data["TypeName"])) {
                        AddBuiltIn(new ConsoleViewRegistration((string)type, ConsoleViewKind.Table,
                            (object[])data["Property"]!, data["GroupBy"], (string)data["Group"]!, null, true, true));
                    }
                }
            }
        }
    }
}
