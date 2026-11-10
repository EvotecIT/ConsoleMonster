using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Routes the Information (including Write-Host), Warning, Verbose, Debug and Error streams of a PowerShell instance
    /// the module owns into a live session's log target, so script output never writes at a random cursor position.
    /// With <c>releaseDelivered</c> (dashboards, whose scripts can run for hours) the Information, Warning, Verbose, Debug
    /// and Progress records are removed from the instance once they reach the log, so those collections stay small; the
    /// Error stream is kept for the caller.
    /// </summary>
    internal sealed class PowerShellStreamCapture : IDisposable {
        private readonly System.Management.Automation.PowerShell _shell;
        private readonly ILiveDispatcher _session;
        private readonly string? _prefix;
        private readonly PSDataCollection<PSObject>? _merged;
        private readonly ConcurrentDictionary<int, int>? _progressIds;
        private readonly bool _release;
        private static int _nextProgressId = -1;

        public PowerShellStreamCapture(System.Management.Automation.PowerShell shell, ILiveDispatcher session, string? prefix = null,
            PSDataCollection<PSObject>? merged = null, bool isolateProgress = false, bool releaseDelivered = false) {
            _shell = shell;
            _session = session;
            _prefix = prefix;
            _merged = merged;
            _release = releaseDelivered;
            _progressIds = isolateProgress ? new ConcurrentDictionary<int, int>() : null;
            if (_merged != null) { _merged.DataAdded += OnMerged; }
            if (_release) {
                // Delivered from the record itself (no index), so emptying the collection afterwards cannot shift a read.
                shell.Streams.Information.DataAdding += OnInformationAdding;
                shell.Streams.Warning.DataAdding += OnWarningAdding;
                shell.Streams.Verbose.DataAdding += OnVerboseAdding;
                shell.Streams.Debug.DataAdding += OnDebugAdding;
                shell.Streams.Progress.DataAdding += OnProgressAdding;
                shell.Streams.Information.DataAdded += Release;
                shell.Streams.Warning.DataAdded += Release;
                shell.Streams.Verbose.DataAdded += Release;
                shell.Streams.Debug.DataAdded += Release;
                shell.Streams.Progress.DataAdded += Release;
            } else {
                shell.Streams.Information.DataAdded += OnInformation;
                shell.Streams.Warning.DataAdded += OnWarning;
                shell.Streams.Verbose.DataAdded += OnVerbose;
                shell.Streams.Debug.DataAdded += OnDebug;
                shell.Streams.Progress.DataAdded += OnProgress;
            }
            shell.Streams.Error.DataAdded += OnError;
        }

        public void Dispose() {
            if (_merged != null) { _merged.DataAdded -= OnMerged; }
            if (_release) {
                _shell.Streams.Information.DataAdding -= OnInformationAdding;
                _shell.Streams.Warning.DataAdding -= OnWarningAdding;
                _shell.Streams.Verbose.DataAdding -= OnVerboseAdding;
                _shell.Streams.Debug.DataAdding -= OnDebugAdding;
                _shell.Streams.Progress.DataAdding -= OnProgressAdding;
                _shell.Streams.Information.DataAdded -= Release;
                _shell.Streams.Warning.DataAdded -= Release;
                _shell.Streams.Verbose.DataAdded -= Release;
                _shell.Streams.Debug.DataAdded -= Release;
                _shell.Streams.Progress.DataAdded -= Release;
            } else {
                _shell.Streams.Information.DataAdded -= OnInformation;
                _shell.Streams.Warning.DataAdded -= OnWarning;
                _shell.Streams.Verbose.DataAdded -= OnVerbose;
                _shell.Streams.Debug.DataAdded -= OnDebug;
                _shell.Streams.Progress.DataAdded -= OnProgress;
            }
            _shell.Streams.Error.DataAdded -= OnError;
            if (_progressIds != null) {
                // PowerShell clears a pipeline's progress when it ends, even if the script did not send -Completed.
                foreach (int id in _progressIds.Values) {
                    _session.Post(ProgressEvents.From(id, -1, null, null, -1, -1, true));
                }
            }
        }

        // Write-Progress becomes an activity in the session's activity list (added by Invoke-ConsoleDashboard when the
        // dashboard has none), nested by ParentActivityId.
        private void OnProgress(object? sender, DataAddedEventArgs e) => Progress(_shell.Streams.Progress[e.Index]);

        private void OnProgressAdding(object? sender, DataAddingEventArgs e) {
            if (e.ItemAdded is ProgressRecord record) { Progress(record); }
        }

        private void Progress(ProgressRecord record) {
            _session.Post(ProgressEvents.From(ProgressId(record.ActivityId), record.ParentActivityId < 0 ? -1 : ProgressId(record.ParentActivityId), record.Activity, record.StatusDescription, record.PercentComplete, record.SecondsRemaining, record.RecordType == ProgressRecordType.Completed));
        }

        // PowerShell's local ids are nonnegative. Negative ids below -1 isolate invocations without hashing collisions;
        // both ends of a parent relationship use the same map, and -1 remains the activity list's root sentinel.
        private int ProgressId(int local) => _progressIds == null ? local : _progressIds.GetOrAdd(local, _ => Interlocked.Decrement(ref _nextProgressId));

        private void OnMerged(object? sender, DataAddedEventArgs e) {
            switch (_merged![e.Index]?.BaseObject) {
                case InformationRecord information: CaptureInformation(information); break;
                case WarningRecord warning: Write(warning.Message, "warn"); break;
                case VerboseRecord verbose: Write(verbose.Message, "verbose"); break;
                case DebugRecord debug: Write(debug.Message, "debug"); break;
                case ErrorRecord error: Write(error.ToString(), PSStreams.IsNativeErrorLine(error) ? "out" : "err"); break;
            }
        }

        private void OnInformation(object? sender, DataAddedEventArgs e) {
            CaptureInformation(_shell.Streams.Information[e.Index]);
        }

        private void CaptureInformation(InformationRecord record) {
            bool host = record.Tags.Contains("PSHOST");
            if (record.MessageData is HostInformationMessage message && (message.ForegroundColor.HasValue || message.BackgroundColor.HasValue)) {
                // Write-Host colours are kept: the line travels as markup to the log region or above an inline area.
                string markup = HostColors.Markup(message.Message, message.ForegroundColor, message.BackgroundColor);
                _session.Post(new LiveEvent("$log", "write", text: _prefix == null ? markup : Text.Markup.Escape(_prefix) + markup, status: "host", isMarkup: true));
                return;
            }
            string text = record.MessageData is HostInformationMessage plain ? plain.Message : PSWidgets.ToText(record.MessageData);
            Write(text, host ? "host" : "info");
        }

        private void OnInformationAdding(object? sender, DataAddingEventArgs e) {
            if (e.ItemAdded is InformationRecord record) { CaptureInformation(record); }
        }

        private void OnWarningAdding(object? sender, DataAddingEventArgs e) {
            if (e.ItemAdded is WarningRecord record) { Write(record.Message, "warn"); }
        }

        private void OnVerboseAdding(object? sender, DataAddingEventArgs e) {
            if (e.ItemAdded is VerboseRecord record) { Write(record.Message, "verbose"); }
        }

        private void OnDebugAdding(object? sender, DataAddingEventArgs e) {
            if (e.ItemAdded is DebugRecord record) { Write(record.Message, "debug"); }
        }

        private static void Release(object? sender, DataAddedEventArgs e) {
            if (sender is System.Collections.IList delivered) { delivered.Clear(); }
        }

        private void OnWarning(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Warning[e.Index].Message, "warn");

        private void OnVerbose(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Verbose[e.Index].Message, "verbose");

        private void OnDebug(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Debug[e.Index].Message, "debug");

        private void OnError(object? sender, DataAddedEventArgs e) => Write(_shell.Streams.Error[e.Index].ToString(), "err");

        private void Write(string text, string level) {
            _session.Post(new LiveEvent("$log", "write", text: _prefix == null ? text : _prefix + text, status: level));
        }
    }
}
