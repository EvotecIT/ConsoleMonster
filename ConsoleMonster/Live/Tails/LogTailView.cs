using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live.Tails {
    /// <summary>
    /// A full-screen log tail with queued appends and navigation, as Watch-ConsoleLog shows it. It shares its follow, pause and
    /// filter model with the app <see cref="Interactive.LogViewport"/>; LogRegion owns storage and line rendering.
    /// </summary>
    public sealed class LogTailView : LiveWidget {
        private readonly LogTail _tail;

        /// <summary>Creates a view that initially follows the newest entry.</summary>
        /// <param name="capacity">Maximum retained history entries. Pending updates are not discarded.</param>
        /// <param name="widgetId">Unique live-session target name.</param>
        /// <param name="highlight">Optional literal, ordinal case-insensitive substring to emphasize.</param>
        public LogTailView(int capacity = 1000, string widgetId = "tail", string? highlight = null) : base(widgetId) {
            if (capacity < 1) { throw new ArgumentOutOfRangeException(nameof(capacity)); }
            _tail = new LogTail(new LogRegion { Capacity = capacity, ShowElapsed = false, ShowTimestamp = true, HighlightText = highlight });
        }

        /// <summary>Whether the viewport follows new entries. Read after dispatch or on the render thread.</summary>
        public bool Following => _tail.Following;

        /// <summary>Invariant source timestamp format, or null for time only, adding dates when retained source dates differ.
        /// The format and timestamp column are independent of terminal width; missing timestamps stay blank.</summary>
        public string? TimestampFormat { get; set; }

        /// <summary>Lines evicted from bounded retained history after application.</summary>
        public long DiscardedHistoryLines => _tail.Region.TotalLines - _tail.Region.Entries.Count;

        /// <summary>Retained entries, oldest first. Enumerate on the render thread or after stopping the session.</summary>
        public IReadOnlyCollection<LogEntry> Entries => _tail.Region.Entries;

        /// <summary>Queues a literal entry through the session. May be called by a producer thread.</summary>
        /// <param name="dispatcher">The owning session or dispatcher.</param>
        /// <param name="entry">The original line and source metadata.</param>
        public void Post(ILiveDispatcher dispatcher, TailedLogEntry entry) {
            if (dispatcher == null) { throw new ArgumentNullException(nameof(dispatcher)); }
            if (entry == null) { throw new ArgumentNullException(nameof(entry)); }
            dispatcher.Post(new LiveEvent(WidgetId, "append") { Payload = entry });
        }

        /// <summary>Queues a recognized navigation key. Quit keys are handled by the runner.</summary>
        /// <param name="dispatcher">The owning session or dispatcher.</param>
        /// <param name="input">An event from a ConsoleInput lease.</param>
        /// <returns>True when the key is a navigation command.</returns>
        public bool Navigate(ILiveDispatcher dispatcher, InputEvent input) {
            if (dispatcher == null) { throw new ArgumentNullException(nameof(dispatcher)); }
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            string? operation = LogTail.Operation(input);
            if (operation == null) { return false; }
            dispatcher.Post(new LiveEvent(WidgetId, operation));
            return true;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "append" && liveEvent.Payload is TailedLogEntry entry) {
                _tail.Region.WriteLiteral(entry.Message, entry.Level, TimeSpan.Zero, entry.Timestamp, entry.Tag);
                return;
            }
            _tail.Navigate(liveEvent.Operation);
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth,
            constraints.IsHeightUnbounded ? Math.Min(_tail.Region.Entries.Count + 2, 20) : constraints.MaxHeight);

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height < 1) { return; }
            int footerRows = region.Height >= 3 ? 2 : 1;
            int logRows = Math.Max(0, region.Height - footerRows);
            _tail.VisibleRows = Math.Max(1, logRows);
            _tail.ClampScroll();
            _tail.Region.TimestampFormat = TimestampFormat ?? DefaultTimestampFormat();
            if (logRows > 0) { _tail.Region.Render(region.Sub(0, 0, region.Width, logRows), context); }
            _tail.RenderState(region, logRows, context, true);
            if (footerRows == 2) {
                region.Write(0, region.Height - 1, RichText.FromMarkup(context.Labels.TailKeys).ToStyledText(context).Lines[0]);
            }
        }

        private string DefaultTimestampFormat() {
            DateTime? date = null;
            foreach (LogEntry entry in _tail.Region.Entries) {
                if (!entry.Timestamp.HasValue) { continue; }
                DateTime sourceDate = entry.Timestamp.Value.Date;
                if (date.HasValue && date.Value != sourceDate) { return "yyyy-MM-dd HH:mm:ss"; }
                date = sourceDate;
            }
            return "HH:mm:ss";
        }
    }
}
