using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Export {
    /// <summary>How a recording is exported: screen size, frame rate, speed, theme, font and window frame.</summary>
    public sealed class RecordingExportOptions {
        /// <summary>Chapter labels and playback times, displayed by the HTML player.</summary>
        public IList<RecordingMarker> Markers { get; } = new List<RecordingMarker>();
        /// <summary>Screen width in cells (default 100).</summary>
        public int Width { get; set; } = 100;

        /// <summary>Screen height in cells (default 30).</summary>
        public int Height { get; set; } = 30;

        /// <summary>Dark (default) or Light. Auto is treated as Dark: a recording is played in one scheme.</summary>
        public ExportTheme Theme { get; set; } = ExportTheme.Dark;

        /// <summary>A custom theme to render with (default: the built-in dark or light theme).</summary>
        public ITheme? CustomTheme { get; set; }

        /// <summary>The title (page title, title bar, asciicast title).</summary>
        public string? Title { get; set; }

        /// <summary>Draws a window frame with a title bar (HTML and SVG).</summary>
        public bool WindowChrome { get; set; }

        /// <summary>The font stack (see <see cref="ExportOptions.DefaultFontFamily"/>).</summary>
        public string FontFamily { get; set; } = ExportOptions.DefaultFontFamily;

        /// <summary>The font size in pixels (default 14).</summary>
        public double FontSize { get; set; } = 14;

        /// <summary>The cell height as a multiple of the font size (default 1.2).</summary>
        public double LineHeight { get; set; } = 1.2;

        /// <summary>Frames per second sampled from the recording (default 10; identical frames are merged).</summary>
        public double FramesPerSecond { get; set; } = 10;

        /// <summary>The most frames kept; a longer recording is sampled less often (default 900).</summary>
        public int MaxFrames { get; set; } = 900;

        /// <summary>Playback speed multiplier baked into the export (default 1).</summary>
        public double Speed { get; set; } = 1;

        /// <summary>How long the last frame stays before the animation loops or ends (default 2 seconds).</summary>
        public TimeSpan HoldLastFrame { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>SVG only: loops the animation (default true).</summary>
        public bool Loop { get; set; } = true;

        /// <summary>The glyph tier (default <see cref="GlyphTier.Full"/>).</summary>
        public GlyphTier Glyphs { get; set; } = GlyphTier.Full;

        /// <summary>The culture for numbers and dates (default: the current culture).</summary>
        public CultureInfo? Culture { get; set; }

        /// <summary>Space around the screen in pixels (default 12).</summary>
        public int Padding { get; set; } = 12;

        /// <summary>Asciicast only: the recording time written to the header (default: none).</summary>
        public DateTimeOffset? Timestamp { get; set; }

        internal ExportOptions ToFrameOptions() {
            ExportOptions o = new ExportOptions {
                Width = Width,
                Height = Height,
                Theme = Theme == ExportTheme.Light ? ExportTheme.Light : ExportTheme.Dark,
                Title = Title,
                WindowChrome = WindowChrome,
                FontFamily = FontFamily,
                FontSize = FontSize,
                LineHeight = LineHeight,
                Glyphs = Glyphs,
                Culture = Culture,
                Padding = Padding
            };
            o.Validate();
            if (Height < 1) {
                throw new ArgumentOutOfRangeException(nameof(Height), "A recording needs a fixed height of at least one row.");
            }
            if (double.IsNaN(FramesPerSecond) || FramesPerSecond <= 0 || FramesPerSecond > 60) {
                throw new ArgumentOutOfRangeException(nameof(FramesPerSecond), "FramesPerSecond must be above 0 and at most 60.");
            }
            if (double.IsNaN(Speed) || double.IsInfinity(Speed) || Speed <= 0) {
                throw new ArgumentOutOfRangeException(nameof(Speed), "Speed must be a positive number.");
            }
            if (MaxFrames < 1) {
                throw new ArgumentOutOfRangeException(nameof(MaxFrames));
            }
            if (HoldLastFrame < TimeSpan.Zero) {
                throw new ArgumentOutOfRangeException(nameof(HoldLastFrame));
            }
            return o;
        }

        internal ExportPalette Palette => Theme == ExportTheme.Light ? ExportPalette.Light : ExportPalette.Dark;

        /// <summary>A render context to use instead of the export one (the in-terminal player renders with the terminal's).</summary>
        internal RenderContext? Context { get; set; }

        internal ITheme RenderTheme => CustomTheme ?? (Theme == ExportTheme.Light ? Styling.Theme.Light : Styling.Theme.Dark);
    }

    /// <summary>A labelled chapter in a recording's playback timeline.</summary>
    public sealed class RecordingMarker {
        /// <summary>Creates a chapter at a playback time.</summary>
        public RecordingMarker(TimeSpan time, string label) { Time = time; Label = label ?? throw new ArgumentNullException(nameof(label)); }
        /// <summary>Time from the start of playback.</summary>
        public TimeSpan Time { get; }
        /// <summary>Chapter label.</summary>
        public string Label { get; }
    }

    /// <summary>One distinct screen of a recording and how long it stays (after speed scaling).</summary>
    internal sealed class RecordedFrame {
        public RecordedFrame(CellBuffer frame, long start) {
            Frame = frame;
            Start = start;
        }

        public CellBuffer Frame { get; }

        /// <summary>Milliseconds from the start of playback.</summary>
        public long Start { get; set; }

        /// <summary>Milliseconds this frame stays.</summary>
        public long Duration { get; set; }
    }

    /// <summary>
    /// Plays a recording into its widget tree without a terminal: events are applied to the named targets in order and the
    /// tree is rendered at a fixed frame rate, with the session clock following the recorded timeline (so header clocks
    /// and spinners move as they did). Identical consecutive frames are merged.
    /// </summary>
    internal static class RecordingFrames {
        public static List<RecordedFrame> Capture(IWidget root, IReadOnlyList<LiveEvent> events, RecordingExportOptions options) {
            List<RecordedFrame> frames = new List<RecordedFrame>();
            Sample(root, events, options, (buffer, time) => Add(frames, buffer, time, options.Speed));
            long hold = (long)options.HoldLastFrame.TotalMilliseconds;
            RecordedFrame last = frames[frames.Count - 1];
            last.Duration = Math.Max(last.Duration, hold);
            for (int i = 0; i < frames.Count - 1; i++) {
                frames[i].Duration = frames[i + 1].Start - frames[i].Start;
            }
            return frames;
        }

        /// <summary>
        /// Plays the events into the tree and hands every sampled frame (a new buffer each time) and its recorded time in
        /// milliseconds to <paramref name="onFrame"/>, without merging.
        /// </summary>
        public static void Sample(IWidget root, IReadOnlyList<LiveEvent> events, RecordingExportOptions options, Action<CellBuffer, long> onFrame) {
            if (root == null) {
                throw new ArgumentNullException(nameof(root));
            }
            ExportOptions frame = options.ToFrameOptions();
            long end = 0;
            foreach (LiveEvent e in events) {
                end = Math.Max(end, e.Time);
            }
            double period = 1000.0 / options.FramesPerSecond;
            while (end / period > options.MaxFrames) {
                period *= 2;
            }
            Dictionary<string, ILiveTarget> targets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
            ILiveTarget? log = null;
            Discover(root, targets, ref log);
            RenderContext context = options.Context ?? ConsoleExporter.CreateContext(frame, options.RenderTheme, options.Height);
            int index = 0;
            long tick = 0;
            double next = 0;
            while (true) {
                long until = (long)Math.Round(next);
                while (index < events.Count && events[index].Time <= until) {
                    Apply(events[index], targets, log);
                    index++;
                }
                onFrame(Render(root, frame, context, tick++, until), until);
                if (index >= events.Count && until >= end) {
                    break;
                }
                next += period;
            }
        }

        private static CellBuffer Render(IWidget root, ExportOptions options, RenderContext context, long tick, long time) {
            CellBuffer buffer = FrameComposer.Compose(root, options.Width, options.Height, context.WithClock(tick, TimeSpan.FromMilliseconds(time)));
            return StripUnsafeLinks(buffer);
        }

        private static void Add(List<RecordedFrame> frames, CellBuffer buffer, long time, double speed) {
            long start = (long)Math.Round(time / speed);
            if (frames.Count > 0 && Same(frames[frames.Count - 1].Frame, buffer)) {
                return;
            }
            frames.Add(new RecordedFrame(buffer, start));
        }

        internal static bool Same(CellBuffer a, CellBuffer b) {
            if (a.Width != b.Width || a.Height != b.Height) {
                return false;
            }
            for (int y = 0; y < a.Height; y++) {
                for (int x = 0; x < a.Width; x++) {
                    if (!a[x, y].Equals(b[x, y])) {
                        return false;
                    }
                }
            }
            return true;
        }

        internal static bool SameRow(CellBuffer a, CellBuffer b, int y) {
            if (a.Width != b.Width) {
                return false;
            }
            for (int x = 0; x < a.Width; x++) {
                if (!a[x, y].Equals(b[x, y])) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>A copy without links that exports must not carry (anything but http, https, mailto and ftp).</summary>
        internal static CellBuffer StripUnsafeLinks(CellBuffer buffer) {
            bool any = false;
            for (int y = 0; y < buffer.Height && !any; y++) {
                for (int x = 0; x < buffer.Width; x++) {
                    string? link = buffer[x, y].Style.Link;
                    if (link != null && !ExportText.IsSafeLink(link)) {
                        any = true;
                        break;
                    }
                }
            }
            if (!any) {
                return buffer;
            }
            for (int y = 0; y < buffer.Height; y++) {
                for (int x = 0; x < buffer.Width; x++) {
                    Cell cell = buffer[x, y];
                    if (cell.Style.Link != null && !ExportText.IsSafeLink(cell.Style.Link)) {
                        buffer.SetStyle(x, y, cell.Style.WithLink(null));
                    }
                }
            }
            return buffer;
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

        private static void Apply(LiveEvent e, Dictionary<string, ILiveTarget> targets, ILiveTarget? log) {
            ILiveTarget? target = e.Target == "$log" ? log : targets.TryGetValue(e.Target, out ILiveTarget? found) ? found : null;
            if (target == null) {
                return;
            }
            try {
                target.Apply(e);
            } catch (Exception) {
                // A malformed event changes nothing, as in a live session.
            }
        }
    }
}
