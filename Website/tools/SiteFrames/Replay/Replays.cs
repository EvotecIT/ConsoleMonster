using VtScreen = ConsoleMonster.Sessions.VtScreen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using ConsoleMonster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using SiteFrames.Vt;

namespace SiteFrames.Replay {
    /// <summary>
    /// Recordings: writes the synthetic JSON-lines file a shown program replays (with <see cref="LiveRecorder"/>), saves
    /// pseudo-console captures as asciicast stamped with a fingerprint of what made them, and turns asciicast back into
    /// frames through <see cref="VtScreen"/>, sampled at a fixed rate.
    /// </summary>
    internal static class Replays {
        /// <summary>Writes events as a JSON-lines recording with <see cref="LiveRecorder"/>.</summary>
        public static string Record(IEnumerable<LiveEvent> events) {
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
            using (LiveRecorder recorder = new LiveRecorder(writer)) {
                foreach (LiveEvent e in events) {
                    recorder.Write(e);
                }
            }
            return writer.ToString();
        }

        /// <summary>A short hash of everything a recording was made from (its program, arguments, size and data).</summary>
        public static string Fingerprint(params string[] parts) {
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create()) {
                return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n\u0001", parts).Replace("\r\n", "\n")))).Substring(0, 16).ToLowerInvariant();
            }
        }

        /// <summary>Writes a fingerprint into an asciicast header (cm_source), so a render can tell a recording is stale.</summary>
        public static string Stamp(string cast, string fingerprint) => "{\"cm_source\":\"" + fingerprint + "\"," + cast.Substring(cast.IndexOf('{') + 1);

        /// <summary>The fingerprint an asciicast was stamped with, or null.</summary>
        public static string? Source(string cast) {
            int end = cast.IndexOf('\n');
            using (JsonDocument header = JsonDocument.Parse(end < 0 ? cast : cast.Substring(0, end))) {
                return header.RootElement.TryGetProperty("cm_source", out JsonElement value) ? value.GetString() : null;
            }
        }

        /// <summary>Saves a pseudo-console capture as asciicast v2 (output events only, times in seconds).</summary>
        public static string ToCast(IReadOnlyList<OutputChunk> chunks, int width, int height, string title) {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"version\":2,\"width\":").Append(width).Append(",\"height\":").Append(height).Append(",\"title\":").Append(JsonSerializer.Serialize(title)).Append(",\"cm_timing\":\"logical-50ms-with-holds\"}\n");
            using var bytes = new MemoryStream();
            var screen = new VtScreen(width, height, keepScrollback: false);
            var pending = new StringBuilder();
            var normalizer = new ConsoleMonster.Sessions.VtOutputNormalizer(width, height);
            double milliseconds = 0;
            bool completed = false;
            screen.SynchronizedFrameCompleted = () => completed = true;
            void Flush() {
                if (pending.Length == 0) { return; }
                sb.Append('[').Append((milliseconds / 1000).ToString("0.000", CultureInfo.InvariantCulture)).Append(",\"o\",").Append(JsonSerializer.Serialize(pending.ToString())).Append("]\n");
                milliseconds += 50;
                pending.Clear(); completed = false;
            }
            void FeedBytes() {
                // A bare VT LF preserves its column. Only redundant CR before LF can be normalized.
                string text = System.Text.RegularExpressions.Regex.Replace(Encoding.UTF8.GetString(bytes.ToArray()), "\r+\n", "\r\n");
                text = System.Text.RegularExpressions.Regex.Replace(text, "\u001b\\][02];[^\u0007\u001b]*(?:\u0007|\u001b\\\\)", string.Empty).Replace("\u001b[6n", string.Empty);
                text = normalizer.Normalize(text);
                foreach (char character in text) {
                    pending.Append(character);
                    screen.Feed(character.ToString());
                    if (completed || (character == '\n' && !screen.SynchronizedOutput)) { Flush(); }
                }
                bytes.SetLength(0);
                bytes.Position = 0;
            }
            foreach (OutputChunk chunk in chunks) {
                bytes.Write(chunk.Data);
                if (chunk.Hold > TimeSpan.Zero) { FeedBytes(); Flush(); milliseconds += chunk.Hold.TotalMilliseconds; }
            }
            FeedBytes(); Flush();
            return sb.ToString();
        }

        /// <summary>The frames of an asciicast, sampled at a fixed rate; <paramref name="alternateOnly"/> keeps the live-screen frames.</summary>
        public static List<TimedFrame> FromCast(string cast, double fps, bool alternateOnly, out CellBuffer scrollback, out int width, out int height) {
            string[] lines = cast.Replace("\r\n", "\n").Split('\n');
            using (JsonDocument header = JsonDocument.Parse(lines[0])) {
                width = header.RootElement.GetProperty("width").GetInt32();
                height = header.RootElement.GetProperty("height").GetInt32();
            }
            List<(long Time, string Text)> events = new List<(long, string)>();
            for (int i = 1; i < lines.Length; i++) {
                if (lines[i].Length == 0) {
                    continue;
                }
                using (JsonDocument doc = JsonDocument.Parse(lines[i])) {
                    JsonElement e = doc.RootElement;
                    events.Add(((long)Math.Round(e[0].GetDouble() * 1000), e[2].GetString() ?? string.Empty));
                }
            }
            VtScreen screen = new VtScreen(width, height);
            List<TimedFrame> frames = new List<TimedFrame>();
            double period = 1000.0 / fps;
            int index = 0;
            long first = -1;
            long end = events.Count > 0 ? events[events.Count - 1].Time : 0;
            for (double next = 0; ; next += period) {
                long until = (long)Math.Round(next);
                while (index < events.Count && events[index].Time <= until) {
                    screen.Feed(events[index++].Text);
                }
                if (!screen.SynchronizedOutput && (!alternateOnly || screen.OnAlternate)) {
                    CellBuffer snapshot = screen.Snapshot();
                    if (first < 0 && !Empty(snapshot)) {
                        first = until;
                    }
                    if (first >= 0) {
                        frames.Add(new TimedFrame(until - first, snapshot));
                    }
                }
                if (index >= events.Count && until >= end) {
                    break;
                }
            }
            scrollback = Capture.Parity.Trim(screen.Transcript());
            return frames;
        }

        /// <summary>Everything an asciicast printed, joined.</summary>
        public static string CastText(string cast) {
            StringBuilder sb = new StringBuilder();
            string[] lines = cast.Replace("\r\n", "\n").Split('\n');
            for (int i = 1; i < lines.Length; i++) {
                if (lines[i].Length == 0) {
                    continue;
                }
                using (JsonDocument doc = JsonDocument.Parse(lines[i])) {
                    sb.Append(doc.RootElement[2].GetString());
                }
            }
            return sb.ToString();
        }

        private static bool Empty(CellBuffer frame) {
            for (int y = 0; y < frame.Height; y++) {
                if (frame.GetRowText(y).Trim().Length > 0) {
                    return false;
                }
            }
            return true;
        }
    }
}
