using System;
using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Routes the Information (including Write-Host), Warning, Verbose, Debug and Error streams of a PowerShell instance
    /// the module owns into a live session's log target, so script output never writes at a random cursor position.
    /// </summary>
    internal sealed class PowerShellStreamCapture : IDisposable {
        private readonly System.Management.Automation.PowerShell _shell;
        private readonly ILiveDispatcher _session;
        private readonly string? _prefix;

        public PowerShellStreamCapture(System.Management.Automation.PowerShell shell, ILiveDispatcher session, string? prefix = null) {
            _shell = shell;
            _session = session;
            _prefix = prefix;
            shell.Streams.Information.DataAdded += OnInformation;
            shell.Streams.Warning.DataAdded += OnWarning;
            shell.Streams.Verbose.DataAdded += OnVerbose;
            shell.Streams.Debug.DataAdded += OnDebug;
            shell.Streams.Error.DataAdded += OnError;
            shell.Streams.Progress.DataAdded += OnProgress;
        }

        public void Dispose() {
            _shell.Streams.Information.DataAdded -= OnInformation;
            _shell.Streams.Warning.DataAdded -= OnWarning;
            _shell.Streams.Verbose.DataAdded -= OnVerbose;
            _shell.Streams.Debug.DataAdded -= OnDebug;
            _shell.Streams.Error.DataAdded -= OnError;
            _shell.Streams.Progress.DataAdded -= OnProgress;
        }

        // Write-Progress becomes an activity in the session's activity list (added by Start-ConsoleDashboard when the
        // dashboard has none), nested by ParentActivityId.
        private void OnProgress(object? sender, DataAddedEventArgs e) {
            ProgressRecord record = _shell.Streams.Progress[e.Index];
            _session.Post(ProgressEvents.From(record.ActivityId, record.ParentActivityId, record.Activity, record.StatusDescription, record.PercentComplete, record.SecondsRemaining, record.RecordType == ProgressRecordType.Completed));
        }

        private void OnInformation(object? sender, DataAddedEventArgs e) {
            InformationRecord record = _shell.Streams.Information[e.Index];
            bool host = record.Tags.Contains("PSHOST");
            string text = record.MessageData is HostInformationMessage message ? message.Message : PSWidgets.ToText(record.MessageData);
            Write(text, host ? "host" : "info");
        }

        private void OnWarning(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Warning[e.Index].Message, "warn");

        private void OnVerbose(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Verbose[e.Index].Message, "verbose");

        private void OnDebug(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Debug[e.Index].Message, "debug");

        private void OnError(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Error[e.Index].ToString(), "err");

        private void Write(string text, string level) {
            _session.Post(new LiveEvent("$log", "write", text: _prefix == null ? text : _prefix + text, state: level));
        }
    }
}
