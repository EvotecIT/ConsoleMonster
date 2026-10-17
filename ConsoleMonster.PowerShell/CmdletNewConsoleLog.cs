using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a log region: the newest lines of a log, bottom-aligned.</para>
    /// <para type="description">In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Start-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen. Write to it explicitly with Write-ConsoleLog.</para>
    /// <example>
    ///   <para>A log that fills the rest of the dashboard</para>
    ///   <code>New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleLog")]
    [OutputType(typeof(LogRegion))]
    public sealed class CmdletNewConsoleLog : PSCmdlet {
        /// <summary><para type="description">The live name (default log).</para></summary>
        [Parameter(Position = 0)]
        public string Name { get; set; } = "log";

        /// <summary><para type="description">Rows requested when printed once (default 8).</para></summary>
        [Parameter]
        public int Height { get; set; } = 8;

        /// <summary><para type="description">Lines kept in memory (default 1000).</para></summary>
        [Parameter]
        public int Capacity { get; set; } = 1000;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            WriteObject(new LogRegion(Name) { PreferredHeight = Height, Capacity = Capacity });
        }
    }
}
