using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A focusable, capped log tail for apps. Entries carry a level and an optional source tag; <see cref="Filter"/> shows one
    /// tag, a minimum level or matching text. The view follows new lines until the user scrolls up or calls
    /// <see cref="Pause"/>; a paused view keeps its rows still and says how many lines arrived, and End or
    /// <see cref="Follow"/> jumps back to the newest line. Writers never accumulate an unbounded UI queue. Inside a running
    /// app the viewport also takes live <c>write</c> events addressed to its key (Text, Status = level, Key = tag), which is
    /// how Write-ConsoleLogEntry reaches it.
    /// </summary>
    public sealed class LogViewport : InputControl, ILiveTarget {
        private readonly LogTail _tail;
        private int _capacity = 1000;
        private int _height = 8;
        private int _horizontalOffset;
        private int _stateRows = -1;

        /// <summary>Creates a named log viewport.</summary>
        public LogViewport(string key) : base(key) {
            _tail = new LogTail(new LogRegion(key)) { VisibleRows = _height };
        }

        /// <summary>Maximum retained entries.</summary>
        public int Capacity {
            get => _capacity;
            set { if (value < 1 || value > 100000) { throw new ArgumentOutOfRangeException(nameof(value)); } Change(() => { _capacity = value; _tail.Region.Capacity = value; }); }
        }

        /// <summary>Preferred visible content rows.</summary>
        public int Height {
            get => _height;
            set => Change(() => { _height = value; _tail.VisibleRows = Math.Max(1, value); });
        }

        /// <summary>
        /// The frame drawn around the log with its label and focus marker. <see cref="BoxStyle.None"/> leaves framing to a
        /// surrounding panel; the viewport then draws no label or focus marker of its own.
        /// </summary>
        public BoxStyle Border { get => Read(() => FrameBorder); set => Change(() => FrameBorder = value); }

        /// <summary>Whether to show entry elapsed time.</summary>
        public bool ShowElapsed { get => Read(() => _tail.Region.ShowElapsed); set => Change(() => _tail.Region.ShowElapsed = value); }

        /// <summary>Which entries are shown, or null for all. Hidden entries stay in the history and return when the filter changes.</summary>
        public LogFilter? Filter { get => Read(() => _tail.Filter); set => Change(() => _tail.Filter = value); }

        /// <summary>Whether the view follows the newest shown line.</summary>
        public bool Following => Read(() => _tail.Following);

        /// <summary>Shown lines that arrived since following was paused.</summary>
        public long NewLines => Read(() => _tail.NewLines);

        /// <summary>Shown rows skipped from the newest end. Zero follows new output; a larger value pauses at that position.</summary>
        public int ScrollOffset { get => Read(() => _tail.Offset); set => Change(() => _tail.ScrollTo(value)); }

        /// <summary>Horizontal scroll in display cells, bounded by the maximum retained message length.</summary>
        public int HorizontalOffset { get => Read(() => _horizontalOffset); set => Change(() => _horizontalOffset = Math.Max(0, Math.Min(32768, value))); }

        /// <summary>Stops following: the rows on screen stay while new lines are counted.</summary>
        public void Pause() => Change(_tail.Pause);

        /// <summary>Follows the newest shown line again and clears the new-line count.</summary>
        public void Follow() => Change(_tail.Follow);

        /// <summary>Returns retained literal messages for an app's copy or export action.</summary>
        public string CopyText() => Read(() => string.Join(System.Environment.NewLine, _tail.Region.Entries.Select(entry => entry.Text)));

        /// <summary>A detached entry snapshot.</summary>
        public IReadOnlyList<LogEntry> Entries => Read(() => (IReadOnlyList<LogEntry>)new List<LogEntry>(_tail.Region.Entries));

        /// <summary>Appends bounded literal text from any thread. Markup is never interpreted.</summary>
        /// <param name="text">The message; line breaks create several entries.</param>
        /// <param name="level">The level: info, warn, err, verbose, debug, pass, skip or a PowerShell stream word.</param>
        /// <param name="time">Session time shown when <see cref="ShowElapsed"/> is on.</param>
        /// <param name="tag">The source (a job, component or item name), or null.</param>
        public void Write(string text, string level = "info", TimeSpan time = default, string? tag = null) => Change(() => {
            text = text ?? string.Empty;
            if (text.Length > 16384) { text = text.Substring(0, 16384); }
            _tail.Region.WriteLiteral(text, level, time, null, tag);
        });

        string ILiveTarget.WidgetId => Key;

        void ILiveTarget.Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "write") { Change(() => _tail.Region.Write(liveEvent)); }
        }

        string? ILiveTarget.Describe(LiveEvent liveEvent) => _tail.Region.Describe(liveEvent);

        /// <inheritdoc />
        protected override object ReadValueCore() => _tail.Offset;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            string? operation = LogTail.Operation(input);
            if (operation != null) { _tail.Navigate(operation); }
            else if (input.Is(ConsoleKey.LeftArrow)) { _horizontalOffset = Math.Max(0, _horizontalOffset - 4); }
            else if (input.Is(ConsoleKey.RightArrow)) { _horizontalOffset = Math.Min(32768, _horizontalOffset + 4); }
            else { return false; }
            return true;
        }

        // The app footer lists the keys this panel handles itself: history always, and the way back to the newest line while paused.
        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            Localization.WidgetLabels labels = context.Labels;
            keys.Add(new KeyBinding(labels.TailKeyHistory, () => { }, new KeyChord(ConsoleKey.PageUp)) { Order = 1 });
            if (!Read(() => _tail.Following)) {
                keys.Add(new KeyBinding(labels.TailKeyFollow, () => { }, new KeyChord(ConsoleKey.End)) { Order = 2 });
            }
        }

        internal override bool Takes(KeyChord chord, RenderContext context) => chord.Modifiers == KeyModifiers.None && (chord.Key == ConsoleKey.UpArrow || chord.Key == ConsoleKey.DownArrow
            || chord.Key == ConsoleKey.PageUp || chord.Key == ConsoleKey.PageDown || chord.Key == ConsoleKey.Home || chord.Key == ConsoleKey.End
            || chord.Key == ConsoleKey.LeftArrow || chord.Key == ConsoleKey.RightArrow);

        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            if (input.MouseAction != MouseAction.Wheel) { return false; }
            _tail.Scroll(Math.Sign(input.WheelDelta) * 3);
            return true;
        }

        /// <inheritdoc />
        protected override Size MeasureContent(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(Math.Max(1, Height), constraints.MaxHeight));

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) {
            // The state line takes the last row only while there is something to say: paused or filtered.
            int stateRows = _tail.HasState && region.Height >= 2 ? 1 : 0;
            // Pausing adds the state line under the log; move the window by one so the rows on screen stay where they were.
            if (_stateRows == 0 && stateRows == 1 && !_tail.Following) { _tail.Region.ScrollOffset += 1; }
            _stateRows = stateRows;
            int rows = region.Height - stateRows;
            _tail.VisibleRows = Math.Max(1, rows);
            _tail.ClampScroll();
            if (stateRows > 0) { _tail.RenderState(region, rows, context, false); }
            if (rows <= 0) { return; }
            if (_horizontalOffset == 0) { _tail.Region.Render(region.Sub(0, 0, region.Width, rows), context); return; }
            var content = new CellBuffer(region.Width + _horizontalOffset, rows);
            _tail.Region.Render(new Region(content), context);
            region.Buffer.Blit(content, _horizontalOffset, 0, region.Width, rows, region.Bounds.X, region.Bounds.Y);
        }
    }
}
