using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Reads -KeyBinding: a dictionary of keys to <c>'label', { action }</c> (use <c>[ordered]</c> to keep the footer
    /// order), or KeyBinding objects. <c>'1-12'</c> is the numbered-item helper; its action receives the number.
    /// </summary>
    internal static class PSKeyBindings {
        private static readonly ConditionalWeakTable<KeyBinding, PSKeyAction> Actions = new ConditionalWeakTable<KeyBinding, PSKeyAction>();
        private static readonly Regex Range = new Regex(@"^\s*1\s*-\s*(\d+)\s*$", RegexOptions.CultureInvariant);

        internal static List<KeyBinding> Read(object? value) {
            List<KeyBinding> bindings = new List<KeyBinding>();
            object? raw = PSText.Unwrap(value);
            if (raw == null) { return bindings; }
            if (raw is IDictionary dictionary) {
                IEnumerable<DictionaryEntry> entries = dictionary.Cast<DictionaryEntry>();
                // A plain hashtable has no order; sort it so the footer is the same on every run.
                if (dictionary is Hashtable) { entries = entries.OrderBy(entry => PSWidgets.ToText(entry.Key), StringComparer.Ordinal); }
                foreach (DictionaryEntry entry in entries) { bindings.Add(Create(PSWidgets.ToText(entry.Key), entry.Value)); }
                return bindings;
            }
            foreach (object item in PSItems.Items(raw)) {
                if (PSText.Unwrap(item) is KeyBinding binding) { bindings.Add(binding); }
                else { throw new PSArgumentException("KeyBinding takes a dictionary of keys to 'label', { action } or KeyBinding objects."); }
            }
            return bindings;
        }

        internal static PSKeyAction? Action(KeyBinding binding) => Actions.TryGetValue(binding, out PSKeyAction? action) ? action : null;

        private static KeyBinding Create(string keys, object? value) {
            object?[] parts = PSText.Unwrap(value) is object[] array ? array : new[] { value };
            if (parts.Length != 2 || !(PSText.Unwrap(parts[1]) is ScriptBlock script)) {
                throw new PSArgumentException("Key '" + keys + "' needs a label and an action: '" + keys + "' = 'label', { ... }.");
            }
            string label = PSWidgets.ToText(parts[0]);
            PSKeyAction action = new PSKeyAction(new PSAppScript(script));
            Match range = Range.Match(keys);
            KeyBinding binding;
            if (range.Success) {
                int count = int.Parse(range.Groups[1].Value, CultureInfo.InvariantCulture);
                binding = KeyBinding.Numbered(label, () => count, number => action.Run(number));
            } else {
                try { binding = KeyBinding.Create(keys, label, () => action.Run(null)); }
                catch (FormatException error) { throw new PSArgumentException(error.Message, error); }
                if (binding.Chords.Count == 0) { throw new PSArgumentException("Key '" + keys + "' names no key. Separate several keys with commas, such as 'Escape, Backspace'."); }
            }
            Actions.Add(binding, action);
            return binding;
        }
    }

    /// <summary>A key binding's script, run on the app's input thread with $State (and the number for numbered keys).</summary>
    internal sealed class PSKeyAction {
        internal PSKeyAction(PSAppScript script) { Script = script; }
        internal PSAppScript Script { get; }
        internal AppState? State { get; set; }
        internal void Run(object? value) => Script.Invoke(State ?? throw new InvalidOperationException(PSAppMessages.ActiveInputThread), null, value);
    }
}
