using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Writes recordings for the native player (<see cref="ConsoleExporter.PlayerScript"/>): the player data (the distinct
    /// rows as HTML, each frame as its start time and row numbers, and the scoped screen CSS), and a self-contained page
    /// that embeds the data, the player script and its styles. No external requests.
    /// </summary>
    internal static class HtmlPlayerWriter {
        public const string DataFormat = "consolemonster-player";

        /// <summary>The player data as JSON, safe to embed in a script element.</summary>
        public static string WriteData(IReadOnlyList<RecordedFrame> frames, RecordingExportOptions recording) {
            ExportOptions options = recording.ToFrameOptions();
            options.Fragment = true;
            ExportPalette palette = recording.Palette;
            HtmlStyles styles = new HtmlStyles();
            Dictionary<string, int> rowIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> rows = new List<string>();
            StringBuilder frameData = new StringBuilder("[");
            long total = 0;
            for (int f = 0; f < frames.Count; f++) {
                CellBuffer frame = frames[f].Frame;
                if (f > 0) {
                    frameData.Append(',');
                }
                frameData.Append('[').Append(frames[f].Start.ToString(CultureInfo.InvariantCulture)).Append(",[");
                for (int y = 0; y < frame.Height; y++) {
                    StringBuilder row = new StringBuilder();
                    HtmlFrameWriter.AppendRow(row, frame, y, palette, styles, "s");
                    string rowHtml = row.ToString();
                    if (!rowIndex.TryGetValue(rowHtml, out int index)) {
                        index = rows.Count;
                        rowIndex.Add(rowHtml, index);
                        rows.Add(rowHtml);
                    }
                    if (y > 0) {
                        frameData.Append(',');
                    }
                    frameData.Append(index.ToString(CultureInfo.InvariantCulture));
                }
                frameData.Append("]]");
                total = Math.Max(total, frames[f].Start + frames[f].Duration);
            }
            frameData.Append(']');

            List<ExportLayer> layers = new List<ExportLayer> { new ExportLayer(frames[0].Frame, palette, null) };
            StringBuilder css = new StringBuilder();
            HtmlFrameWriter.AppendCss(css, layers, options, recording.Width);
            styles.AppendCss(css);

            StringBuilder json = new StringBuilder("{\"format\":\"" + DataFormat + "\",\"version\":1,\"title\":");
            AppendScriptString(json, options.Title ?? string.Empty);
            json.Append(",\"width\":").Append(recording.Width.ToString(CultureInfo.InvariantCulture))
                .Append(",\"height\":").Append(recording.Height.ToString(CultureInfo.InvariantCulture))
                .Append(",\"fps\":").Append(ExportText.Num(recording.FramesPerSecond * recording.Speed))
                .Append(",\"duration\":").Append(total.ToString(CultureInfo.InvariantCulture))
                .Append(",\"chrome\":").Append(options.EffectiveChrome != WindowChromeStyle.None ? "true" : "false")
                .Append(",\"chromeStyle\":\"").Append(options.EffectiveChrome.ToString().ToLowerInvariant()).Append("\",\"chromeBar\":");
            AppendScriptString(json, WindowChrome.HtmlBar(options.Title, options.EffectiveChrome));
            json.Append(",\"theme\":\"").Append(palette.Name).Append("\",\"css\":");
            AppendScriptString(json, css.ToString());
            json.Append(",\"rows\":[");
            for (int i = 0; i < rows.Count; i++) {
                if (i > 0) {
                    json.Append(',');
                }
                AppendScriptString(json, rows[i]);
            }
            json.Append("],\"frames\":").Append(frameData);
            if (recording.Markers.Count > 0) {
                json.Append(",\"markers\":[");
                for (int i = 0; i < recording.Markers.Count; i++) {
                    if (i > 0) { json.Append(','); }
                    json.Append('[').Append(((long)recording.Markers[i].Time.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)).Append(',');
                    AppendScriptString(json, recording.Markers[i].Label);
                    json.Append(']');
                }
                json.Append(']');
            }
            json.Append('}');
            return json.ToString();
        }

        /// <summary>A self-contained page: the data, the player script and its styles inline; it autoplays unless opened at <c>#t=</c>.</summary>
        public static string Write(IReadOnlyList<RecordedFrame> frames, RecordingExportOptions recording) {
            ExportOptions options = recording.ToFrameOptions();
            ExportPalette palette = recording.Palette;
            string data = WriteData(frames, recording);
            StringBuilder html = new StringBuilder(data.Length + 16384);
            html.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<meta name=\"color-scheme\" content=\"").Append(palette.Name).Append("\">\n<meta name=\"generator\" content=\"ConsoleMonster\">\n<title>");
            ExportText.AppendEscaped(html, string.IsNullOrEmpty(options.Title) ? "Console recording" : options.Title);
            html.Append("</title>\n<style>\n");
            // The page itself: a neutral desk under a window frame, otherwise the terminal background.
            string background = palette == ExportPalette.Light ? (options.EffectiveChrome != WindowChromeStyle.None ? "#c9c9cf" : palette.Background) : (options.EffectiveChrome != WindowChromeStyle.None ? "#3a3a3f" : palette.Background);
            html.Append("html,body{margin:0;background:").Append(background).Append("}\nbody{padding:").Append(options.EffectiveChrome != WindowChromeStyle.None ? "20px" : "0").Append("}\n");
            html.Append(ConsoleExporter.PlayerStyles);
            // Size in the terminal's font, not the controls' system font or the host's shrink-to-fit width.
            html.Append(".cm-export.cm-standalone{display:block;font-family:").Append(options.FontFamily)
                .Append(";font-size:").Append(ExportText.Num(options.FontSize)).Append("px;width:calc(")
                .Append(recording.Width).Append("ch + ").Append(options.Padding * 2).Append("px)}")
                .Append(".cm-export.cm-player-host.cm-standalone>.cm-window,.cm-export.cm-player-host.cm-standalone>.cm-player{display:block;width:100%}")
                .Append(".cm-export.cm-player-host.cm-standalone .cm-screen{width:100%;font-size:inherit;line-height:")
                .Append(ExportText.Num(options.FontSize * options.LineHeight)).Append("px}</style>\n</head>\n<body>\n");
            html.Append("<div class=\"cm-recording cm-standalone\" data-cm-recording=\"#cm-data\" data-cm-autoplay></div>\n");
            html.Append("<script type=\"application/json\" id=\"cm-data\">").Append(data).Append("</script>\n");
            html.Append("<script>\n").Append(ConsoleExporter.PlayerScript.Replace("</script", "<\\/script")).Append("\n</script>\n</body>\n</html>\n");
            return html.ToString();
        }

        // A JSON string that is also safe inside a script element: <, > and & are escaped, so no row can close the script.
        private static void AppendScriptString(StringBuilder sb, string value) {
            StringBuilder json = new StringBuilder();
            ExportText.AppendJson(json, value);
            sb.Append(json.ToString().Replace("<", "\\u003c").Replace(">", "\\u003e").Replace("&", "\\u0026").Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029"));
        }
    }
}
