using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Export {
    /// <summary>Transforms source frames once, before any animation writer sees them.</summary>
    internal static class RecordingTimeline {
        private const long SettleMilliseconds = 350;

        /// <summary>Keep exact instruction and trim boundaries in addition to the bounded regular samples.</summary>
        internal static SortedSet<double> SampleTimes(double end, double interval, int maxFrames, RecordingExportOptions options) {
            SortedSet<double> times = new SortedSet<double> { 0, end };
            for (int sample = 0; sample < maxFrames - 1 && sample * interval < end; sample++) { times.Add(sample * interval); }
            foreach (RecordingMarker marker in options.Markers) { Add(marker.Time.TotalMilliseconds); }
            List<RecordingMarker> markers = new List<RecordingMarker>(options.Markers);
            Add(Boundary(options.TrimStart, options.TrimStartChapter, 0, markers));
            Add(Boundary(options.TrimEnd, options.TrimEndChapter, end, markers));
            return times;
            void Add(double at) { if (at >= 0 && at <= end) { times.Add(at); } }
        }

        internal static List<RecordedFrame> Apply(IReadOnlyList<RecordingSourceFrame> source, RecordingExportOptions options) {
            options.ToFrameOptions();
            List<RecordingMarker> markers = options.Markers.OrderBy(marker => marker.Time).ThenBy(marker => marker.SourceSequence ?? long.MaxValue).ToList();
            double end = source[source.Count - 1].Time.TotalMilliseconds + source[source.Count - 1].Duration.TotalMilliseconds;
            double from = Boundary(options.TrimStart, options.TrimStartChapter, 0, markers);
            double until = Boundary(options.TrimEnd, options.TrimEndChapter, end, markers);
            long? fromSequence = options.TrimStartChapter == null ? null : FindChapter(options.TrimStartChapter, markers).SourceSequence;
            long? untilSequence = options.TrimEndChapter == null ? null : FindChapter(options.TrimEndChapter, markers).SourceSequence;
            bool logicalRange = untilSequence.HasValue && (fromSequence ?? long.MinValue) < untilSequence.Value;
            if (from > end || until > end || until < from || (until == from && (options.TrimEnd.HasValue || options.TrimEndChapter != null) && !logicalRange)) {
                throw new ArgumentException("The trim range must contain part of the source recording.");
            }
            foreach (string label in options.ChapterSpeeds.Keys) { FindChapter(label, markers); }
            bool exclusiveEnd = options.TrimEnd.HasValue || options.TrimEndChapter != null;
            List<RecordedFrame> result = new List<RecordedFrame>();
            List<RecordingMarker> chapters = new List<RecordingMarker>();
            options.PlaybackMarkers = chapters;
            double clock = 0, idleBudget = double.PositiveInfinity;
            int markerIndex = 0;
            while (markerIndex < markers.Count && (markers[markerIndex].Time.TotalMilliseconds < from
                || (markers[markerIndex].Time.TotalMilliseconds == from && fromSequence.HasValue && markers[markerIndex].SourceSequence < fromSequence))) { markerIndex++; }
            CellBuffer? previous = null;
            CellBuffer finalScreen = source[0].Frame;
            int newCharacters = 0, newLines = 0;
            long currentSequence = 0;
            for (int i = 0; i < source.Count; i++) {
                RecordingSourceFrame frame = source[i];
                currentSequence = frame.Sequence;
                double frameStart = frame.Time.TotalMilliseconds;
                double frameClock = clock;
                double next = i + 1 < source.Count ? source[i + 1].Time.TotalMilliseconds : end;
                bool beforeStart = next == from && i + 1 < source.Count && (fromSequence.HasValue ? source[i + 1].Sequence <= fromSequence.Value : frameStart < from);
                bool afterEnd = exclusiveEnd && frameStart == until && (!untilSequence.HasValue || frame.Sequence >= untilSequence.Value);
                if (next < from || beforeStart || frameStart > until || afterEnd) { continue; }
                double start = Math.Max(from, frameStart), stop = Math.Min(until, next);
                CountNewText(previous, frame.Frame, out int characters, out int lines);
                newCharacters = Math.Min(1000000, newCharacters + characters);
                newLines = Math.Min(1000000, newLines + lines);
                previous = frame.Frame;
                finalScreen = frame.Frame;
                double position = start, dwell = 0;
                idleBudget = options.IdleLimit > TimeSpan.Zero ? options.IdleLimit.TotalMilliseconds : double.PositiveInfinity;
                while (markerIndex < markers.Count && markers[markerIndex].Time.TotalMilliseconds <= stop) {
                    RecordingMarker marker = markers[markerIndex];
                    double at = marker.Time.TotalMilliseconds;
                    // Recorded arrival order can place a marker before the next logical redraw at this same time.
                    bool beforeNext = i + 1 < source.Count && marker.SourceSequence.HasValue && marker.SourceSequence.Value < source[i + 1].Sequence;
                    bool atExcludedEnd = exclusiveEnd && at == until && (!untilSequence.HasValue || !marker.SourceSequence.HasValue || marker.SourceSequence.Value >= untilSequence.Value);
                    if ((at == stop && stop == next && i + 1 < source.Count && !beforeNext) || atExcludedEnd) { break; }
                    dwell += AppendSource(frame.Frame, position, at);
                    position = at;
                    if (marker.IsChapter) { chapters.Add(new RecordingMarker(TimeSpan.FromMilliseconds(Round(clock)), marker.Label)); }
                    Append(marker.Snapshot ?? frame.Frame, marker.Hold.TotalMilliseconds + (marker.IsChapter ? options.ChapterHold.TotalMilliseconds : 0), true);
                    if (marker.SourceSequence.HasValue) { currentSequence = Math.Max(currentSequence, marker.SourceSequence.Value); }
                    markerIndex++;
                }
                dwell += AppendSource(frame.Frame, position, stop);
                bool settled = next - frameStart >= SettleMilliseconds || i == source.Count - 1 || (exclusiveEnd && stop == until);
                if (settled) {
                    RecordingPace pace = PaceAt(start);
                    double reading = ReadingMilliseconds(pace, newCharacters, newLines) / SpeedAt(start);
                    Append(frame.Frame, Math.Max(0, reading - dwell));
                    newCharacters = newLines = 0;
                }
                if (frame.Synchronized) {
                    // Retained logical redraws need a playable interval even when they share an arrival.
                    double rate = Math.Max(1, Math.Min(60, options.FramesPerSecond * SpeedAt(start)));
                    Append(frame.Frame, Math.Max(0, Math.Ceiling(1000 / rate) - (clock - frameClock)));
                }
                // Keep a final screen even when the recording has no positive source duration.
                if (result.Count == 0) { result.Add(new RecordedFrame(frame.Frame, 0)); }
            }
            if (result.Count == 0) { throw new ArgumentException("The trim range contains no frames."); }
            double finalHold = options.HoldLastFrame.TotalMilliseconds;
            if (clock == 0 || !RecordingFrames.Same(result[result.Count - 1].Frame, finalScreen)) { finalHold = Math.Max(1, finalHold); }
            Append(finalScreen, finalHold, true);
            return result;

            double AppendSource(CellBuffer screen, double start, double stop) {
                double duration = Math.Max(0, stop - start) / SpeedAt(start);
                duration = Math.Min(duration, idleBudget);
                idleBudget -= duration;
                Append(screen, duration);
                return duration;
            }
            void Append(CellBuffer screen, double duration, bool explicitHold = false) {
                if (duration <= 0) { return; }
                long at = Round(clock), finish = Round(clock + duration);
                clock += duration;
                if (finish == at) { return; }
                if (result.Count > 0 && RecordingFrames.Same(result[result.Count - 1].Frame, screen)) { result[result.Count - 1].Duration += finish - at; }
                else { result.Add(new RecordedFrame(screen, at) { Duration = finish - at }); }
                if (explicitHold) { result[result.Count - 1].ExplicitHold += TimeSpan.FromMilliseconds(finish - at); }
            }
            double SpeedAt(double at) {
                string? chapter = null;
                foreach (RecordingMarker marker in markers) {
                    if (marker.Time.TotalMilliseconds > at) { break; }
                    if (marker.Time.TotalMilliseconds == at && marker.SourceSequence > currentSequence) { continue; }
                    if (marker.IsChapter) { chapter = marker.Label; }
                }
                double combined = options.Speed * (chapter != null && options.ChapterSpeeds.TryGetValue(chapter, out double speed) ? speed : 1);
                if (double.IsInfinity(combined) || combined <= 0) { throw new ArgumentOutOfRangeException(nameof(options.Speed), "Combined playback speed is outside the supported range."); }
                return combined;
            }
            RecordingPace PaceAt(double at) {
                RecordingPace pace = options.Pace;
                if (!options.PaceSpecified) {
                    foreach (RecordingMarker marker in markers) {
                        if (marker.Time.TotalMilliseconds > at) { break; }
                        if (marker.Time.TotalMilliseconds == at && marker.SourceSequence > currentSequence) { continue; }
                        if (marker.Pace.HasValue) { pace = marker.Pace.Value; }
                    }
                }
                return pace;
            }
        }

        private static double Boundary(TimeSpan? time, string? chapter, double fallback, List<RecordingMarker> markers) =>
            time?.TotalMilliseconds ?? (chapter == null ? fallback : FindChapter(chapter, markers).Time.TotalMilliseconds);
        private static RecordingMarker FindChapter(string label, List<RecordingMarker> markers) {
            RecordingMarker? found = null;
            foreach (RecordingMarker marker in markers) {
                if (!marker.IsChapter || !string.Equals(marker.Label, label, StringComparison.OrdinalIgnoreCase)) { continue; }
                if (found != null) { throw new ArgumentException("Chapter label is ambiguous: " + label + ". Use source times for trimming and unique labels for chapter speeds."); }
                found = marker;
            }
            return found ?? throw new ArgumentException("The recording has no chapter named " + label + ".");
        }
        /// <summary>Retain source precision on every target framework, including .NET Framework.</summary>
        internal static TimeSpan FromMilliseconds(double milliseconds) => TimeSpan.FromTicks(checked((long)Math.Round(milliseconds * TimeSpan.TicksPerMillisecond)));
        private static long Round(double milliseconds) {
            if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds >= long.MaxValue) { throw new ArgumentOutOfRangeException(nameof(milliseconds), "Export timing exceeds the supported duration."); }
            return checked((long)Math.Round(milliseconds));
        }
        private static double ReadingMilliseconds(RecordingPace pace, int characters, int lines) {
            if (pace == RecordingPace.Source || characters == 0) { return 0; }
            // Terminal playback text rates, line delays and tab holds, with whole-screen caps that retain
            // the preset separation even when a dense screen exceeds the Normal reading budget.
            double cps = pace == RecordingPace.Slow ? 28 : pace == RecordingPace.Fast ? 72 : 42;
            double lineDelay = pace == RecordingPace.Slow ? 0.14 : pace == RecordingPace.Fast ? 0.04 : 0.08;
            double minimum = pace == RecordingPace.Slow ? 2 : pace == RecordingPace.Fast ? 0.55 : 1.2;
            double maximum = pace == RecordingPace.Slow ? 16 : pace == RecordingPace.Fast ? 4 : 8;
            return 1000 * Math.Max(minimum, Math.Min(maximum, characters / cps + lines * lineDelay));
        }
        private static void CountNewText(CellBuffer? previous, CellBuffer current, out int characters, out int lines) {
            characters = lines = 0;
            for (int y = 0; y < current.Height; y++) {
                bool changed = false;
                for (int x = 0; x < current.Width; x++) {
                    Cell cell = current[x, y];
                    if (cell.IsContinuation || string.IsNullOrWhiteSpace(cell.Glyph)) { continue; }
                    if (previous != null && x < previous.Width && y < previous.Height && cell.Glyph == previous[x, y].Glyph) { continue; }
                    characters += new StringInfo(cell.Glyph).LengthInTextElements;
                    changed = true;
                }
                if (changed) { lines++; }
            }
        }
    }
    /// <summary>An unquantized source screen. Playback offsets are rounded only after holds, speed and trims.</summary>
    internal sealed class RecordingSourceFrame {
        internal RecordingSourceFrame(CellBuffer frame, TimeSpan time) { Frame = frame; Time = time; }
        internal CellBuffer Frame { get; }
        internal TimeSpan Time { get; }
        internal TimeSpan Duration { get; set; }
        internal bool Synchronized { get; set; }
        internal long Sequence { get; set; }
    }

}
