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
    /// Turns recordings into frames. A JSON-lines recording (what <see cref="LiveRecorder"/> writes) is read back with
    /// <see cref="LiveRecordingReader"/> and played into the dashboard it was recorded from, the way ConsoleMonster's own
    /// recording export does: events are applied by name in order and the tree is composed at a fixed frame rate, with the
    /// session clock following the recorded timeline. A pseudo-console capture (asciicast) is fed through
    /// <see cref="VtScreen"/> and sampled at the same rate.
    /// </summary>
    internal static class Replays {
        public static List<TimedFrame> FromEvents(IWidget root, IReadOnlyList<LiveEvent> events, int width, int height, double fps, ITheme theme, double speed = 1, ColorDepth depth = ColorDepth.TrueColor, GlyphTier glyphs = GlyphTier.Full) {
            Dictionary<string, ILiveTarget> targets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
            ILiveTarget? log = null;
            Discover(root, targets, ref log);
            TerminalProfile profile = TerminalProfile.Fixed(width, height, depth, glyphs).WithCulture(Site.Culture);
            RenderContext context = ConsoleMonster.Terminal.CreateContext(profile, theme);
            long end = 0;
            foreach (LiveEvent e in events) {
                end = Math.Max(end, e.Time);
            }
            List<TimedFrame> frames = new List<TimedFrame>();
            // Frames are sampled on the recorded timeline (so clocks show recorded time) and shown sooner at higher speeds.
            double period = 1000.0 * speed / fps;
            int index = 0;
            long tick = 0;
            for (double next = 0; ; next += period) {
                long until = (long)Math.Round(next);
                while (index < events.Count && events[index].Time <= until) {
                    LiveEvent e = events[index++];
                    ILiveTarget? target = e.Target == "$log" ? log : targets.TryGetValue(e.Target, out ILiveTarget? found) ? found : null;
                    try {
                        target?.Apply(e);
                    } catch (Exception) {
                        // A malformed event changes nothing, as in a live session.
                    }
                }
                CellBuffer frame = FrameComposer.Compose(root, width, height, context.WithClock(tick++, TimeSpan.FromMilliseconds(until)));
                frames.Add(new TimedFrame((long)Math.Round(until / speed), frame));
                if (index >= events.Count && until >= end) {
                    break;
                }
            }
            return frames;
        }

        private static void Discover(IWidget widget, Dictionary<string, ILiveTarget> targets, ref ILiveTarget? log) {
            if (widget is ILiveTarget target && !string.IsNullOrEmpty(target.Name) && !targets.ContainsKey(target.Name)) {
                targets.Add(target.Name, target);
                if (log == null && target is ILogTarget) {
                    log = target;
                }
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    Discover(child, targets, ref log);
                }
            }
        }

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

        /// <summary>Reads a JSON-lines recording back with <see cref="LiveRecordingReader"/>.</summary>
        public static List<LiveEvent> Read(string jsonLines) {
            using (StringReader reader = new StringReader(jsonLines)) {
                return new List<LiveEvent>(LiveRecordingReader.Read(reader));
            }
        }

        /// <summary>Saves a pseudo-console capture as asciicast v2 (output events only, times in seconds).</summary>
        public static string ToCast(IReadOnlyList<OutputChunk> chunks, int width, int height, string title) {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"version\":2,\"width\":").Append(width).Append(",\"height\":").Append(height).Append(",\"title\":").Append(JsonSerializer.Serialize(title)).Append("}\n");
            Decoder decoder = new UTF8Encoding(false).GetDecoder();
            foreach (OutputChunk chunk in chunks) {
                char[] chars = new char[decoder.GetCharCount(chunk.Data, 0, chunk.Data.Length)];
                decoder.GetChars(chunk.Data, 0, chunk.Data.Length, chars, 0);
                if (chars.Length == 0) {
                    continue;
                }
                sb.Append('[').Append((chunk.Time / 1000.0).ToString("0.000", CultureInfo.InvariantCulture)).Append(",\"o\",").Append(JsonSerializer.Serialize(new string(chars))).Append("]\n");
            }
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
