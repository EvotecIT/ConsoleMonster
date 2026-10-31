using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The newest lines of a log: lines fill from the top and, once the region is full, the newest line stays at the bottom. It is the capture target of a live session: Console.Out,
    /// Console.Error and PowerShell streams arrive here instead of writing at a random cursor position. Only warning and
    /// error tags carry colour; source tags get a stable series colour each. Live operation: <c>write</c> (Text, State =
    /// level, Key = source tag).
    /// </summary>
    public sealed class LogRegion : LiveWidget, ILogTarget {
        private readonly Queue<LogEntry> _entries = new Queue<LogEntry>();

        /// <summary>Creates a log region.</summary>
        /// <param name="widgetId">The live name (default <c>log</c>).</param>
        public LogRegion(string widgetId = "log") : base(widgetId) {
        }

        /// <summary>How many lines are kept (default 1000).</summary>
        public int Capacity { get; set; } = 1000;

        /// <summary>Also append each entry to normal scrollback above an inline live area. The retained entries still obey <see cref="Capacity"/>.</summary>
        public bool Scrollback { get; set; }

        /// <summary>Rows requested when measured without a height limit (default 8).</summary>
        public int PreferredHeight { get; set; } = 8;

        /// <summary>Whether entries show their session time (default true).</summary>
        public bool ShowElapsed { get; set; } = true;

        /// <summary>Entries skipped from the newest end while scrolling; zero follows the newest entries.</summary>
        public int ScrollOffset { get; set; }

        /// <summary>Shows source timestamps instead of session time; missing timestamps reserve a blank aligned cell.</summary>
        public bool ShowTimestamp { get; set; }

        /// <summary>Source timestamp format, using invariant culture (default ISO date, time, milliseconds and offset).</summary>
        public string TimestampFormat {
            get => _timestampFormat;
            set {
                if (!string.Equals(value, _timestampFormat, StringComparison.Ordinal)) { _timestampColumn.Reset(); }
                _timestampFormat = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>Literal ordinal case-insensitive substring to emphasize in plain-text entries; null or empty disables it.</summary>
        public string? HighlightText { get; set; }

        private readonly StatColumn _timestampColumn = new StatColumn(new ColumnFormat(HorizontalAlignment.Left));
        private string _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";
        private int _timestampWidth;

        /// <summary>
        /// Whether long entries continue on further rows, aligned under the message, instead of being truncated (default
        /// false). Lines break after spaces, then after separators, as everywhere else.
        /// </summary>
        public bool Wrap { get; set; }

        /// <summary>The most rows one entry may use when <see cref="Wrap"/> is on; the last row ends with an ellipsis when cut (default 3).</summary>
        public int MaxLinesPerEntry { get; set; } = 3;

        /// <summary>The kept entries, oldest first.</summary>
        public IReadOnlyCollection<LogEntry> Entries => _entries;

        /// <summary>Which kept entries are drawn, or null for all of them. Hidden entries stay kept and count toward <see cref="Capacity"/>.</summary>
        public LogFilter? Filter { get; set; }

        /// <summary>Total lines received (including dropped ones).</summary>
        public long TotalLines { get; private set; }

        // The shared tail model counts shown lines without rescanning the history on every append.
        internal Action<LogEntry>? Appended;
        internal Action<LogEntry>? Evicted;

        internal bool Shows(LogEntry entry) => Filter == null || Filter.Matches(entry);

        /// <summary>Appends text; line breaks create several entries.</summary>
        /// <param name="text">The text.</param>
        /// <param name="level">The level.</param>
        /// <param name="time">Session time.</param>
        public void Write(string text, string level = "info", TimeSpan time = default) {
            WriteLiteral(text, level, time, null, null);
        }

        /// <summary>Appends an entry, preserving its selected rich line, level, session time and source timestamp. Caller-created multiline entries expand into lines.</summary>
        /// <param name="entry">An existing log entry or a new message entry.</param>
        /// <exception cref="ArgumentNullException">The entry is null.</exception>
        public void Write(LogEntry entry) {
            if (entry == null) { throw new ArgumentNullException(nameof(entry)); }
            if (entry.IsLineSlice) {
                Append(new LogEntry(entry.Time, Level(entry.Level), entry.RichText!, entry.Text, entry.LineIndex, entry.Tag));
            } else if (entry.RichText != null) {
                WriteRich(entry.RichText, entry.Level, entry.Time, entry.Tag);
            } else {
                WriteLiteral(entry.Text, entry.Level, entry.Time, entry.Timestamp, entry.Tag);
            }
        }

        /// <summary>Appends literal text with an optional source timestamp; no session time is invented.</summary>
        /// <param name="text">The original text; line breaks create several entries.</param>
        /// <param name="level">The level.</param>
        /// <param name="timestamp">The source timestamp, or null when unknown.</param>
        public void WriteTimestamped(string text, string level, DateTimeOffset? timestamp) {
            WriteLiteral(text, level, TimeSpan.Zero, timestamp, null);
        }

        internal void WriteLiteral(string text, string level, TimeSpan time, DateTimeOffset? timestamp, string? tag) {
            foreach (string line in (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
                Append(new LogEntry(time, Level(level ?? "info"), line.Replace("\t", "    "), timestamp, tag));
            }
        }

        /// <summary>Appends rich text as separate lines, preserving styles across line breaks. Malformed markup is literal.</summary>
        /// <param name="text">The rich text.</param>
        /// <param name="level">The level.</param>
        /// <param name="time">Session time.</param>
        public void Write(RichText text, string level = "info", TimeSpan time = default) => WriteRich(text, level, time, null);

        internal void WriteRich(RichText text, string level, TimeSpan time, string? tag) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            string markup = text.ToMarkup();
            if (!Markup.Validate(markup, out _)) {
                System.Text.StringBuilder literal = new System.Text.StringBuilder();
                foreach (RichSegment segment in text.Segments) { literal.Append(segment.Text); }
                WriteLiteral(literal.ToString(), level, time, null, tag);
                return;
            }
            string[] lines = Markup.Strip(markup).Split('\n');
            for (int i = 0; i < lines.Length; i++) {
                // Keep one complete source: the parser must resolve spanning tags and implicit links before slicing.
                Append(new LogEntry(time, Level(level ?? "info"), text, lines[i], i, string.IsNullOrWhiteSpace(tag) ? null : tag!.Trim()));
            }
        }

        private void Append(LogEntry entry) {
            _entries.Enqueue(entry);
            TotalLines++;
            Appended?.Invoke(entry);
            while (_entries.Count > Math.Max(1, Capacity)) {
                LogEntry evicted = _entries.Dequeue();
                Evicted?.Invoke(evicted);
            }
        }

        /// <summary>Returns a handle that writes through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public LogHandle Handle(ILiveDispatcher dispatcher) => new LogHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "write") {
                Write(liveEvent);
            }
        }

        internal void Write(LiveEvent liveEvent) {
            string text = liveEvent.Text ?? string.Empty;
            TimeSpan time = TimeSpan.FromMilliseconds(liveEvent.Time);
            if (liveEvent.IsMarkup) {
                WriteRich(RichLines.FromEventText(text, true), liveEvent.Status ?? "info", time, liveEvent.Key);
            } else {
                WriteLiteral(text, liveEvent.Status ?? "info", time, null, liveEvent.Key);
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent.Operation != "write") {
                return null;
            }
            string tag = Tag(Level(liveEvent.Status ?? "info"));
            // Rich text narrates as its plain text: stream and plain output never show markup tags.
            string text = liveEvent.Text ?? string.Empty;
            if (liveEvent.IsMarkup) {
                text = RichLines.FromEventText(text, true).PlainText;
            }
            string source = string.IsNullOrWhiteSpace(liveEvent.Key) ? string.Empty : "[" + liveEvent.Key!.Trim() + "] ";
            return (tag.Length > 0 ? tag + " " : string.Empty) + source + text;
        }

        // Scrollback keeps the same semantic tags and literal fallback as the retained log, without clipping messages.
        internal StyledText ScrollbackText(LiveEvent liveEvent, RenderContext context) {
            string level = Level(liveEvent.Status ?? "info");
            string tag = Tag(level, context);
            string? source = string.IsNullOrWhiteSpace(liveEvent.Key) ? null : liveEvent.Key!.Trim();
            StyledText body = RichLines.FromEventText(liveEvent.Text ?? string.Empty, liveEvent.IsMarkup).ToStyledText(context);
            List<StyledLine> lines = new List<StyledLine>();
            foreach (StyledLine line in body.Lines) {
                List<TextSpan> spans = new List<TextSpan>();
                if (ShowElapsed) { spans.Add(new TextSpan(NumberFormat.Clock(TimeSpan.FromMilliseconds(liveEvent.Time), true) + " ", context.Style(ThemeToken.Muted))); }
                if (tag.Length > 0) { spans.Add(new TextSpan(tag + " ", TagStyle(level, context))); }
                if (source != null) { spans.Add(new TextSpan(source + " ", SourceStyle(source, context))); }
                spans.AddRange(line);
                lines.Add(new StyledLine(spans));
            }
            return new StyledText(lines);
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int height = constraints.IsHeightUnbounded ? Math.Min(UnboundedRows(constraints.MaxWidth, context), PreferredHeight) : Math.Min(PreferredHeight, constraints.MaxHeight);
            return new Size(constraints.MaxWidth, Math.Max(1, height));
        }

        // Rows the newest entries need without a height limit: one each, or their wrapped rows when Wrap is on.
        private int UnboundedRows(int width, RenderContext context) {
            LogEntry[] all = VisibleEntries();
            if (!Wrap) {
                return all.Length;
            }
            int count = Math.Min(all.Length, PreferredHeight);
            int levelWidth = 0;
            for (int i = all.Length - count; i < all.Length; i++) {
                levelWidth = Math.Max(levelWidth, DisplayWidth.Measure(Tag(all[i].Level, context)));
            }
            _sourceWidth = SourceWidth(all, all.Length, count);
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

        // The source column grows to the widest tag on screen, never shrinks during a session, and caps long names.
        private const int MaxSourceWidth = 20;
        private readonly StatColumn _sourceColumn = new StatColumn(new ColumnFormat(HorizontalAlignment.Left));
        private int _sourceWidth;
        private bool _sourceSeen;

        private LogEntry[] VisibleEntries() {
            if (Filter == null || Filter.IsEmpty) {
                return _entries.ToArray();
            }
            List<LogEntry> shown = new List<LogEntry>();
            foreach (LogEntry entry in _entries) {
                if (Filter.Matches(entry)) { shown.Add(entry); }
            }
            return shown.ToArray();
        }

        private int SourceWidth(LogEntry[] all, int end, int count) {
            // A filter on one tag shows only that source, and the state line already names it.
            if (Filter?.Tag != null) { return 0; }
            int widest = 0;
            for (int i = end - count; i < end; i++) {
                if (all[i].Tag != null) { widest = Math.Max(widest, Math.Min(MaxSourceWidth, DisplayWidth.Measure(all[i].Tag!))); }
            }
            // Once a session has shown a source, the column keeps its width so messages never jump sideways.
            if (widest > 0) { _sourceSeen = true; }
            return _sourceSeen ? _sourceColumn.Resolve(widest) : 0;
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0) {
                return;
            }
            Style muted = context.Style(ThemeToken.Muted);
            LogEntry[] all = VisibleEntries();
            int end = all.Length - Math.Max(0, Math.Min(ScrollOffset, Math.Max(0, all.Length - region.Height)));
            int count = Math.Min(end, region.Height);
            _timestampWidth = 0;
            if (ShowTimestamp) {
                foreach (LogEntry entry in all) {
                    _timestampWidth = Math.Max(_timestampWidth, DisplayWidth.Measure(SourceTime(entry)));
                }
                _timestampWidth = _timestampColumn.Resolve(_timestampWidth);
            }
            int startRow = 0;
            int levelWidth = 0;
            for (int i = 0; i < count; i++) {
                levelWidth = Math.Max(levelWidth, DisplayWidth.Measure(Tag(all[end - count + i].Level, context)));
            }
            levelWidth = _levelColumn.Resolve(levelWidth);
            _sourceWidth = SourceWidth(all, end, count);
            if (Wrap) {
                RenderWrapped(region, context, all, end, count, levelWidth, muted);
                return;
            }
            for (int i = 0; i < count; i++) {
                LogEntry entry = all[end - count + i];
                int y = startRow + i;
                int x = WritePrefix(region, y, entry, levelWidth, muted, context);
                Style textStyle = IsQuiet(entry.Level) ? muted : context.Style(ThemeToken.Text);
                StyledLine line = TextWrapper.Truncate(EntryLine(entry, textStyle, context), region.Width - x, context.Ellipsis);
                region.Write(x, y, line);
            }
        }

        private int WritePrefix(Region region, int y, LogEntry entry, int levelWidth, Style muted, RenderContext context) {
            int x = 0;
            if (ShowTimestamp) {
                if (_timestampWidth > 0) {
                    region.Write(0, y, SourceTime(entry), muted, _timestampWidth);
                    x = _timestampWidth + 1;
                }
            } else if (ShowElapsed) {
                x = region.Write(0, y, NumberFormat.Clock(entry.Time, true), muted) + 1;
            }
            if (levelWidth > 0) {
                StatColumn.Write(region, x, y, levelWidth, Tag(entry.Level, context), TagStyle(entry.Level, context), LevelColumn.Alignment, context.Ellipsis);
                x += levelWidth + 1;
            }
            if (_sourceWidth > 0) {
                if (entry.Tag != null) {
                    StatColumn.Write(region, x, y, _sourceWidth, entry.Tag, SourceStyle(entry.Tag, context), HorizontalAlignment.Left, context.Ellipsis);
                }
                x += _sourceWidth + 1;
            }
            return x;
        }

        private int PrefixWidth(LogEntry entry, int levelWidth) {
            int x = ShowTimestamp ? (_timestampWidth > 0 ? _timestampWidth + 1 : 0) : ShowElapsed ? DisplayWidth.Measure(NumberFormat.Clock(entry.Time, true)) + 1 : 0;
            x = levelWidth > 0 ? x + levelWidth + 1 : x;
            return _sourceWidth > 0 ? x + _sourceWidth + 1 : x;
        }

        // A source keeps one series colour for the whole session, so the same job reads the same in every row.
        internal static Style SourceStyle(string source, RenderContext context) {
            uint hash = 2166136261;
            foreach (char c in source.ToLowerInvariant()) { hash = (hash ^ c) * 16777619; }
            return context.Series((int)(hash % 6));
        }

        // Newest entries that fit as whole entries, rows filling from the top; the newest entry alone may be cut at the bottom.
        private void RenderWrapped(Region region, RenderContext context, LogEntry[] all, int end, int count, int levelWidth, Style muted) {
            List<KeyValuePair<LogEntry, IReadOnlyList<StyledLine>>> shown = new List<KeyValuePair<LogEntry, IReadOnlyList<StyledLine>>>();
            int rows = 0;
            for (int i = end - 1; i >= end - count; i--) {
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
            IReadOnlyList<StyledLine> lines = TextWrapper.Wrap(EntryLine(entry, style, context), width);
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
        private StyledLine EntryLine(LogEntry entry, Style style, RenderContext context) {
            if (entry.RichText != null) {
                return entry.RichText.ToStyledText(context).Lines[entry.LineIndex].WithBaseStyle(style);
            }
            string? needle = HighlightText;
            if (needle == null || needle.Length == 0) {
                return StyledLine.From(entry.Text, style);
            }
            List<TextSpan> spans = new List<TextSpan>();
            Style emphasis = context.Style(ThemeToken.Highlight).With(TextDecoration.Bold | TextDecoration.Underline);
            int start = 0;
            while (start < entry.Text.Length) {
                int match = entry.Text.IndexOf(needle, start, StringComparison.OrdinalIgnoreCase);
                if (match < 0) { spans.Add(new TextSpan(entry.Text.Substring(start), style)); break; }
                if (match > start) { spans.Add(new TextSpan(entry.Text.Substring(start, match - start), style)); }
                spans.Add(new TextSpan(entry.Text.Substring(match, needle.Length), emphasis));
                start = match + needle.Length;
            }
            return new StyledLine(spans);
        }

        private string SourceTime(LogEntry entry) => entry.Timestamp?.ToString(TimestampFormat, CultureInfo.InvariantCulture) ?? string.Empty;

        private static string Level(string level) => LogTags.Normalize(level);

        // Entries hold a normalized level (Write normalizes once), so drawing a frame only switches on it.
        private static bool IsQuiet(string level) => LogTags.IsQuiet(level);

        private static string Tag(string level, RenderContext? context = null) => LogTags.Tag(level, context);

        private static Style TagStyle(string level, RenderContext context) => LogTags.Style(level, context);
    }
}
