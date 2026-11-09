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
/// Many items with one state and reason, already counted: the total and a few of their identities. A summary of a streamed
/// result uses it, because it keeps counts and samples rather than every item.
/// </summary>
public sealed class AttentionGroup {
    /// <summary>Creates a group.</summary>
    /// <param name="state">The state; healthy groups are ignored.</param><param name="reason">The literal reason, shown Muted.</param>
    /// <param name="count">How many items the group stands for; at least the number of names.</param><param name="names">Some of their literal identities.</param>
    /// <param name="order">Sort key within a state, smallest first.</param>
    public AttentionGroup(HealthState state, string reason, long count, IEnumerable<string> names, IComparable? order = null) {
        string[] samples = (names ?? Array.Empty<string>()).Select(name => name ?? string.Empty).ToArray();
        if (count < Math.Max(1, samples.Length)) throw new ArgumentOutOfRangeException(nameof(count), "A group counts at least one item and every name it shows.");
        State = state; Reason = reason ?? string.Empty; Count = count; Names = samples; Order = order;
    }
    /// <summary>The state.</summary>
    public HealthState State { get; }
    /// <summary>The literal reason.</summary>
    public string Reason { get; }
    /// <summary>How many items the group stands for.</summary>
    public long Count { get; }
    /// <summary>The identities shown; the rest are counted as more.</summary>
    public IReadOnlyList<string> Names { get; }
    /// <summary>The sort key within a state.</summary>
    public IComparable? Order { get; }
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
    /// <summary>Counted groups, for results too large to keep item by item; they sort with the items.</summary>
    public IList<AttentionGroup> Groups { get; } = new List<AttentionGroup>();
    /// <summary>Identical state and reason for this many items or more become one grouped item (default 3).</summary>
    public int GroupAtLeast { get => _groupAtLeast; set => _groupAtLeast = value < 2 ? throw new ArgumentOutOfRangeException(nameof(value), "Grouping needs at least two items.") : value; }
    private int _groupAtLeast = 3;
    /// <summary>The section title.</summary>
    public string Title { get; set; } = "Needs attention";

    internal int Count => (int)Math.Min(int.MaxValue, Total);

    private long Total => Items.Count(item => item.State != HealthState.Healthy) + Groups.Where(group => group.State != HealthState.Healthy).Sum(group => group.Count);

    internal FrameSection ToSection(FrameScale scale, RenderContext context) {
        AttentionItem[] items = Items.Where(item => item.State != HealthState.Healthy).ToArray();
        AttentionGroup[] groups = Groups.Where(group => group.State != HealthState.Healthy).ToArray();
        long total = Total;
        string count = OutOf.HasValue
            ? FrameText.Number(total, context) + " of " + FrameText.Count(OutOf.Value, Noun, PluralNoun, context)
            : FrameText.Count(total, Noun, PluralNoun, context);
        FrameSection section = new FrameSection(Title, context => new AttentionView(this, items, groups, total, scale)) { Grow = 1 };
        section.Facts.Add(count);
        section.Facts.Add("worst first");
        foreach (IGrouping<HealthState, long> group in items.Select(item => (item.State, Count: 1L)).Concat(groups.Select(group => (group.State, group.Count)))
                     .GroupBy(pair => pair.State, pair => pair.Count)) section.States.Add(FrameState.Of(group.Key, group.Sum(), string.Empty));
        return section;
    }

    private sealed class AttentionView : IWidget {
        private readonly AttentionList _list;
        private readonly AttentionItem[] _items;
        private readonly AttentionGroup[] _groups;
        private readonly long _total;
        private readonly FrameScale _scale;
        internal AttentionView(AttentionList list, AttentionItem[] items, AttentionGroup[] groups, long total, FrameScale scale) { _list = list; _items = items; _groups = groups; _total = total; _scale = scale; }

        private List<Entry> Entries(RenderContext context) {
            List<Entry> entries = new List<Entry>();
            HashSet<AttentionItem> grouped = new HashSet<AttentionItem>();
            foreach (IGrouping<string, AttentionItem> same in _items.GroupBy(item => (int)item.State + "\n" + item.Reason)) {
                AttentionItem[] members = same.ToArray();
                if (_list.GroupAtLeast > 1 && members.Length >= _list.GroupAtLeast && members[0].Reason.Length > 0) {
                    entries.Add(new Entry(members[0].State, FrameText.Count(members.Length, _list.Noun, _list.PluralNoun, context), members[0].Reason, null,
                        members.Select(item => item.Identity).ToArray(), members.Select(item => item.Order).OrderBy(order => order, Comparer<IComparable?>.Create(Compare)).First()));
                    foreach (AttentionItem item in members) grouped.Add(item);
                }
            }
            foreach (AttentionItem item in _items) { if (!grouped.Contains(item)) entries.Add(new Entry(item.State, item.Identity, item.Reason, item.Values, null, item.Order)); }
            foreach (AttentionGroup group in _groups) {
                entries.Add(new Entry(group.State, FrameText.Count(group.Count, _list.Noun, _list.PluralNoun, context), group.Reason, null, group.Names.ToArray(), group.Order) { Total = group.Count });
            }
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
            long represented = 0;
            for (int i = 0; i < shown; i++) {
                Entry entry = entries[i];
                List<StyledLine> itemLines = new List<StyledLine>();
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
                    itemLines.Add(Truncate(new StyledLine(spans), width, context));
                    if (reason.Width > 0) itemLines.Add(Truncate(Indent(reason, 2), width, context));
                } else {
                    spans.Add(new TextSpan("  "));
                    spans.AddRange(reason);
                    itemLines.Add(Truncate(new StyledLine(spans), width, context));
                }
                if (itemLines.Count > cap - lines.Count) break;
                lines.AddRange(itemLines);
                if (entry.Names != null) {
                    // A counted group shows its samples and says how many more it stands for; those are represented, not hidden.
                    long unnamed = entry.Total - entry.Names.Length;
                    string more = unnamed > 0 ? (entry.Names.Length > 0 ? ", " : string.Empty) + "+" + FrameText.Number(unnamed, context) + " more" : string.Empty;
                    if (_scale.AttentionCap.HasValue) represented += SummaryNames(entry.Names, width, cap - lines.Count, context, lines) + unnamed;
                    else {
                        StyledLine names = FrameText.Line("[muted]" + FrameText.Escape(string.Join(", ", entry.Names)) + FrameText.Escape(more) + "[/]", context);
                        foreach (StyledLine wrapped in TextWrapper.Wrap(names, Math.Max(1, width - 2))) lines.Add(Indent(wrapped, 2));
                        represented += entry.Total;
                    }
                } else represented++;
                if (lines.Count >= cap) break;
            }
            long hidden = _total - represented;
            if (hidden > 0) {
                lines.Add(FrameText.Line("[muted]+" + FrameText.Number(hidden, context) + " more " + FrameText.Dot(context)
                    + " set $ConsoleViewLengthPreference = 'Standard' for every item[/]", context));
            }
            return lines;
        }

        // Keep whole identities within the Summary line budget, so the omitted count names targets, not wrapped lines.
        private static int SummaryNames(string[] names, int width, int limit, RenderContext context, List<StyledLine> lines) {
            int inner = Math.Max(1, width - 2);
            int used = 0, shown = 0;
            string pending = string.Empty;
            foreach (string name in names) {
                if (used >= limit) break;
                string literal = FrameText.Escape(name);
                string candidate = pending.Length == 0 ? literal : pending + ", " + literal;
                if (FrameText.Width(candidate, context) <= inner) { pending = candidate; shown++; continue; }
                if (pending.Length > 0) {
                    lines.Add(Indent(FrameText.Line("[muted]" + pending + "[/]", context), 2));
                    used++; pending = string.Empty;
                    if (used >= limit) break;
                }
                StyledLine single = FrameText.Line("[muted]" + literal + "[/]", context);
                if (single.Width > inner) {
                    IReadOnlyList<StyledLine> wrapped = TextWrapper.Wrap(single, inner);
                    if (wrapped.Count > limit - used) break;
                    foreach (StyledLine part in wrapped) lines.Add(Indent(part, 2));
                    used += wrapped.Count; shown++;
                } else { pending = literal; shown++; }
            }
            if (pending.Length > 0) lines.Add(Indent(FrameText.Line("[muted]" + pending + "[/]", context), 2));
            return shown;
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
        // How many items the entry stands for: its names, or a counted group's total.
        internal long Total { get => _total ?? Names?.Length ?? 1; init => _total = value; }
        private readonly long? _total;
    }
}
