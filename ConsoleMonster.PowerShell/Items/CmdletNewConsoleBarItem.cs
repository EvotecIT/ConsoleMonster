using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one bar of a bar chart, or one segment of a stacked bar, with its own tone.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleBarChart when a bar or a segment needs its own colour: -Tone takes a theme role (Pass, Fail, Warn, Skip, Info, Accent, Accent2, Muted, Series1 to Series6). Without -Tone the chart's colour is used, and a stacked bar picks the next series colour. The label is shown literally; rich text from New-ConsoleText keeps its colours. To add bars to a chart made earlier, pipe the chart to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>Results as a stacked bar</para>
    ///   <code>
    /// New-ConsoleBarChart -Stacked {
    ///     New-ConsoleBarItem 'Passed' 412 -Tone Pass
    ///     New-ConsoleBarItem 'Warnings' 51 -Tone Warn
    ///     New-ConsoleBarItem 'Failed' 23 -Tone Fail
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>One bar picked out</para>
    ///   <code>
    /// New-ConsoleBarChart -Unit ' ms' {
    ///     New-ConsoleBarItem 'Cloudflare' 12
    ///     New-ConsoleBarItem 'Quad9' 64 -Tone Warn
    /// } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleBarItem")]
    [OutputType(typeof(ChartItem))]
    public sealed class CmdletNewConsoleBarItem : PSCmdlet {
        /// <summary><para type="description">The label (shown literally; rich text from New-ConsoleText keeps its colours).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public object Label { get; set; } = string.Empty;

        /// <summary><para type="description">The value.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public double Value { get; set; }

        /// <summary><para type="description">The colour as a theme role: Pass, Fail, Warn, Skip, Info, Accent, Accent2, Muted or Series1 to Series6.</para></summary>
        [Parameter]
        public ThemeToken? Tone { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Text.RichText? rich = PSText.Rich(Label);
            WriteObject(new ChartItem(rich?.PlainText ?? PSText.PlainText(Label), Value, Tone) { LabelText = rich });
        }
    }
}
