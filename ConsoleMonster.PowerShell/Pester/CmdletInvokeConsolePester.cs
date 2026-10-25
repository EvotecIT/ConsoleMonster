using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.TestRuns;

namespace ConsoleMonster.PowerShell.Pester {
    /// <summary>
    /// <para type="synopsis">Runs Pester tests with a live ConsoleMonster dashboard and a summary table.</para>
    /// <para type="description">Takes the parameters of Invoke-Pester (Path, ExcludePath, TagFilter, ExcludeTagFilter, FullNameFilter, Container, Output, CI, PassThru) or a -Configuration (a PesterConfiguration or a hashtable), and runs Pester 5 or 6, imported when first needed. While the tests run, an inline area (or the full screen with -FullScreen) shows the run totals and the clock, the test files as lanes with passed, failed and skipped counts, the running test, and with -FullScreen the failures (expected against actual, the file and line as a link), the slowest tests and a histogram of durations. When the run ends a table of results by file, the failures and the totals are written to the scrollback, and -ExportHtml saves the final dashboard. In redirected output, jobs and CI logs the output is one line per failure and a summary line. Pester's result object is returned with -PassThru (or Run.PassThru), Run.Throw and Run.Exit (-CI) are applied after the summary, and Write-Host output from tests appears above the inline area. Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter; when the internals are not what it expects (or with Run.Parallel or Output None) Pester runs unchanged with its own output and the summary table follows. Works on Windows PowerShell 5.1 and PowerShell 7.</para>
    /// </summary>
    /// <example>
    ///   <para>The tests of the current folder</para>
    ///   <code>Invoke-ConsolePester</code>
    /// </example>
    /// <example>
    ///   <para>A configuration, on the full screen, with the final dashboard saved</para>
    ///   <code>
    /// $c = New-PesterConfiguration
    /// $c.Run.Path = '.\Tests'
    /// $c.Filter.Tag = 'Unit'
    /// $r = Invoke-ConsolePester -Configuration $c -FullScreen -ExportHtml .\pester.html -PassThru</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Invoke, "ConsolePester", DefaultParameterSetName = "Simple")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletInvokeConsolePester : PSCmdlet {
        private readonly List<string> _hostLines = new List<string>();
        private LiveSession? _session;

        /// <summary><para type="description">Test files or folders (Run.Path); default the current folder.</para></summary>
        [Parameter(Position = 0, ParameterSetName = "Simple")]
        public string[]? Path { get; set; }

        /// <summary><para type="description">Files or folders to leave out (Run.ExcludePath).</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public string[]? ExcludePath { get; set; }

        /// <summary><para type="description">Run only tests with these tags (Filter.Tag).</para></summary>
        [Parameter(ParameterSetName = "Simple")]

        public string[]? TagFilter { get; set; }

        /// <summary><para type="description">Leave out tests with these tags (Filter.ExcludeTag).</para></summary>
        [Parameter(ParameterSetName = "Simple")]

        public string[]? ExcludeTagFilter { get; set; }

        /// <summary><para type="description">Run only tests whose full name matches (Filter.FullName).</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public string[]? FullNameFilter { get; set; }

        /// <summary><para type="description">Containers from New-PesterContainer (Run.Container).</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public object[]? Container { get; set; }

        /// <summary><para type="description">Pester's output verbosity (Output.Verbosity). None runs quietly: no dashboard and no summary.</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        [ValidateSet("Diagnostic", "Detailed", "Normal", "Minimal", "None")]
        public string? Output { get; set; }

        /// <summary><para type="description">CI mode as in Invoke-Pester: test results are written and the process exits with the failure count.</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public SwitchParameter CI { get; set; }

        /// <summary><para type="description">Returns Pester's result object.</para></summary>
        [Parameter(ParameterSetName = "Simple")]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">A PesterConfiguration (New-PesterConfiguration) or a hashtable with the same sections.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Advanced")]
        public object Configuration { get; set; } = null!;

        /// <summary><para type="description">Shows the dashboard on the full screen (alternate screen) instead of inline.</para></summary>
        [Parameter]
        public SwitchParameter FullScreen { get; set; }

        /// <summary><para type="description">Saves the final dashboard as an HTML page.</para></summary>
        [Parameter]
        public string? ExportHtml { get; set; }

        /// <summary><para type="description">The dashboard title (default Pester).</para></summary>
        [Parameter]
        public string Title { get; set; } = "Pester";

        /// <summary><para type="description">Runs Pester unchanged with its own output, followed by the summary table (no adapter).</para></summary>
        [Parameter]
        public SwitchParameter NoLive { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            TerminalProfile profile = Terminal.GetCurrentProfile();
            bool stream = HostOutput.UseHost(this, profile) || profile.Mode != OutputMode.Interactive;
            TestRun run = new TestRun();
            PesterBridge bridge = new PesterBridge(run);
            bool quiet = false;
            Glyphs.IGlyphSet glyphs = Glyphs.GlyphSet.For(stream ? HostOutput.HostProfile(this, profile, false).Glyphs : profile.Glyphs);
            bridge.OnBegin = (version, adapter, reason, isQuiet) => {
                quiet = isQuiet;
                if (!adapter && reason != null && reason.StartsWith("Pester internals changed", StringComparison.Ordinal)) {
                    WriteWarning("Invoke-ConsolePester runs Pester " + version + " with its own output: " + reason + ".");
                } else if (!adapter) {
                    WriteVerbose("Invoke-ConsolePester runs Pester " + version + " with its own output: " + reason + ".");
                }
                if (adapter && !isQuiet && !stream) {
                    StartSession(run, profile);
                }
            };
            bridge.OnContainer = () => _session?.Invalidate();
            bridge.OnFailure = record => {
                if (stream && bridge.UsedAdapter && !quiet) {
                    HostOutput.WriteHostLine(this, TestRunText.FailureLine(record, glyphs));
                }
            };

            PSObject? outcome;
            try {
                outcome = Invoke(bridge, stream);
            } catch (RuntimeException ex) when (!(ex is PipelineStoppedException)) {
                StopSession();
                FlushHostLines();
                ThrowTerminatingError(new ErrorRecord(ex, ex is ItemNotFoundException ? "ConsoleMonsterPesterNotFound" : "ConsoleMonsterPesterFailed", ex is ItemNotFoundException ? ErrorCategory.ObjectNotFound : ErrorCategory.NotSpecified, null));
                return;
            } finally {
                StopSession();
            }
            FlushHostLines();
            foreach (string fault in bridge.Faults) {
                WriteWarning("Invoke-ConsolePester: " + fault);
            }

            run.Finished();
            TestRunSnapshot snapshot = run.Snapshot();
            if (!quiet) {
                if (stream) {
                    HostOutput.WriteHostLine(this, TestRunText.SummaryLine(snapshot, glyphs));
                } else {
                    HostOutput.Write(this, new TestRunSummary(snapshot) { ShowFailures = bridge.UsedAdapter }, profile);
                }
            }
            if (!string.IsNullOrEmpty(ExportHtml)) {
                Export(run, snapshot, profile);
            }

            object? result = outcome?.Properties["Result"]?.Value;
            bool passThru = outcome?.Properties["PassThru"]?.Value is bool p && p;
            if (passThru && result != null) {
                WriteObject(result);
            }
            int failed = Count(result, "FailedCount") + Count(result, "FailedBlocksCount") + Count(result, "FailedContainersCount");
            if (outcome?.Properties["Throw"]?.Value is bool shouldThrow && shouldThrow && failed > 0) {
                ThrowTerminatingError(new ErrorRecord(new RuntimeException("Pester run failed, because " + failed.ToString(System.Globalization.CultureInfo.InvariantCulture) + " test(s) or block(s) failed."), "ConsoleMonsterPesterRunFailed", ErrorCategory.InvalidResult, result));
            }
            if (outcome?.Properties["Exit"]?.Value is bool shouldExit && shouldExit) {
                Host.SetShouldExit(Math.Min(255, failed));
            }
        }

        private PSObject? Invoke(PesterBridge bridge, bool stream) {
            Hashtable options = new Hashtable {
                ["Configuration"] = ParameterSetName == "Advanced" ? Configuration : null,
                ["Simple"] = Simple(),
                ["Fallback"] = NoLive.IsPresent
            };
            options["Stream"] = stream;
            Dictionary<string, ScriptBlock> functions = new Dictionary<string, ScriptBlock>();
            List<PSVariable> variables = new List<PSVariable>();
            if (!stream) {
                HostLineDispatcher lines = new HostLineDispatcher(this);
                bridge.OnRecord = (kind, text) => lines.Post(new LiveEvent("$log", "write", text: RecordMarkup(kind, text), status: "host", isMarkup: true));
                // Write-Host from the tests goes above the inline area (or after the full screen), not over the dashboard.
                functions["Write-Host"] = ScriptBlock.Create(CmdletUseConsoleProgress.HostProxy);
                variables.Add(new PSVariable("__cmProgressBridge", new ProgressBridge(lines)));
            }
            Collection<PSObject> output = ScriptBlock.Create(PesterAdapterScript.Text).InvokeWithContext(functions, variables, bridge, options);
            return output.Count == 0 ? null : output[output.Count - 1];
        }

        // A record as one markup line in the design system's colours: errors in Fail, warnings in Warn, verbose and debug Muted.
        private static string RecordMarkup(string kind, string text) {
            string escaped = Text.Markup.Escape(text.Replace("\r\n", "\n").Replace('\n', ' '));
            switch (kind) {
                case "error":
                    return "[fail]" + escaped + "[/]";
                case "warning":
                    return "[warn]WARNING: " + escaped + "[/]";
                case "verbose":
                    return "[muted]VERBOSE: " + escaped + "[/]";
                case "debug":
                    return "[muted]DEBUG: " + escaped + "[/]";
                default:
                    return escaped;
            }
        }

        private Hashtable Simple() {
            Hashtable simple = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (string name in new[] { "Path", "ExcludePath", "TagFilter", "ExcludeTagFilter", "FullNameFilter", "Container", "Output" }) {
                if (MyInvocation.BoundParameters.TryGetValue(name, out object? value)) {
                    simple[name] = value;
                }
            }
            if (PassThru.IsPresent) {
                simple["PassThru"] = true;
            }
            if (CI.IsPresent) {
                simple["CI"] = true;
            }
            return simple;
        }

        private void StartSession(TestRun run, TerminalProfile profile) {
            LiveSessionOptions options = new LiveSessionOptions {
                Mode = FullScreen.IsPresent ? LiveMode.FullScreen : LiveMode.Inline,
                PrintFinalState = false,
                HandleCancelKey = false,
                Discoverable = false,
                Clock = () => run.Elapsed,
                Profile = profile
            };
            _session = new LiveSession(new TestRunView(run) { Title = Title, Compact = !FullScreen.IsPresent }, options);
            _session.Start();
        }

        private void StopSession() {
            LiveSession? session = _session;
            _session = null;
            if (session != null) {
                session.Stop();
                session.Dispose();
            }
        }

        private void FlushHostLines() {
            foreach (string line in _hostLines) {
                HostOutput.WriteMarkup(this, line);
            }
            _hostLines.Clear();
        }

        private void Export(TestRun run, TestRunSnapshot snapshot, TerminalProfile profile) {
            string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(ExportHtml);
            ExportOptions options = new ExportOptions {
                Width = Math.Max(100, Math.Min(160, profile.Width)),
                Height = Math.Max(32, Math.Min(50, profile.Height)),
                Elapsed = snapshot.Elapsed,
                Glyphs = profile.Glyphs
            };
            string html = ConsoleExporter.ToHtml(new TestRunView(run) { Title = Title }, options);
            string? folder = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllText(path, html, new System.Text.UTF8Encoding(false));
            WriteVerbose("Saved the final dashboard to " + path + ".");
        }

        private static int Count(object? result, string property) {
            if (result == null) {
                return 0;
            }
            object? value = PSObject.AsPSObject(result).Properties[property]?.Value;
            try {
                return value == null ? 0 : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
            } catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException) {
                return 0;
            }
        }

        // Write-Host lines from tests: above the inline area while it runs, after the full screen, or straight to the host
        // when no dashboard is showing (Pester running unchanged).
        private sealed class HostLineDispatcher : ILiveDispatcher {
            private readonly CmdletInvokeConsolePester _cmdlet;

            public HostLineDispatcher(CmdletInvokeConsolePester cmdlet) {
                _cmdlet = cmdlet;
            }

            public void Post(LiveEvent liveEvent) {
                string text = liveEvent.Text ?? string.Empty;
                LiveSession? session = _cmdlet._session;
                if (session != null && !_cmdlet.FullScreen.IsPresent) {
                    session.Post(liveEvent);
                } else if (session != null) {
                    _cmdlet._hostLines.Add(text);
                } else {
                    HostOutput.WriteMarkup(_cmdlet, text);
                }
            }
        }
    }
}
