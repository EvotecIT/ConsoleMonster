using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Places console widgets side by side.</para>
    /// <para type="description">Children share the width equally unless they were created with -Weight. Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content.</para>
    /// </summary>
    /// <example>
    ///   <para>Two panels, the first twice as wide</para>
    ///   <code>New-ConsoleColumns { New-ConsolePanel -Title 'Steps' -Weight 2 { New-ConsoleStatusList -Name steps }; New-ConsolePanel -Title 'Info' { New-ConsoleKeyValue -Name info } }</code>
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

        /// <summary><para type="description">Fixed height in rows for this row of columns.</para></summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard.</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Columns columns = new Columns(PSWidgets.Collect(Content).ToArray()) { Gap = Gap };
            WriteObject(PSWidgets.WithSize(columns, 0, Height, Fill.IsPresent));
        }
    }
}
