using System.Management.Automation;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>Stops recording from inside the recorded child shell.</summary>
    /// <remarks><para>Sends a private terminal control to the parent recorder, which finalizes local files and closes this recorded child terminal. Run it inside a Invoke-ConsoleRecording -Interactive session.</para></remarks>
    /// <example><para>Finish from inside the recorded child shell</para><code>Write-Host 'Deployment finished'
    /// Stop-ConsoleRecording</code></example>
    /// <example>
    /// <para>End a recording from inside the recorded shell</para>
    /// <code># In a shell started with Invoke-ConsoleRecording -Interactive:
    /// Write-ConsoleText 'Inspection complete' -Role Pass
    /// Stop-ConsoleRecording
    /// </code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Stop, "ConsoleRecording", HelpUri = "https://consolemonster.dev/powershell/commands/stop-consolerecording/")]
    public sealed class CmdletStopConsoleRecording : NoPipelineInputCmdlet {
        /// <inheritdoc/>
        protected override void EndProcessing() {
            if (SessionState.PSVariable.GetValue("ConsoleRecording") is SessionRecorder recorder) { recorder.Stop(); return; }
            Host.UI.Write(SessionRecorder.StopSequence);
        }
    }
}
