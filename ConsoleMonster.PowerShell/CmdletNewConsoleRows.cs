using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Stacks console widgets vertically.</para>
    /// <para type="description">Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.</para>
    /// </summary>
    /// <example>
    ///   <para>Two widgets stacked with a blank row between them</para>
    ///   <code>New-ConsoleRows -Gap 1 -Content { New-ConsoleRule -Title 'Summary'; New-ConsoleKeyValue -InputObject @{ Total = 12 } }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRows")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleRows : ContainerCmdlet {
        /// <summary><para type="description">Scriptblock that emits the child widgets.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

        /// <summary><para type="description">Blank rows between children.</para></summary>
        [Parameter]
        public int Gap { get; set; }

        /// <summary><para type="description">Share of the space this group takes inside its parent.</para></summary>
        [Parameter]
        public double Weight { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard.</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            Rows rows = new Rows(children.ToArray()) { Gap = Gap };
            return PSWidgets.WithSize(rows, Weight, 0, Fill.IsPresent);
        }
    }
}
