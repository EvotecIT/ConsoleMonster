using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Adds items to a console widget made earlier: tree nodes, pairs, status items, bars, series, log lines, lanes or values.</para>
    /// <para type="description">The PowerShell way to grow a widget after it was created, instead of calling its .NET methods. Pipe the widget in and give the items as a script block or values; they are the same item commands the widget's own script block takes: Label/Children records for New-ConsoleTree, Key/Value records for New-ConsoleKeyValue, Key/Label/Status records for New-ConsoleStatusList and New-ConsolePhases, Label/Value records for New-ConsoleBarChart, Key/Values records for New-ConsoleLineChart, Message/Level records for New-ConsoleLogRegion and Key/Label records for New-ConsoleLanes. Sparklines and histograms take numbers, a cell map takes status words, and a live table takes objects or hashtables with a property per column. -PassThru writes the widget on, so it can go straight to Show-Console. For a widget inside a running dashboard, use the live commands (Update-Console, Update-Console, Update-Console, Write-ConsoleLogEntry...) instead.</para>
    /// </summary>
    /// <example>
    ///   <para>Add log lines as the script goes, then show the log</para>
    ///   <code>
    /// $log = New-ConsoleLogRegion -Height 5
    /// $log | Add-ConsoleItem { [pscustomobject]@{ Message = 'Discovered 4 domain controllers'; AtSeconds = 2 } }
    /// $log | Add-ConsoleItem { [pscustomobject]@{ Message = 'DC02 lags by 42 minutes'; Level = 'Warning'; AtSeconds = 31 } }
    /// $log | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Grow a tree in a loop</para>
    ///   <code>
    /// $tree = New-ConsoleTree 'Shares'
    /// foreach ($share in Get-SmbShare) {
    ///     $tree | Add-ConsoleItem { [pscustomobject]@{ Label = $share.Name; Detail = $share.Path } }
    /// }
    /// $tree | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Values for a sparkline, written on</para>
    ///   <code>New-ConsoleChart -Label 'req/s' -Type Sparkline | Add-ConsoleItem 3, 5, 8, 13, 9 -PassThru | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.Add, "ConsoleItem")]
    public sealed class CmdletAddConsoleItem : PSCmdlet {
        /// <summary><para type="description">The widget to add to (pipeline input): a tree or tree node, key/value list, status list, phases, bar chart, stacked bar, line chart, log, lanes, cell map, sparkline, histogram or live table.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = null!;

        /// <summary><para type="description">The items: a script block that emits them, or the items themselves.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public object Item { get; set; } = null!;

        /// <summary><para type="description">For a live table: the property whose value is the row key (default the first column).</para></summary>
        [Parameter]
        public string? KeyProperty { get; set; }

        /// <summary><para type="description">Writes the widget to the pipeline after adding.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object widget = PSText.Unwrap(InputObject) ?? InputObject;
            try { ConsoleHostItems.Add(widget, Item, KeyProperty); }
            catch (ArgumentException error) {
                ThrowTerminatingError(new ErrorRecord(error, "ConsoleMonsterItemNotAccepted", ErrorCategory.InvalidArgument, Item));
            }
            if (PassThru.IsPresent) {
                WriteObject(InputObject);
            }
        }
    }
}
