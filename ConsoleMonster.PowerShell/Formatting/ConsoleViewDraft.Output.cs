using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;

namespace ConsoleMonster.PowerShell {
    internal sealed partial class ConsoleViewDraft {
        internal string Script() {
            List<string> lines = Header();
            for (int i = 0; i < Property.Length; i++) {
                lines.Add("$column" + (i + 1) + " = @{");
                AddColumn(lines, (Hashtable)Property[i], "    ");
                lines.Add("}");
            }
            lines.Add("");
            lines.Add("Register-ConsoleView `");
            lines.Add("    -TypeName " + Quote(_typeName) + " `");
            lines.Add("    -Property " + string.Join(", ", Enumerable.Range(1, Property.Length).Select(i => "$column" + i)));
            lines.Add("");
            lines.Add("# To add the summary, append this block to the registration above with a backtick.");
            lines.Add("# -Summary {");
            lines.Add("#     param($items)");
            lines.Add("#     @{ Value = @($items).Count; Label = 'items' }");
            if (_statusProperty != null) {
                string property = "    $statusProperty = " + Quote(_statusProperty);
                foreach (string line in property.Split('\n')) { lines.Add("# " + line); }
                lines.Add("#     $items | Group-Object -Property $statusProperty | ForEach-Object {");
                lines.Add("#         @{ Value = $_.Count; Label = $_.Name }");
                lines.Add("#     }");
            }
            lines.Add("# }");
            return string.Join("\n", lines);
        }

        internal string DataFile() {
            List<string> lines = Header(true);
            lines.Add("@{");
            lines.Add("    SchemaVersion = 1");
            lines.Add("    Views = @(");
            lines.Add("        @{");
            lines.Add("            TypeName = " + Quote(_typeName, true));
            lines.Add("            Property = @(");
            foreach (Hashtable column in Property) {
                lines.Add("                @{");
                AddColumn(lines, column, "                    ", true);
                lines.Add("                }");
            }
            lines.Add("            )");
            lines.Add("        }");
            lines.Add("    )");
            lines.Add("}");
            return string.Join("\n", lines);
        }

        private List<string> Header(bool dataFile = false) {
            List<string> lines = new List<string>();
            string comment = "Type: " + _typeName.Replace("\r", "\\r").Replace("\n", "\\n");
            for (int i = 0; i < comment.Length; i += 90) { lines.Add("# " + comment.Substring(i, Math.Min(90, comment.Length - i))); }
            lines.Add(dataFile ? "# Keep as ModuleName.consoleviews.psd1 beside the module manifest."
                : "# Keep this script in your profile, or use -AsDataFile for ModuleName.consoleviews.psd1.");
            return lines;
        }

        private static void AddColumn(List<string> lines, Hashtable column, string indent, bool dataFile = false) {
            foreach (string key in new[] { "Name", "Expression", "Presenter" }) {
                if (column[key] is string value) { lines.Add(indent + key + " = " + Quote(value, dataFile)); }
            }
        }

        // Escape interpolation and control characters so member/type names remain data in a pasted script.
        // Data files accept literal strings only; scripts can concatenate long literals across lines.
        private static string Quote(string value, bool dataFile = false) {
            string single = "'" + CodeGeneration.EscapeSingleQuotedStringContent(value) + "'";
            if (single.Length <= 60 && !value.Any(char.IsControl)) { return single; }
            StringBuilder result = new StringBuilder();
            List<string> chunks = new List<string>();
            foreach (char ch in value) {
                string part = ch == '`' ? "``" : ch == '$' ? "`$" : ch == '"' || (ch >= '\u201c' && ch <= '\u201e') ? "`" + ch
                    : ch == '\r' ? "`r" : ch == '\n' ? "`n" : ch == '\t' ? "`t" : ch.ToString();
                if (!dataFile && result.Length + part.Length > 50) {
                    chunks.Add("\"" + result + "\"");
                    result.Clear();
                }
                result.Append(part);
            }
            chunks.Add("\"" + result + "\"");
            return chunks.Count == 1 ? chunks[0] : "(" + string.Join(" +\n", chunks) + ")";
        }
    }
}
