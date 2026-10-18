using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Exports JSON-lines recordings (written by a live session with a recorder) to an animated SVG (CSS keyframes, no
    /// script), a self-contained HTML player (play/pause, speed, scrubbing) or asciicast v3 for standard players. A
    /// recording holds updates, not pictures, so it is played into the widget tree it was recorded from (the same
    /// dashboard definition) at a fixed size and frame rate.
    /// </summary>
    public static class Recording {
        /// <summary>Exports a recording as an HTML player.</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Size, frame rate, theme and window frame.</param>
        /// <returns>The HTML.</returns>
        public static string ExportHtml(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) => Export(root, jsonLines, ExportFormat.Html, options);

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
        public static string ExportPlayerData(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) {
            if (jsonLines == null) {
                throw new ArgumentNullException(nameof(jsonLines));
            }
            return ExportPlayerData(root, new List<LiveEvent>(LiveRecordingReader.Read(jsonLines)), options);
        }

        /// <summary>Exports recorded events as player data (JSON); see <see cref="ExportPlayerData(IWidget, TextReader, RecordingExportOptions)"/>.</summary>
        /// <param name="root">The widget tree the events address.</param>
        /// <param name="events">The events, in order, with their times.</param>
        /// <param name="options">Size, frame rate, theme and window frame.</param>
        /// <returns>The JSON.</returns>
        public static string ExportPlayerData(IWidget root, IEnumerable<LiveEvent> events, RecordingExportOptions? options = null) {
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            RecordingExportOptions o = options ?? new RecordingExportOptions();
            return HtmlPlayerWriter.WriteData(RecordingFrames.Capture(root, new List<LiveEvent>(events), o), o);
        }

        /// <summary>Exports a recording as an animated SVG.</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Size, frame rate, theme and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ExportSvg(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) => Export(root, jsonLines, ExportFormat.Svg, options);

        /// <summary>Exports a recording as asciicast v3.</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Size, frame rate and theme.</param>
        /// <returns>The asciicast lines.</returns>
        public static string ExportAsciicast(IWidget root, TextReader jsonLines, RecordingExportOptions? options = null) => Export(root, jsonLines, ExportFormat.Asciicast, options);

        /// <summary>Exports a recording in a format.</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="format">Html, Svg or Asciicast.</param>
        /// <param name="options">The options.</param>
        /// <returns>The exported document.</returns>
        public static string Export(IWidget root, TextReader jsonLines, ExportFormat format, RecordingExportOptions? options = null) {
            if (jsonLines == null) {
                throw new ArgumentNullException(nameof(jsonLines));
            }
            return Export(root, new List<LiveEvent>(LiveRecordingReader.Read(jsonLines)), format, options);
        }

        /// <summary>Exports recorded events in a format.</summary>
        /// <param name="root">The widget tree the events address.</param>
        /// <param name="events">The events, in order, with their times.</param>
        /// <param name="format">Html, Svg or Asciicast.</param>
        /// <param name="options">The options.</param>
        /// <returns>The exported document.</returns>
        public static string Export(IWidget root, IEnumerable<LiveEvent> events, ExportFormat format, RecordingExportOptions? options = null) {
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            RecordingExportOptions o = options ?? new RecordingExportOptions();
            List<RecordedFrame> frames = RecordingFrames.Capture(root, new List<LiveEvent>(events), o);
            switch (format) {
                case ExportFormat.Html:
                    return HtmlPlayerWriter.Write(frames, o);
                case ExportFormat.Svg:
                    return AnimatedSvgWriter.Write(frames, o);
                case ExportFormat.Asciicast:
                    return AsciicastWriter.Write(frames, o);
                default:
                    throw new ArgumentException("Recordings export to Html, Svg or Asciicast.", nameof(format));
            }
        }

        /// <summary>Exports a recording file to another file (UTF-8 without a byte order mark).</summary>
        /// <param name="root">The widget tree the recording was made with.</param>
        /// <param name="recordingPath">The .jsonl recording.</param>
        /// <param name="outputPath">The output file.</param>
        /// <param name="format">The format; null picks it from the output extension (.html, .svg, .cast; .json writes player data).</param>
        /// <param name="options">The options.</param>
        public static void Save(IWidget root, string recordingPath, string outputPath, ExportFormat? format = null, RecordingExportOptions? options = null) {
            if (outputPath == null) {
                throw new ArgumentNullException(nameof(outputPath));
            }
            bool data = format == null && string.Equals(Path.GetExtension(outputPath), ".json", StringComparison.OrdinalIgnoreCase);
            ExportFormat chosen = format ?? ConsoleExporter.FormatFor(outputPath);
            using (StreamReader reader = new StreamReader(recordingPath ?? throw new ArgumentNullException(nameof(recordingPath)), Encoding.UTF8)) {
                File.WriteAllText(outputPath, data ? ExportPlayerData(root, reader, options) : Export(root, reader, chosen, options), new UTF8Encoding(false));
            }
        }
    }
}
