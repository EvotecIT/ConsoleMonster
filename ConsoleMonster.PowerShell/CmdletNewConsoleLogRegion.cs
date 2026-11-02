using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a log region: the newest lines of a log, bottom-aligned.</para>
    /// <para type="description">Printed once, the lines come from a script block of Message/Level records calls (each with a level, a time and an optional Tag naming its source) or from the pipeline. In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Invoke-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen; write to it explicitly with Write-ConsoleLogEntry. -Tag, -Level and -Filter show part of the log; hidden lines are kept.</para>
    /// </summary>
    /// <example>
    ///   <para>A short log printed once</para>
    ///   <code>
    /// New-ConsoleLogRegion -Height 5 -Content {
    ///     [pscustomobject]@{ Message = 'Discovered 4 domain controllers in 2 domains'; Level = 'Info'; AtSeconds = 2 }
    ///     [pscustomobject]@{ Message = 'DC02: USN rollback check failed (event 2095)'; Level = 'Error'; AtSeconds = 44; Tag = 'DC02' }
    ///     [pscustomobject]@{ Message = 'Using cached schema snapshot'; Level = 'Verbose'; AtSeconds = 53 }
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A log that fills the rest of a dashboard</para>
    ///   <code>New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }</code>
    /// </example>
    /// <example>
    ///   <para>Only warnings and errors from one job</para>
    ///   <code>
    /// New-ConsoleLogRegion -Tag 'sql-index' -Level Warn -NoElapsed -Content {
    ///     @{ Message = 'Rebuilding IX_Users_Name'; Tag = 'sql-index' }
    ///     @{ Message = 'Fragmentation 41% on IX_Orders_Date'; Level = 'Warn'; Tag = 'sql-index' }
    ///     @{ Message = 'Backup share is read-only'; Level = 'Error'; Tag = 'backup' }
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLogRegion")]
    [OutputType(typeof(LogRegion))]
    public sealed class CmdletNewConsoleLogRegion : PSCmdlet {
        private readonly List<object> _piped = new List<object>();

        /// <summary><para type="description">The live widget identifier (default log).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "log";

        /// <summary><para type="description">A script block that emits the lines: Message/Level records calls, or strings for info lines.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Lines from the pipeline: Message/Level records objects or strings.</para></summary>
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
        public SwitchParameter NoElapsed { get; set; }

        /// <summary><para type="description">Wrap long lines (up to three rows each) instead of cutting them.</para></summary>
        [Parameter]
        public SwitchParameter Wrap { get; set; }

        /// <summary><para type="description">Shared LevelColumn layout settings as a hashtable, for example @{ MinWidth = 6; Alignment = 'Right' }.</para></summary>
        [Parameter] public Layout.ColumnFormat? LevelColumn { get; set; }

        /// <summary><para type="description">Shows only lines from this source, as given by a record's Tag or Write-ConsoleLogEntry -Tag. Case does not matter.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? Tag { get; set; }

        /// <summary><para type="description">Shows only lines at this level or worse: Warn shows warnings and errors, Verbose hides only debug lines.</para></summary>
        [Parameter]
        [Binding.LogLevelTransformation]
        [ArgumentCompleter(typeof(Binding.LogLevelCompleter))]
        public string? Level { get; set; }

        /// <summary><para type="description">Shows only lines containing this literal text. Case does not matter.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? Filter { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ScriptBlock? content = Content;
            string live = WidgetId;
            LogRegion log = new LogRegion(live) { PreferredHeight = Height, Capacity = Capacity, ShowElapsed = !NoElapsed.IsPresent, Wrap = Wrap.IsPresent };
            if (Tag != null || Level != null || Filter != null) { log.Filter = new LogFilter(Tag, Level, Filter); }
            PSItems.AddAll(this, log, PSItems.Items(content));
            PSItems.AddAll(this, log, _piped);
            if (LevelColumn != null) { log.LevelColumn = LevelColumn; }
            WriteObject(log);
        }
    }
}
