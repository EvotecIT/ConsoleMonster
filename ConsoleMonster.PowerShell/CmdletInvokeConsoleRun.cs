using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs a script block for every input item, optionally in parallel, with a live view of the run.</para>
    /// <para type="description">$_ is the item, as in ForEach-Object. While it runs, a live view shows a summary line (done of total, percentage, time left, counts), one cell per item, the running items with their elapsed time and the failures; without an interactive console (redirected output, CI, a job) it prints one line per finished item and a summary instead. The output is the script block's ordinary output, in input order (or as each item finishes with -Unordered); an item's errors are written as error records and mark it as failed. While the live view is showing, ordinary output and native stream records are delivered when it ends. With -Parallel, while the live view is showing, host messages, warnings, information, verbose and debug messages, and errors also appear immediately in its log; Write-Progress appears in an activity list, with independent parent and child activities per worker. Without -Parallel, progress bars from the script block are hidden while the live view is showing. Without -Parallel the items run one at a time in the caller's scope, as with ForEach-Object, so variables set in the script block stay. -Parallel runs up to -ThrottleLimit items at a time in their own runspaces on Windows PowerShell 5.1 and PowerShell 7: variables of the caller are passed with $using:name, and -TimeoutSeconds stops an item that runs too long. -Label sets each item's label: a property name or a script block. -RetryCount runs a failed or timed-out item again (after -RetryDelaySeconds); only the last attempt's output and errors are kept, the item's cell shows the retry and the summary counts retried items. Ctrl+C stops the run: running items are stopped and the rest are counted as skipped.</para>
    /// </summary>
    /// <example>
    ///   <para>Check servers in parallel</para>
    ///   <code>$port = 443; $servers | Invoke-ConsoleRun -Parallel -ThrottleLimit 8 -Label Name { Test-NetConnection $_.Name -Port $using:port -InformationLevel Quiet }</code>
    /// </example>
    /// <example>
    ///   <para>One at a time, in the current scope</para>
    ///   <code>Get-ChildItem *.log | Invoke-ConsoleRun { Compress-Archive $_ "$($_.BaseName).zip" -Force }</code>
    /// </example>
    /// <example>
    ///   <para>Forty items in parallel with a title and a time limit: items that throw or run past 10 seconds are failures, listed when the run ends</para>
    ///   <code>
    /// 1..40 | Invoke-ConsoleRun -Parallel -ThrottleLimit 8 -TimeoutSeconds 10 -Title 'Patching web farm' -Label { "web$('{0:d2}' -f $_)" } {
    ///     Start-Sleep -Milliseconds (Get-Random -Minimum 200 -Maximum 1500)
    ///     if ($_ % 13 -eq 0) { throw "web$_ did not come back after the reboot" }
    /// }</code>
    /// </example>
    /// <example>
    ///   <para>Retry flaky servers twice, five seconds apart</para>
    ///   <code>$servers | Invoke-ConsoleRun -Parallel -RetryCount 2 -RetryDelaySeconds 5 -Label Name { Restart-Computer $_.Name -Wait -For WinRM -Timeout 300 -ErrorAction Stop }</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Invoke, "ConsoleRun")]

    public sealed class CmdletInvokeConsoleRun : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();
        private volatile bool _stopping;
        private readonly List<System.Management.Automation.PowerShell> _inFlight = new List<System.Management.Automation.PowerShell>();

        /// <summary><para type="description">The items (pipeline input).</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">The script block run for each item ($_ is the item).</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary><para type="description">Runs items at the same time, each in its own runspace (pass caller variables with $using:name).</para></summary>
        [Parameter]
        public SwitchParameter Parallel { get; set; }

        /// <summary><para type="description">How many items run at the same time with -Parallel (default 5).</para></summary>
        [Parameter]
        [ValidateRange(1, 1024)]
        public int ThrottleLimit { get; set; } = 5;

        /// <summary><para type="description">Seconds an item may run with -Parallel before it is stopped and counted as failed (default no limit; ignored with a warning without -Parallel).</para></summary>
        [Parameter]
        [ValidateRange(0, 864000)]
        public double TimeoutSeconds { get; set; }

        /// <summary><para type="description">How many more times a failed or timed-out item runs before it counts as failed (default 0).</para></summary>
        [Parameter]
        [ValidateRange(0, 100)]
        public int RetryCount { get; set; }

        /// <summary><para type="description">Seconds to wait before each retry (default 0).</para></summary>
        [Parameter]
        [ValidateRange(0, 86400)]
        public double RetryDelaySeconds { get; set; }

        /// <summary><para type="description">Writes each item's output as soon as it finishes instead of in input order (when the live view is not showing).</para></summary>
        [Parameter]
        public SwitchParameter Unordered { get; set; }

        /// <summary><para type="description">Each item's label in the view: a property name or a script block ($_ is the item). Default a name-like property, or the item's text.</para></summary>
        [Parameter]

        public object? Label { get; set; }

        /// <summary><para type="description">A title above the live view (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">Disables live redraws and uses append-only output.</para></summary>
        [Parameter] public SwitchParameter NoLive { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.Add(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) {
                return;
            }
            if (TimeoutSeconds > 0 && !Parallel.IsPresent) {
                WriteWarning("-TimeoutSeconds applies to -Parallel runs only; the items run without a time limit.");
            }
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            bool live = !NoLive.IsPresent && !HostOutput.UseHost(this, profile) && profile.Mode == Capabilities.OutputMode.Interactive;
            // Built before the view starts, so an unsupported $using: fails without drawing a run first.
            Hashtable usings = new Hashtable(StringComparer.OrdinalIgnoreCase);
            string? parallelScript = Parallel.IsPresent ? ParallelScript(usings) : null;
            // While the view is live every output (objects that may print further down the pipeline, errors, warnings)
            // waits until it ends, so nothing prints inside the view.
            bool hold = live;
            RunOptions options = new RunOptions {
                Title = Title != null ? PSText.Markup(Title) : null,
                Live = live,
                RetryCount = RetryCount,
                RetryDelay = TimeSpan.FromSeconds(RetryDelaySeconds),
                Session = new LiveSessionOptions { Mode = LiveMode.Inline, PrintFinalState = true, HandleCancelKey = false },
                Print = line => HostOutput.WriteHostLine(this, line)
            };
            List<string> labels = _items.Select(LabelOf).ToList();
            Writer writer = new Writer(Unordered.IsPresent, hold);
            using (RunMonitor monitor = new RunMonitor(_items.Count, options)) {
                if (parallelScript != null) {
                    RunParallel(monitor, labels, writer, parallelScript, usings);
                } else {
                    RunSequential(monitor, labels, writer, live);
                }
            }
            writer.Flush(true);
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            _stopping = true;
            lock (_inFlight) {
                foreach (System.Management.Automation.PowerShell ps in _inFlight) {
                    try {
                        ps.BeginStop(null, null);
                    } catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException) {
                    }
                }
            }
            base.StopProcessing();
        }

        private string LabelOf(PSObject item) {
            object? name = Label is PSObject wrapped ? wrapped.BaseObject : Label;
            try {
                if (name is ScriptBlock script) {
                    Collection<PSObject> result = script.InvokeWithContext(null, new List<PSVariable> { new PSVariable("_", item) });
                    return result.Count > 0 ? PSWidgets.ToText(result[0]) : string.Empty;
                }
                if (name is string property && property.Length > 0) {
                    return PSWidgets.ToText(item.Properties[property]?.Value);
                }
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                return PSWidgets.ToText(item.BaseObject);
            }
            return item.BaseObject is string text ? text : Values.ValueShaper.Title(Binding.PSObjectMemberSource.UnwrapPSObject(item) ?? item);
        }

        // One at a time on the pipeline thread, dot-sourced in the caller's scope as ForEach-Object does (variables set in
        // the body stay); every stream is collected so nothing writes over the view, and progress is quiet while it is live.
        private void RunSequential(RunMonitor monitor, List<string> labels, Writer writer, bool quiet) {
            ScriptBlock sequential = ScriptBlock.Create(SequentialScript);
            for (int i = 0; i < _items.Count; i++) {
                if (_stopping) {
                    break;
                }
                monitor.Started(i, labels[i]);
                Stopwatch watch = Stopwatch.StartNew();
                Collection<PSObject> merged;
                Exception? failure;
                for (int attempt = 1; ; attempt++) {
                    merged = new Collection<PSObject>();
                    failure = null;
                    try {
                        SessionState.InvokeCommand.InvokeScript(false, sequential, null, ScriptBlock, _items[i], quiet, merged);
                    } catch (Exception ex) when (!(ex is PipelineStoppedException) && !_stopping) {
                        failure = ex;
                    }
                    if (_stopping || attempt > RetryCount || FailureOf(merged, failure) == null) { break; }
                    monitor.Retrying(i, labels[i], attempt + 1, FailureOf(merged, failure));
                    if (!Pause(RetryDelaySeconds)) { break; }
                }
                if (_stopping) {
                    monitor.Finished(i, StatusKind.Skip, labels[i], watch.Elapsed);
                    break;
                }
                Finish(monitor, writer, i, labels[i], merged, failure, false, watch.Elapsed);
            }
        }

        // Dot-sourced into the caller's scope: it removes its own variables and restores the caller's $ProgressPreference.
        private const string SequentialScript = "param($__cmBody, $__cmItem, $__cmQuiet, $__cmOutput)\n"
            + "$__cmProgress = Get-Variable -Name ProgressPreference -Scope 0 -ErrorAction Ignore\n"
            + "$__cmProgressValue = if ($__cmProgress) { $__cmProgress.Value } else { $null }\n"
            + "$__cmGlobalProgress = $global:ProgressPreference\n"
            // Script-module functions (Compress-Archive, Install-Module) read the global value, not the caller's scope.
            + "if ($__cmQuiet) { $ProgressPreference = 'SilentlyContinue'; $global:ProgressPreference = 'SilentlyContinue' }\n"
            // The dot-sourced block makes the redirection reach native programs inside the body (their stderr lines).
            + "try { . { , $__cmItem | ForEach-Object -Process $__cmBody } *>&1 | ForEach-Object { $__cmOutput.Add($_) } }\n"
            + "finally {\n"
            + "    if ($__cmQuiet) { $global:ProgressPreference = $__cmGlobalProgress; if ($__cmProgress) { $ProgressPreference = $__cmProgressValue } else { Remove-Variable -Name ProgressPreference -Scope 0 -ErrorAction Ignore } }\n"
            + "    Remove-Variable -Name __cmBody, __cmItem, __cmQuiet, __cmOutput, __cmProgress, __cmProgressValue, __cmGlobalProgress -Scope 0 -ErrorAction Ignore\n"
            + "}";

        // Up to ThrottleLimit items at a time in a runspace pool; completions come back to the pipeline thread.
        private void RunParallel(RunMonitor monitor, List<string> labels, Writer writer, string script, Hashtable usings) {
            TimeSpan? timeout = TimeoutSeconds > 0 ? TimeSpan.FromSeconds(TimeoutSeconds) : (TimeSpan?)null;
            BlockingCollection<int> done = new BlockingCollection<int>();
            Dictionary<int, Flight> flights = new Dictionary<int, Flight>();
            // Items waiting to run again, with the time they may start; an item keeps its index and cell across attempts.
            List<KeyValuePair<int, DateTime>> retries = new List<KeyValuePair<int, DateTime>>();
            int[] attempts = new int[_items.Count];
            using (RunspacePool pool = monitor.Session == null
                ? RunspaceFactory.CreateRunspacePool(1, ThrottleLimit, InitialSessionState.CreateDefault(), Host)
                : RunspaceFactory.CreateRunspacePool(InitialSessionState.CreateDefault2())) {
                pool.SetMaxRunspaces(ThrottleLimit);
                pool.SetMinRunspaces(1);
                pool.Open();
                int next = 0;
                int finished = 0;
                while (finished < _items.Count) {
                    while (!_stopping && flights.Count < ThrottleLimit && (next < _items.Count || retries.Exists(entry => entry.Value <= DateTime.UtcNow))) {
                        int ready = retries.FindIndex(entry => entry.Value <= DateTime.UtcNow);
                        int index;
                        if (ready >= 0) { index = retries[ready].Key; retries.RemoveAt(ready); }
                        else { index = next++; }
                        attempts[index]++;
                        System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();
                        ps.RunspacePool = pool;
                        ps.AddScript(script).AddArgument(_items[index]).AddArgument(usings);
                        Flight flight = new Flight(ps);
                        if (monitor.Session != null) {
                            flight.Capture = new PowerShellStreamCapture(ps, monitor.Session, merged: flight.Output, isolateProgress: true);
                        }
                        flights[index] = flight;
                        lock (_inFlight) {
                            _inFlight.Add(ps);
                        }
                        if (attempts[index] == 1) { monitor.Started(index, labels[index]); }
                        flight.Result = ps.BeginInvoke<PSObject, PSObject>(null, flight.Output, null, ar => {
                            flight.Watch.Stop();
                            done.Add(index);
                        }, null);
                    }
                    if (flights.Count == 0) {
                        if (!_stopping && retries.Count > 0) {
                            // Only delayed retries remain: wait for the next one to become due.
                            Thread.Sleep(50);
                            continue;
                        }
                        // Stopped before every item started: the rest count as skipped.
                        break;
                    }
                    if (timeout.HasValue) {
                        foreach (Flight flight in flights.Values) {
                            if (!flight.TimedOut && flight.Watch.Elapsed > timeout.Value) {
                                flight.TimedOut = true;
                                if (!flight.Result!.IsCompleted) {
                                    try {
                                        flight.Shell.BeginStop(null, null);
                                    } catch (InvalidOperationException) {
                                        // It completed between the check and BeginStop; its recorded deadline still applies.
                                    }
                                }
                            }
                        }
                    }
                    if (!done.TryTake(out int completed, 100)) {
                        continue;
                    }
                    Flight current = flights[completed];
                    flights.Remove(completed);
                    lock (_inFlight) {
                        _inFlight.Remove(current.Shell);
                    }
                    Collection<PSObject> merged = new Collection<PSObject>();
                    Exception? failure = null;
                    try {
                        current.Shell.EndInvoke(current.Result!);
                    } catch (Exception ex) {
                        // An item stopped for its timeout (or by Ctrl+C) ends with PipelineStoppedException: that is the
                        // timeout or the stop, not the item's own failure.
                        failure = ex;
                    }
                    foreach (PSObject item in current.Output) {
                        merged.Add(item);
                    }
                    string? retryReason = current.TimedOut ? "timed out after " + Widgets.NumberFormat.Duration(timeout!.Value) : FailureOf(merged, failure);
                    if (!_stopping && retryReason != null && attempts[completed] <= RetryCount) {
                        // Another attempt replaces this one: its output and errors are dropped, the cell shows the retry.
                        monitor.Retrying(completed, labels[completed], attempts[completed] + 1, retryReason);
                        retries.Add(new KeyValuePair<int, DateTime>(completed, DateTime.UtcNow.AddSeconds(RetryDelaySeconds)));
                        current.Capture?.Dispose();
                        current.Shell.Dispose();
                        current.Output.Dispose();
                        continue;
                    }
                    finished++;
                    if (_stopping && !current.TimedOut) {
                        monitor.Finished(completed, StatusKind.Skip, labels[completed], current.Watch.Elapsed);
                    } else if (current.TimedOut) {
                        Finish(monitor, writer, completed, labels[completed], merged, new TimeoutException("timed out after " + Widgets.NumberFormat.Duration(timeout!.Value)), true, current.Watch.Elapsed);
                    } else {
                        Finish(monitor, writer, completed, labels[completed], merged, failure, false, current.Watch.Elapsed);
                    }
                    current.Capture?.Dispose();
                    current.Shell.Dispose();
                    current.Output.Dispose();
                    writer.Flush(false);
                }
            }
        }

        // The message that marks an attempt failed (a terminating error, or an error record other than a native stderr line), or null.
        private static string? FailureOf(Collection<PSObject> merged, Exception? failure) {
            if (failure != null) { return (failure is RuntimeException runtime && runtime.ErrorRecord?.Exception != null ? runtime.ErrorRecord.Exception : failure).Message; }
            foreach (PSObject item in merged) {
                if (item?.BaseObject is ErrorRecord error && !PSStreams.IsNativeErrorLine(error)) { return error.ToString(); }
            }
            return null;
        }

        // Waits between sequential attempts; false when the run is being stopped.
        private bool Pause(double seconds) {
            Stopwatch wait = Stopwatch.StartNew();
            while (wait.Elapsed.TotalSeconds < seconds) {
                if (_stopping) { return false; }
                Thread.Sleep(Math.Min(100, Math.Max(1, (int)((seconds - wait.Elapsed.TotalSeconds) * 1000))));
            }
            return !_stopping;
        }

        // The script for a runspace: $using:name read from the passed table, the body run with $_ as the item, every stream merged.
        private string ParallelScript(Hashtable usings) {
            string block = PSUsing.Rewrite(ScriptBlock, SessionState, usings);
            if (!block.StartsWith("{", StringComparison.Ordinal)) {
                block = "{" + block + "}";
            }
            return "param($__cmItem, $__cmUsing)\n"
                + "foreach ($__cmPair in $__cmUsing.GetEnumerator()) { Set-Variable -Name $__cmPair.Key -Value $__cmPair.Value }\n"
                + ". { , $__cmItem | ForEach-Object -Process " + block + " } *>&1";
        }

        // Sorts an item's merged streams: output to the writer, errors as error records (they fail the item), the rest re-emitted.
        private void Finish(RunMonitor monitor, Writer writer, int index, string label, Collection<PSObject> merged, Exception? failure, bool timedOut, TimeSpan duration) {
            List<Action> emits = new List<Action>();
            string? message = null;
            foreach (PSObject item in merged) {
                switch (item?.BaseObject) {
                    case null:
                        break;
                    case ErrorRecord native when PSStreams.IsNativeErrorLine(native):
                        // A native tool's stderr line (git, robocopy, curl write progress there): shown as host text,
                        // as PowerShell shows it without redirection; it does not fail the item.
                        string line = native.ToString();
                        emits.Add(() => HostOutput.WriteHostLine(this, line));
                        break;
                    case ErrorRecord error:
                        message ??= error.ToString();
                        emits.Add(() => WriteError(error));
                        break;
                    case WarningRecord warning:
                        emits.Add(() => WriteWarning(warning.Message));
                        break;
                    case VerboseRecord verbose:
                        emits.Add(() => WriteVerbose(verbose.Message));
                        break;
                    case DebugRecord debug:
                        emits.Add(() => WriteDebug(debug.Message));
                        break;
                    case InformationRecord information:
                        emits.Add(() => WriteInformation(information));
                        break;
                    default:
                        PSObject output = item!;
                        emits.Add(() => WriteObject(output));
                        break;
                }
            }
            if (failure != null) {
                Exception reported = failure is RuntimeException runtime && runtime.ErrorRecord?.Exception != null ? runtime.ErrorRecord.Exception : failure;
                message = timedOut ? failure.Message : reported.Message;
                ErrorRecord record = failure is IContainsErrorRecord contains && contains.ErrorRecord != null
                    ? contains.ErrorRecord
                    : new ErrorRecord(reported, timedOut ? "ItemTimedOut" : "ItemFailed", timedOut ? ErrorCategory.OperationTimeout : ErrorCategory.NotSpecified, _items[index]);
                emits.Add(() => WriteError(record));
            }
            monitor.Finished(index, message != null ? StatusKind.Fail : StatusKind.Pass, label, duration, message, timedOut);
            writer.Add(index, emits);
        }

        private sealed class Flight {
            internal Flight(System.Management.Automation.PowerShell shell) {
                Shell = shell;
            }

            internal System.Management.Automation.PowerShell Shell { get; }

            internal PowerShellStreamCapture? Capture { get; set; }

            internal IAsyncResult? Result { get; set; }

            internal PSDataCollection<PSObject> Output { get; } = new PSDataCollection<PSObject>();

            internal Stopwatch Watch { get; } = Stopwatch.StartNew();

            internal bool TimedOut { get; set; }
        }

        // Writes each item's output on the pipeline thread: in input order (an item waits for the ones before it) or as it
        // finishes; held back entirely while the live view is showing.
        private sealed class Writer {
            private readonly Dictionary<int, List<Action>> _pending = new Dictionary<int, List<Action>>();
            private readonly List<int> _finished = new List<int>();
            private readonly bool _unordered;
            private readonly bool _hold;
            private int _next;

            internal Writer(bool unordered, bool hold) {
                _unordered = unordered;
                _hold = hold;
            }

            internal void Add(int index, List<Action> emits) {
                _pending[index] = emits;
                _finished.Add(index);
                Flush(false);
            }

            internal void Flush(bool final) {
                if (_hold && !final) {
                    return;
                }
                if (_unordered) {
                    // In the order the items finished (also when the output was held for the live view).
                    foreach (int index in _finished) {
                        Emit(index);
                    }
                    _finished.Clear();
                    return;
                }
                while (_pending.ContainsKey(_next)) {
                    Emit(_next++);
                }
                if (final) {
                    // Items never run leave gaps: write what finished, in order.
                    foreach (int index in _pending.Keys.OrderBy(key => key).ToList()) {
                        Emit(index);
                    }
                }
            }

            private void Emit(int index) {
                foreach (Action emit in _pending[index]) {
                    emit();
                }
                _pending.Remove(index);
            }
        }
    }
}
