using System.Collections;
using System.Collections.Generic;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public partial class CmdletNewConsoleTree {
        // Adapt the dictionary definition grammar to the same bounded core hierarchy walker used by object selectors.
        private void Fill(TreeNode parent, IDictionary data) {
            TreeHierarchy.Add(parent, DictionaryEntries(data), DictionaryNode, DictionaryChildren, new TreeHierarchyOptions {
                Depth = System.Math.Max(0, Depth), MaxItems = MaxItems, MaxNodes = MaxNodes,
                Identity = item => item is DictionaryItem entry && IsCollection(entry.Value) ? entry.Value! : item
            });
        }

        private static IEnumerable<DictionaryItem> DictionaryEntries(IDictionary dictionary) {
            foreach (DictionaryEntry entry in dictionary) { yield return new DictionaryItem(PSWidgets.ToText(entry.Key), PSText.Unwrap(entry.Value)); }
        }

        private static TreeNode DictionaryNode(DictionaryItem entry) {
            if (entry.Key == null) { return new TreeNode(Markup.Escape(PSWidgets.ToText(entry.Value))); }
            return new TreeNode(Markup.Escape(entry.Key)) { Note = IsCollection(entry.Value) || entry.Value == null ? null : PSWidgets.ToText(entry.Value) };
        }

        private static IEnumerable<DictionaryItem>? DictionaryChildren(DictionaryItem entry) {
            if (entry.Value is IDictionary dictionary) { return DictionaryEntries(dictionary); }
            return entry.Value is IEnumerable list && !(entry.Value is string) ? DictionaryItems(list) : null;
        }

        private static IEnumerable<DictionaryItem> DictionaryItems(IEnumerable values) {
            foreach (object? item in values) {
                object? value = PSText.Unwrap(item);
                if (value is IDictionary dictionary) {
                    foreach (DictionaryItem entry in DictionaryEntries(dictionary)) { yield return entry; }
                } else { yield return new DictionaryItem(null, value); }
            }
        }

        private static bool IsCollection(object? value) => value is IEnumerable && !(value is string);

        private sealed class DictionaryItem {
            internal DictionaryItem(string? key, object? value) { Key = key; Value = value; }
            internal string? Key { get; }
            internal object? Value { get; }
        }
    }
}
