using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;
using System.Resources;
using ConsoleMonster.Canvas;
using ConsoleMonster.Localization;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Owns the global prompt slot, its original script block, and one bounded git cache.</summary>
    internal sealed class PromptInstallation {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly ResourceManager Messages = new ResourceManager("ConsoleMonster.PowerShell.Formatting.PromptMessages", typeof(PromptInstallation).Assembly);
        private readonly SessionState _session;
        private readonly PSModuleInfo _module;
        private readonly ScriptBlock? _saved;
        private FunctionInfo? _original;
        private readonly PromptGitCache _git = new PromptGitCache();
        private readonly object _engine;
        private readonly PropertyInfo _success;
        private ScriptBlock? _installed;
        private bool _removed;

        private PromptInstallation(PSCmdlet cmdlet, object engine, PropertyInfo success) {
            _session = cmdlet.SessionState;
            _module = cmdlet.MyInvocation.MyCommand.Module;
            _saved = FormatCommandInstallation.GlobalFunction(_session, "prompt");
            _engine = engine;
            _success = success;
        }

        internal static bool Install(PSCmdlet cmdlet, bool force) {
            FormattingSessionState state = FormattingSessionState.Current;
            if (state.Prompt != null) { return true; }
            ScriptBlock? original = FormatCommandInstallation.GlobalFunction(cmdlet.SessionState, "prompt");
            string definition = original?.ToString() ?? string.Empty;
            if (!force && (definition.IndexOf("oh-my-posh", StringComparison.OrdinalIgnoreCase) >= 0
                || definition.IndexOf("oh_my_posh", StringComparison.OrdinalIgnoreCase) >= 0
                || definition.IndexOf("starship", StringComparison.OrdinalIgnoreCase) >= 0
                || cmdlet.SessionState.PSVariable.Get("global:STARSHIP_SESSION_KEY") != null
                || cmdlet.SessionState.PSVariable.Get("global:POSH_SESSION_ID") != null
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSH_SESSION_ID"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STARSHIP_SESSION_KEY")))) {
                cmdlet.WriteWarning(Message("ProtectedPrompt"));
                return false;
            }
            // Preserve the original prompt's input status too. Unknown engines retain their existing prompt.
            object? intrinsics = cmdlet.SessionState.PSVariable.GetValue("ExecutionContext");
            object? engine = intrinsics?.GetType().GetField("_context", Flags)?.GetValue(intrinsics);
            PropertyInfo? success = engine?.GetType().GetProperty("QuestionMarkVariableValue", Flags);
            ConstructorInfo? copy = typeof(FunctionInfo).GetConstructor(Flags, null, new[] { typeof(FunctionInfo) }, null);
            if (engine == null || success == null || !success.CanRead || !success.CanWrite || copy == null) {
                cmdlet.WriteWarning(Message("UnsupportedEngine"));
                return false;
            }
            PromptInstallation installation = new PromptInstallation(cmdlet, engine, success);
            try {
                string resource;
                using (Stream stream = typeof(PromptInstallation).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Formatting.PromptFunction.ps1")
                    ?? throw new InvalidOperationException(Message("MissingResource")))
                using (StreamReader reader = new StreamReader(stream)) { resource = reader.ReadToEnd(); }
                // Set-Item mutates the existing FunctionInfo. Retain separate native metadata so dispatch
                // keeps the original function identity, closure, parameters and caller error pipeline.
                FunctionInfo? function = cmdlet.InvokeCommand.GetCommand("global:prompt", CommandTypes.Function) as FunctionInfo;
                installation._original = function == null ? null : (FunctionInfo)copy.Invoke(new object[] { function });
                CommandMetadata? metadata = installation._original == null ? null : new CommandMetadata(installation._original);
                resource = resource.Replace("__BINDING__", metadata == null ? string.Empty : ProxyCommand.GetCmdletBindingAttribute(metadata))
                    .Replace("__PARAMETERS__", metadata == null ? string.Empty : ProxyCommand.GetParamBlock(metadata))
                    .Replace("__ARGUMENTS__", installation._original?.CmdletBinding == true ? string.Empty : "@args");
                Func<bool, InvocationInfo, PSHost, object[], Invocation> invoke = installation.Invoke;
                ScriptBlock prompt = (ScriptBlock)ScriptBlock.Create(resource).Invoke(invoke, installation._original?.CmdletBinding == true)[0].BaseObject;
                cmdlet.SessionState.InvokeProvider.Item.Set("Function:\\global:prompt", prompt);
                installation._installed = FormatCommandInstallation.GlobalFunction(cmdlet.SessionState, "prompt");
                state.Prompt = installation;
                return true;
            } catch { installation._git.Dispose(); throw; }
        }

        private static string Message(string key) => Messages.GetString(key, LabelLanguage.Default) ?? key;

        private Invocation Invoke(bool succeeded, InvocationInfo invocation, PSHost host, object[] stack) {
            Invocation frame = new Invocation(this, succeeded);
            try {
                if (_removed || !DirectHost(invocation, host, stack)) {
                    if (_original == null) { frame.Text = NativePrompt(); }
                    return frame;
                }
                PathInfo location = _session.Path.CurrentLocation;
                (string? branch, bool dirty) = string.Equals(location.Provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)
                    ? _git.Read(location.ProviderPath) : (null, false);
                TimeSpan duration = TimeSpan.Zero;
                // History measures completed commands already. Never replace a PSReadLine callback to time input.
                using (System.Management.Automation.PowerShell history = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                    var records = history.AddCommand("Microsoft.PowerShell.Core\\Get-History").AddParameter("Count", 1).Invoke();
                    if (records.Count > 0 && records[0].Properties["StartExecutionTime"]?.Value is DateTime start
                        && records[0].Properties["EndExecutionTime"]?.Value is DateTime end && end >= start) { duration = end - start; }
                }
                ShellPrompt prompt = new ShellPrompt { Path = location.Path, Home = Convert.ToString(_session.PSVariable.GetValue("HOME")),
                    Branch = branch, Dirty = dirty, Succeeded = succeeded, Duration = duration };
                frame.Text = prompt.ToAnsi(new RenderContext(Terminal.GetCurrentProfile(), Terminal.Theme), Math.Max(1, Terminal.GetCurrentProfile().Width - 1));
                return frame;
            } catch { frame.Restore(); throw; }
        }

        // One frame per call also protects recursive/nested native prompt evaluation. Its public members
        // are consumed only by the embedded PowerShell closure; the type is not a module API.
        internal sealed class Invocation {
            private readonly PromptInstallation _owner;
            private readonly bool _succeeded;
            private readonly PSVariable? _lastExit;
            private readonly object? _exitValue;
            internal Invocation(PromptInstallation owner, bool succeeded) {
                _owner = owner;
                _succeeded = succeeded;
                _lastExit = owner._session.PSVariable.Get("global:LASTEXITCODE");
                _exitValue = _lastExit?.Value;
            }
            public string? Text { get; internal set; }
            private void PrepareOriginal() => _owner._success.SetValue(_owner._engine, _succeeded, null);
            public FunctionInfo? Original() { PrepareOriginal(); return _owner._original; }
            public void Restore() {
                if (_lastExit != null) {
                    if (!Equals(_lastExit.Value, _exitValue)) { _lastExit.Value = _exitValue; }
                    if (!ReferenceEquals(_owner._session.PSVariable.Get("global:LASTEXITCODE"), _lastExit)) { _owner._session.PSVariable.Set(_lastExit); }
                } else if (_owner._session.PSVariable.Get("global:LASTEXITCODE") != null) {
                    _owner._session.PSVariable.Remove("global:LASTEXITCODE");
                }
                PrepareOriginal();
            }
        }

        private string NativePrompt() => "PS " + _session.Path.CurrentLocation + new string('>', Convert.ToInt32(_session.PSVariable.GetValue("nestedPromptLevel")) + 1) + " ";

        private bool DirectHost(InvocationInfo invocation, PSHost host, object[] stack) {
            // The console engine evaluates the literal top-level prompt pipeline. Captures and scripts retain the original.
            return host.Name == "ConsoleHost" && !Console.IsOutputRedirected && !Console.IsInputRedirected
                && invocation.Line.Trim() == "prompt" && invocation.InvocationName == "prompt" && string.IsNullOrEmpty(invocation.ScriptName)
                && invocation.PipelineLength == 1 && stack.Length <= 2
                && Convert.ToInt32(_session.PSVariable.GetValue("nestedPromptLevel")) == 0
                && _session.PSVariable.GetValue("PSSenderInfo") == null
                && Terminal.IsConsole(Terminal.Target) && !Terminal.GetCurrentProfile().IsContinuousIntegration
                && FormatCommandRouter.IsUnrecorded(host);
        }

        internal static void Remove(FormattingSessionState state) {
            PromptInstallation? installation = state.Prompt;
            if (installation == null) { return; }
            installation._removed = true;
            installation._git.Dispose();
            ScriptBlock? current = FormatCommandInstallation.GlobalFunction(installation._session, "prompt");
            if (current != null && current.ToString() == installation._installed?.ToString()) {
                if (installation._saved == null) { FormatCommandInstallation.RemoveGlobalFunction(installation._module, "prompt"); }
                else { installation._session.InvokeProvider.Item.Set("Function:\\global:prompt", installation._saved); }
            }
            state.Prompt = null;
        }
    }
}
