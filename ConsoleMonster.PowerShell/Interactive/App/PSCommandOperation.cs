using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Owns all runspace resources, including cleanup after the app stops awaiting noncooperative work.</summary>
    internal sealed class PSCommandOperation {
        private readonly AppActionContext _context;
        private readonly string _source;
        private readonly string _arguments;
        private readonly string[] _modules;
        private readonly CancellationToken _token;
        private readonly object _gate = new object();
        private System.Management.Automation.PowerShell? _pipeline;

        internal PSCommandOperation(AppActionContext context, string source, string arguments, string[] modules, CancellationToken token) {
            _context = context;
            _source = source;
            _arguments = arguments;
            _modules = modules;
            _token = token;
        }

        /// <summary>Requests asynchronous pipeline stop; disposal remains exclusively with Run.</summary>
        internal void RequestStop() {
            System.Management.Automation.PowerShell? pipeline;
            lock (_gate) { pipeline = _pipeline; }
            if (pipeline == null) { return; }
            try { pipeline.BeginStop(null, null); }
            catch (InvalidOperationException) { }
        }

        internal void Run() {
            _token.ThrowIfCancellationRequested();
            InitialSessionState initial = InitialSessionState.CreateDefault2();
            if (_modules.Length > 0) { initial.ImportPSModule(_modules); }
            using (Runspace runspace = RunspaceFactory.CreateRunspace(initial)) {
                runspace.Open();
                _token.ThrowIfCancellationRequested();
                for (int index = 0; index < Math.Max(1, _context.Rows.Count); index++) {
                    _token.ThrowIfCancellationRequested();
                    string row = _context.Rows.Count == 0 ? PSSerializer.Serialize(null) : ((PSCommandActions.RowData)_context.Rows[index]!).Xml;
                    using (System.Management.Automation.PowerShell pipeline = System.Management.Automation.PowerShell.Create()) {
                        pipeline.Runspace = runspace;
                        pipeline.AddScript("param($rowXml, $argumentsXml)\n$Row = [System.Management.Automation.PSSerializer]::Deserialize($rowXml)\n$arguments = [System.Management.Automation.PSSerializer]::Deserialize($argumentsXml)\n& {\n" + _source + "\n} $Row @arguments");
                        pipeline.AddArgument(row).AddArgument(_arguments);
                        using (PSDataCollection<PSObject> output = new PSDataCollection<PSObject>()) {
                            AttachStreams(pipeline, output);
                            lock (_gate) { _pipeline = pipeline; }
                            try {
                                _token.ThrowIfCancellationRequested();
                                IAsyncResult pending = pipeline.BeginInvoke<PSObject, PSObject>(null, output);
                                // Covers cancellation between the pre-invocation check and pipeline start.
                                if (_token.IsCancellationRequested) { RequestStop(); }
                                try { pipeline.EndInvoke(pending); }
                                catch (PipelineStoppedException) when (_token.IsCancellationRequested) { throw new OperationCanceledException(_token); }
                                _token.ThrowIfCancellationRequested();
                                if (pipeline.HadErrors) { throw new InvalidOperationException("The action reported a PowerShell error."); }
                            } finally { lock (_gate) { _pipeline = null; } }
                        }
                    }
                }
            }
        }

        private void Write(string text, string level = "info") {
            // A detached cancelled worker cannot append late output to a later action's log.
            if (!_token.IsCancellationRequested) { _context.Write(text, level); }
        }

        private void AttachStreams(System.Management.Automation.PowerShell pipeline, PSDataCollection<PSObject> output) {
            output.DataAdded += (_, __) => { foreach (PSObject item in output.ReadAll()) { Write(item?.ToString() ?? string.Empty); } };
            pipeline.Streams.Error.DataAdded += (_, __) => { foreach (ErrorRecord item in pipeline.Streams.Error.ReadAll()) { Write(item.ToString(), "err"); } };
            pipeline.Streams.Warning.DataAdded += (_, __) => { foreach (WarningRecord item in pipeline.Streams.Warning.ReadAll()) { Write(item.Message, "warn"); } };
            pipeline.Streams.Verbose.DataAdded += (_, __) => { foreach (VerboseRecord item in pipeline.Streams.Verbose.ReadAll()) { Write(item.Message, "verbose"); } };
            pipeline.Streams.Debug.DataAdded += (_, __) => { foreach (DebugRecord item in pipeline.Streams.Debug.ReadAll()) { Write(item.Message, "debug"); } };
            pipeline.Streams.Information.DataAdded += (_, __) => { foreach (InformationRecord item in pipeline.Streams.Information.ReadAll()) { Write(Convert.ToString(item.MessageData) ?? string.Empty, "host"); } };
            pipeline.Streams.Progress.DataAdded += (_, __) => {
                foreach (ProgressRecord progress in pipeline.Streams.Progress.ReadAll()) { Write(progress.Activity + ": " + progress.StatusDescription); }
            };
        }
    }
}
