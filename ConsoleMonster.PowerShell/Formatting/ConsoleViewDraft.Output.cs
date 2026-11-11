using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.PowerShell {
    internal sealed partial class ConsoleViewDraft {
        internal string Script(IEnumerable<string> protectedVariables) {
            List<string> lines = new List<string> {
                "# " + CommentType() + ": keep these lines in your profile; "
                    + "ConvertTo-ConsoleView -AsDataFile makes the module form."
            };
            List<string> properties = new List<string>();
            HashSet<string> variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "summary", "view", "items", "roles", "label", "fact", "statusProperty",
                "_", "psitem", "args", "input", "matches", "lastexitcode", "foreach", "switch"
            };
            variables.UnionWith(protectedVariables);
            foreach (object property in Property) {
                if (property is string name) { properties.Add(Argument(name)); continue; }
                Hashtable column = (Hashtable)property;
                string variable = Variable((string)column["Name"]!, variables);
                properties.Add(variable);
                string declaration = variable + " = " + Column(column);
                if (Fits(declaration)) { lines.Add(declaration); }
                else {
                    lines.Add(variable + " = @{");
                    AddColumn(lines, column, "    ");
                    lines.Add("}");
                }
            }
            lines.Add("");
            lines.Add("$summary = " + SummaryScript());
            lines.Add("# Drop -Summary $summary to leave the line out.");
            string registration = "Register-ConsoleView -TypeName " + Argument(_typeName)
                + " -Property " + string.Join(", ", properties) + " -Summary $summary";
            if (Fits(registration)) { lines.Add(registration); }
            else {
                lines.Add("$view = @{");
                lines.Add("    TypeName = " + Quote(_typeName));
                string propertyLine = "    Property = @(" + string.Join(", ", properties.Select(QuoteArgument)) + ")";
                if (Fits(propertyLine)) { lines.Add(propertyLine); }
                else {
                    lines.Add("    Property = @(");
                    foreach (string property in properties) { lines.Add("        " + QuoteArgument(property)); }
                    lines.Add("    )");
                }
                lines.Add("    Summary = $summary");
                lines.Add("}");
                lines.Add("Register-ConsoleView @view");
            }
            return string.Join("\n", lines);
        }

        internal string DataFile() {
            List<string> lines = new List<string> {
                "# " + CommentType() + ": keep as ModuleName.consoleviews.psd1 beside the module manifest.",
                "@{", "    SchemaVersion = 1", "    Views = @(", "        @{",
                "            TypeName = " + Quote(_typeName, true), "            Property = @("
            };
            foreach (object property in Property) {
                if (property is string name) { lines.Add("                " + Quote(name, true)); continue; }
                Hashtable column = (Hashtable)property;
                string compact = "                " + Column(column, true);
                if (Fits(compact)) { lines.Add(compact); }
                else {
                    lines.Add("                @{");
                    AddColumn(lines, column, "                    ", true);
                    lines.Add("                }");
                }
            }
            lines.AddRange(new[] { "            )", "        }", "    )", "}" });
            return string.Join("\n", lines);
        }

        private string CommentType() => _typeName.Replace("\r", "\\r").Replace("\n", "\\n");

        private static bool Fits(string line) => line.Length <= 100 && line.IndexOf('\n') < 0;

        private static string Argument(string value) => Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_.]*$")
            ? value : Quote(value);

        // Array expressions require string literals, while presenter variables remain references.
        private static string QuoteArgument(string argument) => argument.StartsWith("$", StringComparison.Ordinal)
            ? argument : Regex.IsMatch(argument, @"^[A-Za-z_][A-Za-z0-9_.]*$") ? Quote(argument) : argument;

        private static string Variable(string name, HashSet<string> used) {
            string stem = new string(name.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').Take(32).ToArray());
            if (stem.Length == 0 || char.IsDigit(stem[0])) { stem = "column" + stem; }
            stem = stem == stem.ToUpperInvariant() ? stem.ToLowerInvariant()
                : char.ToLowerInvariant(stem[0]) + stem.Substring(1);
            string candidate = stem;
            if (used.Contains(candidate)) { candidate = stem + "Column"; }
            int suffix = 2;
            string unique = candidate;
            while (!used.Add(unique)) { unique = candidate + suffix++; }
            return "$" + unique;
        }

        private static string Column(Hashtable column, bool dataFile = false) => "@{ "
            + string.Join("; ", new[] { "Name", "Expression", "Presenter" }
                .Where(key => column[key] is string)
                .Select(key => key + " = " + Quote((string)column[key]!, dataFile))) + " }";

        private static void AddColumn(List<string> lines, Hashtable column, string indent, bool dataFile = false) {
            foreach (string key in new[] { "Name", "Expression", "Presenter" }) {
                if (column[key] is string value) { lines.Add(indent + key + " = " + Quote(value, dataFile)); }
            }
        }

        // Keep member/type names literal, including PowerShell's smart quote delimiters.
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
