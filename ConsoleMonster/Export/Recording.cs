using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Layout;
using ConsoleMonster.Canvas;
using ConsoleMonster.Live;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Exports JSON-lines recordings (written by a live session with a recorder) to an animated SVG (CSS keyframes, no
    /// script), a self-contained HTML player (play/pause, speed, scrubbing) or asciicast v3 for standard players. A
    /// recording holds updates, not pictures, so it is played into the widget tree it was recorded from (the same
    /// dashboard definition) at a fixed size and frame rate.
    /// </summary>
    public static class Recording {
        /// <summary>Replays dashboard events into distinct timed cell grids for image add-ons and other consumers.</summary>
        public static List<RecordedFrame> CaptureFrames(IWidget root, IEnumerable<LiveEvent> events, RecordingExportOptions? options = null) {
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            return RecordingFrames.Capture(root, new List<LiveEvent>(events), (options ?? new RecordingExportOptions()).Ready());
        }
        /// <summary>Exports a recording as an HTML player.</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Size, frame rate, theme and window frame.</param>
        /// <returns>The HTML.</returns>
        public static string ToHtml(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) => Text(root, jsonLines, OutputFormat.Html, options);

        /// <summary>
        /// Exports a recording as player data (JSON) for the native player (<see cref="ConsoleExporter.PlayerScript"/>), so a
        /// site includes the script once and points any number of players at their data instead of embedding whole pages.
        /// The data holds the distinct rows as HTML, each frame as its start time and row numbers, and the screen CSS
        /// (scoped to its player by the script). It is safe to embed in a <c>script type="application/json"</c> element.
        /// </summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Size, frame rate, theme and window frame.</param>
        /// <returns>The JSON.</returns>
        public static string ToPlayerData(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) {
            if (jsonLines == null) {
                throw new ArgumentNullException(nameof(jsonLines));
            }
            return ToPlayerData(root, new List<LiveEvent>(LiveRecordingReader.Read(jsonLines)), options);
        }

        /// <summary>Exports recorded events as player data (JSON); see <see cref="ToPlayerData(IWidget, TextReader, RecordingExportOptions)"/>.</summary>
        /// <param name="root">The widget tree the events address.</param>
        /// <param name="events">The events, in order, with their times.</param>
        /// <param name="options">Size, frame rate, theme and window frame.</param>
        /// <returns>The JSON.</returns>
        public static string ToPlayerData(IWidget root, IEnumerable<LiveEvent> events, RecordingExportOptions? options = null) {
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            RecordingExportOptions o = (options ?? new RecordingExportOptions()).Ready();
            return HtmlPlayerWriter.WriteData(RecordingFrames.Capture(root, new List<LiveEvent>(events), o), o);
        }

        /// <summary>Exports a recording as an animated SVG.</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Size, frame rate, theme and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ToSvg(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) => Text(root, jsonLines, OutputFormat.Svg, options);

        /// <summary>Exports a recording as asciicast v3.</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Size, frame rate and theme.</param>
        /// <returns>The asciicast lines.</returns>
        public static string ToAsciicast(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) => Text(root, jsonLines, OutputFormat.Asciicast, options);

        /// <summary>Renders a recording file in a format.</summary>
        /// <param name="root">The dashboard the events were recorded from.</param>
        /// <param name="jsonLines">The recording (JSON lines).</param>
        /// <param name="format">Html, Svg, Asciicast or Text.</param>
        /// <param name="options">Pacing, theme and player settings.</param>
        /// <returns>The rendered output.</returns>
        public static RenderedOutput Render(IWidget root, TextReader jsonLines, OutputFormat format, RecordingExportOptions? options = null) =>
            RenderedOutput.FromText(format, Text(root, jsonLines, format, options));

        /// <summary>Renders recorded events in a format.</summary>
        /// <param name="root">The dashboard the events were recorded from.</param>
        /// <param name="events">The events.</param>
        /// <param name="format">Html, Svg, Asciicast or Text.</param>
        /// <param name="options">Pacing, theme and player settings.</param>
        /// <returns>The rendered output.</returns>
        public static RenderedOutput Render(IWidget root, IEnumerable<LiveEvent> events, OutputFormat format, RecordingExportOptions? options = null) =>
            RenderedOutput.FromText(format, Text(root, events, format, options));

        /// <summary>Renders recorded frames in a format.</summary>
        /// <param name="frames">The frames.</param>
        /// <param name="format">Html, Svg, Asciicast or Text.</param>
        /// <param name="options">Pacing, theme and player settings.</param>
        /// <returns>The rendered output.</returns>
        public static RenderedOutput Render(IReadOnlyList<RecordedFrame> frames, OutputFormat format, RecordingExportOptions? options = null) =>
            RenderedOutput.FromText(format, Text(frames, format, options));

        internal static string Text(IWidget root, TextReader jsonLines, OutputFormat format, RecordingExportOptions? options = null) {
            if (jsonLines == null) {
                throw new ArgumentNullException(nameof(jsonLines));
            }
            return Text(root, new List<LiveEvent>(LiveRecordingReader.Read(jsonLines)), format, options);
        }

        internal static string Text(IWidget root, IEnumerable<LiveEvent> events, OutputFormat format, RecordingExportOptions? options = null) {
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            RecordingExportOptions o = (options ?? new RecordingExportOptions()).Ready();
            List<RecordedFrame> frames = RecordingFrames.Capture(root, new List<LiveEvent>(events), o);
            switch (format) {
                case OutputFormat.Html:
                    return HtmlPlayerWriter.Write(frames, o);
                case OutputFormat.Svg:
                    return AnimatedSvgWriter.Write(frames, o);
                case OutputFormat.Asciicast:
                    return AsciicastWriter.Write(frames, o);
                default:
                    throw new ArgumentException("Recordings export to Html, Svg or Asciicast.", nameof(format));
            }
        }

        internal static string Text(IReadOnlyList<RecordedFrame> frames, OutputFormat format, RecordingExportOptions? options = null) {
            if (frames == null) { throw new ArgumentNullException(nameof(frames)); }
            if (frames.Count == 0) { throw new ArgumentException("There are no frames to export.", nameof(frames)); }
            RecordingExportOptions o = (options ?? new RecordingExportOptions()).Clone();
            int width = 1, height = 1;
            foreach (RecordedFrame frame in frames) {
                if (frame == null) { throw new ArgumentException("A frame is null.", nameof(frames)); }
                width = Math.Max(width, frame.Frame.Width);
                height = Math.Max(height, frame.Frame.Height);
            }
            o.Width = width;
            o.Height = height;
            o = o.Ready();
            switch (format) {
                case OutputFormat.Html:
                    return HtmlPlayerWriter.Write(frames, o);
                case OutputFormat.Svg:
                    return AnimatedSvgWriter.Write(frames, o);
                case OutputFormat.Asciicast:
                    // Asciicast has one fixed canvas; clear cells outside smaller captured screens.
                    List<RecordedFrame> normalized = new List<RecordedFrame>(frames.Count);
                    foreach (RecordedFrame frame in frames) {
                        CellBuffer canvas = new CellBuffer(width, height);
                        canvas.Blit(frame.Frame, 0, 0, frame.Frame.Width, frame.Frame.Height, 0, 0);
                        normalized.Add(new RecordedFrame(canvas, frame.Start) { Duration = frame.Duration });
                    }
                    return AsciicastWriter.Write(normalized, o);
                default:
                    throw new ArgumentException("Frames export to Html, Svg or Asciicast.", nameof(format));
            }
        }

        /// <summary>Exports a recording file to another file (UTF-8 without a byte order mark).</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="recordingPath">The .jsonl recording.</param>
        /// <param name="outputPath">The output file.</param>
        /// <param name="format">The format; null picks it from the output extension (.html, .svg, .cast; .json writes player data).</param>
        /// <param name="options">The options.</param>
        public static void Save(IWidget root, string recordingPath, string outputPath, OutputFormat? format = null, RecordingExportOptions? options = null) {
            if (outputPath == null) {
                throw new ArgumentNullException(nameof(outputPath));
            }
            bool data = format == null && string.Equals(Path.GetExtension(outputPath), ".json", StringComparison.OrdinalIgnoreCase);
            OutputFormat chosen = format ?? ConsoleExporter.FormatFor(outputPath);
            using (StreamReader reader = new StreamReader(recordingPath ?? throw new ArgumentNullException(nameof(recordingPath)), Encoding.UTF8)) {
                File.WriteAllText(outputPath, data ? ToPlayerData(root, reader, options) : Text(root, reader, chosen, options), new UTF8Encoding(false));
            }
        }
    }
}
