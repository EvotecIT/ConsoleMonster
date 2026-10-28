using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates an activity list: the work in progress right now, one row per activity, with a spinner or a bar and the time it has taken.</para>
    /// <para type="description">Each activity has an id, a title, an optional status, a percentage (or none, which spins) and an optional parent it is indented under; finished activities disappear, and an empty list takes no space. Update it with Update-Console from the script of a running dashboard or menu run. -MaxVisible caps the rows (more are summed up in one line). The default name, $progress, is also where Write-Progress goes in Invoke-ConsoleDashboard, so a list with that name shows the script's progress records; give another name for activities of your own.</para>
    /// </summary>
    /// <example>
    ///   <para>The checks running on four workers</para>
    ///   <code>
    /// $dashboard = New-ConsoleDashboard -Title 'Checks' -Content { New-ConsolePanel -Title 'Running' -Content { New-ConsoleActivityList -WidgetId running -MaxVisible 4 } }
    /// Invoke-ConsoleDashboard $dashboard {
    ///     Update-Console -WidgetId running -Key 1 -Label 'SRV01 · Free space'
    ///     Start-Sleep -Seconds 1
    ///     Update-Console -WidgetId running -Key 1 -Complete
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleActivityList")]
    [OutputType(typeof(ActivityList))]
    public sealed class CmdletNewConsoleActivityList : NoPipelineInputCmdlet {
        /// <summary><para type="description">The live name used by Update-Console (default $progress, where Write-Progress also goes in Invoke-ConsoleDashboard).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = ActivityList.ProgressTarget;

        /// <summary><para type="description">The most activities shown (default 8); more are summed up in one line.</para></summary>
        [Parameter]
        public int MaxVisible { get; set; } = 8;

        /// <summary><para type="description">Starts each row with the activity's id (#3), for parallel work.</para></summary>
        [Parameter]
        public SwitchParameter ShowIds { get; set; }

        /// <summary><para type="description">Leaves out the time each activity has taken at the end of its row.</para></summary>
        [Parameter]
        public SwitchParameter NoElapsed { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            WriteObject(new ActivityList(WidgetId) { MaxVisible = Math.Max(1, MaxVisible), ShowIds = ShowIds.IsPresent, ShowElapsed = !NoElapsed.IsPresent });
        }
    }
}
