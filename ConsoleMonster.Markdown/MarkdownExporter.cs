using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Sessions;
using OfficeIMO.Markdown;

namespace ConsoleMonster.Markdown {
    /// <summary>Exports widget data and terminal snapshots as Markdown through the optional OfficeIMO bridge.</summary>
    public static class MarkdownExporter {
        /// <summary>Exports tables, records and trees semantically, or preserves the rendered frame when fenced is true.</summary>
        /// <param name="widget">The widget or layout.</param>
        /// <param name="options">Size, culture, glyphs and theme.</param>
        /// <param name="fenced">Preserves the exact terminal layout in a code block instead of exporting semantic data.</param>
        /// <returns>The Markdown document.</returns>
        public static string Export(IWidget widget, ExportOptions? options = null, bool fenced = false) {
            if (widget == null) { throw new ArgumentNullException(nameof(widget)); }
            return fenced ? MarkdownFrameWriter.Write(ConsoleExporter.ToText(widget, options))
                : MarkdownWidgetWriter.Write(widget, ConsoleExporter.Ready(options));
        }

        /// <summary>Preserves an already rendered frame in a fenced text block.</summary>
        /// <param name="frame">The frame; its dimensions are kept.</param>
        /// <returns>The Markdown code block.</returns>
        public static string Export(CellBuffer frame) => MarkdownFrameWriter.Write(ConsoleExporter.ToText(frame));

        /// <summary>Exports the current live-session snapshot as a fenced text block.</summary>
        /// <param name="session">A session that has drawn a frame.</param>
        /// <returns>The Markdown code block.</returns>
        public static string Export(LiveSession session) {
            if (session == null) { throw new ArgumentNullException(nameof(session)); }
            return Export(session.Snapshot() ?? throw new InvalidOperationException("The session has not drawn a frame yet."));
        }

        /// <summary>Exports terminal scrollback and the final main screen as a fenced text block.</summary>
        /// <param name="recording">The recorded terminal session.</param>
        /// <returns>The complete transcript as Markdown.</returns>
        public static string ExportRecording(SessionRecording recording) {
            if (recording == null) { throw new ArgumentNullException(nameof(recording)); }
            return Export(recording.Transcript());
        }

        /// <summary>Replays dashboard events into timestamped fenced frames in an OfficeIMO document.</summary>
        /// <param name="root">The dashboard definition addressed by the recorded events.</param>
        /// <param name="events">Recorded updates, in order.</param>
        /// <param name="options">Replay dimensions, sampling and timing.</param>
        /// <returns>A document containing each distinct replayed frame.</returns>
        public static string ExportRecording(IWidget root, IEnumerable<LiveEvent> events, RecordingExportOptions? options = null) {
            if (root == null) { throw new ArgumentNullException(nameof(root)); }
            MarkdownDoc document = MarkdownDoc.Create();
            foreach (RecordedFrame frame in Recording.CaptureFrames(root, events, options)) {
                document.H3(TimeSpan.FromMilliseconds(frame.Start).ToString("c", System.Globalization.CultureInfo.InvariantCulture));
                document.Code("text", ConsoleExporter.ToText(frame.Frame).TrimEnd('\n'));
            }
            return document.ToMarkdown().Replace("\r\n", "\n").TrimEnd('\n') + "\n";
        }
    }
}
