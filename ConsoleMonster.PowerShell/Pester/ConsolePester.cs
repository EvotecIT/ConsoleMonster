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
    /// <summary>Owns the existing Pester adapter, dashboard and native result policy for a C# PowerShell host. Requires Pester and the caller's owning runspace.</summary>
    public sealed class ConsolePester : IDisposable {
        private readonly PSCmdlet _source;
        private readonly ConsolePesterOptions _options;
        private readonly List<string> _hostLines = new List<string>();
        private LiveSession? _session;

        /// <summary>Creates the host service without starting tests.</summary>
        /// <param name="source">The owning C# host cmdlet.</param>
        /// <param name="options">Native Pester and display configuration.</param>
        public ConsolePester(PSCmdlet source, ConsolePesterOptions? options = null) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _options = options ?? new ConsolePesterOptions();
            if (_options.Configuration != null && (_options.Simple().Count != 0 || _options.CI || _options.PassThru)) {
                throw new ArgumentException("Configuration cannot be combined with simple Pester parameters.", nameof(options));
            }
        }

        /// <summary>Stops and disposes the owned display session.</summary>
        public void Dispose() => StopSession();

        /// <summary>Runs Pester once on the owning pipeline thread, preserving its native result and exit policy.</summary>
        public void Run() {
            TerminalProfile profile = Terminal.GetCurrentProfile();
            bool stream = HostOutput.UseHost(_source, profile) || profile.Mode != OutputMode.Interactive;
            TestRun run = new TestRun();
            PesterBridge bridge = new PesterBridge(run);
            bool quiet = false;
            Glyphs.IGlyphSet glyphs = Glyphs.GlyphSet.For(stream ? HostOutput.HostProfile(_source, profile, false).Glyphs : profile.Glyphs);
            bridge.OnBegin = (version, adapter, reason, isQuiet) => {
                quiet = isQuiet;
                if (!adapter && reason != null && reason.StartsWith("Pester internals changed", StringComparison.Ordinal)) {
                    _source.WriteWarning("Invoke-ConsolePester runs Pester " + version + " with its own output: " + reason + ".");
                } else if (!adapter) {
                    _source.WriteVerbose("Invoke-ConsolePester runs Pester " + version + " with its own output: " + reason + ".");
                }
                if (adapter && !isQuiet && !stream) {
                    StartSession(run, profile);
                }
            };
            bridge.OnContainer = () => _session?.Invalidate();
            bridge.OnFailure = record => {
                if (stream && bridge.UsedAdapter && !quiet) {
                    HostOutput.WriteHostLine(_source, TestRunText.FailureLine(record, glyphs));
                }
            };

            PSObject? outcome;
            try {
                outcome = Invoke(bridge, stream);
            } catch (RuntimeException ex) when (!(ex is PipelineStoppedException)) {
                StopSession();
                FlushHostLines();
                _source.ThrowTerminatingError(new ErrorRecord(ex, ex is ItemNotFoundException ? "ConsoleMonsterPesterNotFound" : "ConsoleMonsterPesterFailed", ex is ItemNotFoundException ? ErrorCategory.ObjectNotFound : ErrorCategory.NotSpecified, null));
                return;
            } finally {
                StopSession();
            }
            FlushHostLines();
            foreach (string fault in bridge.Faults) {
                _source.WriteWarning("Invoke-ConsolePester: " + fault);
            }

            run.Finished();
            TestRunSnapshot snapshot = run.Snapshot();
            if (!quiet) {
                if (stream) {
                    HostOutput.WriteHostLine(_source, TestRunText.SummaryLine(snapshot, glyphs));
                } else {
                    HostOutput.Write(_source, new TestRunSummary(snapshot) { ShowFailures = bridge.UsedAdapter }, profile);
                }
            }
            if (!string.IsNullOrEmpty(_options.ExportHtml)) {
                Export(run, snapshot, profile);
            }

            object? result = outcome?.Properties["Result"]?.Value;
            bool passThru = outcome?.Properties["PassThru"]?.Value is bool p && p;
            if (passThru && result != null) {
                _source.WriteObject(result);
            }
            int failed = Count(result, "FailedCount") + Count(result, "FailedBlocksCount") + Count(result, "FailedContainersCount");
            if (outcome?.Properties["Throw"]?.Value is bool shouldThrow && shouldThrow && failed > 0) {
                _source.ThrowTerminatingError(new ErrorRecord(new RuntimeException("Pester run failed, because " + failed.ToString(System.Globalization.CultureInfo.InvariantCulture) + " test(s) or block(s) failed."), "ConsoleMonsterPesterRunFailed", ErrorCategory.InvalidResult, result));
            }
            if (outcome?.Properties["Exit"]?.Value is bool shouldExit && shouldExit) {
                _source.Host.SetShouldExit(Math.Min(255, failed));
            }
        }

        private PSObject? Invoke(PesterBridge bridge, bool stream) {
            Hashtable options = new Hashtable {
                ["Configuration"] = _options.Configuration,
                ["Simple"] = Simple(),
                ["Fallback"] = !_options.Live
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
            Hashtable simple = _options.Simple();
            if (_options.PassThru) {
                simple["PassThru"] = true;
            }
            if (_options.CI) {
                simple["CI"] = true;
            }
            return simple;
        }

        private void StartSession(TestRun run, TerminalProfile profile) {
            LiveSessionOptions options = new LiveSessionOptions {
                Mode = _options.FullScreen ? LiveMode.FullScreen : LiveMode.Inline,
                PrintFinalState = false,
                HandleCancelKey = false,
                Discoverable = false,
                Clock = () => run.Elapsed,
                Profile = profile
            };
            _session = new LiveSession(new TestRunView(run) { Title = _options.Title, Compact = !_options.FullScreen }, options);
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
                HostOutput.WriteMarkup(_source, line);
            }
            _hostLines.Clear();
        }

        private void Export(TestRun run, TestRunSnapshot snapshot, TerminalProfile profile) {
            string path = _source.SessionState.Path.GetUnresolvedProviderPathFromPSPath(_options.ExportHtml);
            ExportOptions options = new ExportOptions {
                Width = Math.Max(100, Math.Min(160, profile.Width)),
                Height = Math.Max(32, Math.Min(50, profile.Height)),
                Elapsed = snapshot.Elapsed,
                Glyphs = profile.Glyphs
            };
            string html = ConsoleExporter.ToHtml(new TestRunView(run) { Title = _options.Title }, options);
            string? folder = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllText(path, html, new System.Text.UTF8Encoding(false));
            _source.WriteVerbose("Saved the final dashboard to " + path + ".");
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
            private readonly ConsolePester _runner;

            public HostLineDispatcher(ConsolePester runner) {
                _runner = runner;
            }

            public void Post(LiveEvent liveEvent) {
                string text = liveEvent.Text ?? string.Empty;
                LiveSession? session = _runner._session;
                if (session != null && !_runner._options.FullScreen) {
                    session.Post(liveEvent);
                } else if (session != null) {
                    _runner._hostLines.Add(text);
                } else {
                    HostOutput.WriteMarkup(_runner._source, text);
                }
            }
        }
    }
}
