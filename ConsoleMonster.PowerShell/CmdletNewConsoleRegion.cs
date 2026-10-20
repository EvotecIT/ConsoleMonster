using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named region whose content can be replaced while a dashboard runs.</para>
    /// <para type="description">Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Set-ConsoleRegion -Name with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.</para>
    /// </summary>
    /// <example>
    ///   <para>A panel whose content switches from a spinner to a table</para>
    ///   <code>New-ConsolePanel -Title 'Result' { New-ConsoleRegion -Name main { New-ConsoleSpinner -Label 'Querying' } }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRegion")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleRegion : PSCmdlet {
        /// <summary><para type="description">The region name used by Set-ConsoleRegion.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">A script block whose widgets are the first content.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">A widget as the first content.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public IWidget? Widget { get; set; }

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
        protected override void ProcessRecord() {
            List<IWidget> children = PSWidgets.Collect(Content);
            if (Widget != null) {
                children.Add(Widget);
            }
            IWidget? content = children.Count == 0 ? null : children.Count == 1 ? children[0] : new Rows(children.ToArray());
            WriteObject(PSWidgets.WithSize(new Slot(Name, content), Weight, Height, Fill.IsPresent));
        }
    }
}
