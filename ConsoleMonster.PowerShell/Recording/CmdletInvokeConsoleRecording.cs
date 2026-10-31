using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Records a script, an app on a headless terminal, or an interactive child shell and saves the result.</para>
    /// <para type="description">Runs the script in an isolated child process, or opens a child shell with Interactive until it exits. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. With -Headless the script runs here, against a headless terminal of -Width by -Height cells, and -Keys drives it (key names, quoted text, wait:250 pauses, resize:80x24); the recording holds exactly what an app shows, including its log panel, footer and dialogs, and the app ends when the keys run out. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG; Export-ConsoleRecording turns the cast into GIF or APNG. -InProcess explicitly selects the limited PowerShell output fallback.</para>
    /// </summary>
    /// <example><para>Preview recording coloured output and a chapter</para><code>Invoke-ConsoleRecording { Add-ConsoleRecordingMarker 'Build'; Get-Date; Write-Host 'Done' -ForegroundColor Green } -Path ./demo.html -WhatIf</code></example>
    /// <example><para>Preview recording a child shell</para><code>Invoke-ConsoleRecording -Interactive -Path ./session.cast -WhatIf</code></example>
    /// <example><para>Preview recording an app driven by keys, then make a GIF of it</para><code>Invoke-ConsoleRecording -Headless -Width 100 -Height 30 -Keys 'wait:800 Down Down wait:600 Enter wait:1200 q' -Path ./app.cast -WhatIf {
    ///     Show-ConsoleApp -Content { New-ConsoleSelect -Key Color -Choices Red, Green, Blue }
    /// }
    /// Export-ConsoleRecording -RecordingPath ./app.cast -Path ./app.gif -WhatIf</code></example>
    [Cmdlet(VerbsLifecycle.Invoke, "ConsoleRecording", DefaultParameterSetName = "Script", SupportsShouldProcess = true)]
    [OutputType(typeof(FileInfo))]
    public sealed partial class CmdletInvokeConsoleRecording : ConsoleRecordingCommand {
        /// <summary><para type="description">Script to run in the recorded PowerShell child.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Script")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Headless")] public ScriptBlock ScriptBlock { get; set; } = null!;
        /// <summary><para type="description">Export path for a script; Interactive writes a cast here, or uses a timestamped name when omitted.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Script")]
        [Parameter(Mandatory = true, ParameterSetName = "Headless")]
        [Parameter(ParameterSetName = "Interactive")] public string Path { get; set; } = string.Empty;
        /// <summary><para type="description">Export colours.</para></summary>
        [Parameter(ParameterSetName = "Script")]
        [Parameter(ParameterSetName = "Headless")] public ExportTheme Theme { get; set; } = ExportTheme.Dark;
        /// <summary><para type="description">MacOS (default), Windows, Linux or None. Choose None to hide chrome.</para></summary>
        [Parameter(ParameterSetName = "Script"), PSDefaultValue(Value = WindowChromeStyle.MacOS, Help = "MacOS")]
        [Parameter(ParameterSetName = "Headless")] public WindowChromeStyle ChromeStyle { get; set; } = WindowChromeStyle.MacOS;

        /// <summary><para type="description">Export the final frame as a static SVG.</para></summary>
        [Parameter(ParameterSetName = "Script")]
        [Parameter(ParameterSetName = "Headless")] public SwitchParameter FinalFrame { get; set; }
        /// <summary><para type="description">Records an interactive child shell until it exits or stops recording.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Interactive")]
        public SwitchParameter Interactive { get; set; }
        /// <summary><para type="description">Runs the script in this session against a headless terminal and records what it shows. Caller variables and functions are available; only ConsoleMonster output is recorded.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Headless")]
        public SwitchParameter Headless { get; set; }
        /// <summary><para type="description">Input for -Headless: key names (Down, Enter, Ctrl+K, F2), repeats (Down*3), quoted text, paste:"text", resize:80x24 and wait:250 pauses. Input ends after the last key.</para></summary>
        [Parameter(ParameterSetName = "Headless")]
        public string? Keys { get; set; }

        /// <inheritdoc/>
        protected override void EndProcessing() {
            if (Headless.IsPresent && (InProcess.IsPresent || Shell != null)) { throw new ArgumentException("-Headless runs the script in this session; -InProcess and -Shell do not apply."); }
            string target = string.IsNullOrWhiteSpace(Path) ? "interactive recording in the current directory" : SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            if (!ShouldProcess(target, Interactive.IsPresent ? "Record a child shell" : "Run and record the script")) { return; }
            if (Interactive.IsPresent) { RecordInteractive(); return; }
            SessionRecordingOptions options = Options();
            string destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            string extension = System.IO.Path.GetExtension(destination).ToLowerInvariant();
            if (extension != ".html" && extension != ".htm" && extension != ".svg" && extension != ".cast" && extension != ".txt" && extension != ".md" && extension != ".markdown" && extension != ".json") { throw new ArgumentException("Use an HTML, SVG, cast, text, Markdown or player-data JSON destination."); }
            if (FinalFrame.IsPresent && extension != ".svg") { throw new ArgumentException("A final-frame export needs an .svg destination."); }
            if (File.Exists(destination)) { throw new IOException("The export destination already exists: " + destination); }
            if (RecordingPath == null && !string.Equals(System.IO.Path.GetExtension(destination), ".cast", StringComparison.OrdinalIgnoreCase)) { options.Path = System.IO.Path.ChangeExtension(destination, ".cast"); }
            else if (RecordingPath == null) { options.Path = destination; }
            if (string.Equals(options.Path, destination, StringComparison.OrdinalIgnoreCase) && extension != ".cast") { throw new ArgumentException("The native cast path and export destination must be different files."); }
            string cast;
            if (Headless.IsPresent) {
                cast = RecordHeadless(options);
            } else if (InProcess.IsPresent) {
                WriteWarning("In-process fallback records PowerShell output only; native terminal output is not captured.");
                cast = InProcessRecordingHost.Run(this, options, ScriptBlock.ToString(), () => IsStopping);
            } else {
                string executable = System.IO.Path.GetFileNameWithoutExtension(options.Shell!);
                if (!executable.Equals("pwsh", StringComparison.OrdinalIgnoreCase) && !executable.Equals("powershell", StringComparison.OrdinalIgnoreCase)) { throw new ArgumentException("Invoke-ConsoleRecording requires a PowerShell shell. Use Invoke-ConsoleRecording -Interactive for other shells."); }
                string script = "$ErrorActionPreference = 'Stop'; Import-Module '" + ModuleImportPath.Replace("'", "''") + "'; & {\n" + ScriptBlock + "\n}";
                options.Arguments.Add("-NoLogo"); options.Arguments.Add("-NoProfile"); options.Arguments.Add("-EncodedCommand");
                options.Arguments.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
                cast = Run(options, false);
            }
            if (!string.Equals(cast, destination, StringComparison.OrdinalIgnoreCase)) {
                if (File.Exists(destination)) { throw new IOException("The export destination already exists: " + destination); }
                SessionRecording recording = SessionRecording.Read(cast);
                string document = FinalFrame.IsPresent ? recording.ExportFinalSvg(new ExportOptions { Theme = Theme, Title = recording.Title, WindowChrome = ChromeStyle != WindowChromeStyle.None, ChromeStyle = ChromeStyle })
                    : extension == ".json" ? recording.ExportPlayerData(new RecordingExportOptions { Theme = Theme, Title = recording.Title, WindowChrome = ChromeStyle != WindowChromeStyle.None, ChromeStyle = ChromeStyle })
                    : PSOutputExport.IsMarkdown(PSOutputExport.FormatFor(null, destination)) ? MarkdownExportBridge.Recording(recording)
                    : recording.Export(ConsoleExporter.FormatFor(destination), new RecordingExportOptions { Theme = Theme, Title = recording.Title, WindowChrome = ChromeStyle != WindowChromeStyle.None, ChromeStyle = ChromeStyle });
                File.WriteAllText(destination, document, new UTF8Encoding(false));
            }
            WriteObject(new FileInfo(destination));
        }
    }
}
