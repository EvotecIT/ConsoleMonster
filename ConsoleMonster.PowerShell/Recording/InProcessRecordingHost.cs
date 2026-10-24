using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Security;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>A limited, explicitly labelled PowerShell host capture; it does not emulate a native terminal.</summary>
    internal sealed class InProcessRecordingHost : PSHost {
        private readonly PSHost _parent;
        private readonly SessionRecorder _recorder;
        private readonly RecordingUi _ui;
        private bool _exit;
        private int _exitCode;
        private InProcessRecordingHost(PSHost parent, SessionRecorder recorder) {
            _parent = parent; _recorder = recorder; _ui = new RecordingUi(parent.UI, recorder);
        }
        public static string Run(PSCmdlet command, SessionRecordingOptions options, string? script, Func<bool> cancelled) {
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                InProcessRecordingHost host = new InProcessRecordingHost(command.Host, recorder);
                InitialSessionState state = InitialSessionState.CreateDefault2();
                PSModuleInfo? module = command.MyInvocation.MyCommand.Module;
                if (module != null) {
                    string manifest = System.IO.Path.Combine(module.ModuleBase, "ConsoleMonster.psd1");
                    state.ImportPSModule(new[] { System.IO.File.Exists(manifest) ? manifest : typeof(InProcessRecordingHost).Assembly.Location });
                }
                state.Commands.Add(new SessionStateCmdletEntry("Add-ConsoleRecordingMarker", typeof(CmdletAddConsoleRecordingMarker), null));
                state.Commands.Add(new SessionStateCmdletEntry("Stop-ConsoleRecording", typeof(CmdletStopConsoleRecording), null));
                state.Variables.Add(new SessionStateVariableEntry("ConsoleRecording", recorder, "Limited recording sink."));
                using (Runspace runspace = RunspaceFactory.CreateRunspace(host, state)) {
                    runspace.Open();
                    runspace.SessionStateProxy.Path.SetLocation(options.WorkingDirectory);
                    if (script != null) { Execute(runspace, host, script, cancelled, options.Timeout); }
                    else {
                        while (!host._exit && !recorder.HasExited && !cancelled()) {
                            command.Host.UI.Write("PS (recording, PowerShell output only)> ");
                            string? line = command.Host.UI.ReadLine();
                            if (line == null) { break; }
                            Execute(runspace, host, line, cancelled, options.Timeout);
                        }
                    }
                }
                recorder.Stop();
                if (recorder.LimitReached) { command.Host.UI.WriteWarningLine("The recording reached MaxBytes."); }
                if (host._exitCode != 0) {
                    command.WriteError(new ErrorRecord(new InvalidOperationException("The recorded child exited with code " + host._exitCode + "."),
                        "ConsoleRecordingChildFailed", ErrorCategory.OperationStopped, recorder.Path));
                }
                return recorder.Path;
            }
        }
        private static void Execute(Runspace runspace, InProcessRecordingHost host, string script, Func<bool> cancelled, TimeSpan timeout) {
            using (System.Management.Automation.PowerShell shell = System.Management.Automation.PowerShell.Create()) {
                shell.Runspace = runspace;
                shell.AddScript(script).AddCommand("Out-Default");
                try {
                    IAsyncResult running = shell.BeginInvoke();
                    System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
                    while (!running.AsyncWaitHandle.WaitOne(30)) {
                        if (cancelled() || host._recorder.HasExited) { shell.Stop(); break; }
                        if (clock.Elapsed > timeout) { shell.Stop(); throw new TimeoutException("The in-process recording exceeded TimeoutSeconds."); }
                    }
                    shell.EndInvoke(running);
                }
                catch (PipelineStoppedException) when (cancelled() || host._recorder.HasExited) { }
                catch (RuntimeException error) { host.UI.WriteErrorLine(error.Message); throw; }
            }
        }
        public override Guid InstanceId { get; } = Guid.NewGuid();
        public override string Name => "ConsoleMonster in-process PowerShell output only";
        public override Version Version => new Version(1, 0);
        public override PSHostUserInterface UI => _ui;
        public override CultureInfo CurrentCulture => _parent.CurrentCulture;
        public override CultureInfo CurrentUICulture => _parent.CurrentUICulture;
        public override void SetShouldExit(int exitCode) { _exitCode = exitCode; _exit = true; }
        public override void EnterNestedPrompt() { throw new NotSupportedException("Nested prompts are unavailable in the recording fallback."); }
        public override void ExitNestedPrompt() { _exit = true; }
        public override void NotifyBeginApplication() { }
        public override void NotifyEndApplication() { }

        private sealed class RecordingUi : PSHostUserInterface {
            private readonly PSHostUserInterface _parent;
            private readonly SessionRecorder _recorder;
            public RecordingUi(PSHostUserInterface parent, SessionRecorder recorder) { _parent = parent; _recorder = recorder; }
            public override PSHostRawUserInterface RawUI => _parent.RawUI;
            private void Capture(string value) { if (!_recorder.HasExited) { _recorder.CapturePowerShellOutput(value); } }
            public override string ReadLine() => _parent.ReadLine();
            public override SecureString ReadLineAsSecureString() => _parent.ReadLineAsSecureString();
            public override void Write(string value) { Capture(value); _parent.Write(value); }
            public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) { Capture(value); _parent.Write(foregroundColor, backgroundColor, value); }
            public override void WriteLine(string value) { Capture(value + "\r\n"); _parent.WriteLine(value); }
            public override void WriteErrorLine(string value) { Capture(value + "\r\n"); _parent.WriteErrorLine(value); }
            public override void WriteDebugLine(string message) { Capture(message + "\r\n"); _parent.WriteDebugLine(message); }
            public override void WriteVerboseLine(string message) { Capture(message + "\r\n"); _parent.WriteVerboseLine(message); }
            public override void WriteWarningLine(string message) { Capture(message + "\r\n"); _parent.WriteWarningLine(message); }
            public override void WriteProgress(long sourceId, ProgressRecord record) { Capture(record.Activity + ": " + record.StatusDescription + " " + record.PercentComplete + "%\r\n"); _parent.WriteProgress(sourceId, record); }
            public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions) => _parent.Prompt(caption, message, descriptions);
            public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName) => _parent.PromptForCredential(caption, message, userName, targetName);
            public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) => _parent.PromptForCredential(caption, message, userName, targetName, allowedCredentialTypes, options);
            public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice) => _parent.PromptForChoice(caption, message, choices, defaultChoice);
        }
    }
}
