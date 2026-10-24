using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a cell map: one status symbol per item of a run, with a legend of counts.</para>
    /// <para type="description">The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. -Columns and -RowLabels turn it into a labelled grid (one row per server, one column per check) with one legend below, which -LegendLines lets wrap. To add cells later, pipe the map to Add-ConsoleItem; in a live dashboard, update cells with Update-ConsoleCell.</para>
    /// </summary>
    /// <example>
    ///   <para>Forty items part-way through a run</para>
    ///   <code>
    /// $states = @('Pass') * 28 + @('Fail') * 2 + @('Warn') + @('Running') * 4
    /// New-ConsoleCellMap -Status $states -Count 40 | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>States from objects</para>
    ///   <code>$results | ForEach-Object Result | New-ConsoleCellMap | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A grid of three servers by four checks, with the server names beside the rows</para>
    ///   <code>New-ConsoleCellMap -WidgetId checks -Count 12 -Columns 4 -RowLabels 'SRV01', 'SRV02', 'SRV03' -LegendLines 2 -Status Pass, Pass, Fail, Warn, Pass | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleCellMap")]
    [OutputType(typeof(CellMap))]
    public sealed class CmdletNewConsoleCellMap : PSCmdlet {
        private readonly List<object> _states = new List<object>();

        /// <summary><para type="description">The item states, in order: Pass, Fail, Warn, Skip, Info, Running or Pending (pipeline input works too).</para></summary>
        [Parameter(Position = 1, ValueFromPipeline = true)]
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

        /// <summary><para type="description">The cells per row (0 = as many as fit). Set it to draw a grid, such as one row per server and one column per check; cells past the width are cut off rather than moved to the next row.</para></summary>
        [Parameter]
        public int Columns { get; set; }

        /// <summary><para type="description">Labels drawn muted before the rows, one per row in order; the cells start after the widest label. With -Columns they make a labelled grid with one legend below it.</para></summary>
        [Parameter]
        public string[]? RowLabels { get; set; }

        /// <summary><para type="description">The most lines the legend wraps onto when it is wider than the map (default 1: it is cut off instead).</para></summary>
        [Parameter]
        public int LegendLines { get; set; } = 1;

        /// <summary><para type="description">The live name (default cells).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "cells";

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
            CellMap map = new CellMap(WidgetId) { MaxRows = MaxRows, ShowLegend = !NoLegend.IsPresent, Columns = Math.Max(0, Columns), LegendLines = Math.Max(1, LegendLines) };
            if (RowLabels != null) {
                foreach (string label in RowLabels) {
                    map.RowLabels.Add(label ?? string.Empty);
                }
            }
            PSItems.AddAll(this, map, _states);
            if (Count > map.Count) {
                map.Resize(Count);
            }
            WriteObject(map);
        }
    }
}
