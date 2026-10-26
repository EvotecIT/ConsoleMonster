using System;
using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    internal static class PSFlowItems {
        internal static bool TryAdd(Flow flow, object item, out string? problem) {
            problem = null;
            try {
                if (item is FlowNode node) { flow.Add(node); return true; }
                if (item is FlowEdge edge) { flow.Add(edge); return true; }
                if (item is FlowBranch branch) { flow.Branch = branch; return true; }
                object? Field(string name) {
                    if (item is IDictionary dictionary) {
                        foreach (DictionaryEntry entry in dictionary) if (string.Equals(PSWidgets.ToText(entry.Key), name, StringComparison.OrdinalIgnoreCase)) return PSText.Unwrap(entry.Value);
                        return null;
                    }
                    return PSText.Unwrap(PSObject.AsPSObject(item).Properties[name]?.Value);
                }
                string Text(string name) => PSWidgets.ToText(Field(name));
                StatusKind? status = null;
                if (Field("Status") is object value) {
                    if (!StatusKinds.TryParse(PSWidgets.ToText(value), out StatusKind parsed)) { problem = "Unknown flow status: " + value; return false; }
                    status = parsed;
                }
                string kind = Text("Kind").ToLowerInvariant();
                PSItems.RecordFields(item, kind == "node" ? "Kind Key Label Status" : kind == "edge" ? "Kind Key From To Label Status Cut" : "Kind From To Label Status");
                switch (kind) {
                    case "node": flow.Add(new FlowNode(Text("Key"), Text("Label"), status)); return true;
                    case "edge": flow.Add(new FlowEdge(Text("Key"), Text("From"), Text("To"), Text("Label"), status, LanguagePrimitives.IsTrue(Field("Cut")))); return true;
                    case "branch": flow.Branch = new FlowBranch(Text("From"), Text("Label"), Field("To") == null ? null : Text("To"), status); return true;
                    default: problem = "A flow row needs Kind = Node, Edge or Branch."; return false;
                }
            } catch (ArgumentException exception) { problem = exception.Message; return false; }
        }
    }
}
