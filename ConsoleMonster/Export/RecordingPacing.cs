using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Export {
    /// <summary>Reading pace applied after output settles. Source adds no reading time.</summary>
    public enum RecordingPace {
        /// <summary>Keep source delays, subject to the explicit speed and idle-limit options.</summary>
        Source,
        /// <summary>Leave more time to read new output.</summary>
        Slow,
        /// <summary>Comfortable reading time (the default).</summary>
        Normal,
        /// <summary>Short reading holds for familiar material.</summary>
        Fast
    }

    public sealed partial class RecordingExportOptions {
        private RecordingPace _pace = RecordingPace.Normal;
        private bool _paceSpecified;
        private Dictionary<string, double> _chapterSpeeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Reading pace. An explicitly assigned value overrides recorded pace directives.</summary>
        public RecordingPace Pace { get => _pace; set { _pace = value; _paceSpecified = true; } }
        /// <summary>Maximum source delay on an unchanged screen after speed scaling; zero disables capping (default 2 seconds). Reading and explicit holds are never capped.</summary>
        public TimeSpan IdleLimit { get; set; } = TimeSpan.FromSeconds(2);
        /// <summary>Extra time to freeze the screen at each chapter marker (default zero).</summary>
        public TimeSpan ChapterHold { get; set; }
        /// <summary>Inclusive source time at which to begin; null begins at zero.</summary>
        public TimeSpan? TrimStart { get; set; }
        /// <summary>Exclusive source time at which to end; null includes the final screen.</summary>
        public TimeSpan? TrimEnd { get; set; }
        /// <summary>Begin at this chapter's marker. Cannot be combined with TrimStart.</summary>
        public string? TrimStartChapter { get; set; }
        /// <summary>End before this chapter's marker. Cannot be combined with TrimEnd.</summary>
        public string? TrimEndChapter { get; set; }
        /// <summary>Speed multipliers by chapter label, multiplied by Speed. Explicit holds keep their requested duration.</summary>
        public IDictionary<string, double> ChapterSpeeds => _chapterSpeeds;
        internal IReadOnlyList<RecordingMarker>? PlaybackMarkers { get; set; }
        internal bool PaceSpecified => _paceSpecified;
        /// <summary>Copies export settings and source markers without sharing mutable collections.</summary>
        public RecordingExportOptions Clone() {
            RecordingExportOptions result = (RecordingExportOptions)MemberwiseClone();
            result._markers = new List<RecordingMarker>(_markers);
            result._chapterSpeeds = new Dictionary<string, double>(_chapterSpeeds, StringComparer.OrdinalIgnoreCase);
            result.PlaybackMarkers = null;
            return result;
        }
        private void ValidateTimeline() {
            if (!Enum.IsDefined(typeof(RecordingPace), Pace)) { throw new ArgumentOutOfRangeException(nameof(Pace)); }
            if (IdleLimit < TimeSpan.Zero || ChapterHold < TimeSpan.Zero || TrimStart < TimeSpan.Zero || TrimEnd < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(IdleLimit), "Timeline durations cannot be negative."); }
            if ((TrimStart.HasValue && TrimStartChapter != null) || (TrimEnd.HasValue && TrimEndChapter != null)) { throw new ArgumentException("Choose a time or a chapter for each trim boundary."); }
            foreach (KeyValuePair<string, double> speed in ChapterSpeeds) {
                if (string.IsNullOrWhiteSpace(speed.Key) || double.IsNaN(speed.Value) || double.IsInfinity(speed.Value) || speed.Value <= 0) { throw new ArgumentException("Chapter speeds need a label and a finite positive multiplier."); }
            }
        }
    }

    /// <summary>A source-timeline chapter or export instruction. Holds and pace changes survive cast round-trips.</summary>
    public sealed class RecordingMarker {
        /// <summary>Creates a marker. A blank label is useful for a hold or pace instruction that is not a chapter.</summary>
        public RecordingMarker(TimeSpan time, string label, TimeSpan hold = default, RecordingPace? pace = null, bool isChapter = true) {
            if (time < TimeSpan.Zero || hold < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(time)); }
            if (pace.HasValue && !Enum.IsDefined(typeof(RecordingPace), pace.Value)) { throw new ArgumentOutOfRangeException(nameof(pace)); }
            Time = time; Label = label ?? throw new ArgumentNullException(nameof(label)); Hold = hold; Pace = pace; IsChapter = isChapter;
        }
        /// <summary>Position in the original recording, before trimming, speed or holds.</summary>
        public TimeSpan Time { get; }
        /// <summary>Chapter label; empty for an unnamed instruction.</summary>
        public string Label { get; }
        /// <summary>Additional export hold at this exact marker, unaffected by speed and idle compression.</summary>
        public TimeSpan Hold { get; }
        /// <summary>Reading pace from this marker until the next pace instruction.</summary>
        public RecordingPace? Pace { get; }
        /// <summary>Whether this marker starts a named chapter.</summary>
        public bool IsChapter { get; }
        // Session casts preserve the screen at marker arrival, even if subsequent output shares its timestamp.
        internal CellBuffer? Snapshot { get; set; }
        // Arrival order disambiguates metadata between logical redraws sharing one cast timestamp.
        internal long? SourceSequence { get; set; }
    }
}
