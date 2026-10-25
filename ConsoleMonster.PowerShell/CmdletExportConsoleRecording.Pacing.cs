using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Export;

namespace ConsoleMonster.PowerShell {
    public sealed partial class CmdletExportConsoleRecording {
        /// <summary><para type="description">Slow, Normal (default), Fast or Source. Adds reading time after output settles. Explicitly specifying Pace overrides recorded demo pace directives. Source adds no reading time; use IdleLimitSeconds 0 to retain all source delays.</para></summary>
        [Parameter] public RecordingPace Pace { get; set; } = RecordingPace.Normal;
        /// <summary><para type="description">Extra export hold at every chapter marker, unaffected by speed or idle capping.</para></summary>
        [Parameter, ValidateRange(0, 86400)] public double ChapterHoldSeconds { get; set; }
        /// <summary><para type="description">Final screen hold after the source timeline and reading holds (default 2 seconds).</para></summary>
        [Parameter, ValidateRange(0, 86400)] public double HoldLastFrameSeconds { get; set; } = 2;
        /// <summary><para type="description">Inclusive source position at which to begin. Cannot be combined with TrimStartChapter.</para></summary>
        [Parameter, ValidateRange(0, 31536000)] public double TrimStartSeconds { get; set; }
        /// <summary><para type="description">Exclusive source position at which to end. Cannot be combined with TrimEndChapter.</para></summary>
        [Parameter, ValidateRange(0, 31536000)] public double TrimEndSeconds { get; set; }
        /// <summary><para type="description">Begin at this chapter marker, retaining its hold and all later output.</para></summary>
        [Parameter, ValidateNotNullOrEmpty] public string? TrimStartChapter { get; set; }
        /// <summary><para type="description">End before this chapter marker. Its output and hold are excluded.</para></summary>
        [Parameter, ValidateNotNullOrEmpty] public string? TrimEndChapter { get; set; }
        /// <summary><para type="description">Hashtable of chapter labels to positive speed multipliers, for example @{ Install = 4; Results = 1 }. Multiplies Speed for source and reading delays; explicit holds retain their duration. Labels must be unique in the recording.</para></summary>
        [Parameter] public Hashtable? ChapterSpeed { get; set; }

        private RecordingExportOptions PlaybackOptions() {
            RecordingExportOptions options = new RecordingExportOptions {
                Width = Width, Height = Height, FramesPerSecond = FramesPerSecond, Speed = Speed,
                Theme = Theme, Title = Title, IdleLimit = RecordingTimeline.FromMilliseconds(IdleLimitSeconds * 1000),
                ChapterHold = RecordingTimeline.FromMilliseconds(ChapterHoldSeconds * 1000), HoldLastFrame = RecordingTimeline.FromMilliseconds(HoldLastFrameSeconds * 1000),
                TrimStart = MyInvocation.BoundParameters.ContainsKey(nameof(TrimStartSeconds)) ? RecordingTimeline.FromMilliseconds(TrimStartSeconds * 1000) : (TimeSpan?)null,
                TrimEnd = MyInvocation.BoundParameters.ContainsKey(nameof(TrimEndSeconds)) ? RecordingTimeline.FromMilliseconds(TrimEndSeconds * 1000) : (TimeSpan?)null,
                TrimStartChapter = TrimStartChapter, TrimEndChapter = TrimEndChapter,
                WindowChrome = MyInvocation.BoundParameters.ContainsKey(nameof(WindowChrome)) ? WindowChrome.IsPresent : MyInvocation.BoundParameters.ContainsKey(nameof(ChromeStyle)),
                ChromeStyle = ChromeStyle
            };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Pace))) { options.Pace = Pace; }
            if (ChapterSpeed != null) {
                foreach (DictionaryEntry entry in ChapterSpeed) {
                    if (!(entry.Key is string label)) { throw new ArgumentException("ChapterSpeed keys must be chapter labels."); }
                    options.ChapterSpeeds.Add(label, Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture));
                }
            }
            return options;
        }
    }
}
