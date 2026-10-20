using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Lays console widgets out in an equal-width grid.</para>
    /// <para type="description">Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.</para>
    /// </summary>
    /// <example>
    ///   <para>Three charts in a row</para>
    ///   <code>New-ConsoleGrid -Columns 3 { New-ConsoleSparkline -Name a; New-ConsoleSparkline -Name b; New-ConsoleSparkline -Name c }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleGrid")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleGrid : PSCmdlet {
        /// <summary><para type="description">Scriptblock that emits the cells.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock Content { get; set; } = null!;

        /// <summary><para type="description">Number of columns (default 2).</para></summary>
        [Parameter]
        public int Columns { get; set; } = 2;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            WriteObject(new Grid(Columns, PSWidgets.Collect(Content).ToArray()));
        }
    }
}
