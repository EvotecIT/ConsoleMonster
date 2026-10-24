using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Sets the state of cells of a cell map (New-ConsoleCellMap) in the running dashboard.</para>
    /// <para type="description">-Index names the cells by position, from 0 (in a grid with -Columns, row * columns + column); -Status is their new state: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success and Error work too). An index past the end grows the map. -Add appends cells in the -Status state instead, and -Count sets the number of cells (new ones are pending). Safe to call from any runspace or thread.</para>
    /// </summary>
    /// <example>
    ///   <para>A check starts and passes on the second server of a grid with five checks per row</para>
    ///   <code>
    /// Update-ConsoleCell -WidgetId checks -Index 7 -Status Running
    /// Update-ConsoleCell -WidgetId checks -Index 7 -Status Pass</code>
    /// </example>
    /// <example>
    ///   <para>Grow a map as items are found, then mark them</para>
    ///   <code>
    /// Update-ConsoleCell -WidgetId files -Count 40
    /// Update-ConsoleCell -WidgetId files -Index 0, 1, 2 -Status Pass</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "ConsoleCell", DefaultParameterSetName = "Set")]
    public sealed class CmdletUpdateConsoleCell : LiveCmdlet {
        /// <summary><para type="description">The cells to set, by position from 0.</para></summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "Set")]
        public int[] Index { get; set; } = new int[0];

        /// <summary><para type="description">The new state (for -Add, the state of the new cells; default Pending).</para></summary>
        [Parameter(Position = 2, Mandatory = true, ParameterSetName = "Set")]
        [Parameter(ParameterSetName = "Add")]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Status { get; set; } = StatusKind.Pending;

        /// <summary><para type="description">Appends cells in the -Status state.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Add")]
        public SwitchParameter Add { get; set; }

        /// <summary><para type="description">How many cells -Add appends (default 1).</para></summary>
        [Parameter(ParameterSetName = "Add")]
        [ValidateRange(1, int.MaxValue)]
        public int Times { get; set; } = 1;

        /// <summary><para type="description">Sets the number of cells: new ones are pending, extra ones are removed (at least 1).</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Count")]
        [ValidateRange(1, int.MaxValue)]
        public int Count { get; set; }

        /// <summary><para type="description">The cell map name (default cells).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "cells";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            switch (ParameterSetName) {
                case "Count":
                    Send(new LiveEvent(WidgetId, "count", value: Count));
                    break;
                case "Add":
                    for (int i = 0; i < Times; i++) {
                        Send(new LiveEvent(WidgetId, "add", status: Status.ToString()));
                    }
                    break;
                default:
                    foreach (int index in Index) {
                        if (index >= 0) {
                            Send(new LiveEvent(WidgetId, "set", index.ToString(CultureInfo.InvariantCulture), status: Status.ToString()));
                        }
                    }
                    break;
            }
        }
    }
}
