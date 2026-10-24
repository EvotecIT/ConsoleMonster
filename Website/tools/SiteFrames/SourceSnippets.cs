using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

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
        /// data the reader can take on trust: the short form keeps their outline and says how many lines each fold hides, so a
        /// statement opened in the block also closes there (see <see cref="SourceFold"/>). The full form
        /// is the code exactly as it ran, without the markers. Lines between <c>#region hidden</c> and <c>#endregion</c> (or
        /// <c>// &lt;hidden&gt;</c> and <c>// &lt;/hidden&gt;</c>) are presentation for a recording: the short form leaves them
        /// out and the full form keeps them.
        /// <para>
        /// PowerShell regions nest (other <c>#region</c> blocks inside are kept as written), <c>#endregion</c> may carry a label,
        /// and a block that never ends is an error, so no code silently disappears. There is no way to hide code from both
        /// forms: the full form is always the code that ran.
        /// </para>
        /// </summary>
        public static (string Short, string Full) Split(string code) {
            string[] lines = code.Replace("\r\n", "\n").Split('\n');
            List<string> shortForm = new List<string>();
            List<string> full = new List<string>();
            for (int i = 0; i < lines.Length; i++) {
                string trimmed = Normalise(lines[i].Trim());
                if (trimmed == "#region harness" || trimmed == "// <harness>") {
                    throw new InvalidOperationException("Line " + (i + 1) + ": harness regions are not allowed; the shown code must be the code that ran.");
                }
                bool hidden = trimmed == "#region hidden" || trimmed == "// <hidden>";
                bool csharp = trimmed == "// <more>" || trimmed == "// <hidden>";
                bool powershell = trimmed == "#region more" || trimmed == "#region hidden";
                if (!csharp && !powershell) {
                    shortForm.Add(lines[i]);
                    full.Add(lines[i]);
                    continue;
                }
                int opened = i;
                string end = csharp ? (hidden ? "// </hidden>" : "// </more>") : "#endregion";
                List<string> block = new List<string>();
                int depth = 0;
                bool closed = false;
                for (i++; i < lines.Length; i++) {
                    string inner = Normalise(lines[i].Trim());
                    if (powershell && inner.StartsWith("#region", StringComparison.Ordinal)) {
                        depth++;
                    } else if (powershell && IsEndRegion(inner)) {
                        if (depth == 0) {
                            closed = true;
                            break;
                        }
                        depth--;
                    } else if (csharp && inner == end) {
                        closed = true;
                        break;
                    }
                    if (inner == "#region harness" || inner == "// <harness>") {
                        throw new InvalidOperationException("Line " + (i + 1) + ": harness regions are not allowed; the shown code must be the code that ran.");
                    }
                    block.Add(lines[i]);
                }
                if (!closed) {
                    throw new InvalidOperationException("Line " + (opened + 1) + ": \"" + trimmed + "\" is never closed with \"" + end + "\".");
                }
                full.AddRange(block);
                if (block.Count == 0 || hidden) {
                    continue;
                }
                shortForm.AddRange(SourceFold.Fold(block, csharp));
            }
            return (string.Join("\n", shortForm), string.Join("\n", full));
        }

        /// <summary>
        /// The short and full forms of a complete program (a C# Program.cs or a PowerShell script): the short form leads with the
        /// program's own statements, without its <c>using</c> lines or <c>Import-Module ConsoleMonster</c>, and folds its data
        /// (<c>// &lt;more&gt;</c>, <c>#region more</c>); the full form is the whole file as it runs, without the region marker lines.
        /// </summary>
        public static (string Short, string Full) Program(string code) {
            (string shortForm, string full) = Split(code);
            string[] lines = shortForm.Split('\n');
            int first = 0;
            while (first < lines.Length && (IsUsingDirective(lines[first]) || lines[first].Trim() == "Import-Module ConsoleMonster" || lines[first].Trim().Length == 0)) {
                first++;
            }
            List<string> shown = new List<string>(lines.Length - first);
            // A script's comment-based help (a <# … #> block at the top) is folded to one line in the short form, so the
            // sample leads with what the script does; the full program keeps it.
            if (first < lines.Length && lines[first].TrimStart().StartsWith("<#", StringComparison.Ordinal)) {
                int close = Array.FindIndex(lines, first, l => l.Contains("#>", StringComparison.Ordinal));
                if (close > first + 1) {
                    shown.Add("# … " + (close - first + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " lines of help in the full program");
                    first = close + 1;
                }
            }
            for (int i = first; i < lines.Length; i++) {
                shown.Add(lines[i]);
            }
            return (string.Join("\n", shown), full);
        }

        // Namespace imports are setup; resource-using statements belong to the executable example.
        private static bool IsUsingDirective(string line) {
            string text = line.Trim();
            if (!text.StartsWith("using ", StringComparison.Ordinal) && !text.StartsWith("global using ", StringComparison.Ordinal)) { return false; }
            var root = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
            return root.Usings.Count > 0 && root.Members.Count == 0 && !root.ContainsDiagnostics;
        }

        // PowerShell accepts #Region and #EndRegion in any case; markers compare in lower case.
        private static string Normalise(string trimmed) => trimmed.StartsWith("#", StringComparison.Ordinal) ? trimmed.ToLowerInvariant() : trimmed;

        private static bool IsEndRegion(string trimmed) => trimmed == "#endregion" || trimmed.StartsWith("#endregion ", StringComparison.Ordinal);
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
