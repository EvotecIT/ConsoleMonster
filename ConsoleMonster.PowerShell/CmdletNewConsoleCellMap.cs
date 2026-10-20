using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a cell map: one status symbol per item of a run, with a legend of counts.</para>
    /// <para type="description">The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. To add cells later, pipe the map to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>Forty items part-way through a run</para>
    ///   <code>
    /// $states = @('Pass') * 28 + @('Fail') * 2 + @('Warn') + @('Running') * 4
    /// New-ConsoleCellMap -Status $states -Count 40 | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>States from objects</para>
    ///   <code>$results | ForEach-Object Result | New-ConsoleCellMap | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleCellMap")]
    [OutputType(typeof(CellMap))]
    public sealed class CmdletNewConsoleCellMap : PSCmdlet {
        private readonly List<object> _states = new List<object>();

        /// <summary><para type="description">The item states, in order: Pass, Fail, Warn, Skip, Info, Running or Pending (pipeline input works too).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public object[]? Status { get; set; }

        /// <summary><para type="description">The total number of items; cells beyond the given states are pending.</para></summary>
        [Parameter]
        public int Count { get; set; }

        /// <summary><para type="description">The most rows of cells (0 = all); a last line counts the items not shown.</para></summary>
        [Parameter]
        public int MaxRows { get; set; }

        /// <summary><para type="description">Hide the legend line with each state's count.</para></summary>
        [Parameter]
        public SwitchParameter NoLegend { get; set; }

        /// <summary><para type="description">The live name (default cells).</para></summary>
        [Parameter]
        public string Name { get; set; } = "cells";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Status != null) {
                foreach (object state in Status) {
                    _states.AddRange(PSItems.Items(state));
                }
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            CellMap map = new CellMap(Name) { MaxRows = MaxRows, ShowLegend = !NoLegend.IsPresent };
            PSItems.AddAll(this, map, _states);
            if (Count > map.Count) {
                map.Resize(Count);
            }
            WriteObject(map);
        }
    }
}
