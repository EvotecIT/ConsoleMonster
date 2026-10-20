using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes a progress update, drawn by ConsoleMonster when Enable-ConsoleFormatting -Feature Progress is on.</para>
    /// <para type="description">Takes the same parameters as Write-Progress. With Enable-ConsoleFormatting -Feature Progress, Write-Progress is an alias of this command for the session, so scripts and functions that call Write-Progress get ConsoleMonster's progress area: one row per activity below the cursor (nested with -ParentId), with a bar, the percentage, the status and the elapsed time. Output and errors written in between appear above the area, warnings and Write-Host lines too, and the area disappears when every activity completes or the command ends. $ProgressPreference and -ProgressAction are honoured. Without the feature, in redirected output, jobs, remoting and other hosts, it writes PowerShell's own progress record. Progress written by compiled cmdlets (Copy-Item, Invoke-WebRequest) is still shown by PowerShell.</para>
    /// <example>
    ///   <para>An existing loop, after Enable-ConsoleFormatting -Feature Progress</para>
    ///   <code>foreach ($i in 1..50) { Write-Progress -Activity 'Copying' -Status "File $i of 50" -PercentComplete ($i * 2); Start-Sleep -Milliseconds 50 }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleProgress")]
    public sealed class CmdletWriteConsoleProgress : PSCmdlet {
        /// <summary><para type="description">The activity (the row's title).</para></summary>
        [Parameter(Position = 0)]
        public string? Activity { get; set; }

        /// <summary><para type="description">The status shown after the bar.</para></summary>
        [Parameter(Position = 1)]
        public string? Status { get; set; }

        /// <summary><para type="description">The activity's id; different ids are separate rows.</para></summary>
        [Parameter(Position = 2)]
        [ValidateRange(0, int.MaxValue)]
        public int Id { get; set; }

        /// <summary><para type="description">The percentage done (0-100), or -1 when it is not known.</para></summary>
        [Parameter]
        [ValidateRange(-1, 100)]
        public int PercentComplete { get; set; } = -1;

        /// <summary><para type="description">The seconds left, or -1 when it is not known.</para></summary>
        [Parameter]
        public int SecondsRemaining { get; set; } = -1;

        /// <summary><para type="description">The current operation, shown after the status.</para></summary>
        [Parameter]
        public string? CurrentOperation { get; set; }

        /// <summary><para type="description">The parent activity's id: the row is nested under it.</para></summary>
        [Parameter]
        [ValidateRange(-1, int.MaxValue)]
        public int ParentId { get; set; } = -1;

        /// <summary><para type="description">The activity is done: its row (and its children) disappear.</para></summary>
        [Parameter]
        public SwitchParameter Completed { get; set; }

        /// <summary><para type="description">Accepted for compatibility with Write-Progress and ignored.</para></summary>
        [Parameter]
        public int SourceId { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            ActionPreference preference = Preference();
            if (preference == ActionPreference.SilentlyContinue || preference == ActionPreference.Ignore) {
                return;
            }
            string? text = string.IsNullOrEmpty(CurrentOperation) ? Status : string.IsNullOrEmpty(Status) ? CurrentOperation : Status + " - " + CurrentOperation;
            if (preference == ActionPreference.Continue && ConsoleFormatting.Displayed(this) && SessionProgress.Report(Id, ParentId, Activity, text, PercentComplete, SecondsRemaining, Completed.IsPresent)) {
                return;
            }
            // PowerShell's own progress: the same record Write-Progress writes.
            ProgressRecord record = new ProgressRecord(Id, string.IsNullOrEmpty(Activity) ? " " : Activity, string.IsNullOrEmpty(Status) ? " " : Status) {
                PercentComplete = PercentComplete,
                SecondsRemaining = SecondsRemaining,
                CurrentOperation = CurrentOperation,
                ParentActivityId = ParentId,
                RecordType = Completed.IsPresent ? ProgressRecordType.Completed : ProgressRecordType.Processing
            };
            WriteProgress(record);
        }

        // Delegate Stop, Inquire, Break and Suspend to PowerShell before any custom area can consume the record.
        private ActionPreference Preference() {
            object? value = MyInvocation.BoundParameters.TryGetValue("ProgressAction", out object? action) ? action : GetVariableValue("ProgressPreference");
            if (value is PSObject wrapped) {
                value = wrapped.BaseObject;
            }
            if (value is ActionPreference preference) {
                return preference;
            }
            return Enum.TryParse(value?.ToString(), true, out ActionPreference parsed) ? parsed : ActionPreference.Continue;
        }
    }
}
