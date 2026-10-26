using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Translates explicit PowerShell action definitions into isolated, cancellable core commands.</summary>
    internal static class PSCommandActions {
        internal static AppCommand Create(string key, string label, string source, object[] arguments, string[] modules) {
            // Capture on the caller's runspace. Workers receive serialized values, never the caller's PSObject graph.
            string argumentData = PSSerializer.Serialize(arguments, 3);
            string[] imports = modules == null ? Array.Empty<string>() : (string[])modules.Clone();
            PSCommandRunner runner = new PSCommandRunner(source, argumentData, imports);
            AppCommand command = new AppCommand(key, label, runner.InvokeAsync);
            command.CaptureRows = rows => {
                List<object?> snapshots = new List<object?>();
                foreach (object? row in rows) { snapshots.Add(new RowData(PSSerializer.Serialize(row, 3))); }
                return snapshots.AsReadOnly();
            };
            return command;
        }

        internal static IEnumerable<AppCommand> Collect(object[]? definitions) {
            if (definitions == null) { yield break; }
            foreach (object definition in definitions) {
                object value = PSObject.AsPSObject(definition).BaseObject;
                if (value is AppCommand command) { yield return command; }
                else if (value is IDictionary dictionary) {
                    List<string> keys = new List<string>();
                    foreach (object key in dictionary.Keys) {
                        if (!(key is string text) || string.IsNullOrWhiteSpace(text)) { throw new ArgumentException("Action keys must be nonempty strings."); }
                        keys.Add(text);
                    }
                    keys.Sort(StringComparer.OrdinalIgnoreCase);
                    foreach (string key in keys) {
                        if (!(dictionary[key] is ScriptBlock script)) { throw new ArgumentException("Each action value must be a script block. Use New-ConsoleCommand for confirmation, arguments or modules."); }
                        AppCommand shorthand = Create(key, key, script.ToString(), Array.Empty<object>(), Array.Empty<string>());
                        shorthand.ConfirmPrompt = key + " the selected rows?";
                        yield return shorthand;
                    }
                } else { throw new ArgumentException("Actions must be command objects or a hashtable of script blocks."); }
            }
        }

        internal sealed class RowData {
            internal RowData(string xml) { Xml = xml; }
            internal string Xml { get; }
        }
    }
}
