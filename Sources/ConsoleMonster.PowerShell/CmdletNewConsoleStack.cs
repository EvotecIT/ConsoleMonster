using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Layers console widgets on top of each other in the same space.</para>
    /// <para type="description">Each widget the scriptblock emits is a layer, bottom first; later layers paint over earlier ones and replace only the cells they write. Combine with New-ConsoleAlign to place an overlay, such as a badge in a corner of a chart. To place widgets one below another, use New-ConsoleRows.</para>
    /// <example>
    ///   <para>A chart with a status badge in its top-right corner</para>
    ///   <code>New-ConsoleStack {
    ///     New-ConsoleLineChart -Name latency
    ///     New-ConsoleAlign -Horizontal Right { New-ConsoleText '[pass]LIVE[/]' }
    /// }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleStack")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleStack : PSCmdlet {
        /// <summary><para type="description">Scriptblock that emits the layers, bottom first.</para></summary>
        [Parameter(Position = 0)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Layers to use instead of, or after, the scriptblock output.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public IWidget[]? Widget { get; set; }

        /// <summary><para type="description">Share of the width (in columns) or height (in a live dashboard) this widget takes.</para></summary>
        [Parameter]
        public double Weight { get; set; }

        /// <summary><para type="description">Fixed height in rows.</para></summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard.</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        private readonly List<IWidget> _layers = new List<IWidget>();

        /// <inheritdoc />
        protected override void BeginProcessing() {
            _layers.AddRange(PSWidgets.Collect(Content));
        }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Widget != null) {
                _layers.AddRange(Widget);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            WriteObject(PSWidgets.WithSize(new Stack(_layers.ToArray()), Weight, Height, Fill.IsPresent));
        }
    }
}
