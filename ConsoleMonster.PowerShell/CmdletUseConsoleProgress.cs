using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs a script and shows its Write-Progress calls as an inline progress area.</para>
    /// <para type="description">The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the area disappears (or stays with -Keep) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. In jobs, remoting, the ISE and redirected output the script simply runs. If another runspace owns the terminal's progress area, the script also runs with PowerShell's native progress. Works on Windows PowerShell 5.1 and PowerShell 7.</para>
    /// </summary>
    /// <example>
    ///   <para>An existing loop, unchanged</para>
    ///   <code>
    /// Use-ConsoleProgress {
    ///     foreach ($i in 1..50) {
    ///         Write-Progress -Activity 'Copying' -Status "File $i of 50" -PercentComplete ($i * 2)
    ///         Start-Sleep -Milliseconds 50
    ///     }
    /// }</code>
    /// </example>
    [Cmdlet(VerbsOther.Use, "ConsoleProgress")]
    [OutputType(typeof(object))]
    public sealed class CmdletUseConsoleProgress : PSCmdlet {
        private const string ProgressProxy = @"
[CmdletBinding()]
param(
    [Parameter(Position = 0)][string] $Activity,
    [Parameter(Position = 1)][string] $Status,
    [Parameter(Position = 2)][int] $Id = 0,
    [int] $PercentComplete = -1,
    [int] $SecondsRemaining = -1,
    [string] $CurrentOperation,
    [int] $ParentId = -1,
    [switch] $Completed,
    [int] $SourceId
)
if ($ProgressPreference -eq 'SilentlyContinue' -or $ProgressPreference -eq 'Ignore') { return }
$text = if ($CurrentOperation) { if ($Status) { $Status + ' - ' + $CurrentOperation } else { $CurrentOperation } } else { $Status }
$__cmProgressBridge.Progress($Id, $ParentId, $Activity, $text, $PercentComplete, $SecondsRemaining, $Completed.IsPresent)
";

        internal const string HostProxy = @"
param(
    [Parameter(Position = 0, ValueFromPipeline = $true, ValueFromRemainingArguments = $true)][object[]] $Object,
    [switch] $NoNewline,
    [object] $Separator = ' ',
    [System.ConsoleColor] $ForegroundColor,
    [System.ConsoleColor] $BackgroundColor
)
$__cmProgressBridge.Host((@($Object | ForEach-Object { if ($_ -is [System.Collections.IEnumerable] -and $_ -isnot [string]) { $_ -join $Separator } else { ""$_"" } }) -join $Separator), $NoNewline.IsPresent, $PSBoundParameters['ForegroundColor'], $PSBoundParameters['BackgroundColor'])
";

        /// <summary><para type="description">The script to run.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary><para type="description">Leaves the final state of the progress area in the scrollback.</para></summary>
        [Parameter]
        public SwitchParameter Keep { get; set; }

        /// <summary><para type="description">Redraw rate (default 15).</para></summary>
        [Parameter]
        [ValidateRange(1, 60)]
        public int FramesPerSecond { get; set; } = 15;

        /// <summary><para type="description">Disables live redraws and uses append-only output.</para></summary>
        [Parameter] public SwitchParameter NoLive { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            if (HostOutput.UseHost(this, profile)) {
                foreach (PSObject item in ScriptBlock.Invoke()) {
                    WriteObject(item);
                }
                return;
            }
            using (IDisposable? foreground = SessionProgress.TryEnterForeground()) {
                if (foreground == null) {
                    // Another runspace owns the one terminal area. Keep the script and native progress in this runspace.
                    foreach (PSObject item in ScriptBlock.Invoke()) {
                        WriteObject(item);
                    }
                    return;
                }
                RunWithArea();
            }
        }

        private void RunWithArea() {
            ActivityList activities = new ActivityList();
            LiveSessionOptions options = new LiveSessionOptions {
                Mode = LiveMode.Inline,
                AppendOnly = NoLive.IsPresent,
                PrintFinalState = Keep.IsPresent,
                FramesPerSecond = FramesPerSecond,
                // Ctrl+C stops the script the PowerShell way; the session is stopped (and the area cleared) in finally.
                HandleCancelKey = false
            };
            Collection<PSObject> output;
            using (LiveSession session = new LiveSession(activities, options)) {
                session.Start();
                try {
                    Dictionary<string, ScriptBlock> functions = new Dictionary<string, ScriptBlock> {
                        ["Write-Progress"] = ScriptBlock.Create(ProgressProxy),
                        ["Write-Host"] = ScriptBlock.Create(HostProxy)
                    };
                    ProgressBridge bridge = new ProgressBridge(session);
                    List<PSVariable> variables = new List<PSVariable> { new PSVariable("__cmProgressBridge", bridge) };
                    // With Enable-ConsoleFormatting -Feature Progress, Write-Progress is an alias, which wins over the
                    // function above: Write-ConsoleProgress hands its calls to this area while the script runs.
                    ProgressBridge? previous = SessionProgress.Override;
                    SessionProgress.Override = bridge;
                    try {
                        output = ScriptBlock.InvokeWithContext(functions, variables);
                    } finally {
                        SessionProgress.Override = previous;
                    }
                } finally {
                    session.Stop();
                }
            }
            foreach (PSObject item in output) {
                WriteObject(item);
            }
        }
    }

    /// <summary>Receives calls from the Write-Progress and Write-Host stand-ins inside Use-ConsoleProgress.</summary>
    public sealed class ProgressBridge {
        private readonly ILiveDispatcher _session;
        private readonly System.Text.StringBuilder _pending = new System.Text.StringBuilder();

        internal ProgressBridge(ILiveDispatcher session) {
            _session = session;
        }

        /// <summary>One Write-Progress call.</summary>
        /// <param name="id">The activity id.</param>
        /// <param name="parentId">The parent activity id, or -1.</param>
        /// <param name="activity">The activity.</param>
        /// <param name="status">The status.</param>
        /// <param name="percent">The percentage, or -1.</param>
        /// <param name="secondsRemaining">Seconds remaining, or -1.</param>
        /// <param name="completed">Whether the activity completed.</param>
        public void Progress(int id, int parentId, string? activity, string? status, int percent, int secondsRemaining, bool completed) {
            _session.Post(ProgressEvents.From(id, parentId, activity, status, percent, secondsRemaining, completed));
        }

        /// <summary>One Write-Host call: the text is written above the progress area, in its colours.</summary>
        /// <param name="text">The text.</param>
        /// <param name="noNewLine">Whether the line continues with the next call.</param>
        /// <param name="foreground">The foreground colour, or null.</param>
        /// <param name="background">The background colour, or null.</param>
        public void Host(string? text, bool noNewLine, object? foreground = null, object? background = null) {
            ConsoleColor? fg = ToColor(foreground);
            ConsoleColor? bg = ToColor(background);
            _pending.Append(fg.HasValue || bg.HasValue ? HostColors.Markup(text, fg, bg) : Text.Markup.Escape(text ?? string.Empty));
            if (noNewLine) {
                return;
            }
            foreach (string line in _pending.ToString().Replace("\r\n", "\n").Split('\n')) {
                _session.Post(new LiveEvent("$log", "write", text: line, status: "host", isMarkup: true));
            }
            _pending.Clear();
        }

        private static ConsoleColor? ToColor(object? value) {
            object? raw = value is PSObject ps ? ps.BaseObject : value;
            return raw is ConsoleColor color ? color : (ConsoleColor?)null;
        }
    }
}
