using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Writes asciicast v3 (newline-delimited JSON): a header with the terminal size and theme, then output events as
    /// <c>[interval, "o", data]</c> with the interval in seconds since the previous event. The first event draws the whole
    /// screen; later events redraw only the rows that changed. No input events are written.
    /// </summary>
    internal static class AsciicastWriter {
        private const string Escape = "\u001b";

        public static string Write(IReadOnlyList<RecordedFrame> frames, RecordingExportOptions options) {
            StringBuilder sb = new StringBuilder();
            ExportPalette palette = options.Palette;
            sb.Append("{\"version\":3,\"term\":{\"cols\":").Append(options.Width.ToString(CultureInfo.InvariantCulture))
                .Append(",\"rows\":").Append(options.Height.ToString(CultureInfo.InvariantCulture))
                .Append(",\"type\":\"xterm-256color\",\"theme\":{\"fg\":");
            ExportText.AppendJson(sb, palette.Foreground);
            sb.Append(",\"bg\":");
            ExportText.AppendJson(sb, palette.Background);
            sb.Append(",\"palette\":");
            ExportText.AppendJson(sb, string.Join(":", ExportPalette.Ansi16Hex()));
            sb.Append("}}");
            if (options.Timestamp.HasValue) {
                sb.Append(",\"timestamp\":").Append(options.Timestamp.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(options.Title)) {
                sb.Append(",\"title\":");
                ExportText.AppendJson(sb, options.Title);
            }
            sb.Append("}\n");

            long previous = 0;
            CellBuffer? shown = null;
            foreach (RecordedFrame frame in frames) {
                StringBuilder data = new StringBuilder();
                if (shown == null) {
                    data.Append(Escape).Append("[?25l").Append(Escape).Append("[0m").Append(Escape).Append("[2J");
                }
                for (int y = 0; y < frame.Frame.Height; y++) {
                    if (shown != null && RecordingFrames.SameRow(shown, frame.Frame, y)) {
                        continue;
                    }
                    AppendRow(data, frame.Frame, y);
                }
                Event(sb, frame.Start - previous, data.ToString());
                previous = frame.Start;
                shown = frame.Frame;
            }
            if (frames.Count > 0) {
                RecordedFrame last = frames[frames.Count - 1];
                Event(sb, last.Duration, Escape + "[0m" + Escape + "[?25h");
            }
            return sb.ToString();
        }

        private static void AppendRow(StringBuilder data, CellBuffer frame, int y) {
            data.Append(Escape).Append('[').Append((y + 1).ToString(CultureInfo.InvariantCulture)).Append(";1H").Append(Escape).Append("[0m");
            int end = CellEncoder.ContentEnd(frame, y, ColorDepth.TrueColor);
            Style? current = null;
            CellEncoder.AppendRun(data, frame, y, 0, end, ColorDepth.TrueColor, ref current, false, true);
            data.Append(Escape).Append("[0m").Append(Escape).Append("[K");
        }

        private static void Event(StringBuilder sb, long milliseconds, string data) {
            double seconds = Math.Max(0, milliseconds) / 1000.0;
            sb.Append('[').Append(seconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(",\"o\",");
            ExportText.AppendJson(sb, data);
            sb.Append("]\n");
        }
    }
}
