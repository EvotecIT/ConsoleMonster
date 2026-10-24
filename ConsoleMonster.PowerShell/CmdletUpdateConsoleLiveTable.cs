using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Adds, updates, removes or clears rows of a live table (New-ConsoleLiveTable) in the running dashboard.</para>
    /// <para type="description">-Key identifies the row: a new key adds a row, an existing key replaces its values. -Remove deletes the row and -Clear deletes them all; the column widths stay.</para>
    /// </summary>
    /// <example>
    ///   <para>Track a job from start to finish</para>
    ///   <code>Update-ConsoleLiveTable -WidgetId jobs -Key $job.Id -Value $job.Name, 'Running', $elapsed</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "ConsoleLiveTable", DefaultParameterSetName = "Row")]
    public sealed class CmdletUpdateConsoleLiveTable : LiveCmdlet {
        /// <summary><para type="description">The live table name (default table).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "table";

        /// <summary><para type="description">The stable row key.</para></summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "Row")]
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "Remove")]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The values, one per column (numbers, dates, durations and statuses go through the column presenters).</para></summary>
        [Parameter(Position = 2, ParameterSetName = "Row")]
        public object[]? Value { get; set; }

        /// <summary><para type="description">Removes the row with that key.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Remove")]
        public SwitchParameter Remove { get; set; }

        /// <summary><para type="description">Removes every row.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Clear")]
        public SwitchParameter Clear { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Clear.IsPresent) {
                Send(new LiveEvent(WidgetId, "clear"));
            } else if (Remove.IsPresent) {
                Send(new LiveEvent(WidgetId, "remove", Key));
            } else {
                object?[] values = new object?[Value?.Length ?? 0];
                for (int i = 0; i < values.Length; i++) {
                    object? value = Value![i];
                    values[i] = value is PSObject ps ? ps.BaseObject : value;
                }
                Send(new LiveEvent(WidgetId, "update", Key, LiveTable.TextOf(values)) { Payload = values });
            }
        }
    }
}
