using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Live;

namespace ConsoleMonster.Replay {
    /// <summary>What a point on a replay timeline is.</summary>
    public enum ReplayMarkerKind {
        /// <summary>A named marker added with <see cref="LiveSession.Mark"/>.</summary>
        Marker = 0,

        /// <summary>Something turned to a failure (a lane, phase or status set to fail, an error log line).</summary>
        Failure = 1,

        /// <summary>Something turned to a warning (a warn state or a warning log line).</summary>
        Warning = 2,

        /// <summary>A log line.</summary>
        Log = 3
    }

    /// <summary>A point of interest on a replay timeline: a marker, failure, warning or log entry.</summary>
    public sealed class ReplayMarker {
        /// <summary>Creates a marker.</summary>
        /// <param name="time">Time from the start of the recording.</param>
        /// <param name="kind">What it is.</param>
        /// <param name="text">Plain text describing it.</param>
        /// <param name="isLog">Whether it comes from a log line (failures and warnings can).</param>
        public ReplayMarker(TimeSpan time, ReplayMarkerKind kind, string text, bool isLog = false) {
            Time = time;
            Kind = kind;
            Text = text ?? string.Empty;
            IsLog = isLog || kind == ReplayMarkerKind.Log;
        }

        /// <summary>Time from the start of the recording.</summary>
        public TimeSpan Time { get; }

        /// <summary>What it is.</summary>
        public ReplayMarkerKind Kind { get; }

        /// <summary>Plain text describing it (the marker label, log line, or target and state).</summary>
        public string Text { get; }

        /// <summary>Whether it comes from a log line, so the log search and log jumps include it.</summary>
        public bool IsLog { get; }

        /// <inheritdoc />
        public override string ToString() => ReplayPlayer.FormatTime(Time) + " " + Kind + " " + Text;

        /// <summary>
        /// Finds the markers in recorded events: <see cref="LiveSession.MarkerTarget"/> events, transitions to a fail or warn
        /// state, and log lines (error and warning lines count as failures and warnings).
        /// </summary>
        /// <param name="events">The events in recording order.</param>
        /// <returns>The markers in time order.</returns>
        public static IReadOnlyList<ReplayMarker> From(IEnumerable<LiveEvent> events) {
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            List<ReplayMarker> markers = new List<ReplayMarker>();
            Dictionary<string, ReplayMarkerKind?> states = new Dictionary<string, ReplayMarkerKind?>(StringComparer.Ordinal);
            foreach (LiveEvent e in events) {
                TimeSpan time = TimeSpan.FromMilliseconds(Math.Max(0, e.Time));
                string text = e.Text == null ? string.Empty : e.IsMarkup ? ConsoleMonster.Text.Markup.Strip(e.Text) : e.Text;
                if (e.Target == LiveSession.MarkerTarget) {
                    markers.Add(new ReplayMarker(time, ReplayMarkerKind.Marker, text));
                    continue;
                }
                if (e.State == "snapshot") {
                    continue;
                }
                ReplayMarkerKind? severity = Severity(e.State);
                bool log = e.Operation == "write" && (e.Target == "$log" || e.Key == null);
                if (log) {
                    markers.Add(new ReplayMarker(time, severity ?? ReplayMarkerKind.Log, text, true));
                    continue;
                }
                if (e.State == null) {
                    continue;
                }
                string id = e.Target + "\u0001" + (e.Key ?? string.Empty);
                states.TryGetValue(id, out ReplayMarkerKind? before);
                states[id] = severity;
                if (severity.HasValue && before != severity) {
                    string what = e.Key == null ? e.Target : e.Target + " " + e.Key;
                    markers.Add(new ReplayMarker(time, severity.Value, what + ": " + e.State!.ToLowerInvariant() + (text.Length > 0 ? " · " + text : string.Empty)));
                }
            }
            // Events are applied in queue order, which can differ slightly from their stamps: a stable sort keeps the order.
            return markers.OrderBy(m => m.Time).ToList();
        }

        private static ReplayMarkerKind? Severity(string? state) {
            switch ((state ?? string.Empty).ToLowerInvariant()) {
                case "fail":
                case "failed":
                case "failure":
                case "error":
                case "err":
                    return ReplayMarkerKind.Failure;
                case "warn":
                case "warning":
                    return ReplayMarkerKind.Warning;
                default:
                    return null;
            }
        }
    }
}
