using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Layers console widgets on top of each other in the same space.</para>
    /// <para type="description">Each widget the scriptblock emits is a layer, bottom first; later layers paint over earlier ones and replace only the cells they write. Combine with New-ConsoleRegion to place an overlay, such as a badge in a corner of a chart. To place widgets one below another, use New-ConsoleRows.</para>
    /// </summary>
    /// <example>
    ///   <para>A chart with a status badge in its top-right corner</para>
    ///   <code>
    /// New-ConsoleStack -Content {
    ///     New-ConsoleChart -WidgetId latency -Type Line
    ///     New-ConsoleRegion -Horizontal Right -Content { New-ConsoleText -Markup '[pass]LIVE[/]' }
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleStack")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleStack : ContainerCmdlet {
        /// <summary><para type="description">Scriptblock that emits the layers, bottom first.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

        /// <summary><para type="description">Share of the width (in columns) or height (in a live dashboard) this widget takes.</para></summary>
        [Parameter]
        public double Weight { get; set; }

        /// <summary><para type="description">Fixed height in rows.</para></summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard.</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) => PSWidgets.WithSize(new Stack(children.ToArray()), Weight, Height, Fill.IsPresent);
    }
}
