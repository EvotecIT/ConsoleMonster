using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Translates explicit PowerShell action definitions into isolated, cancellable core commands.</summary>
    internal static class PSCommandActions {
        internal static AppCommand Create(string key, string label, string source, object[] arguments, string[] modules) {
            // Capture on the caller's runspace. Workers receive serialized values, never the caller's PSObject graph.
            string argumentData = PSSerializer.Serialize(arguments, 3);
            string[] imports = (string[])modules.Clone();
            AppCommand command = new AppCommand(key, label, (context, token) => InvokeAsync(context, source, argumentData, imports, token));
            command.CaptureRows = rows => {
                List<object?> snapshots = new List<object?>();
                foreach (object? row in rows) { snapshots.Add(new RowData(PSSerializer.Serialize(row, 3))); }
                return snapshots.AsReadOnly();
            };
            return command;
        }

        internal static IEnumerable<AppCommand> Collect(object[]? definitions) {
            if (definitions == null) { yield break; }
            foreach (object definition in definitions) {
                object value = PSObject.AsPSObject(definition).BaseObject;
                if (value is AppCommand command) { yield return command; }
                else if (value is IDictionary dictionary) {
                    List<string> keys = new List<string>();
                    foreach (object key in dictionary.Keys) {
                        if (!(key is string text) || string.IsNullOrWhiteSpace(text)) { throw new ArgumentException("Action keys must be nonempty strings."); }
                        keys.Add(text);
                    }
                    keys.Sort(StringComparer.OrdinalIgnoreCase);
                    foreach (string key in keys) {
                        if (!(dictionary[key] is ScriptBlock script)) { throw new ArgumentException("Each action value must be a script block. Use New-ConsoleCommand for confirmation, arguments or modules."); }
                        AppCommand shorthand = Create(key, key, script.ToString(), Array.Empty<object>(), Array.Empty<string>());
                        shorthand.ConfirmPrompt = key + " the selected rows?";
                        yield return shorthand;
                    }
                } else { throw new ArgumentException("Actions must be command objects or a hashtable of script blocks."); }
            }
        }

        private static async Task InvokeAsync(AppActionContext context, string source, string argumentData, string[] modules, CancellationToken token) {
            // The runspace owns its module imports and all PowerShell APIs used by the operation.
            InitialSessionState initial = InitialSessionState.CreateDefault2();
            if (modules.Length > 0) { initial.ImportPSModule(modules); }
            using (Runspace runspace = RunspaceFactory.CreateRunspace(initial)) {
                runspace.Open();
                int count = Math.Max(1, context.Rows.Count);
                for (int index = 0; index < count; index++) {
                    token.ThrowIfCancellationRequested();
                    string row = context.Rows.Count == 0 ? PSSerializer.Serialize(null) : ((RowData)context.Rows[index]!).Xml;
                    using (System.Management.Automation.PowerShell pipeline = System.Management.Automation.PowerShell.Create()) {
                        pipeline.Runspace = runspace;
                        pipeline.AddScript("param($rowXml, $argumentsXml)\n$Row = [System.Management.Automation.PSSerializer]::Deserialize($rowXml)\n$arguments = [System.Management.Automation.PSSerializer]::Deserialize($argumentsXml)\n& {\n" + source + "\n} $Row @arguments");
                        pipeline.AddArgument(row).AddArgument(argumentData);
                        using (PSDataCollection<PSObject> output = new PSDataCollection<PSObject>()) {
                            output.DataAdded += (_, __) => { foreach (PSObject item in output.ReadAll()) { context.Write(item?.ToString() ?? string.Empty); } };
                            pipeline.Streams.Error.DataAdded += (_, __) => { foreach (ErrorRecord item in pipeline.Streams.Error.ReadAll()) { context.Write(item.ToString(), "err"); } };
                            pipeline.Streams.Warning.DataAdded += (_, __) => { foreach (WarningRecord item in pipeline.Streams.Warning.ReadAll()) { context.Write(item.Message, "warn"); } };
                            pipeline.Streams.Verbose.DataAdded += (_, __) => { foreach (VerboseRecord item in pipeline.Streams.Verbose.ReadAll()) { context.Write(item.Message, "verbose"); } };
                            pipeline.Streams.Debug.DataAdded += (_, __) => { foreach (DebugRecord item in pipeline.Streams.Debug.ReadAll()) { context.Write(item.Message, "debug"); } };
                            pipeline.Streams.Information.DataAdded += (_, __) => { foreach (InformationRecord item in pipeline.Streams.Information.ReadAll()) { context.Write(Convert.ToString(item.MessageData) ?? string.Empty, "host"); } };
                            pipeline.Streams.Progress.DataAdded += (_, e) => {
                                foreach (ProgressRecord progress in pipeline.Streams.Progress.ReadAll()) { context.Write(progress.Activity + ": " + progress.StatusDescription, "info"); }
                            };
                            IAsyncResult pending = pipeline.BeginInvoke<PSObject, PSObject>(null, output);
                            using (token.Register(() => {
                                try { pipeline.BeginStop(null, null); }
                                catch (InvalidOperationException) { }
                            })) {
                                while (!pending.IsCompleted) { await Task.Delay(15).ConfigureAwait(false); }
                                try { pipeline.EndInvoke(pending); }
                                catch (PipelineStoppedException) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
                            }
                            token.ThrowIfCancellationRequested();
                            if (pipeline.HadErrors) { throw new InvalidOperationException("The action reported a PowerShell error."); }
                        }
                    }
                }
            }
        }

        private sealed class RowData {
            internal RowData(string xml) { Xml = xml; }
            internal string Xml { get; }
        }
    }
}
