using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    // PowerShell maps dictionaries and typed hints onto the existing core hint model.
    internal static class PSFooterHints {
        internal static IEnumerable<KeyHint> Read(object? input) {
            foreach (object item in PSItems.Items(input)) {
                if (item is KeyHint hint) { yield return hint; }
                else if (item is IDictionary dictionary) {
                    foreach (DictionaryEntry entry in dictionary) {
                        // The key text names a key too (F5, Ctrl+R), so a click on the hint in an app presses it.
                        string keys = PSWidgets.ToText(entry.Key);
                        yield return Input.KeyChord.TryParse(keys, out Input.KeyChord chord)
                            ? new KeyHint(keys, PSWidgets.ToText(entry.Value), chord)
                            : new KeyHint(keys, PSWidgets.ToText(entry.Value));
                    }
                } else { throw new PSArgumentException("FooterHints must contain key/label dictionaries or KeyHint values."); }
            }
        }
    }
}
