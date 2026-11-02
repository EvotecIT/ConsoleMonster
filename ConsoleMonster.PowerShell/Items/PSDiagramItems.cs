using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Collects typed diagram content and maps pipeline column selectors to the core model.</summary>
    internal static class PSDiagramItems {
        internal static void Add(Diagram diagram, object item) {
            object value = PSText.Unwrap(item)!;
            switch (value) {
                case DiagramNode node: diagram.Nodes.Add(node); break;
                case DiagramLink link: diagram.Links.Add(link); break;
                case DiagramGroup group: diagram.Groups.Add(group); break;
                case DiagramUseCase useCase: diagram.UseCases.Add(useCase); break;
                default: throw new ArgumentException("Diagram content must emit nodes, links, groups or use cases; use the pipeline for connection rows.");
            }
        }
        internal static object? Select(object row, object? selector, string defaultColumn) {
            object? selected = PSText.Unwrap(selector);
            if (selected is ScriptBlock script) {
                var variables = new List<PSVariable> { new PSVariable("_", row), new PSVariable("PSItem", row) };
                var values = script.InvokeWithContext(null, variables, row);
                if (values.Count > 1) throw new ArgumentException("A diagram selector must return one value per row. Expand multiple connections into separate rows.");
                return values.Count == 0 ? null : PSText.Unwrap(values[0]);
            }
            string name = selected == null ? defaultColumn : PSWidgets.ToText(selected);
            object source = PSText.Unwrap(row)!;
            if (source is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) if (string.Equals(PSWidgets.ToText(entry.Key), name, StringComparison.OrdinalIgnoreCase)) return PSText.Unwrap(entry.Value);
                return null;
            }
            return PSText.Unwrap(PSObject.AsPSObject(row).Properties[name]?.Value);
        }
        internal static void Rows(Diagram diagram, IEnumerable<object> rows, object? from, object? to, object? label, object? status, object? group) {
            foreach (object row in rows) {
                string source = PSWidgets.ToText(Select(row, from, "From")), target = PSWidgets.ToText(Select(row, to, "To"));
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) throw new ArgumentException("A diagram row needs From and To. Select another column or a script block with -From and -To.");
                StatusKind health = StatusKind.Pass;
                object? value = Select(row, status, "Status");
                if (value != null && !StatusKinds.TryParse(PSWidgets.ToText(value), out health)) throw new ArgumentException("Unknown diagram status: " + value);
                diagram.Links.Add(new DiagramLink(source, target) { Label = PSWidgets.ToText(Select(row, label, "Label")), Status = health });
                string name = PSWidgets.ToText(Select(row, group, "Group"));
                if (name.Length > 0 && !diagram.AllNodes().Any(n => n.Id == source)) {
                    DiagramGroup? owner = diagram.Groups.FirstOrDefault(g => g.Label == name);
                    if (owner == null) { owner = new DiagramGroup(name); diagram.Groups.Add(owner); }
                    owner.Nodes.Add(new DiagramNode(source));
                }
            }
            diagram.Resolve();
        }
    }
}
