using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs a script block again and again and shows its latest output as a live view.</para>
    /// <para type="description">Like watch: every -PollIntervalSeconds seconds the script block runs and its output replaces the previous one, shaped as Show-ConsoleTable and the other commands shape it (objects as a table, one dictionary as a record, text as it is). The heading shows the interval, -Title, the run number and the time. -Differences highlights the cells that changed since the previous run. A run that fails shows its error and the watch goes on. Warnings, errors and Write-Host text from the script block are shown under its output in the view (an error marks the run in the heading) instead of interrupting it. Ctrl+C ends the watch and the last frame stays in the scrollback; -Count ends it after that many runs. Piped into another command (Out-File, Out-String), or without an interactive console (redirected output, CI, a job), every run is printed instead, so use -Count there; assigning the result to a variable at the prompt does not capture the live view, so pipe it instead.</para>
    /// </summary>
    /// <example>
    ///   <para>Watch the busiest processes</para>
    ///   <code>Watch-Console { Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 } -PollIntervalSeconds 2 -Differences</code>
    /// </example>
    /// <example>
    ///   <para>Three snapshots of a service, printed to a log</para>
    ///   <code>Watch-Console { Get-Service Spooler } -Count 3 -PollIntervalSeconds 5 | Out-File watch.log</code>
    /// </example>
    /// <example>
    ///   <para>Free space per drive with a title, changed cells highlighted</para>
    ///   <code>Watch-Console -Title 'Disk space' -PollIntervalSeconds 10 -Differences { Get-Volume | Where-Object DriveLetter | Select-Object DriveLetter, FileSystemLabel, SizeRemaining, Size }</code>
    /// </example>
    [Cmdlet(VerbsCommon.Watch, "Console")]
    public sealed class CmdletWatchConsole : NoPipelineInputCmdlet {
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();

        /// <summary><para type="description">The script block to run.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary><para type="description">Seconds between runs (default 2).</para></summary>
        [Parameter]
        [ValidateRange(0.05, 86400)]
        public double PollIntervalSeconds { get; set; } = 2;

        /// <summary><para type="description">How many runs, or 0 (default) until Ctrl+C.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int Count { get; set; }

        /// <summary><para type="description">Highlights the cells that changed since the previous run.</para></summary>
        [Parameter]
        public SwitchParameter Differences { get; set; }

        /// <summary><para type="description">A title for the heading (markup); default the script block's text.</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">Disables live redraws and uses append-only output.</para></summary>
        [Parameter] public SwitchParameter NoLive { get; set; }

        /// <summary><para type="description">Typed live-session configuration for the watch; omit for inline output with its final frame retained.</para></summary>
        [Parameter] public LiveSessionOptions? Session { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            // Live only when the output is drawn on an interactive console: piped into another command, every run is printed
            // into the pipeline instead.
            bool last = MyInvocation.PipelinePosition >= MyInvocation.PipelineLength;
            bool live = !NoLive.IsPresent && last && !HostOutput.UseHost(this, profile) && profile.Mode == Capabilities.OutputMode.Interactive;
            WatchOptions options = new WatchOptions {
                PollInterval = TimeSpan.FromSeconds(PollIntervalSeconds),
                Count = Count,
                Differences = Differences.IsPresent,
                Title = Title != null ? PSText.Markup(Title) : Text.Markup.Escape(ScriptBlock.ToString().Trim()),
                Live = live,
                // Ctrl+C stops the pipeline the PowerShell way (StopProcessing); the session keeps the last frame.
                Session = Session ?? new LiveSessionOptions { Mode = LiveMode.Inline, PrintFinalState = true, HandleCancelKey = false },
                Print = widget => PSFormatting.Emit(this, widget, 0, null, false)
            };
            try {
                ConsoleWatch.Run(run => Produce(), options, _stop.Token);
            } catch (OperationCanceledException) {
                // Ctrl+C: the watch ended; the last frame stays.
            } finally {
                _stop.Dispose();
            }
        }

        // One run: every stream of the script block is collected (merged into its output), so nothing writes to the host
        // while the live view draws; warnings, errors and host text are shown under the output.
        private IWidget Produce() {
            Collection<PSObject> merged;
            try {
                merged = Merged.Invoke(ScriptBlock);
            } catch (Exception ex) when (Stopping || ex is PipelineStoppedException || ex.InnerException is PipelineStoppedException) {
                throw new OperationCanceledException("The watch was stopped.", ex);
            } catch (RuntimeException ex) {
                // A failed run shows PowerShell's error (the command's own error, its position in the script block), not
                // the exception that carried it out of the script block.
                object failed = (object?)ex.ErrorRecord ?? ex;
                ErrorRecord? record = ex.ErrorRecord;
                return new WatchFrame(new ExceptionView(PSErrors.Describe(failed)) { ShowStack = false }, (record?.Exception ?? ex).GetType().Name);
            }
            if (Stopping) {
                throw new OperationCanceledException("The watch was stopped.");
            }
            List<PSObject> output = new List<PSObject>();
            List<string> notes = new List<string>();
            int errors = 0;
            foreach (PSObject item in merged) {
                switch (item?.BaseObject) {
                    case null:
                        break;
                    case ErrorRecord native when PSStreams.IsNativeErrorLine(native):
                        // A native tool's stderr line is host text, not an error of the run.
                        notes.Add(Text.Markup.Escape(native.ToString()));
                        break;
                    case ErrorRecord error:
                        errors++;
                        notes.Add("[fail]" + Text.Markup.Escape(error.ToString()) + "[/]");
                        break;
                    case WarningRecord warning:
                        notes.Add("[warn]" + Text.Markup.Escape(warning.Message) + "[/]");
                        break;
                    case InformationRecord information:
                        notes.Add(Text.Markup.Escape(information.MessageData?.ToString() ?? string.Empty));
                        break;
                    case VerboseRecord verbose:
                        notes.Add("[muted]" + Text.Markup.Escape(verbose.Message) + "[/]");
                        break;
                    case DebugRecord debug:
                        notes.Add("[muted]" + Text.Markup.Escape(debug.Message) + "[/]");
                        break;
                    default:
                        output.Add(item!);
                        break;
                }
            }
            IWidget view = PSFormatting.ViewOf(output, this);
            if (notes.Count > 0) {
                view = new Rows(view, new TextBlock(string.Join("\n", notes.ToArray())));
            }
            // A run with errors shows its content and says so in the heading.
            return errors > 0 ? new WatchFrame(view, errors == 1 ? "1 error" : errors.ToString(System.Globalization.CultureInfo.InvariantCulture) + " errors") : view;
        }

        private static readonly ScriptBlock Merged = ScriptBlock.Create("param($__cmWatched) & $__cmWatched *>&1");

        /// <inheritdoc />
        protected override void StopProcessing() {
            try {
                _stop.Cancel();
            } catch (ObjectDisposedException) {
            }
            base.StopProcessing();
        }
    }
}
