using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The one log-tail model behind LogViewport (apps) and LogTailView (Watch-ConsoleLog): follow and pause, lines that
    /// arrived while paused, scrolling over the filtered history and the state line. LogRegion keeps and draws the lines.
    /// Callers serialize access (the control lock or the live session's render thread).
    /// </summary>
    internal sealed class LogTail {
        private LogFilter? _counted;
        private int _shown;

        internal LogTail(LogRegion region) {
            Region = region ?? throw new ArgumentNullException(nameof(region));
            region.Appended = OnAppended;
            region.Evicted = entry => {
                if (!region.Shows(entry)) { return; }
                _shown--;
                // The oldest rows are gone; keep a paused view on its surviving rows.
                if (!Following) { ClampScroll(); }
            };
            Recount();
        }

        internal LogRegion Region { get; }

        /// <summary>Whether the view sticks to the newest shown line.</summary>
        internal bool Following { get; private set; } = true;

        /// <summary>Shown lines that arrived since following was paused.</summary>
        internal long NewLines { get; private set; }

        /// <summary>Log rows on screen; set by the drawing control so paging and the top of history match what is visible.</summary>
        internal int VisibleRows { get; set; } = 1;

        /// <summary>Shown lines skipped from the newest end.</summary>
        internal int Offset => Region.ScrollOffset;

        internal LogFilter? Filter {
            get => Region.Filter;
            // A different filter shows different lines, so the view returns to the newest matching one.
            set { Region.Filter = value; Recount(); Follow(); }
        }

        private int Shown {
            get {
                // A filter assigned straight on the region (a dashboard log region) is picked up here.
                if (!ReferenceEquals(_counted, Region.Filter)) { Recount(); }
                return _shown;
            }
        }

        private int Top => Math.Max(0, Shown - Math.Max(1, VisibleRows));

        private void Recount() {
            _counted = Region.Filter;
            _shown = 0;
            foreach (LogEntry entry in Region.Entries) { if (Region.Shows(entry)) { _shown++; } }
        }

        private void OnAppended(LogEntry entry) {
            if (!ReferenceEquals(_counted, Region.Filter)) { Recount(); return; }
            if (!Region.Shows(entry)) { return; }
            _shown++;
            if (Following) { return; }
            // Paused: the rows on screen stay put while newer lines arrive below them; every applied line is counted.
            NewLines++;
            Region.ScrollOffset = (int)Math.Min(int.MaxValue, (long)Region.ScrollOffset + 1);
        }

        /// <summary>Applies a navigation operation from <see cref="Operation"/> or the mouse wheel.</summary>
        internal void Navigate(string operation) {
            int page = Math.Max(1, VisibleRows);
            switch (operation) {
                case "up": Scroll(1); break;
                case "down": Scroll(-1); break;
                case "page-up": Scroll(page); break;
                case "page-down": Scroll(-page); break;
                case "home": Pause(); Region.ScrollOffset = Top; break;
                case "follow": Follow(); break;
            }
        }

        /// <summary>Scrolls toward older (positive) or newer (negative) lines. Moving up pauses; reaching the newest line by moving down follows again.</summary>
        internal void Scroll(int amount) {
            Region.ScrollOffset = (int)Math.Max(0, Math.Min(Top, (long)Region.ScrollOffset + amount));
            if (amount > 0) { Pause(); }
            else if (amount < 0 && Region.ScrollOffset == 0) { Follow(); }
        }

        internal void Pause() => Following = false;

        internal void Follow() {
            Following = true;
            NewLines = 0;
            Region.ScrollOffset = 0;
        }

        /// <summary>Sets the offset directly: zero follows, anything else pauses at that position.</summary>
        internal void ScrollTo(int offset) {
            if (offset <= 0) { Follow(); return; }
            Pause();
            Region.ScrollOffset = Math.Min(offset, Top);
        }

        internal void ClampScroll() => Region.ScrollOffset = Math.Min(Region.ScrollOffset, Top);

        /// <summary>Maps the shared navigation keys: arrows, PgUp/PgDn, Home (oldest) and End (follow the newest).</summary>
        internal static string? Operation(InputEvent input) {
            if (input.Is(ConsoleKey.PageUp)) { return "page-up"; }
            if (input.Is(ConsoleKey.UpArrow)) { return "up"; }
            if (input.Is(ConsoleKey.Home)) { return "home"; }
            if (input.Is(ConsoleKey.PageDown)) { return "page-down"; }
            if (input.Is(ConsoleKey.DownArrow)) { return "down"; }
            if (input.Is(ConsoleKey.End)) { return "follow"; }
            return null;
        }

        /// <summary>
        /// Draws the state line, then the filter and discarded history when present. A full-screen tail says
        /// <c>Following latest</c> or <c>⏸ Paused · viewing history · 3 new lines</c>; an embedded panel stays quiet while
        /// following and says <c>⏸ Paused · 3 new</c>.
        /// </summary>
        /// <param name="region">The target.</param>
        /// <param name="y">The row.</param>
        /// <param name="context">The render context.</param>
        /// <param name="fullScreen">Whether this is a full-screen tail rather than a panel inside an app.</param>
        internal void RenderState(Region region, int y, RenderContext context, bool fullScreen) {
            WidgetLabels labels = context.Labels;
            string separator = " " + context.Glyphs.Get(Glyphs.Glyph.Dot) + " ";
            int x = 0;
            if (!Following || fullScreen) {
                string state = Following ? labels.TailFollowing : fullScreen ? labels.TailPaused : labels.TailPausedShort;
                if (!Following && context.Glyphs.Tier != GlyphTier.Ascii) { state = labels.TailPauseMarker + " " + state; }
                if (!Following && NewLines > 0) { state += separator + WidgetLabels.Format(fullScreen ? labels.TailNewLines : labels.TailNewShort, NewLines); }
                x = region.Write(x, y, state, context.Style(Following ? ThemeToken.Muted : ThemeToken.Warn));
            }
            LogFilter? filter = Region.Filter;
            if (filter != null && !filter.IsEmpty) {
                x = region.Write(x, y, (x > 0 ? separator : string.Empty) + labels.TailFiltered + " ", context.Style(ThemeToken.Muted));
                if (filter.Tag != null) { x = region.Write(x, y, filter.Tag + " ", LogRegion.SourceStyle(filter.Tag, context)); }
                if (filter.Level != null) { x = region.Write(x, y, LevelText(filter.Level, context) + " ", LogTags.Style(filter.Level, context)); }
                if (filter.Text != null) { x = region.Write(x, y, "\"" + filter.Text + "\"", context.Style(ThemeToken.Text)); }
            }
            long discarded = Region.TotalLines - Region.Entries.Count;
            if (fullScreen && discarded > 0) {
                region.Write(x, y, separator + WidgetLabels.Format(labels.TailCounts, discarded), context.Style(ThemeToken.Muted));
            }
        }

        /// <summary>Whether the state line has anything to say when an embedded panel stays quiet while following.</summary>
        internal bool HasState => !Following || (Region.Filter != null && !Region.Filter.IsEmpty);

        // "WARN+" reads as "warnings and worse"; info levels carry no tag word of their own.
        private static string LevelText(string level, RenderContext context) {
            string tag = LogTags.Tag(level, context);
            return (tag.Length == 0 ? level.ToUpperInvariant() : tag) + "+";
        }
    }
}
