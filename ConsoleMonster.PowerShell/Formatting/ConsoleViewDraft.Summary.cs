using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace ConsoleMonster.PowerShell {
    internal sealed partial class ConsoleViewDraft {
        internal ScriptBlock Summary() {
            string block = SummaryScript();
            return ScriptBlock.Create(block.Substring(1, block.Length - 2));
        }

        private string SummaryScript() {
            List<string> lines = new List<string> {
                "{", "    param($items)",
                "    @{ Value = @($items).Count; Label = " + Quote(CountLabel()) + " }"
            };
            if (_statusProperty != null) {
                lines.AddRange(new[] {
                    "    $roles = @{",
                    "        failed = 'Fail'; error = 'Fail'; stopped = 'Fail'",
                    "        warning = 'Warn'; degraded = 'Warn'",
                    "        completed = 'Pass'; passed = 'Pass'; success = 'Pass'; ok = 'Pass'",
                    "    }"
                });
                if (!Regex.IsMatch(_statusProperty!, @"^[A-Za-z_][A-Za-z0-9_]*$")) {
                    lines.Add("    $items | Group-Object -Property {");
                    lines.Add("        $_.PSObject.Properties[" + Quote(_statusProperty) + "].Value");
                    lines.Add("    } | ForEach-Object {");
                } else { lines.Add("    $items | Group-Object -Property " + Quote(_statusProperty) + " | ForEach-Object {"); }
                lines.AddRange(new[] {
                    "        $label = $_.Name.ToLowerInvariant()",
                    "        $fact = @{ Value = $_.Count; Label = $label }",
                    "        if ($roles.ContainsKey($label)) { $fact.Role = $roles[$label] }",
                    "        $fact",
                    "    }"
                });
            }
            lines.Add("}");
            return string.Join("\n", lines);
        }

        private string CountLabel() {
            string word = _typeName.Split('.').Last().ToLowerInvariant();
            if (!Regex.IsMatch(word, "^[a-z]+$")) { return "items"; }
            if (word.EndsWith("y", StringComparison.Ordinal) && word.Length > 1
                && "aeiou".IndexOf(word[word.Length - 2]) < 0) { return word.Substring(0, word.Length - 1) + "ies"; }
            return word.EndsWith("s", StringComparison.Ordinal) || word.EndsWith("x", StringComparison.Ordinal)
                || word.EndsWith("z", StringComparison.Ordinal) || word.EndsWith("ch", StringComparison.Ordinal)
                || word.EndsWith("sh", StringComparison.Ordinal) ? word + "es" : word + "s";
        }
    }
}
