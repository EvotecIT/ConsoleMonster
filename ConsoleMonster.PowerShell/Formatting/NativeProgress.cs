using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Routes native Write-Progress data to the optional progress area or the original host stream.</summary>
    public static class NativeProgress {
        /// <summary>Preserves native preferences and record semantics; the proxy supplies runtime-native parameters.</summary>
        /// <param name="cmdlet">The native-parameter proxy executing on its pipeline thread.</param>
        /// <param name="id">The native activity identifier.</param>
        /// <param name="parentId">The native parent activity identifier.</param>
        /// <param name="activity">The native activity title.</param>
        /// <param name="status">The native activity status.</param>
        /// <param name="currentOperation">The operation text.</param>
        /// <param name="percentComplete">Completion percent, or minus one when unknown.</param>
        /// <param name="secondsRemaining">Estimated seconds, or minus one when unknown.</param>
        /// <param name="completed">Whether the activity has finished.</param>
        public static void Write(PSCmdlet cmdlet, int id, int parentId, string? activity, string? status, string? currentOperation, int percentComplete, int secondsRemaining, bool completed) {
            object? value = cmdlet.MyInvocation.BoundParameters.TryGetValue("ProgressAction", out object? action) ? action : cmdlet.SessionState.PSVariable.GetValue("ProgressPreference");
            value = PSText.Unwrap(value);
            ActionPreference preference = value is ActionPreference actual ? actual : Enum.TryParse(value?.ToString(), true, out ActionPreference parsed) ? parsed : ActionPreference.Continue;
            if (preference == ActionPreference.SilentlyContinue || preference == ActionPreference.Ignore) { return; }
            string? text = string.IsNullOrEmpty(currentOperation) ? status : string.IsNullOrEmpty(status) ? currentOperation : status + " - " + currentOperation;
            if (preference == ActionPreference.Continue && ConsoleFormatting.Displayed(cmdlet) && SessionProgress.Report(id, parentId, activity, text, percentComplete, secondsRemaining, completed)) { return; }
            cmdlet.WriteProgress(new ProgressRecord(id, string.IsNullOrEmpty(activity) ? " " : activity, string.IsNullOrEmpty(status) ? " " : status) {
                PercentComplete = percentComplete, SecondsRemaining = secondsRemaining, CurrentOperation = currentOperation,
                ParentActivityId = parentId, RecordType = completed ? ProgressRecordType.Completed : ProgressRecordType.Processing
            });
        }
    }
}
