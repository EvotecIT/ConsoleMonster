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
    /// emphasised; phases flow onto more lines when the width is short. Live operations (Key = phase): <c>add</c> (Text =
    /// label), <c>state</c> (State, Text = detail), <c>detail</c> (Text).
    /// </summary>
    public sealed class Phases : LiveWidget {
        private readonly List<StatusItem> _phases = new List<StatusItem>();
        private readonly Dictionary<string, StatusItem> _byKey = new Dictionary<string, StatusItem>(StringComparer.Ordinal);
        private readonly Dictionary<string, TimeSpan> _started = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

        /// <summary>Creates a phases widget.</summary>
        /// <param name="name">The live name (default <c>phases</c>).</param>
        public Phases(string name = "phases") : base(name) {
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
                item = new StatusItem(key, label ?? Markup.Escape(key), status, detail);
                _byKey[key] = item;
                _phases.Add(item);
            } else {
                item.Status = status;
                if (label != null) {
                    item.Label = label;
                }
                if (detail != null) {
                    item.Detail = detail;
                }
            }
            return this;
        }

        /// <summary>Returns a handle that updates the phases through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public PhasesHandle Handle(ILiveDispatcher dispatcher) => new PhasesHandle(dispatcher, Name);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Key == null) {
                return;
            }
            switch (liveEvent.Operation) {
                case "add":
                    Set(liveEvent.Key, StatusKind.Pending, liveEvent.Text);
                    break;
                case "state":
                    StatusKinds.TryParse(liveEvent.State, out StatusKind status);
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
                return "phase " + Markup.Strip(item.Label) + " " + item.Status.ToWord() + (string.IsNullOrEmpty(item.Detail) ? string.Empty : " (" + item.Detail + ")");
            }
            return null;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int lines = Flow(constraints.MaxWidth, context).Count;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? lines : Math.Min(lines, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            List<List<StyledLine>> lines = Flow(region.Width, context);
            string separator = " " + context.Glyphs.Get(Glyph.Chevron) + " ";
            Style sepStyle = context.Style(ThemeToken.Subtle);
            for (int row = 0; row < lines.Count && row < region.Height; row++) {
                int x = 0;
                for (int i = 0; i < lines[row].Count; i++) {
                    if (i > 0) {
                        x = region.Write(x, row, separator, context.HasColor ? context.Style(ThemeToken.Muted) : sepStyle);
                    }
                    x = region.Write(x, row, lines[row][i]);
                }
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
