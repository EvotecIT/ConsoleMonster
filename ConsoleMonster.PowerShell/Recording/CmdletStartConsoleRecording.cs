using System.IO;
using System.Management.Automation;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Opens a recorded child shell in a pseudo-console.</para>
    /// <para type="description">Records terminal output and resizes to local asciicast v2. Exit the child shell or run Stop-ConsoleRecording inside it to finalize. Input bytes are never stored; output secret patterns are redacted before writing. The title shows a REC indicator. Use -InProcess for explicitly limited PowerShell output capture when pseudo-consoles are unavailable.</para>
    /// </summary>
    /// <example><para>Record a PowerShell session</para><code>Start-ConsoleRecording -Path ./demo.cast -Name 'Deployment'</code></example>
    [Cmdlet(VerbsLifecycle.Start, "ConsoleRecording")]
    [OutputType(typeof(FileInfo))]
    public sealed class CmdletStartConsoleRecording : ConsoleRecordingCommand {
        /// <inheritdoc/>
        protected override void EndProcessing() {
            SessionRecordingOptions options = Options();
            if (InProcess.IsPresent) {
                WriteWarning("In-process fallback records PowerShell output only. Native programs, cursor movement and terminal input are not captured.");
                WriteObject(new FileInfo(InProcessRecordingHost.Run(this, options, null, () => IsStopping)));
                return;
            }
            // PowerShell shells load the recording commands in their own process; other shells may print OSC controls.
            string executable = System.IO.Path.GetFileNameWithoutExtension(options.Shell!);
            if (executable.Equals("pwsh", System.StringComparison.OrdinalIgnoreCase) || executable.Equals("powershell", System.StringComparison.OrdinalIgnoreCase)) {
                options.Arguments.Add("-NoLogo"); options.Arguments.Add("-NoExit"); options.Arguments.Add("-Command");
                options.Arguments.Add("Import-Module '" + ModuleImportPath.Replace("'", "''") + "'");
            }
            WriteObject(new FileInfo(Run(options, true)));
        }
    }
}
