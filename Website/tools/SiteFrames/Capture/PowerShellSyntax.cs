using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SiteFrames.Capture {
    /// <summary>
    /// Parses PowerShell code the site shows with PowerShell's own parser
    /// (<c>[System.Management.Automation.Language.Parser]::ParseInput</c>) in one <c>pwsh</c> process, so a code block a
    /// reader copies never has a syntax error. The tool does not reference the PowerShell SDK; pwsh is already needed to
    /// capture the frames.
    /// </summary>
    internal static class PowerShellSyntax {
        // Reads the blocks, parses each one and prints one line per error: index, line within the block, message.
        private const string Parser =
            "$blocks = [System.IO.File]::ReadAllText($args[0], [System.Text.Encoding]::UTF8) | ConvertFrom-Json\n" +
            "$index = 0\n" +
            "foreach ($block in @($blocks)) {\n" +
            "    $tokens = $null\n" +
            "    $errors = $null\n" +
            "    $null = [System.Management.Automation.Language.Parser]::ParseInput([string] $block, [ref] $tokens, [ref] $errors)\n" +
            "    foreach ($e in @($errors)) { \"{0}`t{1}`t{2}\" -f $index, $e.Extent.StartLineNumber, ($e.Message -replace '\\s+', ' ') }\n" +
            "    $index++\n" +
            "}\n";

        /// <summary>
        /// The parse errors of each block as "where:line: message", where a block's location is its page and the line its
        /// code starts on. Throws when pwsh cannot be run, since an unchecked page must not pass.
        /// </summary>
        public static List<string> Check(IReadOnlyList<(string Where, int Line, string Code)> blocks) {
            List<string> problems = new List<string>();
            if (blocks.Count == 0) {
                return problems;
            }
            string folder = Path.Combine(Path.GetTempPath(), "cmsite-parse-" + Guid.NewGuid().ToString("N").Substring(0, 12));
            Directory.CreateDirectory(folder);
            try {
                string input = Path.Combine(folder, "blocks.json");
                string script = Path.Combine(folder, "parse.ps1");
                List<string> codes = new List<string>(blocks.Count);
                foreach ((string _, int _, string code) in blocks) {
                    codes.Add(code);
                }
                File.WriteAllText(input, JsonSerializer.Serialize(codes), new UTF8Encoding(false));
                File.WriteAllText(script, Parser, new UTF8Encoding(true));
                ProcessStartInfo info = new ProcessStartInfo(PowerShellHost.Core.Executable) {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    CreateNoWindow = true
                };
                foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-File", script, input }) {
                    info.ArgumentList.Add(argument);
                }
                using (Process process = Process.Start(info) ?? throw new InvalidOperationException("pwsh could not be started to parse the PowerShell code blocks.")) {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0) {
                        throw new InvalidOperationException("Parsing the PowerShell code blocks failed: " + error.Trim());
                    }
                    foreach (string line in output.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
                        string[] parts = line.Split('\t', 3);
                        if (parts.Length == 3 && int.TryParse(parts[0], out int index) && int.TryParse(parts[1], out int within) && index >= 0 && index < blocks.Count) {
                            (string where, int start, string _) = blocks[index];
                            problems.Add(where + ":" + (start + within - 1) + ": PowerShell does not parse this code: " + parts[2]);
                        }
                    }
                }
            } finally {
                Site.RemoveTemporary(folder);
            }
            return problems;
        }
    }
}
