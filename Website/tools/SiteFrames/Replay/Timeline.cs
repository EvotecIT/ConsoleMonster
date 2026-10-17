using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using SiteFrames.Frames;

namespace SiteFrames.Replay {
    /// <summary>A distinct screen and the moment it appeared, in milliseconds from the start.</summary>
    internal readonly struct TimedFrame {
        public TimedFrame(long time, CellBuffer frame) {
            Time = time;
            Frame = frame;
        }

        public long Time { get; }

        public CellBuffer Frame { get; }
    }

    /// <summary>
    /// Encodes frames for the site's player: the first frame lists every row as HTML, each later frame only the rows that
    /// changed. Identical consecutive frames are merged, so the file holds what a viewer can see change.
    /// </summary>
    internal static class Timeline {
        public static JsonObject Encode(Site site, IReadOnlyList<TimedFrame> frames, ColorDepth depth, Screen screen, long hold, out string finalBody) {
            if (frames.Count == 0) {
                throw new ArgumentException("A timeline needs at least one frame.");
            }
            // Inline captures grow as they draw; every frame is padded to the tallest.
            int height = 0;
            int width = 0;
            foreach (TimedFrame frame in frames) {
                height = Math.Max(height, frame.Frame.Height);
                width = Math.Max(width, frame.Frame.Width);
            }
            JsonArray encoded = new JsonArray();
            string[]? previous = null;
            long last = 0;
            foreach (TimedFrame frame in frames) {
                string[] rows = FrameHtml.Rows(frame.Frame, depth, screen, site.Styles);
                if (rows.Length < height) {
                    string[] padded = new string[height];
                    Array.Copy(rows, padded, rows.Length);
                    for (int y = rows.Length; y < height; y++) {
                        padded[y] = string.Empty;
                    }
                    rows = padded;
                }
                site.Note(frame.Frame);
                JsonArray changed = new JsonArray();
                for (int y = 0; y < rows.Length; y++) {
                    if (previous == null || previous[y] != rows[y]) {
                        changed.Add(new JsonArray(y, rows[y]));
                    }
                }
                if (previous != null && changed.Count == 0) {
                    continue;
                }
                encoded.Add(new JsonObject { ["t"] = frame.Time, ["rows"] = changed });
                previous = rows;
                last = frame.Time;
            }
            finalBody = string.Join("\n", previous!);
            return new JsonObject {
                ["w"] = width,
                ["h"] = height,
                ["duration"] = last + hold,
                ["count"] = encoded.Count,
                ["frames"] = encoded
            };
        }
    }
}
