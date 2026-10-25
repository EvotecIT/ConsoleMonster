using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Replay {
    /// <summary>Options for <see cref="ReplayPlayer.Play(IWidget, TextReader, ReplayOptions, CancellationToken)"/>.</summary>
    public sealed class ReplayOptions {
        private double _speed = 1;

        /// <summary>
        /// The run name shown in the header (default the recorded dashboard title, or "Replay"). A recorded
        /// <see cref="Widgets.Dashboard"/>'s own header and footer are not replayed: its title (when it adds to this name),
        /// subtitle and status show in the player's header.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>The starting speed (0.25 to 16, default 1).</summary>
        public double Speed {
            get => _speed;
            set {
                if (double.IsNaN(value) || value < 0.25 || value > 16) { throw new ArgumentOutOfRangeException(nameof(value), "Speed must be between 0.25 and 16."); }
                _speed = value;
            }
        }

        /// <summary>Plays inline, below the cursor, instead of on the whole window.</summary>
        public bool Inline { get; set; }

        /// <summary>Starts playing at once (default true); false opens paused at the start.</summary>
        public bool AutoPlay { get; set; } = true;

        /// <summary>The height of the recorded screen in rows, or null: the window (full screen) or the dashboard's own height (inline).</summary>
        public int? Height { get; set; }

        /// <summary>How often the recording is sampled, in frames per second (default 10; identical frames are merged).</summary>
        public double FramesPerSecond { get; set; } = 10;

        /// <summary>The most frames kept; a longer recording is sampled less often (default 3000).</summary>
        public int MaxFrames { get; set; } = 3000;

        /// <summary>Input, target, language and other interactive settings, or null for the defaults.</summary>
        public InteractiveOptions? Interactive { get; set; }
    }

    /// <summary>
    /// Rewinds runs: an in-terminal player for recordings (the JSON-lines files written with
    /// <see cref="LiveSessionOptions.RecordPath"/> or <see cref="LiveSessionOptions.AutoRecord"/>). The recording is played
    /// into the same dashboard definition, then shown with play/pause, speed, frame stepping, a timeline to scrub, jumps to
    /// markers, failures, warnings and log entries, and a search over the log. Without an interactive console it prints
    /// the markers as lines instead.
    /// </summary>
    public static class ReplayPlayer {
        /// <summary>Plays a recording.</summary>
        /// <param name="root">The dashboard (or any widget tree) whose widget names match the recording.</param>
        /// <param name="jsonLines">The recording.</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="cancellationToken">Ends the player.</param>
        public static void Play(IWidget root, TextReader jsonLines, ReplayOptions? options = null, CancellationToken cancellationToken = default) {
            if (jsonLines == null) {
                throw new ArgumentNullException(nameof(jsonLines));
            }
            Play(root, new List<LiveEvent>(LiveRecordingReader.Read(jsonLines)), options, cancellationToken);
        }

        /// <summary>Plays recorded events.</summary>
        /// <param name="root">The dashboard (or any widget tree) whose widget names match the recording.</param>
        /// <param name="events">The events in recording order.</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="cancellationToken">Ends the player.</param>
        public static void Play(IWidget root, IReadOnlyList<LiveEvent> events, ReplayOptions? options = null, CancellationToken cancellationToken = default) {
            if (root == null) {
                throw new ArgumentNullException(nameof(root));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            options ??= new ReplayOptions();
            InteractiveOptions interactive = options.Interactive ?? InteractiveOptions.Default;
            IReadOnlyList<ReplayMarker> markers = ReplayMarker.From(events);
            // A recorded dashboard's own header and footer are not replayed: the player is the one frame, and its header
            // carries the recorded title, subtitle and status (the recorded clock is the position).
            Dashboard? dashboard = root as Dashboard;
            string title = !string.IsNullOrEmpty(options.Title) ? options.Title! : dashboard != null && !string.IsNullOrEmpty(dashboard.Title) ? Markup.Strip(dashboard.Title) : "Replay";
            InteractionMode mode = interactive.ResolveMode();
            if (mode != InteractionMode.Rich) {
                WriteMarkers(title, events, markers, interactive);
                return;
            }
            PromptLabels labels = interactive.ResolveLabels();
            using (InputLease lease = ConsoleInput.Acquire(interactive.Input)) {
                TerminalProfile profile = interactive.ResolveProfile();
                ITheme theme = interactive.ResolveTheme();
                RenderContext context = Terminal.CreateContext(profile, theme);
                int width = Math.Max(20, profile.Width);
                IWidget screen = dashboard != null ? new DashboardBody(dashboard) : root;
                int height = ScreenHeight(screen, options, profile, context, width);
                ReplayFrames frames = ReplayFrames.Capture(screen, events, options, profile.WithSize(width, height), theme);
                IPromptSurface? surface = interactive.Surface;
                bool owns = false;
                if (surface == null) {
                    surface = PromptHost.Resolve(context) ?? (options.Inline ? (IPromptSurface)new InlineSurface(profile, null, height + ReplayView.Chrome) : new ScreenSurface(profile));
                    owns = true;
                }
                try {
                    ReplayView view = new ReplayView(title, !string.IsNullOrEmpty(options.Title), frames, markers, options, labels);
                    InteractionLoop.Run(view, lease.Source, surface, context, cancellationToken);
                    surface.Complete(view.Summary(context));
                } finally {
                    if (owns) {
                        surface.Dispose();
                    }
                }
            }
        }

        /// <summary>Formats a timeline position as <c>m:ss.f</c> (or <c>h:mm:ss</c> past an hour).</summary>
        /// <param name="time">The position.</param>
        /// <returns>The text.</returns>
        public static string FormatTime(TimeSpan time) {
            if (time < TimeSpan.Zero) {
                time = TimeSpan.Zero;
            }
            if (time.TotalHours >= 1) {
                return ((int)time.TotalHours).ToString(CultureInfo.InvariantCulture) + ":" + time.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + time.Seconds.ToString("00", CultureInfo.InvariantCulture);
            }
            return ((int)time.TotalMinutes).ToString(CultureInfo.InvariantCulture) + ":" + time.Seconds.ToString("00", CultureInfo.InvariantCulture) + "." + (time.Milliseconds / 100).ToString(CultureInfo.InvariantCulture);
        }

        private static int ScreenHeight(IWidget root, ReplayOptions options, TerminalProfile profile, RenderContext context, int width) {
            int window = Math.Max(4, profile.Height - ReplayView.Chrome);
            if (options.Height.HasValue) {
                return Math.Max(1, Math.Min(500, options.Height.Value));
            }
            if (!options.Inline) {
                return window;
            }
            int own = root.Measure(new Constraints(width, window), context).Height;
            return Math.Max(3, Math.Min(window, own));
        }

        // Without an interactive console: the markers as lines, so a CI log still shows what happened when.
        private static void WriteMarkers(string title, IReadOnlyList<LiveEvent> events, IReadOnlyList<ReplayMarker> markers, InteractiveOptions interactive) {
            long end = 0;
            foreach (LiveEvent e in events) {
                end = Math.Max(end, e.Time);
            }
            ILineConsole lines = interactive.ResolveLineConsole();
            lines.WriteLine(title + " (" + FormatTime(TimeSpan.FromMilliseconds(end)) + ", " + events.Count.ToString(CultureInfo.InvariantCulture) + " events)");
            foreach (ReplayMarker marker in markers) {
                lines.WriteLine("  " + FormatTime(marker.Time) + "  " + marker.Kind.ToString().ToLowerInvariant() + "  " + marker.Text);
            }
        }
    }

    /// <summary>The distinct screens of a recording; unchanged rows are shared between frames to keep memory low.</summary>
    internal sealed class ReplayFrames {
        private ReplayFrames(int width, int height) {
            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }

        public List<long> Times { get; } = new List<long>();

        public List<Cell[][]> Rows { get; } = new List<Cell[][]>();

        /// <summary>The recorded dashboard header at each frame, or null entries when the root is not a dashboard.</summary>
        public List<ReplayChrome?> Chrome { get; } = new List<ReplayChrome?>();

        public long End { get; private set; }

        public int Count => Times.Count;

        /// <summary>The index of the frame shown at <paramref name="time"/> (the last that started at or before it).</summary>
        public int IndexAt(long time) {
            int low = 0;
            int high = Times.Count - 1;
            while (low < high) {
                int middle = (low + high + 1) / 2;
                if (Times[middle] <= time) {
                    low = middle;
                } else {
                    high = middle - 1;
                }
            }
            return low;
        }

        public static ReplayFrames Capture(IWidget root, IReadOnlyList<LiveEvent> events, ReplayOptions options, TerminalProfile profile, ITheme theme) {
            ReplayFrames frames = new ReplayFrames(profile.Width, profile.Height);
            RecordingExportOptions export = new RecordingExportOptions {
                Width = profile.Width,
                Height = profile.Height,
                FramesPerSecond = options.FramesPerSecond,
                MaxFrames = Math.Max(1, options.MaxFrames),
                Glyphs = profile.Glyphs,
                CustomTheme = theme,
                Culture = profile.Culture,
                Context = Terminal.CreateContext(profile, theme)
            };
            Cell[][]? previous = null;
            ReplayChrome? previousChrome = null;
            Dashboard? dashboard = (root as DashboardBody)?.Dashboard;
            RecordingFrames.Sample(root, events, export, (buffer, time) => {
                ReplayChrome? chrome = dashboard == null ? null : new ReplayChrome(dashboard.Title, dashboard.Subtitle, dashboard.Detail, dashboard.Status);
                Cell[][] rows = new Cell[buffer.Height][];
                bool same = previous != null;
                for (int y = 0; y < buffer.Height; y++) {
                    Cell[] row = new Cell[buffer.Width];
                    for (int x = 0; x < buffer.Width; x++) {
                        row[x] = buffer[x, y];
                    }
                    if (previous != null && SameRow(previous[y], row)) {
                        row = previous[y];
                    } else {
                        same = false;
                    }
                    rows[y] = row;
                }
                frames.End = Math.Max(frames.End, time);
                if (same && Equals(chrome, previousChrome)) {
                    return;
                }
                frames.Times.Add(time);
                frames.Rows.Add(rows);
                frames.Chrome.Add(chrome);
                previous = rows;
                previousChrome = chrome;
            });
            return frames;
        }

        private static bool SameRow(Cell[] a, Cell[] b) {
            if (a.Length != b.Length) {
                return false;
            }
            for (int i = 0; i < a.Length; i++) {
                if (!a[i].Equals(b[i])) {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>A recorded dashboard's header state at one frame.</summary>
    internal sealed class ReplayChrome : IEquatable<ReplayChrome> {
        public ReplayChrome(string title, string? subtitle, string? status, StatusKind? statusLevel) {
            Title = title ?? string.Empty;
            Subtitle = subtitle;
            Status = status;
            StatusLevel = statusLevel;
        }

        public string Title { get; }

        public string? Subtitle { get; }

        public string? Status { get; }

        public StatusKind? StatusLevel { get; }

        public bool Equals(ReplayChrome? other) => other != null && Title == other.Title && Subtitle == other.Subtitle && Status == other.Status && StatusLevel == other.StatusLevel;

        public override bool Equals(object? obj) => Equals(obj as ReplayChrome);

        public override int GetHashCode() => Title.GetHashCode() ^ (Status?.GetHashCode() ?? 0);
    }

    /// <summary>
    /// A recorded dashboard without its header and footer: the body under the same one-row gap, while live events still
    /// reach the dashboard itself (title, status) through <see cref="Children"/>.
    /// </summary>
    internal sealed class DashboardBody : IWidget, IContainer {
        public DashboardBody(Dashboard dashboard) {
            Dashboard = dashboard;
        }

        public Dashboard Dashboard { get; }

        public IReadOnlyList<IWidget> Children => new IWidget[] { Dashboard };

        public Size Measure(Constraints constraints, RenderContext context) {
            Size body = Dashboard.Body.Measure(constraints.Shrink(0, 1), context);
            int height = body.Height + 1;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            if (region.Height > 1) {
                Dashboard.Body.Render(region.Sub(0, 1, region.Width, region.Height - 1), context);
            }
        }
    }
}
