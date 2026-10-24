using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Places console widgets side by side.</para>
    /// <para type="description">Children share the width equally unless they were created with -Weight. -FixedWidth gives children a fixed width in cells instead, by position (a narrow histogram beside two tables). Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content: by default below the sum of the children's minimum widths, or below -CollapseBelow cells.</para>
    /// </summary>
    /// <example>
    ///   <para>Two panels, the first twice as wide</para>
    ///   <code>New-ConsoleColumns -Content { New-ConsolePanel -Title 'Steps' -Weight 2 -Content { New-ConsoleStatusList -WidgetId steps }; New-ConsolePanel -Title 'Info' -Content { New-ConsoleKeyValue -WidgetId info } }</code>
    /// </example>
    /// <example>
    ///   <para>Two tables sharing the width and a histogram 17 cells wide, stacking below 90 cells</para>
    ///   <code>
    /// New-ConsoleColumns -FixedWidth 0, 0, 17 -CollapseBelow 90 -Content {
    ///     New-ConsolePanel -Title 'Failures' -Weight 1.5 -Content { New-ConsoleLiveTable -WidgetId failures -Column 'Server', 'Check' }
    ///     New-ConsolePanel -Title 'Slowest' -Content { New-ConsoleLiveTable -WidgetId slowest -Column 'Check', 'Took' }
    ///     New-ConsolePanel -Title 'Durations' -Content { New-ConsoleHistogram -WidgetId durations -Edge 0, 2, 4, 8 -Height 5 }
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleColumns")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleColumns : PSCmdlet {
        /// <summary><para type="description">Scriptblock that emits the child widgets.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock Content { get; set; } = null!;

        /// <summary><para type="description">Blank columns between children (default 1).</para></summary>
        [Parameter]
        public int Gap { get; set; } = 1;

        /// <summary><para type="description">Fixed widths in cells, one per child in order; 0 (or no entry) keeps the child's share of the width (-Weight).</para></summary>
        [Parameter]
        public int[]? FixedWidth { get; set; }

        /// <summary><para type="description">The width in cells below which the columns stack vertically (default: the sum of the children's minimum widths, counting 16 cells for each shared column).</para></summary>
        [Parameter]
        public int CollapseBelow { get; set; } = -1;

        /// <summary><para type="description">Fixed height in rows for this row of columns.</para></summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard.</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Columns columns = new Columns { Gap = Gap, CollapseBelow = CollapseBelow < 0 ? -1 : CollapseBelow };
            List<IWidget> children = PSWidgets.Collect(Content);
            for (int i = 0; i < children.Count; i++) {
                int width = FixedWidth != null && i < FixedWidth.Length ? FixedWidth[i] : 0;
                if (width > 0) {
                    // A fixed width replaces the child's own share; the wrapper only carried that share.
                    IWidget child = children[i] is Sized sized ? sized.Widget : children[i];
                    columns.Add(child, LayoutSize.Fixed(width));
                } else {
                    columns.Add(children[i]);
                }
            }
            WriteObject(PSWidgets.WithSize(columns, 0, Height, Fill.IsPresent));
        }
    }
}
