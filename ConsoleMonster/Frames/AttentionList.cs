using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Frames;

/// <summary>One non-healthy item: its state, identity, short aligned values and a reason.</summary>
public sealed class AttentionItem {
    /// <summary>Creates an item.</summary>
    /// <param name="state">The item's state; healthy items are ignored.</param><param name="identity">The literal identity, shown in Text.</param>
    /// <param name="reason">The literal reason, shown Muted.</param><param name="order">Sort key within a state, smallest first; for example a negative age so the oldest comes first.</param>
    public AttentionItem(HealthState state, string identity, string reason, IComparable? order = null) {
        State = state; Identity = identity ?? string.Empty; Reason = reason ?? string.Empty; Order = order;
    }
    /// <summary>The state.</summary>
    public HealthState State { get; }
    /// <summary>The literal identity.</summary>
    public string Identity { get; }
    /// <summary>The literal reason. Items with the same state and reason can be grouped.</summary>
    public string Reason { get; }
    /// <summary>The sort key within a state.</summary>
    public IComparable? Order { get; }
    /// <summary>Short literal values between identity and reason, aligned across items (ages, counts).</summary>
    public IList<string> Values { get; } = new List<string>();
}

/// <summary>
/// The <c>Needs attention</c> list: every non-healthy item, worst first. It is never capped outside
/// <see cref="Views.ConsoleViewLength.Summary"/>, and identical reasons for several targets become one item that still names every target.
/// </summary>
public sealed class AttentionList {
    /// <summary>The singular noun for the items, for example <c>partnership</c>.</summary>
    public string Noun { get; set; } = "item";
    /// <summary>The plural noun; null adds an <c>s</c>.</summary>
    public string? PluralNoun { get; set; }
    /// <summary>The total the items come from, for <c>3 of 12 partnerships</c>; null shows only the item count.</summary>
    public long? OutOf { get; set; }
    /// <summary>The items, in any order.</summary>
    public IList<AttentionItem> Items { get; } = new List<AttentionItem>();
    /// <summary>Identical state and reason for this many items or more become one grouped item (default 3).</summary>
    public int GroupAtLeast { get => _groupAtLeast; set => _groupAtLeast = value < 2 ? throw new ArgumentOutOfRangeException(nameof(value), "Grouping needs at least two items.") : value; }
    private int _groupAtLeast = 3;
    /// <summary>The section title.</summary>
    public string Title { get; set; } = "Needs attention";

    internal int Count => Items.Count(item => item.State != HealthState.Healthy);

    internal FrameSection ToSection(FrameScale scale, RenderContext context) {
        AttentionItem[] items = Items.Where(item => item.State != HealthState.Healthy).ToArray();
        string count = OutOf.HasValue
            ? FrameText.Number(items.Length, context) + " of " + FrameText.Count(OutOf.Value, Noun, PluralNoun, context)
            : FrameText.Count(items.Length, Noun, PluralNoun, context);
        FrameSection section = new FrameSection(Title, context => new AttentionView(this, items, scale)) { Grow = 1 };
        section.Facts.Add(count);
        section.Facts.Add("worst first");
        foreach (IGrouping<HealthState, AttentionItem> group in items.GroupBy(item => item.State)) section.States.Add(FrameState.Of(group.Key, group.Count(), string.Empty));
        return section;
    }

    private sealed class AttentionView : IWidget {
        private readonly AttentionList _list;
        private readonly AttentionItem[] _items;
        private readonly FrameScale _scale;
        internal AttentionView(AttentionList list, AttentionItem[] items, FrameScale scale) { _list = list; _items = items; _scale = scale; }

        private List<Entry> Entries(RenderContext context) {
            List<Entry> entries = new List<Entry>();
            HashSet<AttentionItem> grouped = new HashSet<AttentionItem>();
            foreach (IGrouping<string, AttentionItem> same in _items.GroupBy(item => (int)item.State + "\n" + item.Reason)) {
                AttentionItem[] members = same.ToArray();
                if (_list.GroupAtLeast > 1 && members.Length >= _list.GroupAtLeast && members[0].Reason.Length > 0) {
                    entries.Add(new Entry(members[0].State, FrameText.Count(members.Length, _list.Noun, _list.PluralNoun, context), members[0].Reason, null,
                        members.Select(item => item.Identity).ToArray(), members.Min(item => item.Order)));
                    foreach (AttentionItem item in members) grouped.Add(item);
                }
            }
            foreach (AttentionItem item in _items) { if (!grouped.Contains(item)) entries.Add(new Entry(item.State, item.Identity, item.Reason, item.Values, null, item.Order)); }
            return entries.OrderByDescending(entry => FrameText.Rank(entry.State)).ThenBy(entry => entry.Order, Comparer<IComparable?>.Create(Compare))
                .ThenBy(entry => entry.Identity, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static int Compare(IComparable? a, IComparable? b) {
            if (a == null) return b == null ? 0 : 1;
            if (b == null) return -1;
            // Keys of different types never fail the frame; they sort by type name, then by their own order.
            if (a.GetType() != b.GetType()) return string.CompareOrdinal(a.GetType().FullName, b.GetType().FullName);
            return a.CompareTo(b);
        }

        private List<StyledLine> Lines(int width, RenderContext context) {
            List<Entry> entries = Entries(context);
            int cap = _scale.AttentionCap ?? int.MaxValue;
            int shown = Math.Min(entries.Count, cap);
            int identity = 0;
            int values = 0;
            for (int i = 0; i < entries.Count; i++) {
                identity = Math.Max(identity, FrameText.Width(FrameText.Escape(entries[i].Identity), context));
                values = Math.Max(values, entries[i].Values?.Count ?? 0);
            }
            identity = Math.Min(identity, Math.Max(12, width * 2 / 5));
            int[] valueWidths = new int[values];
            foreach (Entry entry in entries) {
                if (entry.Values == null) continue;
                for (int v = 0; v < entry.Values.Count; v++) valueWidths[v] = Math.Max(valueWidths[v], FrameText.Width(FrameText.Escape(entry.Values[v]), context));
            }
            // Reasons stay on the row while every row fits; otherwise each reason moves under its row.
            bool narrow = false;
            for (int i = 0; i < shown && !narrow; i++) narrow = WideWidth(entries[i], identity, valueWidths, context) > width;
            List<StyledLine> lines = new List<StyledLine>();
            for (int i = 0; i < shown; i++) {
                Entry entry = entries[i];
                string tag = FrameText.Tag(entry.State);
                List<TextSpan> spans = new List<TextSpan>();
                spans.AddRange(FrameText.Line("[" + tag + "]" + FrameText.Glyph(entry.State, context) + "[/] ", context));
                spans.AddRange(FrameText.Fit(FrameText.Line(FrameText.Escape(entry.Identity), context), identity, false, context));
                if (entry.Values != null) {
                    for (int v = 0; v < entry.Values.Count; v++) {
                        spans.Add(new TextSpan("  "));
                        string markup = v == 0 ? "[" + tag + "]" + FrameText.Escape(entry.Values[v]) + "[/]" : "[muted]" + FrameText.Escape(entry.Values[v]) + "[/]";
                        spans.AddRange(FrameText.Fit(FrameText.Line(markup, context), valueWidths[v], true, context));
                    }
                } else if (valueWidths.Length > 0 && entry.Names == null) {
                    for (int v = 0; v < valueWidths.Length; v++) { spans.Add(new TextSpan("  " + new string(' ', valueWidths[v]))); }
                }
                StyledLine reason = FrameText.Line("[muted]" + FrameText.Escape(entry.Reason) + "[/]", context);
                if (narrow) {
                    lines.Add(Truncate(new StyledLine(spans), width, context));
                    if (reason.Width > 0) lines.Add(Truncate(Indent(reason, 2), width, context));
                } else {
                    spans.Add(new TextSpan("  "));
                    spans.AddRange(reason);
                    lines.Add(Truncate(new StyledLine(spans), width, context));
                }
                if (entry.Names != null) {
                    StyledLine names = FrameText.Line("[muted]" + FrameText.Escape(string.Join(", ", entry.Names)) + "[/]", context);
                    foreach (StyledLine wrapped in TextWrapper.Wrap(names, Math.Max(1, width - 2))) lines.Add(Indent(wrapped, 2));
                }
            }
            if (shown < entries.Count) {
                long hidden = 0;
                for (int i = shown; i < entries.Count; i++) hidden += entries[i].Names?.Length ?? 1;
                lines.Add(FrameText.Line("[muted]+" + FrameText.Number(hidden, context) + " more " + FrameText.Dot(context)
                    + " set $ConsoleViewLengthPreference = 'Standard' for every item[/]", context));
            }
            return lines;
        }

        private static int WideWidth(Entry entry, int identity, int[] values, RenderContext context) {
            int width = 2 + identity;
            foreach (int value in values) width += 2 + value;
            return width + 2 + FrameText.Width(FrameText.Escape(entry.Reason), context);
        }

        private static StyledLine Indent(StyledLine line, int cells) {
            List<TextSpan> spans = new List<TextSpan> { new TextSpan(new string(' ', cells)) };
            spans.AddRange(line);
            return new StyledLine(spans);
        }

        private static StyledLine Truncate(StyledLine line, int width, RenderContext context) => line.Width > width ? TextWrapper.Truncate(line, width, context.Ellipsis) : line;

        public Size Measure(Constraints constraints, RenderContext context) {
            List<StyledLine> lines = Lines(constraints.MaxWidth, context);
            int width = 0;
            foreach (StyledLine line in lines) width = Math.Max(width, line.Width);
            return new Size(Math.Min(width, constraints.MaxWidth), Math.Min(lines.Count, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            List<StyledLine> lines = Lines(region.Width, context);
            for (int y = 0; y < lines.Count && y < region.Height; y++) region.Write(0, y, lines[y]);
        }
    }

    private sealed class Entry {
        internal Entry(HealthState state, string identity, string reason, IList<string>? values, string[]? names, IComparable? order) {
            State = state; Identity = identity; Reason = reason; Values = values; Names = names; Order = order;
        }
        internal HealthState State { get; }
        internal string Identity { get; }
        internal string Reason { get; }
        internal IList<string>? Values { get; }
        internal string[]? Names { get; }
        internal IComparable? Order { get; }
    }
}
