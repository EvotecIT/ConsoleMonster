using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ConsoleMonster.Capabilities;
using SiteFrames.Frames;

namespace SiteFrames.Replay {
    /// <summary>
    /// Writes frames the site captured (pseudo-console runs of the shown programs and scripted-key storyboards) in the
    /// engine player's documented data format, so every replay on the site plays in the engine's player.
    /// The rows are the site's cell HTML, styled by the site's frames.css, so the data carries no CSS of its own.
    /// </summary>
    internal static class PlayerData {
        // Player data is fetched as JSON and never placed inside a page, so markup characters stay as they are: escaping each
        // <, > and quote of the rows' HTML as \u00XX made a large replay almost half escapes.
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public static string Write(Site site, IReadOnlyList<TimedFrame> frames, string title, double fps, long hold, out long duration, out int count, IReadOnlyList<(long Time, string Label)>? markers = null) {
            if (frames.Count == 0) {
                throw new ArgumentException("Player data needs at least one frame.");
            }
            // Inline captures grow as they draw; every frame is padded to the tallest.
            int height = 0;
            int width = 0;
            foreach (TimedFrame frame in frames) {
                height = Math.Max(height, frame.Frame.Height);
                width = Math.Max(width, frame.Frame.Width);
            }
            Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> rows = new List<string>();
            StringBuilder data = new StringBuilder("[");
            int[]? previous = null;
            count = 0;
            foreach (TimedFrame frame in frames) {
                string[] html = FrameHtml.Rows(frame.Frame, ColorDepth.TrueColor, Screen.Dark, site.Styles);
                site.Note(frame.Frame);
                int[] ids = new int[height];
                for (int y = 0; y < height; y++) {
                    string row = y < html.Length ? html[y] : string.Empty;
                    if (!index.TryGetValue(row, out int id)) {
                        id = rows.Count;
                        index.Add(row, id);
                        rows.Add(row);
                    }
                    ids[y] = id;
                }
                // Identical consecutive frames are merged: the data holds what a viewer can see change.
                if (previous != null && Same(previous, ids)) {
                    continue;
                }
                if (count > 0) {
                    data.Append(',');
                }
                data.Append('[').Append(frame.Time.ToString(CultureInfo.InvariantCulture)).Append(",[").Append(string.Join(",", ids)).Append("]]");
                previous = ids;
                count++;
            }
            data.Append(']');
            duration = frames[frames.Count - 1].Time + hold;
            StringBuilder json = new StringBuilder("{\"format\":\"consolemonster-player\",\"version\":1,\"title\":");
            json.Append(JsonSerializer.Serialize(title, Json))
                .Append(",\"width\":").Append(width.ToString(CultureInfo.InvariantCulture))
                .Append(",\"height\":").Append(height.ToString(CultureInfo.InvariantCulture))
                .Append(",\"fps\":").Append(fps.ToString("0.###", CultureInfo.InvariantCulture))
                .Append(",\"duration\":").Append(duration.ToString(CultureInfo.InvariantCulture))
                .Append(",\"chrome\":false,\"theme\":\"dark\",\"css\":\"\",\"rows\":[");
            for (int i = 0; i < rows.Count; i++) {
                if (i > 0) {
                    json.Append(',');
                }
                json.Append(JsonSerializer.Serialize(rows[i], Json));
            }
            json.Append("],\"frames\":").Append(data);
            if (markers != null && markers.Count > 0) {
                json.Append(",\"markers\":[");
                for (int i = 0; i < markers.Count; i++) {
                    if (i > 0) {
                        json.Append(',');
                    }
                    json.Append('[').Append(markers[i].Time.ToString(CultureInfo.InvariantCulture)).Append(',').Append(JsonSerializer.Serialize(markers[i].Label, Json)).Append(']');
                }
                json.Append(']');
            }
            json.Append('}');
            return json.ToString();
        }

        private static bool Same(int[] a, int[] b) {
            for (int i = 0; i < a.Length; i++) {
                if (a[i] != b[i]) {
                    return false;
                }
            }
            return true;
        }
    }
}
