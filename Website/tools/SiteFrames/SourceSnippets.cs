using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SiteFrames {
    /// <summary>
    /// Reads example code out of the tool's own source files: the lines between <c>// &lt;example&gt;</c> and
    /// <c>// &lt;/example&gt;</c> inside a method, dedented. What the site shows is therefore the code that produced the frame.
    /// </summary>
    internal static class SourceSnippets {
        private static readonly Dictionary<string, string[]> Files = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Splits example code into the short form a page shows first and the full program. Lines between
        /// <c>// &lt;more&gt;</c> and <c>// &lt;/more&gt;</c> (C#) or <c>#region more</c> and <c>#endregion</c> (PowerShell) are
        /// data the reader can take on trust: the short form keeps the first of them and says how many follow. The full form
        /// is the code exactly as it ran, without the markers.
        /// </summary>
        public static (string Short, string Full) Split(string code) {
            string[] lines = code.Replace("\r\n", "\n").Split('\n');
            List<string> shortForm = new List<string>();
            List<string> full = new List<string>();
            for (int i = 0; i < lines.Length; i++) {
                string trimmed = lines[i].Trim();
                bool csharp = trimmed == "// <more>";
                bool powershell = trimmed == "#region more";
                if (!csharp && !powershell) {
                    shortForm.Add(lines[i]);
                    full.Add(lines[i]);
                    continue;
                }
                string end = csharp ? "// </more>" : "#endregion";
                List<string> block = new List<string>();
                for (i++; i < lines.Length && lines[i].Trim() != end; i++) {
                    block.Add(lines[i]);
                }
                full.AddRange(block);
                if (block.Count == 0) {
                    continue;
                }
                shortForm.Add(block[0]);
                if (block.Count > 1) {
                    string indent = block[0].Substring(0, block[0].Length - block[0].TrimStart().Length);
                    string more = (block.Count - 1) + " more " + (block.Count == 2 ? "line" : "lines") + " in the full program";
                    shortForm.Add(indent + (csharp ? "// … " : "# … ") + more);
                }
            }
            return (string.Join("\n", shortForm), string.Join("\n", full));
        }

        /// <summary>A whole method (signature and body, without its documentation comment) from a source file, dedented.</summary>
        public static string Method(string sourceFile, string signature) {
            string[] lines = File.ReadAllText(sourceFile).Replace("\r\n", "\n").Split('\n');
            int start = Array.FindIndex(lines, l => l.Contains(signature, StringComparison.Ordinal));
            if (start < 0) {
                throw new InvalidOperationException("No " + signature + " in " + sourceFile);
            }
            int depth = 0;
            bool opened = false;
            List<string> body = new List<string>();
            for (int i = start; i < lines.Length; i++) {
                body.Add(lines[i]);
                foreach (char ch in lines[i]) {
                    if (ch == '{') { depth++; opened = true; }
                    else if (ch == '}') { depth--; }
                }
                if (opened && depth == 0) {
                    break;
                }
            }
            return Dedent(body);
        }

        private static string Dedent(List<string> body) {
            int indent = int.MaxValue;
            foreach (string line in body) {
                if (line.Trim().Length == 0) {
                    continue;
                }
                indent = Math.Min(indent, line.Length - line.TrimStart(' ').Length);
            }
            StringBuilder sb = new StringBuilder();
            foreach (string line in body) {
                sb.Append(line.Length >= indent ? line.Substring(indent) : line.TrimStart()).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        public static string Get(string sourceFile, string method) {
            if (!Files.TryGetValue(sourceFile, out string[]? lines)) {
                lines = File.ReadAllText(sourceFile).Replace("\r\n", "\n").Split('\n');
                Files[sourceFile] = lines;
            }
            int start = -1;
            for (int i = 0; i < lines.Length; i++) {
                if (lines[i].Contains(" " + method + "(", StringComparison.Ordinal) && (lines[i].TrimStart().StartsWith("private static", StringComparison.Ordinal) || lines[i].TrimStart().StartsWith("public static", StringComparison.Ordinal))) {
                    start = i;
                    break;
                }
            }
            if (start < 0) {
                throw new InvalidOperationException("No method " + method + " in " + sourceFile);
            }
            List<string> body = new List<string>();
            bool inside = false;
            for (int i = start; i < lines.Length; i++) {
                string trimmed = lines[i].Trim();
                if (trimmed == "// <example>") {
                    inside = true;
                    continue;
                }
                if (trimmed == "// </example>") {
                    break;
                }
                if (inside) {
                    body.Add(lines[i]);
                }
            }
            if (body.Count == 0) {
                throw new InvalidOperationException("No example markers in " + method);
            }
            int indent = int.MaxValue;
            foreach (string line in body) {
                if (line.Trim().Length == 0) {
                    continue;
                }
                int spaces = line.Length - line.TrimStart(' ').Length;
                indent = Math.Min(indent, spaces);
            }
            StringBuilder sb = new StringBuilder();
            foreach (string line in body) {
                sb.Append(line.Length >= indent ? line.Substring(indent) : line.TrimStart()).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
