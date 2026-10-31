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
    /// label), <c>state</c> (State, Text = detail), <c>detail</c> (Text); for items (Key = phase, U+001F, item):
    /// <c>item</c> (State, Text = label) and <c>item-detail</c> (Text).
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
                    Set(liveEvent.Key, StatusKind.Pending, liveEvent.Text);
                    break;
                case "state":
                    StatusKinds.TryParse(liveEvent.Status, out StatusKind status);
                    TimeSpan now = TimeSpan.FromMilliseconds(liveEvent.Time);
                    string? detail = liveEvent.Text;
                    if (status == StatusKind.Running) {
                        _started[liveEvent.Key] = now;
                    } else if (detail == null && status != StatusKind.Pending && _started.TryGetValue(liveEvent.Key, out TimeSpan started)) {
                        detail = NumberFormat.Duration(now - started);
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
                int split = liveEvent.Key.IndexOf(ItemSeparator);
                if (split > 0 && _byKey.TryGetValue(liveEvent.Key.Substring(0, split), out StatusItem? phase)
                    && _phaseItems.TryGetValue(phase.Key, out List<StatusItem>? items) && items.Find(i => i.Key == liveEvent.Key.Substring(split + 1)) is StatusItem child) {
                    return Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.Phase, Markup.Strip(phase.Label)) + ": " + Markup.Strip(child.Label) + " " + child.Status.ToWord();
                }
            }
            return null;
        }

        /// <summary>Draws a numbered ledger with hanging labels and right-aligned details, placing a detail below its label when they cannot share a row.</summary>
        public bool StepCounter { get; set; }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            if (StepCounter) return LedgerMeasure(constraints, context);
            if (Compact) return new Size(Math.Min(constraints.MaxWidth, ChainLine(constraints.MaxWidth, context).Width), Math.Min(1, constraints.MaxHeight));
            int lines = Flow(constraints.MaxWidth, context).Count;
            List<StatusItem>? items = FanOutItems(out _);
            if (items != null) { lines += Math.Min(items.Count, _fanOut); }
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? lines : Math.Min(lines, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (StepCounter) { LedgerRender(region, context); return; }
            if (Compact) { if (region.Height > 0) { region.Write(0, 0, ChainLine(region.Width, context)); } return; }
            List<List<StyledLine>> lines = Flow(region.Width, context);
            List<StatusItem>? items = FanOutItems(out int fanOutPhase);
            string separator = " " + context.Glyphs.Get(Glyph.Chevron) + " ";
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
                    x = region.Write(x, row, lines[row][i]);
                }
            }
            if (items == null) { return; }
            int indent = Math.Max(0, Math.Min(fanOutX, region.Width - 16));
            List<StyledLine> fan = FanOutLines(items, Math.Max(1, region.Width - indent), context);
            for (int i = 0; i < fan.Count && lines.Count + i < region.Height; i++) {
                region.Write(indent, lines.Count + i, fan[i]);
            }
        }

        private List<List<StyledLine>> Flow(int width, RenderContext context) {
            string separator = " " + context.Glyphs.Get(Glyph.Chevron) + " ";
            int separatorWidth = DisplayWidth.Measure(separator);
            List<List<StyledLine>> lines = new List<List<StyledLine>>();
            List<StyledLine> current = new List<StyledLine>();
            int used = 0;
            foreach (StatusItem phase in _phases) {
                StyledLine chip = Chip(phase, context);
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

        private static StyledLine Chip(StatusItem phase, RenderContext context) {
            IGlyphSet glyphs = context.Glyphs;
            string marker = phase.Status == StatusKind.Running && glyphs.Tier != GlyphTier.Ascii
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
            if (!string.IsNullOrEmpty(phase.Detail)) {
                spans.Add(new TextSpan(" " + phase.Detail, context.Style(ThemeToken.Muted)));
            }
            return new StyledLine(spans);
        }
    }
}
