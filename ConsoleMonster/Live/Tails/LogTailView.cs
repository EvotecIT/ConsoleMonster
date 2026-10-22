using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live.Tails {
    /// <summary>A bounded log viewport with queued appends and navigation. LogRegion owns storage and line rendering.</summary>
    public sealed class LogTailView : LiveWidget {
        private readonly LogRegion _log;
        private int _visibleRows = 1;

        /// <summary>Creates a view that initially follows the newest entry.</summary>
        /// <param name="capacity">Maximum retained history entries. Pending updates are not discarded.</param>
        /// <param name="name">Unique live-session target name.</param>
        /// <param name="highlight">Optional literal, ordinal case-insensitive substring to emphasize.</param>
        public LogTailView(int capacity = 1000, string name = "tail", string? highlight = null) : base(name) {
            if (capacity < 1) { throw new ArgumentOutOfRangeException(nameof(capacity)); }
            _log = new LogRegion { Capacity = capacity, ShowTime = false, ShowTimestamp = true, HighlightText = highlight };
        }

        /// <summary>Whether the viewport follows new entries. Read after dispatch or on the render thread.</summary>
        public bool Following { get; private set; } = true;

        /// <summary>Lines evicted from bounded retained history after application.</summary>
        public long DiscardedHistoryLines => _log.TotalLines - _log.Entries.Count;

        /// <summary>Retained entries, oldest first. Enumerate on the render thread or after stopping the session.</summary>
        public IReadOnlyCollection<LogEntry> Entries => _log.Entries;

        /// <summary>Queues a literal entry through the session. May be called by a producer thread.</summary>
        /// <param name="dispatcher">The owning session or dispatcher.</param>
        /// <param name="entry">The original line and source metadata.</param>
        public void Post(ILiveDispatcher dispatcher, TailedLogEntry entry) {
            if (dispatcher == null) { throw new ArgumentNullException(nameof(dispatcher)); }
            if (entry == null) { throw new ArgumentNullException(nameof(entry)); }
            dispatcher.Post(new LiveEvent(Name, "append") { Payload = entry });
        }

        /// <summary>Queues a recognized navigation key. Quit keys are handled by the runner.</summary>
        /// <param name="dispatcher">The owning session or dispatcher.</param>
        /// <param name="input">An event from a ConsoleInput lease.</param>
        /// <returns>True when the key is a navigation command.</returns>
        public bool Navigate(ILiveDispatcher dispatcher, InputEvent input) {
            if (dispatcher == null) { throw new ArgumentNullException(nameof(dispatcher)); }
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            string? operation = LogTailKeys.Operation(input);
            if (operation == null) { return false; }
            dispatcher.Post(new LiveEvent(Name, operation));
            return true;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "append" && liveEvent.Payload is TailedLogEntry entry) {
                long before = _log.TotalLines;
                _log.WriteTimestamped(entry.Message, entry.Level, entry.Timestamp);
                if (!Following) {
                    _log.ScrollOffset = (int)Math.Min(int.MaxValue, (long)_log.ScrollOffset + _log.TotalLines - before);
                }
                ClampScroll();
                return;
            }
            int maximum = Math.Max(0, _log.Entries.Count - _visibleRows);
            switch (liveEvent.Operation) {
                case "up": Scroll(1, maximum); break;
                case "page-up": Scroll(_visibleRows, maximum); break;
                case "home": Following = false; _log.ScrollOffset = maximum; break;
                case "down": Scroll(-1, maximum); break;
                case "page-down": Scroll(-_visibleRows, maximum); break;
                case "follow": Following = true; _log.ScrollOffset = 0; break;
            }
        }

        private void Scroll(int amount, int maximum) {
            _log.ScrollOffset = (int)Math.Max(0, Math.Min(maximum, (long)_log.ScrollOffset + amount));
            Following = amount < 0 && _log.ScrollOffset == 0;
        }

        private void ClampScroll() => _log.ScrollOffset = Math.Min(_log.ScrollOffset, Math.Max(0, _log.Entries.Count - _visibleRows));

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth,
            constraints.IsHeightUnbounded ? Math.Min(_log.Entries.Count + 2, 20) : constraints.MaxHeight);

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height < 1) { return; }
            int footerRows = region.Height >= 3 ? 2 : 1;
            _visibleRows = Math.Max(1, region.Height - footerRows);
            ClampScroll();
            int logRows = Math.Max(0, region.Height - footerRows);
            _log.TimestampFormat = region.Width < 100 ? "HH:mm:ss" : "yyyy-MM-dd HH:mm:ss.fff zzz";
            if (logRows > 0) { _log.Render(region.Sub(0, 0, region.Width, logRows), context); }
            WidgetLabels labels = context.Labels;
            string state = Following ? labels.TailFollowing : labels.TailPaused;
            string counts = WidgetLabels.Format(labels.TailCounts, DiscardedHistoryLines);
            region.Write(0, logRows, state + " | " + counts, context.Style(ThemeToken.Muted));
            if (footerRows == 2) { region.Write(0, region.Height - 1, labels.TailKeys, context.Style(ThemeToken.Muted)); }
        }
    }
}
