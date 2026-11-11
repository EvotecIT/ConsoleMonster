using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A step strip for pipelines: <c>✓ Restore 2.1s  ›  ✓ Build 12s  ›  ⠹ Test 64%  ›  ○ Pack</c>. The active phase is
    /// emphasised; phases flow onto more lines when the width is short. A phase can carry items (<see cref="SetItem"/>);
    /// the running phase fans out to them under the strip (<see cref="FanOut"/>). <see cref="Compact"/> draws one line that
    /// folds to fit, and <see cref="StepCounter"/> a numbered ledger. Live operations (Key = phase): <c>add</c> (Text =
    /// label; for an existing phase it changes only the label), <c>insert</c> (Text = label, Value = position),
    /// <c>state</c> (State, Text = detail), <c>detail</c> (Text); for items (Key = phase, U+001F, item) and their
    /// sub-items (Key = phase, U+001F, item, U+001F, sub-item): <c>item</c> (State, Text = label) and <c>item-detail</c>
    /// (Text). A <c>state</c> or <c>item</c> event that finishes a row may give the time it took in milliseconds as Value;
    /// otherwise the time is measured from the event that started it.
    /// </summary>
    public sealed partial class Phases : LiveWidget {
        private readonly List<StatusItem> _phases = new List<StatusItem>();
        private readonly Dictionary<string, StatusItem> _byKey = new Dictionary<string, StatusItem>(StringComparer.Ordinal);
        private readonly Dictionary<string, TimeSpan> _started = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

        /// <summary>Creates a phases widget.</summary>
        /// <param name="widgetId">The live name (default <c>phases</c>).</param>
        public Phases(string widgetId = "phases") : base(widgetId) {
        }

        /// <summary>The phases in order.</summary>
        public IReadOnlyList<StatusItem> Items => _phases;

        /// <summary>Adds a phase or updates its status.</summary>
        /// <param name="key">Unique key (also the label when <paramref name="label"/> is null).</param>
        /// <param name="status">Status.</param>
        /// <param name="label">Label (markup), or <c>null</c>.</param>
        /// <param name="detail">Detail (duration, percentage), or <c>null</c>.</param>
        /// <returns>This instance.</returns>
        public Phases Set(string key, StatusKind status = StatusKind.Pending, string? label = null, string? detail = null) {
            if (!_byKey.TryGetValue(key, out StatusItem? item)) {
                item = new StatusItem(key, string.IsNullOrEmpty(label) ? Markup.Escape(key) : label!, status, string.IsNullOrEmpty(detail) ? null : detail);
                _byKey[key] = item;
                _phases.Add(item);
            } else {
                item.Status = status;
                // Null or empty keeps the label (PowerShell passes $null to a .NET string as empty).
                if (!string.IsNullOrEmpty(label)) {
                    item.Label = label!;
                }
                if (!string.IsNullOrEmpty(detail)) {
                    item.Detail = detail;
                }
            }
            Advance(item);
            return this;
        }

        /// <summary>
        /// Adds a phase at a position, for work planned after the run started that belongs between existing phases. A
        /// phase that already exists keeps its place and is updated as <see cref="Set"/> does. Live operation:
        /// <c>insert</c> (Key = phase, Text = label, Value = position).
        /// </summary>
        /// <param name="index">The zero-based position; a position past the end appends.</param>
        /// <param name="key">Unique key (also the label when <paramref name="label"/> is null).</param>
        /// <param name="status">Status.</param>
        /// <param name="label">Label (markup), or <c>null</c>.</param>
        /// <param name="detail">Detail, or <c>null</c>.</param>
        /// <returns>This instance.</returns>
        public Phases Insert(int index, string key, StatusKind status = StatusKind.Pending, string? label = null, string? detail = null) {
            if (index < 0) { throw new ArgumentOutOfRangeException(nameof(index)); }
            if (_byKey.ContainsKey(key)) { return Set(key, status, label, detail); }
            var item = new StatusItem(key, string.IsNullOrEmpty(label) ? Markup.Escape(key) : label!, status, string.IsNullOrEmpty(detail) ? null : detail);
            index = Math.Min(index, _phases.Count);
            _byKey[key] = item;
            _phases.Insert(index, item);
            if (index <= _frontier) { _frontier++; }
            Advance(item);
            return this;
        }

        /// <summary>Returns a handle that updates the phases through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public PhasesHandle Handle(ILiveDispatcher dispatcher) => new PhasesHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Key == null) {
                return;
            }
            if ((liveEvent.Operation == "item" || liveEvent.Operation == "item-detail") && ApplyItem(liveEvent)) {
                return;
            }
            switch (liveEvent.Operation) {
                case "add":
                    // Adding a phase that exists changes only its label, so a running step can be retitled.
                    if (_byKey.TryGetValue(liveEvent.Key, out StatusItem? existing)) {
                        if (!string.IsNullOrEmpty(liveEvent.Text)) { existing.Label = liveEvent.Text!; }
                    } else {
                        Set(liveEvent.Key, StatusKind.Pending, liveEvent.Text);
                    }
                    break;
                case "insert":
                    Insert((int)Math.Max(0, Math.Min(int.MaxValue, liveEvent.Value)), liveEvent.Key, StatusKind.Pending, liveEvent.Text);
                    break;
                case "state":
                    StatusKinds.TryParse(liveEvent.Status, out StatusKind status);
                    string? detail = liveEvent.Text;
                    TimeSpan? took = Track(liveEvent.Key, status, liveEvent);
                    if (detail == null && took.HasValue) {
                        // Without a detail the phase shows the time it took, as it always has.
                        detail = NumberFormat.Duration(took.Value);
                        _took.Remove(liveEvent.Key);
                    }
                    Set(liveEvent.Key, status, null, detail);
                    break;
                case "detail":
                    if (_byKey.TryGetValue(liveEvent.Key, out StatusItem? item)) {
                        item.Detail = liveEvent.Text;
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent.Operation == "state" && liveEvent.Key != null && _byKey.TryGetValue(liveEvent.Key, out StatusItem? item)) {
                return Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.Phase, Markup.Strip(item.Label)) + " " + item.Status.ToWord() + (string.IsNullOrEmpty(item.Detail) ? string.Empty : " (" + item.Detail + ")");
            }
            if (liveEvent.Operation == "item" && liveEvent.Key != null && liveEvent.Status != null) {
                string[] path = liveEvent.Key.Split(ItemSeparator);
                if (path.Length >= 2 && _byKey.TryGetValue(path[0], out StatusItem? phase) && FindItem(path) is StatusItem child) {
                    string parent = path.Length == 3 && FindItem(new[] { path[0], path[1] }) is StatusItem owner ? Markup.Strip(owner.Label) + ": " : string.Empty;
                    return Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.Phase, Markup.Strip(phase.Label)) + ": " + parent + Markup.Strip(child.Label) + " " + child.Status.ToWord();
                }
            }
            return null;
        }

        /// <summary>
        /// Draws a numbered ledger with hanging labels and right-aligned details, placing a detail below its label when they
        /// cannot share a row. A running phase spins and shows its latest detail with the time it has been running (from
        /// the session clock); a finished phase keeps the duration it took (from its <c>state</c> events). A phase's items
        /// (<see cref="SetItem"/>) are listed under it. When the ledger is taller than its space (an inline live area),
        /// finished phases fold their items into a "done/total" count and running phases keep only their running and
        /// problem items; if that is still too tall, it shows whole phases around the furthest phase that has started, with
        /// the next phase below it, every running phase above pinned under "+N earlier", and "+N more" for the rest. The
        /// window only moves forward. An unbounded render (the final printed state) lists every item.
        /// </summary>
        public bool StepCounter { get; set; }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            if (StepCounter) return LedgerMeasure(constraints, context);
            if (Compact) return new Size(Math.Min(constraints.MaxWidth, ChainLine(constraints.MaxWidth, context).Width), Math.Min(1, constraints.MaxHeight));
            var strip = StripLayout(constraints.MaxWidth, context);
            int lines = Flow(constraints.MaxWidth, context, strip.Separator, strip.DetailsBelow).Count
                + (strip.DetailsBelow ? 1 : 0);
            List<StatusItem>? items = FanOutItems(out _);
            if (items != null) { lines += Math.Min(items.Count, _fanOut); }
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? lines : Math.Min(lines, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (StepCounter) { LedgerRender(region, context); return; }
            if (Compact) { if (region.Height > 0) { region.Write(0, 0, ChainLine(region.Width, context)); } return; }
            var strip = StripLayout(region.Width, context);
            List<List<StyledLine>> lines = Flow(region.Width, context, strip.Separator, strip.DetailsBelow);
            List<StatusItem>? items = FanOutItems(out int fanOutPhase);
            string separator = strip.Separator;
            Style sepStyle = context.Style(ThemeToken.Subtle);
            int phase = 0, fanOutX = 0;
            for (int row = 0; row < lines.Count && row < region.Height; row++) {
                int x = 0;
                for (int i = 0; i < lines[row].Count; i++, phase++) {
                    if (i > 0) {
                        x = region.Write(x, row, separator, context.HasColor ? context.Style(ThemeToken.Muted) : sepStyle);
                    }
                    // Fan-out rows hang under the marker of the phase they belong to.
                    if (phase == fanOutPhase) { fanOutX = x; }
                    if (strip.DetailsBelow && !string.IsNullOrEmpty(_phases[phase].Detail) && region.Height > 1) {
                        region.Write(x, 1, _phases[phase].Detail!, context.Style(ThemeToken.Muted));
                    }
                    x = region.Write(x, row, lines[row][i]);
                }
            }
            if (items == null) { return; }
            int indent = Math.Max(0, Math.Min(fanOutX, region.Width - 16));
            List<StyledLine> fan = FanOutLines(items, Math.Max(1, region.Width - indent), context);
            int fanStart = lines.Count + (strip.DetailsBelow ? 1 : 0);
            for (int i = 0; i < fan.Count && fanStart + i < region.Height; i++) {
                region.Write(indent, fanStart + i, fan[i]);
            }
        }

        // Keep every phase and duration: a wide strip puts long details on a second aligned row before wrapping names.
        private (string Separator, bool DetailsBelow) StripLayout(int width, RenderContext context) {
            string separator = " " + context.Glyphs.Get(Glyph.Chevron) + " ";
            int inline = 0, stacked = 0;
            bool details = false;
            foreach (StatusItem phase in _phases) {
                StyledLine label = Chip(phase, context, includeDetail: false);
                inline += Chip(phase, context).Width;
                stacked += Math.Max(label.Width, DisplayWidth.Measure(phase.Detail ?? string.Empty));
                details |= !string.IsNullOrEmpty(phase.Detail);
            }
            int gaps = Math.Max(0, _phases.Count - 1);
            if (inline + gaps * DisplayWidth.Measure(separator) <= width) return (separator, false);
            if (stacked + gaps * DisplayWidth.Measure(separator) <= width) return (separator, details);
            if (stacked + gaps <= width) return (" ", details);
            return (separator, false);
        }

        private List<List<StyledLine>> Flow(int width, RenderContext context, string separator, bool detailsBelow) {
            int separatorWidth = DisplayWidth.Measure(separator);
            List<List<StyledLine>> lines = new List<List<StyledLine>>();
            List<StyledLine> current = new List<StyledLine>();
            int used = 0;
            foreach (StatusItem phase in _phases) {
                StyledLine chip = Chip(phase, context, includeDetail: !detailsBelow);
                if (detailsBelow) {
                    int padding = DisplayWidth.Measure(phase.Detail ?? string.Empty) - chip.Width;
                    if (padding > 0) {
                        List<TextSpan> spans = new List<TextSpan>(chip) { new TextSpan(new string(' ', padding)) };
                        chip = new StyledLine(spans);
                    }
                }
                int needed = (current.Count > 0 ? separatorWidth : 0) + chip.Width;
                if (current.Count > 0 && used + needed > width) {
                    lines.Add(current);
                    current = new List<StyledLine>();
                    used = 0;
                    needed = chip.Width;
                }
                current.Add(chip);
                used += needed;
            }
            if (current.Count > 0 || lines.Count == 0) {
                lines.Add(current);
            }
            return lines;
        }

        // Only phase chips animate; fan-out items use the still running marker, so a spinner frame never reads as a line into the name.
        private static StyledLine Chip(StatusItem phase, RenderContext context, bool animate = true, bool includeDetail = true) {
            IGlyphSet glyphs = context.Glyphs;
            string marker = animate && phase.Status == StatusKind.Running && glyphs.Tier != GlyphTier.Ascii
                ? context.Spinner()
                : glyphs.StatusMarker(phase.Status);
            List<TextSpan> spans = new List<TextSpan> {
                new TextSpan(marker, context.Style(phase.Status)),
                new TextSpan(" ")
            };
            Style labelStyle;
            if (phase.Status == StatusKind.Running) {
                labelStyle = context.Style(ThemeToken.Accent).With(TextDecoration.Bold);
            } else if (phase.Status == StatusKind.Pending) {
                labelStyle = context.Style(ThemeToken.Muted);
            } else {
                labelStyle = context.Style(ThemeToken.Text);
            }
            foreach (TextSpan span in context.Markup(phase.Label).Lines[0]) {
                spans.Add(new TextSpan(span.Text, labelStyle.Combine(span.Style)));
            }
            if (includeDetail && !string.IsNullOrEmpty(phase.Detail)) {
                spans.Add(new TextSpan(" " + phase.Detail, context.Style(ThemeToken.Muted)));
            }
            return new StyledLine(spans);
        }
    }
}
