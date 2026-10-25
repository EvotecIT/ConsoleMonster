using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SiteFrames.Capture {
    /// <summary>
    /// Parses PowerShell code the site shows with PowerShell's own parser
    /// (<c>[System.Management.Automation.Language.Parser]::ParseInput</c>), so a code block a reader copies never has a
    /// syntax error. Every block is parsed by PowerShell 7 and, where it is installed, by Windows PowerShell 5.1, since the
    /// site's code has to run on both: syntax only 7 knows (<c>??</c>, <c>&amp;&amp;</c>, the ternary) fails on 5.1. The tool does
    /// not reference the PowerShell SDK; the hosts are already needed to capture the frames.
    /// </summary>
    internal static partial class PowerShellSyntax {
        // Reads the blocks, parses each one and prints one line per error (index, line within the block, message), then
        // "parsed <count>", so a run that stopped early can never pass for a clean one.
        private const string Parser =
            "$ErrorActionPreference = 'Stop'\n" +
            "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)\n" +
            "$blocks = [System.IO.File]::ReadAllText($args[0], [System.Text.Encoding]::UTF8) | ConvertFrom-Json\n" +
            "$index = 0\n" +
            "foreach ($block in @($blocks)) {\n" +
            "    $tokens = $null\n" +
            "    $errors = $null\n" +
            "    $null = [System.Management.Automation.Language.Parser]::ParseInput([string] $block, [ref] $tokens, [ref] $errors)\n" +
            "    foreach ($e in @($errors)) { \"{0}`t{1}`t{2}\" -f $index, $e.Extent.StartLineNumber, ($e.Message -replace '\\s+', ' ') }\n" +
            "    $index++\n" +
            "}\n" +
            "\"parsed $index\"\n";

        /// <summary>
        /// The parse errors of each block as "where:line: message", where a block's location is its page and the line its
        /// code starts on. Throws when pwsh cannot be run, or a parser run does not finish, since an unchecked page must
        /// not pass. Windows PowerShell 5.1 is used when it is installed.
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
                File.WriteAllText(input, JsonSerializer.Serialize(blocks.Select(b => b.Code).ToList()), new UTF8Encoding(false));
                File.WriteAllText(script, Parser, new UTF8Encoding(true));
                // Each error once, naming the hosts that reject it.
                Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                List<string> order = new List<string>();
                foreach (PowerShellHost host in new[] { PowerShellHost.Core, PowerShellHost.Desktop }) {
                    List<string>? lines = Run(host, script, input, blocks.Count, required: host == PowerShellHost.Core);
                    foreach (string line in lines ?? new List<string>()) {
                        if (!errors.TryGetValue(line, out List<string>? hosts)) {
                            hosts = new List<string>();
                            errors[line] = hosts;
                            order.Add(line);
                        }
                        hosts.Add(host.Label);
                    }
                }
                foreach (string line in order) {
                    string[] parts = line.Split('\t', 3);
                    if (parts.Length != 3 || !int.TryParse(parts[0], out int index) || !int.TryParse(parts[1], out int within) || index < 0 || index >= blocks.Count) {
                        throw new InvalidOperationException("Unexpected output from the PowerShell parser: " + line);
                    }
                    (string where, int start, string _) = blocks[index];
                    problems.Add(where + ":" + (start + within - 1) + ": " + string.Join(" and ", errors[line]) + " does not parse this code: " + parts[2]);
                }
            } finally {
                Site.RemoveTemporary(folder);
            }
            return problems;
        }

        // The error lines of one host's run, or null when an optional host is not installed.
        private static List<string>? Run(PowerShellHost host, string script, string input, int count, bool required) {
            ProcessStartInfo info = new ProcessStartInfo(host.Executable) {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };
            foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script, input }) {
                info.ArgumentList.Add(argument);
            }
            Process process;
            try {
                process = Process.Start(info)!;
            } catch (Win32Exception) when (!required) {
                return null;
            } catch (Win32Exception ex) {
                throw new InvalidOperationException(host.Label + " (" + host.Executable + ") was not found; it is needed to parse the PowerShell code blocks.", ex);
            }
            using (process) {
                // Both streams are read at once, so a full error pipe cannot stall the parser.
                System.Threading.Tasks.Task<string> error = process.StandardError.ReadToEndAsync();
                System.Threading.Tasks.Task<string> output = process.StandardOutput.ReadToEndAsync();
                if (!process.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds)) {
                    process.Kill(true);
                    throw new InvalidOperationException(host.Label + " did not finish parsing the PowerShell code blocks within two minutes.");
                }
                List<string> lines = output.Result.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                string last = lines.Count > 0 ? lines[lines.Count - 1] : string.Empty;
                if (process.ExitCode != 0 || last != "parsed " + count) {
                    throw new InvalidOperationException(host.Label + " did not parse every PowerShell code block (exit code " + process.ExitCode + "): " + error.Result.Trim());
                }
                lines.RemoveAt(lines.Count - 1);
                return lines;
            }
        }
    }
}
