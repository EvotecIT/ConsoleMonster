using System.IO;
using ConsoleMonster.Sessions;
namespace ConsoleMonster.PowerShell {
    public sealed partial class CmdletInvokeConsoleRecording {
        // Interactive recording shares the same bounded recorder and teardown.
        private void RecordInteractive() {
            SessionRecordingOptions options = Options();
            if (!string.IsNullOrEmpty(Path)) { options.Path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path); }
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
