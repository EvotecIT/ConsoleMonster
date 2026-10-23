using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Sessions {
    /// <summary>Reads asciicast v2 sessions and replays their VT output through ConsoleMonster's cell renderer.</summary>
    public sealed class SessionRecording {
        private readonly List<CastEvent> _events = new List<CastEvent>();
        private string _header = string.Empty;
        /// <summary>Initial terminal columns.</summary>
        public int Width { get; private set; }
        /// <summary>Initial terminal rows.</summary>
        public int Height { get; private set; }
        /// <summary>Recording title.</summary>
        public string Title { get; private set; } = "Console session";
        /// <summary>Capture mode stated in the file, or external for third-party casts.</summary>
        public string Mode { get; private set; } = "external asciicast";

        /// <summary>Reads a local cast. No companion file or remote resource is needed for playback.</summary>
        public static SessionRecording Read(string path) {
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8)) { return Read(reader); }
        }
        /// <summary>Reads a v2 cast, rejecting invalid headers, dimensions and non-monotonic event times.</summary>
        public static SessionRecording Read(TextReader reader) {
            if (reader == null) { throw new ArgumentNullException(nameof(reader)); }
            SessionRecording recording = new SessionRecording();
            recording._header = reader.ReadLine() ?? throw new FormatException("Missing asciicast header.");
            Dictionary<string, object?> header = MiniJson.Read(recording._header) as Dictionary<string, object?> ?? throw new FormatException("Invalid asciicast header.");
            if (Number(header, "version") != 2) { throw new FormatException("Session recordings require asciicast v2."); }
            recording.Width = Number(header, "width"); recording.Height = Number(header, "height");
            Dimensions(recording.Width, recording.Height);
            if (header.TryGetValue("title", out object? title)) { recording.Title = title as string ?? recording.Title; }
            if (header.TryGetValue("consolemonster", out object? metadata) && metadata is Dictionary<string, object?> map && map.TryGetValue("mode", out object? mode)) { recording.Mode = mode as string ?? recording.Mode; }
            string? line;
            double previous = 0;
            long size = recording._header.Length;
            while ((line = reader.ReadLine()) != null) {
                size += line.Length;
                if (line.Length > 1024 * 1024 || size > 128 * 1024 * 1024 || recording._events.Count > 1000000) { throw new FormatException("The recording exceeds the playback input limit."); }
                if (string.IsNullOrWhiteSpace(line)) { continue; }
                List<object?> item = MiniJson.Read(line) as List<object?> ?? throw new FormatException("Invalid asciicast event.");
                if (item.Count != 3 || !(item[1] is string kind) || !(item[2] is string data) || !double.TryParse(item[0] as string, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
                    || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < previous || seconds > 86400 * 365) { throw new FormatException("Invalid asciicast event time or payload."); }
                previous = seconds;
                if (kind == "r") { ParseSize(data, out int w, out int h); Dimensions(w, h); }
                if (kind == "o" || kind == "r" || kind == "m") { recording._events.Add(new CastEvent(seconds, kind, data)); }
            }
            return recording;
        }
        private static int Number(Dictionary<string, object?> map, string key) {
            if (!map.TryGetValue(key, out object? value) || !int.TryParse(value as string, NumberStyles.None, CultureInfo.InvariantCulture, out int number)) { throw new FormatException("Missing or invalid " + key + "."); }
            return number;
        }
        private static void Dimensions(int width, int height) {
            if (width < 2 || height < 2 || width > 1000 || height > 1000 || (long)width * height > 250000) { throw new FormatException("Invalid terminal dimensions."); }
        }
        private static void ParseSize(string data, out int width, out int height) {
            string[] parts = data.Split('x');
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out width) || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out height)) { throw new FormatException("Invalid resize event."); }
        }
        /// <summary>Reconstructs the last visible frame, including cursor movement, colour and resizes.</summary>
        public CellBuffer FinalFrame() => Replay().Snapshot();
        /// <summary>Reconstructs print-once terminal scrollback and the final main screen.</summary>
        public CellBuffer Transcript() => Replay().Transcript();
        private VtScreen Replay() {
            VtScreen screen = new VtScreen(Width, Height);
            foreach (CastEvent item in _events) { Apply(screen, item); }
            return screen;
        }
        private static void Apply(VtScreen screen, CastEvent item) {
            if (item.Kind == "o") { screen.Feed(item.Data); }
            else if (item.Kind == "r") { ParseSize(item.Data, out int width, out int height); screen.Resize(width, height); }
        }
        /// <summary>Exports an HTML player, animated SVG, v2 asciicast, or scrollback as text or fenced Markdown.</summary>
        public string Export(ExportFormat format, RecordingExportOptions? options = null) {
            if (format == ExportFormat.Asciicast) { return ToAsciicast(); }
            if (format == ExportFormat.Text) { return ConsoleExporter.ToText(Transcript()); }
            if (format == ExportFormat.Markdown || format == ExportFormat.FencedText) { return ConsoleExporter.ToMarkdown(Transcript()); }
            RecordingExportOptions effective = Ready(options);
            List<RecordedFrame> frames = Frames(effective);
            if (format == ExportFormat.Html) { return HtmlPlayerWriter.Write(frames, effective); }
            if (format == ExportFormat.Svg) { return AnimatedSvgWriter.Write(frames, effective); }
            throw new ArgumentOutOfRangeException(nameof(format));
        }
        /// <summary>Exports the existing native player's data format, including chapter markers.</summary>
        public string ExportPlayerData(RecordingExportOptions? options = null) {
            RecordingExportOptions effective = Ready(options);
            return HtmlPlayerWriter.WriteData(Frames(effective), effective);
        }
        /// <summary>Exports a static final-frame SVG with the same theme and cell geometry as the player.</summary>
        public string ExportFinalSvg(ExportOptions? options = null) => ConsoleExporter.ToSvg(FinalFrame(), options);
        /// <summary>Returns an interoperable v2 cast without input events.</summary>
        public string ToAsciicast() {
            StringBuilder output = new StringBuilder(_header).Append('\n');
            foreach (CastEvent item in _events) { output.Append('[').Append(SessionCastWriter.Seconds(item.Time)).Append(',').Append(SessionCastWriter.Json(item.Kind)).Append(',').Append(SessionCastWriter.Json(item.Data)).Append("]\n"); }
            return output.ToString();
        }
        private RecordingExportOptions Ready(RecordingExportOptions? options) {
            RecordingExportOptions source = options ?? new RecordingExportOptions();
            RecordingExportOptions result = new RecordingExportOptions {
                Theme = source.Theme, CustomTheme = source.CustomTheme, Title = source.Title ?? Title,
                WindowChrome = source.WindowChrome, ChromeStyle = source.ChromeStyle, FontFamily = source.FontFamily, FontSize = source.FontSize,
                LineHeight = source.LineHeight, FramesPerSecond = source.FramesPerSecond, MaxFrames = source.MaxFrames,
                Speed = source.Speed, HoldLastFrame = source.HoldLastFrame, Glyphs = source.Glyphs,
                Culture = source.Culture, Padding = source.Padding, Timestamp = source.Timestamp
            };
            if (result.MaxFrames < 2) { throw new ArgumentOutOfRangeException(nameof(options), "Session playback needs at least two frames."); }
            int width = Width, height = Height;
            foreach (CastEvent item in _events) { if (item.Kind == "r") { ParseSize(item.Data, out int w, out int h); width = Math.Max(width, w); height = Math.Max(height, h); } }
            Dimensions(width, height);
            result.Width = width; result.Height = height;
            result.ToFrameOptions();
            foreach (RecordingMarker marker in source.Markers) { result.Markers.Add(marker); }
            foreach (CastEvent item in _events) { if (item.Kind == "m") { result.Markers.Add(new RecordingMarker(TimeSpan.FromSeconds(item.Time / result.Speed), item.Data)); } }
            return result;
        }
        private List<RecordedFrame> Frames(RecordingExportOptions options) {
            VtScreen screen = new VtScreen(Width, Height);
            List<RecordedFrame> frames = new List<RecordedFrame>();
            double lastTime = _events.Count > 0 ? _events[_events.Count - 1].Time : 0;
            double interval = Math.Max(1.0 / options.FramesPerSecond, lastTime / (options.MaxFrames - 1));
            frames.Add(new RecordedFrame(Pad(screen.Snapshot(), options.Width, options.Height), 0));
            double next = interval;
            foreach (CastEvent item in _events) {
                Apply(screen, item);
                if (item.Time >= next && frames.Count < options.MaxFrames - 1) {
                    frames.Add(new RecordedFrame(Pad(screen.Snapshot(), options.Width, options.Height), (long)(item.Time * 1000 / options.Speed)));
                    next = item.Time + interval;
                }
            }
            frames.Add(new RecordedFrame(Pad(screen.Snapshot(), options.Width, options.Height), (long)(lastTime * 1000 / options.Speed)));
            // Coalesce same-time frames so both the SVG and HTML show the final write at that moment.
            for (int i = frames.Count - 2; i >= 0; i--) { if (frames[i].Start == frames[i + 1].Start) { frames.RemoveAt(i); } }
            for (int i = 0; i < frames.Count - 1; i++) { frames[i].Duration = frames[i + 1].Start - frames[i].Start; }
            frames[frames.Count - 1].Duration = Math.Max(1, (long)options.HoldLastFrame.TotalMilliseconds);
            return frames;
        }
        /// <summary>Shares the core replay/sampling with the site's existing row renderer.</summary>
        internal List<RecordedFrame> CaptureFrames(RecordingExportOptions? options = null) => Frames(Ready(options));
        private static CellBuffer Pad(CellBuffer source, int width, int height) {
            CellBuffer frame = new CellBuffer(width, height);
            for (int y = 0; y < source.Height; y++) { for (int x = 0; x < source.Width; x++) { Cell cell = source[x, y]; if (!cell.IsContinuation) { frame.Set(x, y, cell.Glyph ?? " ", cell.Style, Math.Max(1, (int)cell.Width)); } } }
            return frame;
        }
        private sealed class CastEvent {
            public CastEvent(double time, string kind, string data) { Time = time; Kind = kind; Data = data; }
            public double Time { get; } public string Kind { get; } public string Data { get; }
        }
    }
}
