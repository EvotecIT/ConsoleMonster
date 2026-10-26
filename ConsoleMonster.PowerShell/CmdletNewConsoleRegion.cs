using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named region whose content can be replaced while a dashboard runs.</para>
    /// <para type="description">Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Update-Console -WidgetId with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.</para>
    /// </summary>
    /// <example>
    ///   <para>A panel whose content switches from a spinner to a table</para>
    ///   <code>New-ConsolePanel -Title 'Result' -Content { New-ConsoleRegion -WidgetId main -Content { New-ConsoleProgress -Label 'Querying' -Indeterminate -WidgetId 'spinner' } }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRegion")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleRegion : ContainerCmdlet {
        /// <summary><para type="description">The region name used by Update-Console.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string WidgetId { get; set; } = string.Empty;

        /// <summary><para type="description">A script block whose widgets are the first content.</para></summary>
        [Parameter(Position = 1, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

        /// <summary><para type="description">The share of the remaining space in a row or column layout.</para></summary>
        [Parameter]
        public double Weight { get; set; }

        /// <summary><para type="description">A fixed height in rows.</para></summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary><para type="description">Takes the remaining height.</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            IWidget? content = children.Count == 0 ? null : children.Count == 1 ? children[0] : new Rows(children.ToArray());
            return PSWidgets.WithSize(new RegionWidget(WidgetId, content), Weight, Height, Fill.IsPresent);
        }
    }
}
