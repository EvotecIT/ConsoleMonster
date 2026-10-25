using System.Management.Automation;
using ConsoleMonster.Sessions;
using ConsoleMonster.Export;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Adds a chapter or an export hold to a recorded terminal session.</para><para type="description">Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.</para></summary>
    /// <example><para>Mark a deployment step</para><code>Add-ConsoleRecordingMarker 'Deploy'</code></example>
    /// <example><para>Hold this screen during export without waiting while recording</para><code>Add-ConsoleRecordingMarker -HoldSeconds 3</code></example>
    [Cmdlet(VerbsCommon.Add, "ConsoleRecordingMarker")]
    public sealed class CmdletAddConsoleRecordingMarker : PSCmdlet {
        /// <summary><para type="description">Chapter label.</para></summary>
        [Parameter(Position = 0)] [ValidateNotNullOrEmpty] public string? Label { get; set; }
        /// <summary><para type="description">Freezes the exported screen for this many seconds without sleeping during capture. The idle limit never trims this hold. Label is optional for a hold.</para></summary>
        [Parameter, ValidateRange(0, 86400)] public double HoldSeconds { get; set; }
        /// <inheritdoc/>
        protected override void EndProcessing() {
            if (Label == null && !MyInvocation.BoundParameters.ContainsKey(nameof(HoldSeconds))) { throw new System.ArgumentException("Supply a chapter label or -HoldSeconds."); }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(HoldSeconds))) {
                System.TimeSpan hold = RecordingTimeline.FromMilliseconds(HoldSeconds * 1000);
                if (SessionState.PSVariable.GetValue("ConsoleRecording") is SessionRecorder active) { active.AddMarker(hold, Label); return; }
                Host.UI.Write(SessionRecorder.MarkerSequence(hold, Label)); return;
            }
            if (SessionState.PSVariable.GetValue("ConsoleRecording") is SessionRecorder recorder) { recorder.AddMarker(Label!); return; }
            Host.UI.Write(SessionRecorder.MarkerSequence(Label!));
        }
    }
}
