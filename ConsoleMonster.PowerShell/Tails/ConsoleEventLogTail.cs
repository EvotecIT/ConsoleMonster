using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using ConsoleMonster.Live.Tails;

namespace ConsoleMonster.PowerShell {
    /// <summary>Polls the platform's Get-WinEvent command on the owning runspace and keeps an event-record watermark.</summary>
    internal sealed class ConsoleEventLogTail {
        private readonly string _logName;
        private readonly int _history;
        private readonly CommandInfo _command;
        private bool _initialized;
        private long _lastRecord;
        private DateTimeOffset? _lastTime;

        internal ConsoleEventLogTail(string logName, int history, CommandInfo command) { _logName = logName; _history = history; _command = command; }

        internal IReadOnlyList<TailedLogEntry> ReadAvailable() {
            List<PSObject> latest = Query(1);
            if (latest.Count == 0) {
                _initialized = true;
                _lastRecord = 0;
                _lastTime = null;
                return Array.Empty<TailedLogEntry>();
            }
            long newest = Number(latest[0], "RecordId");
            DateTimeOffset? newestTime = Time(latest[0]);
            if (_initialized && (newest < _lastRecord || newest == _lastRecord && newestTime != _lastTime)) {
                // A cleared log can restart record IDs. Observe its replacement as a new source.
                _lastRecord = 0;
                _lastTime = null;
            }
            if (!_initialized) {
                _initialized = true;
                if (_history == 0) { _lastRecord = newest; _lastTime = newestTime; return Array.Empty<TailedLogEntry>(); }
                List<PSObject> history = Query(_history);
                history.Reverse();
                return Convert(history);
            }
            if (newest == _lastRecord) { return Array.Empty<TailedLogEntry>(); }
            return Convert(Query(256, _lastRecord));
        }

        private List<PSObject> Query(int count, long? after = null) {
            using (System.Management.Automation.PowerShell reader = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                reader.AddCommand(_command).AddParameter("LogName", _logName)
                    .AddParameter("MaxEvents", count).AddParameter("ErrorAction", ActionPreference.Stop);
                if (after.HasValue) {
                    reader.AddParameter("Oldest").AddParameter("FilterXPath", "*[System[EventRecordID > " + after.Value.ToString(CultureInfo.InvariantCulture) + "]]");
                }
                try {
                    Collection<PSObject> values = reader.Invoke();
                    return new List<PSObject>(values);
                } catch (RuntimeException error) when (error.ErrorRecord?.FullyQualifiedErrorId?.StartsWith("NoMatchingEventsFound", StringComparison.Ordinal) == true) {
                    return new List<PSObject>();
                }
            }
        }

        private IReadOnlyList<TailedLogEntry> Convert(List<PSObject> values) {
            List<TailedLogEntry> entries = new List<TailedLogEntry>();
            foreach (PSObject value in values) {
                long record = Number(value, "RecordId");
                DateTimeOffset? timestamp = Time(value);
                string level = Level(Number(value, "Level"));
                string provider = Text(value, "ProviderName");
                string message = Text(value, "Message");
                if (string.IsNullOrEmpty(message)) { message = PSLogTailMessages.Get("MissingEventMessage"); }
                string body = "[" + provider + "#" + Number(value, "Id").ToString(CultureInfo.InvariantCulture) + "] " + message;
                string prefix = (timestamp.HasValue ? timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture) + " " : string.Empty)
                    + "[" + level.ToUpperInvariant() + "] ";
                entries.Add(new TailedLogEntry(prefix + body, level, timestamp, body));
                _lastRecord = record;
                _lastTime = timestamp;
            }
            return entries;
        }

        private static long Number(PSObject value, string name) => System.Convert.ToInt64(value.Properties[name]?.Value, CultureInfo.InvariantCulture);

        private static string Text(PSObject value, string name) {
            try { return System.Convert.ToString(value.Properties[name]?.Value, CultureInfo.InvariantCulture) ?? string.Empty; }
            catch (GetValueException) when (name == "Message") { return string.Empty; }
        }

        private static DateTimeOffset? Time(PSObject value) {
            object? time = value.Properties["TimeCreated"]?.Value;
            if (time is DateTimeOffset offset) { return offset; }
            if (time is DateTime date) { return new DateTimeOffset(date); }
            return null;
        }

        private static string Level(long level) => level == 1 || level == 2 ? "err" : level == 3 ? "warn" : level == 5 ? "verbose" : "info";
    }
}
