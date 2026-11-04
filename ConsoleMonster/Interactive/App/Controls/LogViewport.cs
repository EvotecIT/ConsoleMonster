using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
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
        private int _contentWidth;
        private GlyphTier _glyphTier;
        private int _stateRows = -1;
        private readonly FindBar _find = new FindBar();

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
        /// surrounding panel; the viewport retains a small focus gutter without a label.
        /// </summary>
        public BoxStyle Border { get => Read(() => FrameBorder); set => Change(() => FrameBorder = value); }

        /// <summary>Whether to show entry elapsed time.</summary>
        public bool ShowElapsed { get => Read(() => _tail.Region.ShowElapsed); set => Change(() => _tail.Region.ShowElapsed = value); }

        /// <summary>Which entries are shown, or null for all. Hidden entries stay in the history and return when the filter changes.</summary>
        public LogFilter? Filter { get => Read(() => _tail.Filter); set => ChangeLog(() => _tail.Filter = value); }

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
        public void Write(string text, string level = "info", TimeSpan time = default, string? tag = null) => ChangeLog(() => {
            text = text ?? string.Empty;
            if (text.Length > 16384) { text = text.Substring(0, 16384); }
            _tail.Region.WriteLiteral(text, level, time, null, tag);
        });

        string ILiveTarget.WidgetId => Key;

        void ILiveTarget.Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "write") { ChangeLog(() => _tail.Region.Write(liveEvent)); }
        }

        string? ILiveTarget.Describe(LiveEvent liveEvent) => _tail.Region.Describe(liveEvent);

        /// <inheritdoc />
        protected override object ReadValueCore() => _tail.Offset;

        /// <inheritdoc />
        protected override bool HandleInputCore(InputEvent input, RenderContext context) {
            if (_find.Open) {
                switch (_find.Handle(input)) {
                    case FindKey.Edited: _find.Search(Lines(), FirstShown()); Reveal(); return true;
                    case FindKey.Next: if (_find.Step(1)) { Reveal(); } return true;
                    case FindKey.Previous: if (_find.Step(-1)) { Reveal(); } return true;
                    case FindKey.Closed: _find.Close(true); return true;
                    case FindKey.Kept: return true;
                }
            } else if (input.Is(ConsoleKey.F, KeyModifiers.Control) || (input.Kind == InputEventKind.Key && input.Char == '/' && (input.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0)) {
                _find.Start();
                _find.Search(Lines(), FirstShown());
                return true;
            } else if (_find.Active) {
                // With the bar closed, n and N keep moving through the matches and Escape clears them.
                if (input.Kind == InputEventKind.Key && input.Char == 'n' && input.Modifiers == KeyModifiers.None) { if (_find.Step(1)) { Reveal(); } return true; }
                if (input.Kind == InputEventKind.Key && input.Char == 'N' && (input.Modifiers & ~KeyModifiers.Shift) == KeyModifiers.None) { if (_find.Step(-1)) { Reveal(); } return true; }
                if (input.Is(ConsoleKey.Escape)) { _find.Close(false); return true; }
            }
            string? operation = LogTail.Operation(input);
            if (operation != null) { _tail.Navigate(operation); }
            else if (input.Is(ConsoleKey.LeftArrow)) { _horizontalOffset = Math.Max(0, _horizontalOffset - 4); }
            else if (input.Is(ConsoleKey.RightArrow)) { _horizontalOffset = Math.Min(32768, _horizontalOffset + 4); }
            else { return false; }
            return true;
        }

        // The app footer lists the keys this panel handles itself: history always, and the way back to the newest line while paused.
        internal override void AddOwnKeys(List<KeyBinding> keys, RenderContext context) {
            PromptLabels prompt = PromptLabels.For(context.Profile.Language);
            if (Read(() => _find.Open)) { FindBar.AddKeys(keys, prompt); return; }
            keys.Add(new KeyBinding(prompt.KeySearch, () => { }, new KeyChord('/')) { Order = 70 });
            if (Read(() => _find.Active)) {
                keys.Add(new KeyBinding(prompt.KeyNext, () => { }, new KeyChord('n')) { Order = 4 });
                keys.Add(new KeyBinding(prompt.KeyPrevious, () => { }, new KeyChord('N')) { Order = 5 });
            }
            Localization.WidgetLabels labels = context.Labels;
            keys.Add(new KeyBinding(labels.TailKeyHistory, () => { }, new KeyChord(ConsoleKey.PageUp)) { Order = 1 });
            if (!Read(() => _tail.Following)) {
                keys.Add(new KeyBinding(labels.TailKeyFollow, () => { }, new KeyChord(ConsoleKey.End)) { Order = 2 });
            }
        }

        internal override bool Takes(KeyChord chord, RenderContext context) => (Read(() => _find.Open) && FindBar.Takes(chord))
            || chord.Equals(new KeyChord('/')) || chord.Equals(new KeyChord(ConsoleKey.F, KeyModifiers.Control))
            || (Read(() => _find.Active) && (chord.Equals(new KeyChord('n')) || chord.Equals(new KeyChord('N')) || chord.Equals(new KeyChord(ConsoleKey.Escape))))
            || chord.Modifiers == KeyModifiers.None && (chord.Key == ConsoleKey.UpArrow || chord.Key == ConsoleKey.DownArrow
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
            _contentWidth = region.Width;
            _glyphTier = context.Glyphs.Tier;
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
            if (_horizontalOffset == 0) { _tail.Region.Render(region.Sub(0, 0, region.Width, rows), context); }
            else {
                var content = new CellBuffer(region.Width + _horizontalOffset, rows);
                _tail.Region.Render(new Region(content), context);
                region.Buffer.Blit(content, _horizontalOffset, 0, region.Width, rows, region.Bounds.X, region.Bounds.Y);
            }
            if (_find.Active) {
                int first = FirstShown();
                List<LogEntry> entries = _tail.Region.Entries.Where(entry => _tail.Region.Shows(entry)).ToList();
                for (int i = 0; i < _find.Matches.Count; i++) {
                    FindBar.Match match = _find.Matches[i];
                    int y = match.Line - first;
                    if (y < 0 || y >= rows) { continue; }
                    LogEntry entry = entries[match.Line];
                    int from = MatchColumn(entry, match.Start) - _horizontalOffset;
                    int to = MatchColumn(entry, match.Start + match.Length) - _horizontalOffset;
                    _find.HighlightCells(region, y, from, to, i == _find.Current, context);
                }
            }
            if (_find.Open) { InputArrangement.RecordBar(this, _find.Widget(PromptLabels.For(context.Profile.Language))); }
        }

        // A click outside the find bar closes it; the matches stay highlighted for n and N.
        internal override void ClosePopupCore() { if (_find.Open) { _find.Close(true); } }

        // The shown (filtered) lines, oldest first, as the find bar searches them.
        private List<string> Lines() {
            List<string> lines = new List<string>();
            foreach (LogEntry entry in _tail.Region.Entries) { if (_tail.Region.Shows(entry)) { lines.Add(entry.Text); } }
            return lines;
        }

        // Appends, capacity eviction and filtering change the search document even when the query stays the same.
        private void ChangeLog(Action edit) => Change(() => {
            LogEntry? anchor = null;
            int column = 0;
            if (_find.Active && _find.Current >= 0) {
                FindBar.Match current = _find.Matches[_find.Current];
                anchor = _tail.Region.Entries.Where(entry => _tail.Region.Shows(entry)).ElementAtOrDefault(current.Line);
                column = current.Start;
            }
            edit();
            if (!_find.Active) { return; }
            List<string> lines = new List<string>();
            int line = -1;
            foreach (LogEntry entry in _tail.Region.Entries) {
                if (!_tail.Region.Shows(entry)) { continue; }
                if (ReferenceEquals(entry, anchor)) { line = lines.Count; }
                lines.Add(entry.Text);
            }
            _find.Search(lines, line >= 0 ? line : FirstShown(), line >= 0 ? column : 0);
        });

        // The shown line drawn on the first visible row.
        private int FirstShown() {
            int shown = 0;
            foreach (LogEntry entry in _tail.Region.Entries) { if (_tail.Region.Shows(entry)) { shown++; } }
            return Math.Max(0, shown - _tail.Offset - Math.Max(1, _tail.VisibleRows));
        }

        // Scrolls the current match into view, about a third of the way down the rows.
        private void Reveal() {
            if (_find.Current < 0) { return; }
            FindBar.Match match = _find.Matches[_find.Current];
            int line = match.Line;
            LogEntry entry = _tail.Region.Entries.Where(item => _tail.Region.Shows(item)).ElementAt(line);
            int from = MatchColumn(entry, match.Start), to = MatchColumn(entry, match.Start + match.Length);
            if (from < _horizontalOffset) { _horizontalOffset = from; }
            else if (_contentWidth > 0 && to >= _horizontalOffset + _contentWidth) {
                // Reserve the last cell for the log renderer's truncation marker.
                _horizontalOffset = Math.Max(0, to - _contentWidth + 1);
            }
            int rows = Math.Max(1, _tail.VisibleRows);
            int first = FirstShown();
            if (line >= first && line < first + rows) { return; }
            int shown = Lines().Count;
            int top = Math.Max(0, Math.Min(Math.Max(0, shown - rows), line - rows / 3));
            _tail.ScrollTo(shown - rows - top);
        }

        private int MatchColumn(LogEntry entry, int offset) => _tail.Region.MessageColumn(entry)
            + Text.DisplayWidth.Measure(SymbolFallback.Constrain(entry.Text.Substring(0, offset), _glyphTier));
    }
}
