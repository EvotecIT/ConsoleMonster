using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Demos {
    /// <summary>An executable command or a presentation instruction in a PowerShell demo.</summary>
    public enum DemoStepKind {
        /// <summary>Type and execute PowerShell.</summary>
        Command,
        /// <summary>Add a recording chapter.</summary>
        Chapter,
        /// <summary>Wait the specified number of seconds.</summary>
        Pause,
        /// <summary>Print a narration line.</summary>
        Narration,
        /// <summary>Freeze the exported screen without waiting during capture.</summary>
        Hold,
        /// <summary>Set the export reading pace for subsequent output.</summary>
        Pace,
        /// <summary>Wait for a regular expression in command output.</summary>
        Wait
    }

    /// <summary>One parsed demo step, with its source line for diagnostics.</summary>
    public sealed class DemoStep {
        internal DemoStep(DemoStepKind kind, string text, int line, double wpm, bool hidden = false, bool typeOnly = false) {
            Kind = kind; Text = text; Line = line; WordsPerMinute = wpm; Hidden = hidden; TypeOnly = typeOnly;
        }
        /// <summary>The instruction to perform.</summary>
        public DemoStepKind Kind { get; }
        /// <summary>PowerShell source, chapter, narration, seconds or output pattern.</summary>
        public string Text { get; }
        /// <summary>One-based source line.</summary>
        public int Line { get; }
        /// <summary>Typing speed, with five characters per word.</summary>
        public double WordsPerMinute { get; }
        /// <summary>Execute without showing or recording setup input or output.</summary>
        public bool Hidden { get; }
        /// <summary>Leave the command on the input line without executing.</summary>
        public bool TypeOnly { get; }
    }

    /// <summary>Plain PowerShell commands with optional <c># demo:</c> presentation directives.</summary>
    public sealed class DemoScript {
        private DemoScript(List<DemoStep> steps) { Steps = steps.AsReadOnly(); }
        /// <summary>Steps in source order. Parsing does not execute PowerShell.</summary>
        public IReadOnlyList<DemoStep> Steps { get; }
        /// <summary>Original file path for PowerShell's PSScriptRoot and PSCommandPath, or null for parsed text.</summary>
        public string? SourcePath { get; private set; }
        /// <summary>Reads a UTF-8 demo file.</summary>
        public static DemoScript Read(string path) {
            string fullPath = Path.GetFullPath(path);
            DemoScript script = Parse(File.ReadAllText(fullPath));
            script.SourcePath = fullPath;
            return script;
        }
        /// <summary>Parses commands and directives. Multiline strings, blocks, pipelines and here-strings remain one command.</summary>
        public static DemoScript Parse(string source) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            List<DemoStep> steps = new List<DemoStep>();
            StringBuilder command = new StringBuilder();
            PowerShellCommandBoundary boundary = new PowerShellCommandBoundary();
            double wpm = 100;
            bool hidden = false, typeOnly = false;
            int line = 0, start = 0, commandEnd = 0;
            using (StringReader reader = new StringReader(source)) {
                string? text;
                while ((text = reader.ReadLine()) != null) {
                    line++;
                    string trimmed = text.Trim();
                    // else/catch/finally can start on a new line after a closed block.
                    if (command.Length > 0 && boundary.Complete && (trimmed.StartsWith("# demo:", StringComparison.OrdinalIgnoreCase) || !FollowsBlock(trimmed, command.ToString()))) {
                        Flush();
                    }
                    if (command.Length == 0 && trimmed.StartsWith("# demo:", StringComparison.OrdinalIgnoreCase)) {
                        string directive = trimmed.Substring(7).Trim();
                        int space = directive.IndexOf(' ');
                        string name = (space < 0 ? directive : directive.Substring(0, space)).ToLowerInvariant();
                        string value = space < 0 ? string.Empty : directive.Substring(space + 1).Trim();
                        switch (name) {
                            case "setup": hidden = true; break;
                            case "type-only": typeOnly = true; break;
                            case "wpm": wpm = Number(value, line, 1, 1000); break;
                            case "chapter": Add(DemoStepKind.Chapter); break;
                            case "hold": Number(value, line, 0, 86400); Add(DemoStepKind.Hold); break;
                            case "pace":
                                if (value != "slow" && value != "normal" && value != "fast") { throw new FormatException("Invalid demo pace at line " + line + ". Use slow, normal or fast."); }
                                Add(DemoStepKind.Pace); break;
                            case "pause": Number(value, line, 0, 3600); Add(DemoStepKind.Pause); break;
                            case "narrate": Add(DemoStepKind.Narration); break;
                            case "wait":
                                try { _ = new Regex(value, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)); }
                                catch (ArgumentException error) { throw new FormatException("Invalid wait pattern at line " + line + ".", error); }
                                Add(DemoStepKind.Wait); break;
                            default: throw new FormatException("Unknown demo directive at line " + line + ": " + name);
                        }
                        void Add(DemoStepKind kind) {
                            if (value.Length == 0) { throw new FormatException("Missing demo directive value at line " + line + "."); }
                            steps.Add(new DemoStep(kind, value, line, wpm));
                        }
                        continue;
                    }
                    if (command.Length == 0 && (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))) { continue; }
                    if (command.Length == 0) { start = line; }
                    else { command.Append('\n'); }
                    command.Append(text);
                    boundary.Feed(text);
                    if (boundary.HasCode) { commandEnd = command.Length; }
                }
            }
            if (command.Length != 0) {
                if (!boundary.Complete) { throw new FormatException("Incomplete PowerShell command starting at line " + start + "."); }
                Flush();
            }
            if (hidden || typeOnly) { throw new FormatException("A setup or type-only directive needs a following command."); }
            return new DemoScript(steps);
            void Flush() {
                if (commandEnd > 0) {
                    steps.Add(new DemoStep(DemoStepKind.Command, command.ToString(0, commandEnd), start, wpm, hidden, typeOnly));
                    hidden = typeOnly = false;
                }
                command.Clear(); commandEnd = 0;
            }
        }
        private static bool FollowsBlock(string line, string command) {
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("<#", StringComparison.Ordinal)) { return true; }
            if (line.StartsWith("{", StringComparison.Ordinal)) {
                string header = command.TrimStart();
                foreach (string keyword in new[] { "function", "filter", "class", "enum", "if", "elseif", "switch", "try", "catch", "finally", "foreach", "for", "while", "do", "begin", "process", "end", "clean" }) {
                    if (header.Equals(keyword, StringComparison.OrdinalIgnoreCase) || header.StartsWith(keyword + " ", StringComparison.OrdinalIgnoreCase) || header.StartsWith(keyword + "(", StringComparison.OrdinalIgnoreCase)) { return true; }
                }
            }
            foreach (string keyword in new[] { "else", "elseif", "catch", "finally" }) {
                if (line.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) && (line.Length == keyword.Length || !char.IsLetterOrDigit(line[keyword.Length]))) { return true; }
            }
            string first = command.TrimStart();
            return (first.StartsWith("do ", StringComparison.OrdinalIgnoreCase) || first.StartsWith("do{", StringComparison.OrdinalIgnoreCase) || first.Equals("do", StringComparison.OrdinalIgnoreCase)) && line.StartsWith("while", StringComparison.OrdinalIgnoreCase);
        }
        private static double Number(string text, int line, double min, double max) {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || double.IsNaN(value) || value < min || value > max) {
                throw new FormatException("Invalid demo number at line " + line + ".");
            }
            return value;
        }
    }
}
