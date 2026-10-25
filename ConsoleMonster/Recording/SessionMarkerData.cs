using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Export;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Sessions {
    /// <summary>ConsoleMonster's optional cast marker metadata. Standard m events remain plain chapter labels.</summary>
    internal static class SessionMarkerData {
        internal static string Write(string label, TimeSpan hold, RecordingPace? pace, bool isChapter) {
            _ = new RecordingMarker(TimeSpan.Zero, label, hold, pace, isChapter);
            return MiniJson.Write(new Dictionary<string, object?> {
                ["label"] = label, ["hold"] = hold.TotalSeconds, ["pace"] = pace?.ToString(), ["chapter"] = isChapter
            });
        }
        internal static RecordingMarker Read(TimeSpan at, string data) {
            try {
                var map = MiniJson.Read(data) as Dictionary<string, object?> ?? throw new FormatException("Invalid marker metadata.");
                if (!(map["label"] is string label) || !(map["chapter"] is bool chapter)
                    || !double.TryParse(map["hold"] as string, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
                    || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > TimeSpan.MaxValue.TotalSeconds) { throw new FormatException("Invalid marker metadata."); }
                RecordingPace? pace = null;
                if (map.TryGetValue("pace", out object? value) && value != null) {
                    if (!(value is string text) || !Enum.TryParse(text, true, out RecordingPace parsed) || !Enum.IsDefined(typeof(RecordingPace), parsed)) { throw new FormatException("Invalid marker pace."); }
                    pace = parsed;
                }
                return new RecordingMarker(at, label, RecordingTimeline.FromMilliseconds(seconds * 1000), pace, chapter);
            } catch (Exception error) when (error is ArgumentException || error is OverflowException || error is KeyNotFoundException) {
                throw new FormatException("Invalid ConsoleMonster marker metadata.", error);
            }
        }
    }
}
