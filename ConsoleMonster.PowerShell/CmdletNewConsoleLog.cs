using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a log region: the newest lines of a log, bottom-aligned.</para>
    /// <para type="description">Printed once, the lines come from a script block of New-ConsoleLogEntry calls (each with a level and a time) or from the pipeline. In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Start-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen; write to it explicitly with Write-ConsoleLog. To add lines to a log made earlier, pipe it to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>A short log printed once</para>
    ///   <code>
    /// New-ConsoleLog -Height 5 {
    ///     New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -At 2s
    ///     New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -At 44s
    ///     New-ConsoleLogEntry 'Using cached schema snapshot' -Level Verbose -At 53s
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>A log that fills the rest of a dashboard</para>
    ///   <code>New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLog")]
    [OutputType(typeof(LogRegion))]
    public sealed class CmdletNewConsoleLog : PSCmdlet {
        private readonly List<object> _piped = new List<object>();

        /// <summary><para type="description">The live name (default log). A script block here is taken as -Content.</para></summary>
        [Parameter(Position = 0)]
        public object Name { get; set; } = "log";

        /// <summary><para type="description">A script block that emits the lines: New-ConsoleLogEntry calls, or strings for info lines.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Lines from the pipeline: New-ConsoleLogEntry objects or strings.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Rows requested when printed once (default 8).</para></summary>
        [Parameter]
        public int Height { get; set; } = 8;

        /// <summary><para type="description">Lines kept in memory (default 1000).</para></summary>
        [Parameter]
        public int Capacity { get; set; } = 1000;

        /// <summary><para type="description">Hide the time at the start of each line.</para></summary>
        [Parameter]
        public SwitchParameter NoTime { get; set; }

        /// <summary><para type="description">Wrap long lines (up to three rows each) instead of cutting them.</para></summary>
        [Parameter]
        public SwitchParameter Wrap { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            object? name = PSText.Unwrap(Name);
            ScriptBlock? content = name is ScriptBlock script ? script : Content;
            string live = name is ScriptBlock || name == null ? "log" : PSWidgets.ToText(name);
            LogRegion log = new LogRegion(live) { PreferredHeight = Height, Capacity = Capacity, ShowTime = !NoTime.IsPresent, Wrap = Wrap.IsPresent };
            PSItems.AddAll(this, log, PSItems.Items(content));
            PSItems.AddAll(this, log, _piped);
            WriteObject(log);
        }
    }
}
