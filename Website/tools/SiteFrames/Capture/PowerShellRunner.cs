using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SiteFrames.Vt;

namespace SiteFrames.Capture {
    /// <summary>A PowerShell host the site runs examples on.</summary>
    internal sealed class PowerShellHost {
        private PowerShellHost(string id, string label, string executable) {
            Id = id;
            Label = label;
            Executable = executable;
        }

        /// <summary>Windows PowerShell 5.1.</summary>
        public static PowerShellHost Desktop { get; } = new PowerShellHost("ps5", "Windows PowerShell 5.1", "powershell.exe");

        /// <summary>PowerShell 7.</summary>
        public static PowerShellHost Core { get; } = new PowerShellHost("ps7", "PowerShell 7", "pwsh.exe");

        public static PowerShellHost[] All { get; } = { Core, Desktop };

        public string Id { get; }

        public string Label { get; }

        public string Executable { get; }
    }

    /// <summary>
    /// Runs PowerShell scripts with the ConsoleMonster module from this repository's source build, inside a pseudo console of a
    /// fixed size, with the terminal profile pinned through the <c>CONSOLEMONSTER_*</c> overrides, and saves the VT stream.
    /// </summary>
    internal static class PowerShellRunner {
        public static Dictionary<string, string?> Environment(string theme, string color = "truecolor", string glyphs = "full", string? mode = null) {
            Dictionary<string, string?> env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
                ["CONSOLEMONSTER_COLOR"] = color,
                ["CONSOLEMONSTER_GLYPHS"] = glyphs,
                ["CONSOLEMONSTER_THEME"] = theme,
                ["CONSOLEMONSTER_CULTURE"] = "en-US",
                ["CONSOLEMONSTER_LANGUAGE"] = "en",
                ["CONSOLEMONSTER_MODE"] = mode,
                ["CONSOLEMONSTER_EMOJI_WIDTH"] = null,
                ["CONSOLEMONSTER_AMBIGUOUS_WIDTH"] = null,
                ["NO_COLOR"] = null,
                ["CI"] = null,
                ["TF_BUILD"] = null,
                ["GITHUB_ACTIONS"] = null,
                ["WT_SESSION"] = null,
                ["WT_PROFILE_ID"] = null,
                ["TERM_PROGRAM"] = null,
                ["COLORTERM"] = null,
                ["POWERSHELL_TELEMETRY_OPTOUT"] = "1",
                ["POWERSHELL_UPDATECHECK"] = "Off"
            };
            return env;
        }

        /// <summary>Runs a script and returns the raw VT stream with its timing.</summary>
        public static List<OutputChunk> Run(Site site, PowerShellHost host, string script, int width, int height, Dictionary<string, string?> environment, string? workingDirectory = null, TimeSpan? timeout = null) {
            string runner = Path.Combine(Path.GetTempPath(), "cmsite-runner-" + Path.GetFileNameWithoutExtension(script) + "-" + host.Id + ".ps1");
            string module = Path.Combine(site.Repository, "Module", "ConsoleMonster.psd1");
            string directory = workingDirectory ?? Path.GetDirectoryName(script)!;
            StringBuilder sb = new StringBuilder();
            sb.Append("$ErrorActionPreference = 'Stop'\n");
            sb.Append("$ProgressPreference = 'SilentlyContinue'\n");
            sb.Append("[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)\n");
            // Built-in formatting (Format-Table) follows the thread culture; pin it like CONSOLEMONSTER_CULTURE pins ConsoleMonster.
            sb.Append("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')\n");
            sb.Append("[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')\n");
            sb.Append("Import-Module '").Append(module.Replace("'", "''")).Append("' -Force\n");
            sb.Append("Set-Location -LiteralPath '").Append(directory.Replace("'", "''")).Append("'\n");
            sb.Append(". '").Append(script.Replace("'", "''")).Append("'\n");
            File.WriteAllText(runner, sb.ToString(), new UTF8Encoding(true));
            try {
                string command = host.Executable + " -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + runner + "\"";
                return PseudoConsole.Run(command, width, height, environment, directory, timeout ?? TimeSpan.FromMinutes(3));
            } finally {
                File.Delete(runner);
            }
        }

        /// <summary>The VT bytes of a run, joined.</summary>
        public static byte[] Bytes(List<OutputChunk> chunks) {
            using (MemoryStream stream = new MemoryStream()) {
                foreach (OutputChunk chunk in chunks) {
                    stream.Write(chunk.Data, 0, chunk.Data.Length);
                }
                return stream.ToArray();
            }
        }

        /// <summary>The version a host reports, as major.minor.</summary>
        public static string Version(PowerShellHost host) {
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(host.Executable, "-NoLogo -NoProfile -NonInteractive -Command \"$v = $PSVersionTable.PSVersion; '{0}.{1}' -f $v.Major, $v.Minor\"") {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(info)!) {
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output;
            }
        }
    }
}
