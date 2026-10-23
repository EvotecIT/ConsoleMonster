using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Adapts restricted PowerShell data and PSD1 syntax to the core theme-file model.</summary>
    internal static class ConsoleThemeDataFile {
        internal static ThemeFile Load(string path) {
            if (string.Equals(System.IO.Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase)) { return ThemeFile.Load(path); }
            CheckExtension(path);
            using (System.Management.Automation.PowerShell reader = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                // Invoke the data command directly; neither script source nor the file content is executed.
                reader.AddCommand("Microsoft.PowerShell.Utility\\Import-PowerShellDataFile")
                    .AddParameter("LiteralPath", path).AddParameter("ErrorAction", ActionPreference.Stop);
                var result = reader.Invoke();
                if (result.Count != 1 || !(result[0].BaseObject is IDictionary data)) { throw new FormatException("A PSD1 theme must contain one data hashtable."); }
                return ThemeFile.Import(data);
            }
        }

        internal static void Save(ThemeFile document, string path) {
            CheckExtension(path);
            if (string.Equals(System.IO.Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase)) {
                document.Save(path);
                return;
            }
            StringBuilder text = new StringBuilder();
            Write(text, document.ToData(), 0);
            File.WriteAllText(path, text.Append('\n').ToString(), new UTF8Encoding(true));
        }

        internal static void CheckExtension(string path) {
            string extension = System.IO.Path.GetExtension(path);
            if (!string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".psd1", StringComparison.OrdinalIgnoreCase)) {
                throw new FormatException("Theme files must use the .json or .psd1 extension.");
            }
        }

        private static void Write(StringBuilder text, object? value, int depth) {
            if (value is IDictionary<string, object?> map) {
                text.Append("@{\n");
                foreach (KeyValuePair<string, object?> pair in map) {
                    text.Append(' ', (depth + 1) * 4);
                    Quote(text, pair.Key);
                    text.Append(" = ");
                    Write(text, pair.Value, depth + 1);
                    text.Append('\n');
                }
                text.Append(' ', depth * 4).Append('}');
            } else if (value == null) {
                text.Append("$null");
            } else if (value is int number) {
                text.Append(number.ToString(CultureInfo.InvariantCulture));
            } else if (value is string literal) {
                Quote(text, literal);
            } else {
                throw new InvalidOperationException("Unexpected value in the canonical theme document.");
            }
        }

        private static void Quote(StringBuilder text, string value) => text.Append('\'').Append(value.Replace("'", "''")).Append('\'');
    }
}
