using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Records a script in a child PowerShell terminal and exports the result.</para>
    /// <para type="description">Runs the script in an isolated child process. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG. -InProcess explicitly selects the limited PowerShell output fallback.</para>
    /// </summary>
    /// <example><para>Record native coloured output and a chapter</para><code>Invoke-ConsoleRecording { Add-ConsoleRecordingMarker 'Build'; git --version; Write-Host 'Done' -ForegroundColor Green } -Destination ./demo.html</code></example>
    [Cmdlet(VerbsLifecycle.Invoke, "ConsoleRecording")]
    [OutputType(typeof(FileInfo))]
    public sealed class CmdletInvokeConsoleRecording : ConsoleRecordingCommand {
        /// <summary><para type="description">Script to run in the recorded PowerShell child.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] public ScriptBlock ScriptBlock { get; set; } = null!;
        /// <summary><para type="description">Export path. Extension selects HTML, SVG, cast, text or Markdown.</para></summary>
        [Parameter(Mandatory = true)] public string Destination { get; set; } = string.Empty;
        /// <summary><para type="description">Export colours.</para></summary>
        [Parameter] public ExportTheme Theme { get; set; } = ExportTheme.Dark;
        /// <summary><para type="description">Export the final frame as a static SVG.</para></summary>
        [Parameter] public SwitchParameter FinalFrame { get; set; }
        /// <inheritdoc/>
        protected override void EndProcessing() {
            SessionRecordingOptions options = Options();
            string destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);
            string extension = System.IO.Path.GetExtension(destination).ToLowerInvariant();
            if (extension != ".html" && extension != ".htm" && extension != ".svg" && extension != ".cast" && extension != ".txt" && extension != ".md" && extension != ".markdown" && extension != ".json") { throw new ArgumentException("Use an HTML, SVG, cast, text, Markdown or player-data JSON destination."); }
            if (FinalFrame.IsPresent && extension != ".svg") { throw new ArgumentException("A final-frame export needs an .svg destination."); }
            if (File.Exists(destination)) { throw new IOException("The export destination already exists: " + destination); }
            if (Path == null && !string.Equals(System.IO.Path.GetExtension(destination), ".cast", StringComparison.OrdinalIgnoreCase)) { options.Path = System.IO.Path.ChangeExtension(destination, ".cast"); }
            else if (Path == null) { options.Path = destination; }
            if (string.Equals(options.Path, destination, StringComparison.OrdinalIgnoreCase) && extension != ".cast") { throw new ArgumentException("The native cast path and export destination must be different files."); }
            string cast;
            if (InProcess.IsPresent) {
                WriteWarning("In-process fallback records PowerShell output only; native terminal output is not captured.");
                cast = InProcessRecordingHost.Run(this, options, ScriptBlock.ToString(), () => IsStopping);
            } else {
                string executable = System.IO.Path.GetFileNameWithoutExtension(options.Shell!);
                if (!executable.Equals("pwsh", StringComparison.OrdinalIgnoreCase) && !executable.Equals("powershell", StringComparison.OrdinalIgnoreCase)) { throw new ArgumentException("Invoke-ConsoleRecording requires a PowerShell shell. Use Start-ConsoleRecording for other shells."); }
                string script = "$ErrorActionPreference = 'Stop'; Import-Module '" + ModuleImportPath.Replace("'", "''") + "'; & {\n" + ScriptBlock + "\n}";
                options.Arguments.Add("-NoLogo"); options.Arguments.Add("-NoProfile"); options.Arguments.Add("-EncodedCommand");
                options.Arguments.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
                cast = Run(options, false);
            }
            if (!string.Equals(cast, destination, StringComparison.OrdinalIgnoreCase)) {
                if (File.Exists(destination)) { throw new IOException("The export destination already exists: " + destination); }
                SessionRecording recording = SessionRecording.Read(cast);
                string document = FinalFrame.IsPresent ? recording.ExportFinalSvg(new ExportOptions { Theme = Theme, Title = Name, WindowChrome = true })
                    : extension == ".json" ? recording.ExportPlayerData(new RecordingExportOptions { Theme = Theme, Title = Name, WindowChrome = true })
                    : recording.Export(ConsoleExporter.FormatFor(destination), new RecordingExportOptions { Theme = Theme, Title = Name, WindowChrome = true });
                File.WriteAllText(destination, document, new UTF8Encoding(false));
            }
            WriteObject(new FileInfo(destination));
        }
    }
}
