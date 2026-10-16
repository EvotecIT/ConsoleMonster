using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The newest lines of a log: lines fill from the top and, once the region is full, the newest line stays at the bottom. It is the capture target of a live session: Console.Out,
    /// Console.Error and PowerShell streams arrive here instead of writing at a random cursor position. Only warning and
    /// error tags carry colour. Live operation: <c>write</c> (Text, State = level).
    /// </summary>
    public sealed class LogRegion : LiveWidget, ILogTarget {
        private readonly Queue<LogEntry> _entries = new Queue<LogEntry>();

        /// <summary>Creates a log region.</summary>
        /// <param name="name">The live name (default <c>log</c>).</param>
        public LogRegion(string name = "log") : base(name) {
        }

        /// <summary>How many lines are kept (default 1000).</summary>
        public int Capacity { get; set; } = 1000;

        /// <summary>Rows requested when measured without a height limit (default 8).</summary>
        public int PreferredHeight { get; set; } = 8;

        /// <summary>Whether entries show their session time (default true).</summary>
        public bool ShowTime { get; set; } = true;

        /// <summary>The kept entries, oldest first.</summary>
        public IReadOnlyCollection<LogEntry> Entries => _entries;

        /// <summary>Total lines received (including dropped ones).</summary>
        public long TotalLines { get; private set; }

        /// <summary>Appends text; line breaks create several entries.</summary>
        /// <param name="text">The text.</param>
        /// <param name="level">The level.</param>
        /// <param name="time">Session time.</param>
        public void Write(string text, string level = "info", TimeSpan time = default) {
            string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string line in lines) {
                _entries.Enqueue(new LogEntry(time, level ?? "info", line.Replace("\t", "    ")));
                TotalLines++;
                while (_entries.Count > Math.Max(1, Capacity)) {
                    _entries.Dequeue();
                }
            }
        }

        /// <summary>Returns a handle that writes through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public LogHandle Handle(ILiveDispatcher dispatcher) => new LogHandle(dispatcher, Name);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "write") {
                Write(liveEvent.Text ?? string.Empty, liveEvent.State ?? "info", TimeSpan.FromMilliseconds(liveEvent.Time));
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent.Operation != "write") {
                return null;
            }
            string tag = Tag(liveEvent.State ?? "info");
            return (tag.Length > 0 ? tag + " " : string.Empty) + (liveEvent.Text ?? string.Empty);
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int height = constraints.IsHeightUnbounded ? Math.Min(_entries.Count, PreferredHeight) : Math.Min(PreferredHeight, constraints.MaxHeight);
            return new Size(constraints.MaxWidth, Math.Max(1, height));
        }

        /// <summary>
        /// Layout of the level column (ERR, WARN, VERB...). Every row shares its width, including info lines that have no
        /// tag, so messages always start in the same column; the width never shrinks during a session.
        /// </summary>
        public ColumnFormat LevelColumn {
            get => _levelColumn.Format;
            set => _levelColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly StatColumn _levelColumn = new StatColumn(new ColumnFormat(HorizontalAlignment.Left));

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0) {
                return;
            }
            Style muted = context.Style(ThemeToken.Muted);
            LogEntry[] all = _entries.ToArray();
            int count = Math.Min(all.Length, region.Height);
            int startRow = 0;
            int levelWidth = 0;
            for (int i = 0; i < count; i++) {
                levelWidth = Math.Max(levelWidth, Tag(all[all.Length - count + i].Level).Length);
            }
            levelWidth = levelWidth > 0 ? _levelColumn.Resolve(levelWidth) : 0;
            for (int i = 0; i < count; i++) {
                LogEntry entry = all[all.Length - count + i];
                int y = startRow + i;
                int x = 0;
                if (ShowTime) {
                    x = region.Write(0, y, NumberFormat.Clock(entry.Time, true), muted) + 1;
                }
                if (levelWidth > 0) {
                    StatColumn.Write(region, x, y, levelWidth, Tag(entry.Level), TagStyle(entry.Level, context), LevelColumn.Alignment);
                    x += levelWidth + 1;
                }
                Style textStyle = IsQuiet(entry.Level) ? muted : context.Style(ThemeToken.Text);
                StyledLine line = TextWrapper.Truncate(StyledLine.From(entry.Text, textStyle), region.Width - x, context.Ellipsis);
                region.Write(x, y, line);
            }
        }

        private static bool IsQuiet(string level) => level == "verbose" || level == "debug";

        private static string Tag(string level) {
            switch (level) {
                case "err":
                case "error":
                case "fail":
                    return "ERR";
                case "warn":
                case "warning":
                    return "WARN";
                case "verbose":
                    return "VERB";
                case "debug":
                    return "DBG";
                case "pass":
                    return "OK";
                default:
                    return string.Empty;
            }
        }
        private static Style TagStyle(string level, RenderContext context) {
            switch (level) {
                case "err":
                case "error":
                case "fail":
                    return context.Style(ThemeToken.Fail);
                case "warn":
                case "warning":
                    return context.Style(ThemeToken.Warn);
                case "pass":
                    return context.Style(ThemeToken.Pass);
                default:
                    return context.Style(ThemeToken.Muted);
            }
        }
    }
}
