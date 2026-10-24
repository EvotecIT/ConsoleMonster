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
                        yield return new KeyHint(PSWidgets.ToText(entry.Key), PSWidgets.ToText(entry.Value));
                    }
                } else { throw new PSArgumentException("FooterHints must contain key/label dictionaries or KeyHint values."); }
            }
        }
    }
}
