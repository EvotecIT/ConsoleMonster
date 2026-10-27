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

        /// <summary>PowerShell 7 (<c>pwsh</c> outside Windows, where CI renders the site and parses its code blocks).</summary>
        public static PowerShellHost Core { get; } = new PowerShellHost("ps7", "PowerShell 7", OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh");

        public static PowerShellHost[] All { get; } = { Core, Desktop };
        public static IEnumerable<PowerShellHost> Available => OperatingSystem.IsWindows() ? All : new[] { Core };
        public bool CanCapture => this != Desktop || OperatingSystem.IsWindows();

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
                ["SITEFRAMES_CAPTURE_POLICY"] = CapturePins.Policy,
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
                ["BUILD_BUILDID"] = null,
                ["JENKINS_URL"] = null,
                ["GITLAB_CI"] = null,
                ["TEAMCITY_VERSION"] = null,
                ["TERM"] = "xterm-256color",
                ["WT_SESSION"] = null,
                ["WT_PROFILE_ID"] = null,
                ["TERM_PROGRAM"] = null,
                ["COLORTERM"] = null,
                ["POWERSHELL_TELEMETRY_OPTOUT"] = "1",
                ["POWERSHELL_UPDATECHECK"] = "Off"
            };
            return env;
        }

        /// <summary>
        /// Runs a script and returns the raw VT stream with its timing. With <paramref name="allowErrors"/> errors are shown and
        /// the run goes on (demos that fail on purpose); <paramref name="showProgress"/> keeps progress records (the progress
        /// demo). <paramref name="arguments"/> are passed to the script as a reader would type them (only its public
        /// parameters). <paramref name="prompt"/> is a command line the recording shows typed before the script runs (such as
        /// <c>ssh deploy@app-web-03</c>); it belongs to the recording, so the shown script never contains it.
        /// <paramref name="session"/> plays the script as typed at a prompt: each top-level command is shown after a
        /// <c>PS&gt;</c> prompt (with the comment lines just above it) and then run, its output formatted on its own, so the
        /// shown script is only the commands. Definitions such as functions run without a prompt. <paramref name="setup"/>
        /// is PowerShell the recording runs first (such as the folder to start in); it is never shown either.
        /// <paramref name="hostedOutput"/> selects a script's own hosted recorder sink without adding the checkpoint driver.
        /// </summary>
        public static List<OutputChunk> Run(Site site, PowerShellHost host, string script, int width, int height, Dictionary<string, string?> environment, string? workingDirectory = null, TimeSpan? timeout = null, bool allowErrors = false, bool showProgress = false, string? arguments = null, string? prompt = null, bool interactive = false, bool session = false, string? setup = null, bool recordCheckpoints = false, Action<string>? recording = null, bool hostedOutput = false) {
            string runner = Path.Combine(Path.GetTempPath(), "cmsite-runner-" + Path.GetFileNameWithoutExtension(script) + "-" + host.Id + ".ps1");
            string modules = ModuleLinkRoot(site);
            string directory = workingDirectory ?? Path.GetDirectoryName(script)!;
            StringBuilder sb = new StringBuilder();
            sb.Append(allowErrors ? "$ErrorActionPreference = 'Continue'\n" : "$ErrorActionPreference = 'Stop'\n");
            sb.Append(showProgress ? "$ProgressPreference = 'Continue'\n" : "$ProgressPreference = 'SilentlyContinue'\n");
            sb.Append("[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)\n");
            // Built-in formatting (Format-Table) follows the thread culture; pin it like CONSOLEMONSTER_CULTURE pins ConsoleMonster.
            sb.Append("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')\n");
            sb.Append("[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')\n");
            // Scripts import the module by name, as a reader's copy does; the link first on PSModulePath makes that name load
            // this repository's build. The check stops a capture that would record an installed version instead.
            sb.Append("$env:PSModulePath = '").Append(modules.Replace("'", "''")).Append("' + [IO.Path]::PathSeparator + $env:PSModulePath\n");
            sb.Append("Import-Module ConsoleMonster -Force\n");
            sb.Append("$cmdlets = (Get-Command Show-Console -ErrorAction SilentlyContinue).DLL\n");
            sb.Append("if (@(Get-Module ConsoleMonster).Count -ne 1 -or -not $cmdlets -or -not $cmdlets.StartsWith('").Append(modules.Replace("'", "''")).Append("', [StringComparison]::OrdinalIgnoreCase)) { throw 'ConsoleMonster did not load from this repository (build ConsoleMonster.PowerShell first)' }\n");
            sb.Append("Set-Location -LiteralPath '").Append(directory.Replace("'", "''")).Append("'\n");
            if (prompt != null) {
                sb.Append("Write-Host '$ ").Append(prompt.Replace("'", "''")).Append("' -ForegroundColor Yellow\n");
                sb.Append("Start-Sleep -Milliseconds 900\n");
            }
            if (setup != null) {
                sb.Append(setup).Append('\n');
            }
            if (recordCheckpoints && session) { throw new ArgumentException("A typed session cannot use logical live checkpoints."); }
            if (recordCheckpoints) {
                sb.Append("& '").Append(Path.Combine(site.Tool, "Capture", "CheckpointCapture.ps1").Replace("'", "''"))
                    .Append("' -Width ").Append(width).Append(" -Height ").Append(height).Append(" -ScriptBlock {\n");
            }
            if (session) {
                sb.Append(SessionDriver.Replace("{script}", script.Replace("'", "''")));
            } else {
                sb.Append(". '").Append(script.Replace("'", "''")).Append("'");
                if (!string.IsNullOrEmpty(arguments)) {
                    sb.Append(' ').Append(arguments);
                }
                sb.Append('\n');
            }
            if (recordCheckpoints) { sb.Append("}\n"); }
            if (allowErrors) {
                sb.Append("exit 0\n");
            }
            File.WriteAllText(runner, sb.ToString(), new UTF8Encoding(true));
            try {
                // Prompts refuse to ask in a -NonInteractive session; runs that answer them with scripted keys leave it out.
                var args = new List<string> { "-NoLogo", "-NoProfile" };
                if (!interactive) { args.Add("-NonInteractive"); }
                args.AddRange(new[] { "-ExecutionPolicy", "Bypass", "-File", runner });
                return TerminalCapture.Run(host.Executable, args, width, height, environment, directory, timeout ?? TimeSpan.FromMinutes(3), recording: recording, hostedOutput: recordCheckpoints || hostedOutput);
            } finally {
                File.Delete(runner);
            }
        }

        /// <summary>
        /// A folder of links into this repository (junctions on Windows, one folder per checkout): <c>ConsoleMonster</c> to the <c>Module</c> folder, so
        /// <c>Import-Module ConsoleMonster</c> in a shown script loads the repository build when the folder is first on
        /// PSModulePath, and <c>ConsoleMonster.PowerShell</c> beside it, because the module's development loader looks for the
        /// cmdlet binaries at <c>$PSScriptRoot\..\ConsoleMonster.PowerShell\bin</c>. Created once under the temporary folder and
        /// reused.
        /// </summary>
        public static string ModuleLinkRoot(Site site) {
            // One folder per checkout, so two worktrees rendering at once never point each other's links at themselves.
            string id;
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create()) {
                id = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(site.Repository).TrimEnd('\\', '/').ToUpperInvariant()))).Substring(0, 12).ToLowerInvariant();
            }
            string root = Path.Combine(Path.GetTempPath(), "cmsite-modules-" + id);
            Directory.CreateDirectory(root);
            Link(Path.Combine(root, "ConsoleMonster"), Path.Combine(site.Repository, "Module"));
            Link(Path.Combine(root, "ConsoleMonster.PowerShell"), Path.Combine(site.Repository, "ConsoleMonster.PowerShell"));
            return root;
        }

        private static void Link(string link, string target) {
            if (Directory.Exists(link)) {
                DirectoryInfo info = new DirectoryInfo(link);
                if (info.LinkTarget != null && string.Equals(Path.GetFullPath(info.LinkTarget).TrimEnd('\\', '/'), Path.GetFullPath(target).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase)) {
                    return;
                }
                if (info.LinkTarget == null) {
                    throw new InvalidOperationException(link + " exists and is not a link; remove it.");
                }
                // A link left by another checkout: remove the link itself (never recursively, so the target is untouched).
                Directory.Delete(link, false);
            }
            if (OperatingSystem.IsWindows()) {
                System.Diagnostics.ProcessStartInfo mklink = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"") {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(mklink)!) {
                    process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0) {
                        throw new InvalidOperationException("Could not link " + link + " to " + target + ": " + error);
                    }
                }
            } else {
                Directory.CreateSymbolicLink(link, target);
            }
        }
        // Plays a script as a typed session: a prompt and the command, then its output, for each top-level statement.
        internal static string SessionRecipe => SessionDriver;
        private const string SessionDriver =
            "$cmRecorder = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecorder', $true)\n" +
            "$cmTokens = $null\n$cmErrors = $null\n" +
            "$cmAst = [System.Management.Automation.Language.Parser]::ParseFile('{script}', [ref] $cmTokens, [ref] $cmErrors)\n" +
            "$cmComments = @($cmTokens | Where-Object { $_.Kind -eq 'Comment' -and $_.Text -notmatch '^#(end)?region' })\n" +
            "$cmPrevious = 0\n" +
            "foreach ($cmStatement in $cmAst.EndBlock.Statements) {\n" +
            "    $cmTyped = -not ($cmStatement -is [System.Management.Automation.Language.FunctionDefinitionAst])\n" +
            "    if ($cmTyped) {\n" +
            "        foreach ($cmComment in $cmComments) {\n" +
            "            if ($cmComment.Extent.StartOffset -ge $cmPrevious -and $cmComment.Extent.EndOffset -le $cmStatement.Extent.StartOffset) {\n" +
            "                Start-Sleep -Milliseconds 600\n                [Console]::Write($cmRecorder::MarkerSequence('site:comment'))\n                Write-Host \"PS> $($cmComment.Text)\" -ForegroundColor Yellow\n" +
            "            }\n" +
            "        }\n" +
            "        Start-Sleep -Milliseconds 700\n        [Console]::Write($cmRecorder::MarkerSequence('site:prompt'))\n        Write-Host \"PS> $($cmStatement.Extent.Text)\" -ForegroundColor Yellow\n        Start-Sleep -Milliseconds 400\n" +
            "    }\n" +
            "    $cmPrevious = $cmStatement.Extent.EndOffset\n" +
            "    [Console]::Write($cmRecorder::MarkerSequence('site:output'))\n" +
            "    . ([scriptblock]::Create($cmStatement.Extent.Text)) | Out-Default\n" +
            "}\n[Console]::Write($cmRecorder::MarkerSequence('site:end'))\n";

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
