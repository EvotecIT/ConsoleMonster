using System.Management.Automation;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Adds a named playback chapter to a recorded terminal session.</para><para type="description">Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.</para></summary>
    /// <example><para>Mark a deployment step</para><code>Add-ConsoleRecordingMarker 'Deploy'</code></example>
    [Cmdlet(VerbsCommon.Add, "ConsoleRecordingMarker")]
    public sealed class CmdletAddConsoleRecordingMarker : PSCmdlet {
        /// <summary><para type="description">Chapter label.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] [ValidateNotNullOrEmpty] public string Label { get; set; } = string.Empty;
        /// <inheritdoc/>
        protected override void EndProcessing() {
            if (SessionState.PSVariable.GetValue("ConsoleRecording") is SessionRecorder recorder) { recorder.AddMarker(Label); return; }
            Host.UI.Write(SessionRecorder.MarkerSequence(Label));
        }
    }
}
