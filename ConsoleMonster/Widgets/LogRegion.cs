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

        /// <summary>
        /// Whether long entries continue on further rows, aligned under the message, instead of being truncated (default
        /// false). Lines break after spaces, then after separators, as everywhere else.
        /// </summary>
        public bool Wrap { get; set; }

        /// <summary>The most rows one entry may use when <see cref="Wrap"/> is on; the last row ends with an ellipsis when cut (default 3).</summary>
        public int MaxLinesPerEntry { get; set; } = 3;

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

        /// <summary>Appends a multicolour line (line breaks inside it create several entries, each keeping its styles).</summary>
        /// <param name="text">The rich text.</param>
        /// <param name="level">The level.</param>
        /// <param name="time">Session time.</param>
        public void Write(RichText text, string level = "info", TimeSpan time = default) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            foreach (string line in text.ToMarkup().Replace("\r\n", "\n").Split('\n')) {
                _entries.Enqueue(new LogEntry(time, level ?? "info", RichText.FromMarkup(line)));
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
                string text = liveEvent.Text ?? string.Empty;
                if (liveEvent.IsMarkup) {
                    Write(RichLines.FromEventText(text, true), liveEvent.State ?? "info", TimeSpan.FromMilliseconds(liveEvent.Time));
                } else {
                    Write(text, liveEvent.State ?? "info", TimeSpan.FromMilliseconds(liveEvent.Time));
                }
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent.Operation != "write") {
                return null;
            }
            string tag = Tag(liveEvent.State ?? "info");
            // Rich text narrates as its plain text: stream and plain output never show markup tags.
            string text = liveEvent.Text ?? string.Empty;
            if (liveEvent.IsMarkup) {
                text = Markup.Strip(text);
            }
            return (tag.Length > 0 ? tag + " " : string.Empty) + text;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int height = constraints.IsHeightUnbounded ? Math.Min(UnboundedRows(constraints.MaxWidth, context), PreferredHeight) : Math.Min(PreferredHeight, constraints.MaxHeight);
            return new Size(constraints.MaxWidth, Math.Max(1, height));
        }

        // Rows the newest entries need without a height limit: one each, or their wrapped rows when Wrap is on.
        private int UnboundedRows(int width, RenderContext context) {
            if (!Wrap) {
                return _entries.Count;
            }
            LogEntry[] all = _entries.ToArray();
            int count = Math.Min(all.Length, PreferredHeight);
            int levelWidth = 0;
            for (int i = all.Length - count; i < all.Length; i++) {
                levelWidth = Math.Max(levelWidth, Tag(all[i].Level).Length);
            }
            int rows = 0;
            for (int i = all.Length - 1; i >= all.Length - count && rows < PreferredHeight; i--) {
                rows += WrapEntry(all[i], Style.Plain, Math.Max(1, width - PrefixWidth(all[i], levelWidth)), context).Count;
            }
            return rows;
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
            if (Wrap) {
                RenderWrapped(region, context, all, count, levelWidth, muted);
                return;
            }
            for (int i = 0; i < count; i++) {
                LogEntry entry = all[all.Length - count + i];
                int y = startRow + i;
                int x = WritePrefix(region, y, entry, levelWidth, muted, context);
                Style textStyle = IsQuiet(entry.Level) ? muted : context.Style(ThemeToken.Text);
                StyledLine line = TextWrapper.Truncate(RichLines.Line(entry.RichText, entry.Text, textStyle, context), region.Width - x, context.Ellipsis);
                region.Write(x, y, line);
            }
        }

        private int WritePrefix(Region region, int y, LogEntry entry, int levelWidth, Style muted, RenderContext context) {
            int x = 0;
            if (ShowTime) {
                x = region.Write(0, y, NumberFormat.Clock(entry.Time, true), muted) + 1;
            }
            if (levelWidth > 0) {
                StatColumn.Write(region, x, y, levelWidth, Tag(entry.Level), TagStyle(entry.Level, context), LevelColumn.Alignment, context.Ellipsis);
                x += levelWidth + 1;
            }
            return x;
        }

        private int PrefixWidth(LogEntry entry, int levelWidth) {
            int x = ShowTime ? DisplayWidth.Measure(NumberFormat.Clock(entry.Time, true)) + 1 : 0;
            return levelWidth > 0 ? x + levelWidth + 1 : x;
        }

        // Newest entries that fit as whole entries, rows filling from the top; the newest entry alone may be cut at the bottom.
        private void RenderWrapped(Region region, RenderContext context, LogEntry[] all, int count, int levelWidth, Style muted) {
            List<KeyValuePair<LogEntry, IReadOnlyList<StyledLine>>> shown = new List<KeyValuePair<LogEntry, IReadOnlyList<StyledLine>>>();
            int rows = 0;
            for (int i = all.Length - 1; i >= all.Length - count; i--) {
                LogEntry entry = all[i];
                Style textStyle = IsQuiet(entry.Level) ? muted : context.Style(ThemeToken.Text);
                IReadOnlyList<StyledLine> lines = WrapEntry(entry, textStyle, Math.Max(1, region.Width - PrefixWidth(entry, levelWidth)), context);
                if (shown.Count > 0 && rows + lines.Count > region.Height) {
                    break;
                }
                shown.Insert(0, new KeyValuePair<LogEntry, IReadOnlyList<StyledLine>>(entry, lines));
                rows += lines.Count;
                if (rows >= region.Height) {
                    break;
                }
            }
            int y = 0;
            foreach (KeyValuePair<LogEntry, IReadOnlyList<StyledLine>> item in shown) {
                int x = WritePrefix(region, y, item.Key, levelWidth, muted, context);
                foreach (StyledLine line in item.Value) {
                    if (y >= region.Height) {
                        return;
                    }
                    region.Write(x, y, line);
                    y++;
                }
            }
        }

        // Wrapping keeps styles across lines; the last kept row of a cut entry ends with an ellipsis.
        private IReadOnlyList<StyledLine> WrapEntry(LogEntry entry, Style style, int width, RenderContext context) {
            IReadOnlyList<StyledLine> lines = TextWrapper.Wrap(RichLines.Line(entry.RichText, entry.Text, style, context), width);
            int max = Math.Max(1, MaxLinesPerEntry);
            if (lines.Count <= max) {
                return lines;
            }
            List<StyledLine> kept = new List<StyledLine>();
            for (int i = 0; i < max - 1; i++) {
                kept.Add(lines[i]);
            }
            // The last kept row shows that more text was cut: the ellipsis follows the text, cutting it when needed.
            StyledLine last = lines[max - 1];
            string marker = context.Ellipsis;
            int markerWidth = DisplayWidth.Measure(marker);
            StyledLine body = last.Width + markerWidth <= width ? last : TextWrapper.Truncate(last, Math.Max(0, width - markerWidth), string.Empty);
            List<TextSpan> spans = new List<TextSpan>(body);
            spans.Add(new TextSpan(marker, style));
            kept.Add(new StyledLine(spans));
            return kept;
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
