using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Starts, updates or finishes an activity of an activity list (New-ConsoleActivityList) in the running dashboard.</para>
    /// <para type="description">-Id identifies the activity: a new id adds a row, an existing one updates it, and -Completed removes it (with the activities under it). -Activity is the title, -Detail a short text after it, -PercentComplete the share done (leave it out for work of unknown length, which spins) and -SecondsRemaining an estimate shown as time left. -ParentId indents the activity under another one. The time each activity has taken follows the session clock, so a simulated clock (New-ConsoleSimulatedClock) drives it too. Safe to call from any runspace or thread.</para>
    /// </summary>
    /// <example>
    ///   <para>Show a check while it runs, then remove it</para>
    ///   <code>
    /// Update-ConsoleActivity -WidgetId running -Id 7 -Activity 'SRV01 · Cert chain'
    /// Update-ConsoleActivity -WidgetId running -Id 7 -Completed</code>
    /// </example>
    /// <example>
    ///   <para>A report written in steps, with a bar</para>
    ///   <code>1..4 | ForEach-Object { Update-ConsoleActivity -WidgetId running -Id 9000 -Activity 'Writing fleet.html' -PercentComplete ($_ * 25) }</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "ConsoleActivity", DefaultParameterSetName = "Update")]
    public sealed class CmdletUpdateConsoleActivity : LiveCmdlet {
        /// <summary><para type="description">The activity id (a new id adds a row).</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public int Id { get; set; }

        /// <summary><para type="description">The title.</para></summary>
        [Parameter(Position = 2, ParameterSetName = "Update")]
        public object? Activity { get; set; }

        /// <summary><para type="description">A short text after the title ("retrying", "3 of 8").</para></summary>
        [Parameter(ParameterSetName = "Update")]
        public string? Detail { get; set; }

        /// <summary><para type="description">The share done, 0 to 100; leave it out for work of unknown length (it spins).</para></summary>
        [Parameter(ParameterSetName = "Update")]
        public double PercentComplete { get; set; } = -1;

        /// <summary><para type="description">An estimate of the seconds left, shown as time left.</para></summary>
        [Parameter(ParameterSetName = "Update")]
        public double SecondsRemaining { get; set; } = -1;

        /// <summary><para type="description">The id of the activity this one is indented under.</para></summary>
        [Parameter(ParameterSetName = "Update")]
        public int ParentId { get; set; } = -1;

        /// <summary><para type="description">Finishes the activity: its row (and the rows under it) disappear.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Complete")]
        public SwitchParameter Completed { get; set; }

        /// <summary><para type="description">The activity list name (default $progress).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = ActivityList.ProgressTarget;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            string key = Id.ToString(CultureInfo.InvariantCulture);
            if (Completed.IsPresent) {
                Send(new LiveEvent(WidgetId, "complete", key));
                return;
            }
            if (ParentId >= 0) {
                key += "/" + ParentId.ToString(CultureInfo.InvariantCulture);
            }
            string title = Activity == null ? string.Empty : PSWidgets.ToText(Activity);
            Send(new LiveEvent(WidgetId, "update", key, title, string.IsNullOrEmpty(Detail) ? null : Detail, PercentComplete < 0 ? -1 : PercentComplete, SecondsRemaining < 0 ? -1 : SecondsRemaining));
        }
    }
}
