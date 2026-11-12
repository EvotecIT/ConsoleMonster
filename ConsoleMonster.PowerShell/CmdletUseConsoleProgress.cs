using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using ConsoleMonster.PowerShell.Display;

namespace ConsoleMonster.PowerShell {
    /// <summary>Runs a script and shows its Write-Progress calls as an inline progress area.</summary>
    /// <remarks><para>The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the final state stays (or disappears with -PrintFinalState:$false) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. Inside the script, $ConsoleSession is this progress area's session, so Wait-ConsoleDashboard can capture or pause it. In jobs, remoting, the ISE and redirected output the script simply runs with $ConsoleSession set to null. If another runspace owns the terminal's progress area, the script also runs with PowerShell's native progress. Works on Windows PowerShell 5.1 and PowerShell 7.</para></remarks>
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
    /// <example>
    /// <para>Return server results and suppress the final progress area</para>
    /// <code>Use-ConsoleProgress -PrintFinalState:$false {
    ///     foreach ($server in 'web01', 'web02') {
    ///         Write-Progress -Activity 'Inspect servers' -Status $server -PercentComplete 50
    ///         [pscustomobject]@{ Server = $server; Result = 'Pass' }
    ///     }
    /// } | Show-ConsoleTable
    /// </code>
    /// </example>
    [Cmdlet(VerbsOther.Use, "ConsoleProgress", HelpUri = "https://consolemonster.dev/powershell/commands/use-consoleprogress/")]
    [OutputType(typeof(object))]
    public sealed class CmdletUseConsoleProgress : NoPipelineInputCmdlet {
        private static readonly string ProgressProxy = PSScriptResources.Read("ProgressProxy.ps1");

        internal static readonly string HostProxy = PSScriptResources.Read("HostProxy.ps1");

        /// <summary><para type="description">The work to run while native Write-Progress calls update the inline progress area.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary><para type="description">Prints the final progress state in scrollback (default true); pass -PrintFinalState:$false to leave nothing behind.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$true")]
        public SwitchParameter PrintFinalState { get; set; } = new SwitchParameter(true);

        /// <summary><para type="description">Redraw rate (default 15).</para></summary>
        [Parameter]
        [ValidateRange(1, 60)]
        [PSDefaultValue(Help = "15")]
        public double FramesPerSecond { get; set; } = 15;

        /// <summary><para type="description">Disables live redraws and uses append-only output.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter NoLive { get; set; }

        /// <summary><para type="description">Disables animation and limits live redraws to once a second without changing the layout. Accessible mode always enables reduced motion.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter ReducedMotion { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            if (HostOutput.UseHost(this, profile)) {
                foreach (PSObject item in InvokeWithoutArea()) {
                    WriteObject(item);
                }
                return;
            }
            using (IDisposable? foreground = SessionProgress.TryEnterForeground()) {
                if (foreground == null) {
                    // Another runspace owns the one terminal area. Keep the script and native progress in this runspace.
                    foreach (PSObject item in InvokeWithoutArea()) {
                        WriteObject(item);
                    }
                    return;
                }
                RunWithArea();
            }
        }

        // A host fallback owns no live session. Shadow an outer dashboard variable rather than accidentally waiting on it.
        private Collection<PSObject> InvokeWithoutArea() => ScriptBlock.InvokeWithContext(null,
            new List<PSVariable> { new PSVariable("ConsoleSession", null) });

        private void RunWithArea() {
            ActivityList activities = new ActivityList();
            LiveSessionOptions options = new LiveSessionOptions { ReducedMotion = MyInvocation.BoundParameters.ContainsKey(nameof(ReducedMotion)) ? (bool?)ReducedMotion.IsPresent : null,
                Mode = LiveMode.Inline,
                Live = !NoLive.IsPresent,
                PrintFinalState = PrintFinalState.IsPresent,
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
                    List<PSVariable> variables = new List<PSVariable> {
                        new PSVariable("__cmProgressBridge", bridge), new PSVariable("ConsoleSession", session)
                    };
                    // With Enable-ConsoleFormatting -Feature Progress, Write-Progress is an alias, which wins over the
                    // function above: Update-Console hands its calls to this area while the script runs.
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
    internal sealed class ProgressBridge {
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
