using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Adds empty space around console widgets.</para>
    /// <para type="description">-Padding takes one, two or four numbers of cells: all sides; left and right, then top and bottom; or left, top, right and bottom (the order of the .NET Padding widget). Strings in the script block are markup text. Use New-ConsoleAlign to place content inside its space instead.</para>
    /// </summary>
    /// <example>
    ///   <para>Two cells left and right, one row above and below</para>
    ///   <code>New-ConsolePanel -Title 'Padding' -Content { New-ConsolePadding -Padding 2, 1 -Content { 'two cells of padding' } } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Indent a block by four cells</para>
    ///   <code>New-ConsolePadding -Padding 4, 0, 0, 0 -Content { New-ConsoleText -Markup '[muted]indented note[/]' } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsolePadding")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsolePadding : PSCmdlet {
        /// <summary><para type="description">Script block that emits the content (several widgets are stacked vertically).</para></summary>
        [Parameter(Position = 0)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">A widget to pad instead of a script block.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public IWidget? Widget { get; set; }

        /// <summary><para type="description">The space in cells: one value for all sides; two for left and right, then top and bottom; four for left, top, right and bottom (default 1, 0: one cell left and right).</para></summary>
        [Parameter]
        [ValidateCount(1, 4)]
        public int[] Padding { get; set; } = { 1, 0 };

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
        protected override void ProcessRecord() {
            List<IWidget> children = PSWidgets.Collect(Content);
            if (Widget != null) {
                children.Add(Widget);
            }
            IWidget content = children.Count == 1 ? children[0] : new Rows(children.ToArray());
            if (Padding.Length == 3) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("Give -Padding one, two or four numbers."), "ConsoleMonsterPadding", ErrorCategory.InvalidArgument, Padding));
            }
            int left, top, right, bottom;
            switch (Padding.Length) {
                case 1:
                    left = top = right = bottom = Padding[0];
                    break;
                case 2:
                    left = right = Padding[0];
                    top = bottom = Padding[1];
                    break;
                default:
                    left = Padding[0];
                    top = Padding[1];
                    right = Padding[2];
                    bottom = Padding[3];
                    break;
            }
            WriteObject(PSWidgets.WithSize(new ConsoleMonster.Layout.Padding(content, left, top, right, bottom), Weight, Height, Fill.IsPresent));
        }
    }
}
