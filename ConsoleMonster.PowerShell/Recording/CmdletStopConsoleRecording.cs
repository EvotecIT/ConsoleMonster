using System.Management.Automation;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Stops recording from inside the recorded child shell.</para><para type="description">Sends a private terminal control to the parent recorder, which finalizes local files and closes this recorded child terminal. Run it inside a Start-ConsoleRecording session.</para></summary>
    /// <example><para>Finish from inside the recorded child shell</para><code>Write-Host 'Deployment finished'
    /// Stop-ConsoleRecording</code></example>
    [Cmdlet(VerbsLifecycle.Stop, "ConsoleRecording")]
    public sealed class CmdletStopConsoleRecording : PSCmdlet {
        /// <inheritdoc/>
        protected override void EndProcessing() {
            if (SessionState.PSVariable.GetValue("ConsoleRecording") is SessionRecorder recorder) { recorder.Stop(); return; }
            Host.UI.Write(SessionRecorder.StopSequence);
        }
    }
}
