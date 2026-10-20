using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Adds a value to a chart in a running dashboard.</para>
    /// <para type="description">Appends to a sparkline or line-chart series (-Key names the series), counts a value in a histogram, or adds to a bar or stacked-bar segment (-Key names it).</para>
    /// </summary>
    /// <example>
    ///   <para>Append a throughput sample</para>
    ///   <code>Add-ConsoleValue -Name throughput -Key completed -Value 42</code>
    /// </example>
    [Cmdlet(VerbsCommon.Add, "ConsoleValue")]
    public sealed class CmdletAddConsoleValue : LiveCmdlet {
        /// <summary><para type="description">The chart name.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">The value.</para></summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
        public double Value { get; set; }

        /// <summary><para type="description">The series, bar or segment.</para></summary>
        [Parameter]
        public string? Key { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Send(new LiveEvent(Name, "add", Key, value: Value));
        }
    }
}
