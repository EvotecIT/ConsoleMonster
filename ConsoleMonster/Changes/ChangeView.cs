using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Changes {
    /// <summary>How a <see cref="ChangeView"/> shows a change set.</summary>
    public enum ChangeViewMode {
        /// <summary>The counts by kind, then the table of changes and line diffs of long text values.</summary>
        Full,

        /// <summary>The counts by kind only.</summary>
        Summary,

        /// <summary>One row per changed property (and per added or removed object).</summary>
        Table,

        /// <summary>For each changed object, its old and new values side by side.</summary>
        SideBySide,

        /// <summary>A unified text diff: <c>-</c> old and <c>+</c> new lines, with line diffs of multi-line values.</summary>
        Unified
    }

    /// <summary>A line of a text diff.</summary>
    public readonly struct DiffLine {
        /// <summary>Creates a line.</summary>
        /// <param name="kind">Added, Removed or Unchanged.</param>
        /// <param name="text">The text.</param>
        public DiffLine(ChangeKind kind, string text) {
            Kind = kind;
            Text = text ?? string.Empty;
        }

        /// <summary>Added, Removed or Unchanged.</summary>
        public ChangeKind Kind { get; }

        /// <summary>The text.</summary>
        public string Text { get; }
    }

    /// <summary>
    /// Shows a <see cref="ChangeSet"/> in the design-system tones: added in the pass colour (<c>+</c>), removed in the fail
    /// colour (<c>-</c>), changed in the warning colour (<c>~</c>).
    /// </summary>
    public sealed class ChangeView : IWidget {
        /// <summary>Creates a change view.</summary>
        /// <param name="changes">The changes.</param>
        /// <param name="mode">The view.</param>
        public ChangeView(ChangeSet changes, ChangeViewMode mode = ChangeViewMode.Full) {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            Mode = mode;
        }

        /// <summary>The changes.</summary>
        public ChangeSet Changes { get; }

        /// <summary>The view.</summary>
        public ChangeViewMode Mode { get; set; }

        /// <summary>A title above the view (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>Values longer than this many cells (or with line breaks) show as a line diff instead of in the table (default 60).</summary>
        public int LongValue { get; set; } = 60;

        /// <summary>
        /// The most lines an added or removed object takes in the table, one per compared property (default 6, 0 for no
        /// limit); beyond it the last line says "+N more" with <see cref="MoreHint"/>. The side-by-side view lists all.
        /// </summary>
        public int MaxProperties { get; set; } = 6;

        /// <summary>Plain text after "+N more" saying where to see every value (Show-ConsoleChanges: "-PassThru or -View SideBySide"), or null.</summary>
        public string? MoreHint { get; set; }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Build(context).Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Build(context).Render(region, context);

        /// <summary>A line diff of two texts (longest common subsequence over lines).</summary>
        /// <param name="before">The old text.</param>
        /// <param name="after">The new text.</param>
        /// <returns>The lines in order.</returns>
        public static IReadOnlyList<DiffLine> TextDiff(string? before, string? after) {
            string[] a = (before ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            string[] b = (after ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            if ((long)a.Length * b.Length > 4_000_000) {
                // Too large for the table: old lines, then new lines.
                return a.Select(line => new DiffLine(ChangeKind.Removed, line)).Concat(b.Select(line => new DiffLine(ChangeKind.Added, line))).ToList();
            }
            int[,] lcs = new int[a.Length + 1, b.Length + 1];
            for (int i = a.Length - 1; i >= 0; i--) {
                for (int j = b.Length - 1; j >= 0; j--) {
                    lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }
            List<DiffLine> lines = new List<DiffLine>();
            int x = 0;
            int y = 0;
            while (x < a.Length && y < b.Length) {
                if (a[x] == b[y]) {
                    lines.Add(new DiffLine(ChangeKind.Unchanged, a[x]));
                    x++;
                    y++;
                } else if (lcs[x + 1, y] >= lcs[x, y + 1]) {
                    lines.Add(new DiffLine(ChangeKind.Removed, a[x++]));
                } else {
                    lines.Add(new DiffLine(ChangeKind.Added, b[y++]));
                }
            }
            while (x < a.Length) {
                lines.Add(new DiffLine(ChangeKind.Removed, a[x++]));
            }
            while (y < b.Length) {
                lines.Add(new DiffLine(ChangeKind.Added, b[y++]));
            }
            return lines;
        }

        private Localization.WidgetLabels _labels = Localization.WidgetLabels.Current;

        private IWidget Build(RenderContext context) {
            _labels = context.Labels;
            List<IWidget> parts = new List<IWidget>();
            if (!string.IsNullOrEmpty(Title)) {
                parts.Add(new TextBlock("[title]" + Title + "[/]"));
            }
            if (Mode == ChangeViewMode.Full || Mode == ChangeViewMode.Summary) {
                parts.Add(new TextBlock(Summary()));
            }
            if (Changes.HasChanges || Changes.Changes.Count > 0) {
                switch (Mode) {
                    case ChangeViewMode.Full:
                        parts.Add(BuildTable());
                        parts.AddRange(LongDiffs());
                        break;
                    case ChangeViewMode.Table:
                        parts.Add(BuildTable());
                        break;
                    case ChangeViewMode.SideBySide:
                        parts.AddRange(SideBySide());
                        break;
                    case ChangeViewMode.Unified:
                        parts.Add(new TextBlock(Unified()));
                        break;
                }
            }
            return parts.Count == 1 ? parts[0] : new Rows(parts.ToArray()) { Gap = 1 };
        }

        private string Summary() {
            List<string> parts = new List<string> {
                "[pass]" + Markup.Escape(Localization.WidgetLabels.Format(_labels.ChangesAdded, Changes.Added.ToString(CultureInfo.InvariantCulture))) + "[/]",
                "[fail]" + Markup.Escape(Localization.WidgetLabels.Format(_labels.ChangesRemoved, Changes.Removed.ToString(CultureInfo.InvariantCulture))) + "[/]",
                "[warn]" + Markup.Escape(Localization.WidgetLabels.Format(_labels.ChangesChanged, Changes.Changed.ToString(CultureInfo.InvariantCulture))) + "[/]",
                "[muted]" + Markup.Escape(Localization.WidgetLabels.Format(_labels.ChangesUnchanged, Changes.Unchanged.ToString(CultureInfo.InvariantCulture))) + "[/]"
            };
            if (Changes.NotCompared.Count > 0) {
                // Members whose getters stalled were skipped, not judged equal: say so, with their names when few.
                string names = Changes.NotCompared.Count <= 3 ? " (" + string.Join(", ", Changes.NotCompared) + ")" : string.Empty;
                parts.Add("[warn]" + Markup.Escape(Localization.WidgetLabels.Format(_labels.ChangesNotCompared, Changes.NotCompared.Count.ToString(CultureInfo.InvariantCulture)) + names) + "[/]");
            }
            return string.Join("  ", parts);
        }

        private Table BuildTable() {
            Table table = new Table { Border = TableBorder.Rounded };
            table.AddColumn("").AddColumn(Markup.Escape(_labels.ChangesItem)).AddColumn(Markup.Escape(_labels.ChangesProperty)).AddColumn(Markup.Escape(_labels.ChangesBefore)).AddColumn(Markup.Escape(_labels.ChangesAfter));
            foreach (ObjectChange change in Changes.Changes) {
                string marker = Marker(change.Kind);
                string key = Markup.Escape(change.Key);
                switch (change.Kind) {
                    case ChangeKind.Added:
                    case ChangeKind.Removed:
                        AddObjectRows(table, change, marker, key);
                        break;
                    case ChangeKind.Unchanged:
                        table.AddRow(marker, "[muted]" + key + "[/]", "", "", "");
                        break;
                    default:
                        bool first = true;
                        foreach (PropertyChange property in change.Properties) {
                            bool longValue = IsLong(property.Before) || IsLong(property.After);
                            table.AddRow(first ? marker : "", first ? Tone(change.Kind, key) : "", Markup.Escape(property.Path),
                                longValue ? "[muted]" + Markup.Escape(_labels.ChangesSeeDiff) + "[/]" : "[fail]" + Markup.Escape(property.BeforeText) + "[/]",
                                longValue ? "[muted]" + Markup.Escape(_labels.ChangesSeeDiff) + "[/]" : "[pass]" + Markup.Escape(property.AfterText) + "[/]");
                            first = false;
                        }
                        break;
                }
            }
            return table;
        }

        // An added or removed object: one line per compared property (the value on its side, the other side empty), at most
        // MaxProperties lines, then "+N more" and the hint. A simple value keeps one line.
        private void AddObjectRows(Table table, ObjectChange change, string marker, string key) {
            bool added = change.Kind == ChangeKind.Added;
            string tone = added ? "pass" : "fail";
            if (change.Values.Count == 0) {
                string value = "[" + tone + "]" + Markup.Escape(Clip(Describe(change))) + "[/]";
                table.AddRow(marker, Tone(change.Kind, key), "", added ? "" : value, added ? value : "");
                return;
            }
            int shown = change.Values.Count;
            if (MaxProperties > 0 && shown > MaxProperties) {
                // The "+N more" line takes the place of the last property, so the object never grows past MaxProperties lines.
                shown = MaxProperties - 1;
            }
            for (int i = 0; i < shown; i++) {
                PropertyChange property = change.Values[i];
                string value = "[" + tone + "]" + Markup.Escape(Clip(ObjectDiff.Text(added ? property.After : property.Before))) + "[/]";
                table.AddRow(i == 0 ? marker : "", i == 0 ? Tone(change.Kind, key) : "", Markup.Escape(property.Path), added ? "" : value, added ? value : "");
            }
            int more = change.Values.Count - shown;
            if (more > 0) {
                string count = "[muted]" + Markup.Escape(Localization.WidgetLabels.Format(_labels.MoreCount, more.ToString(CultureInfo.InvariantCulture))) + "[/]";
                string hint = string.IsNullOrEmpty(MoreHint) ? "" : "[muted]" + Markup.Escape(MoreHint) + "[/]";
                table.AddRow(shown == 0 ? marker : "", shown == 0 ? Tone(change.Kind, key) : "", count, added ? "" : hint, added ? hint : "");
            }
        }

        private IEnumerable<IWidget> LongDiffs() {
            foreach (ObjectChange change in Changes.Changes.Where(c => c.Kind == ChangeKind.Changed)) {
                foreach (PropertyChange property in change.Properties.Where(p => IsLong(p.Before) || IsLong(p.After))) {
                    List<string> lines = new List<string> { "[warn]~[/] " + Markup.Escape(change.Key) + " [muted]" + Markup.Escape(property.Path) + "[/]" };
                    lines.AddRange(TextDiff(ObjectDiff.Text(property.Before), ObjectDiff.Text(property.After)).Select(DiffMarkup));
                    yield return new TextBlock(string.Join("\n", lines));
                }
            }
        }

        private IEnumerable<IWidget> SideBySide() {
            foreach (ObjectChange change in Changes.Changes) {
                if (change.Kind == ChangeKind.Unchanged) {
                    continue;
                }
                if (change.Kind != ChangeKind.Changed && change.Values.Count == 0) {
                    // A simple added or removed value: its key and the value.
                    yield return new TextBlock(Marker(change.Kind) + " " + Tone(change.Kind, Markup.Escape(change.Key)) + "  [muted]" + Markup.Escape(Clip(Describe(change))) + "[/]");
                    continue;
                }
                Table table = new Table { Title = Marker(change.Kind) + " " + Tone(change.Kind, Markup.Escape(change.Key)), Border = TableBorder.Rounded };
                table.AddColumn(Markup.Escape(_labels.ChangesProperty)).AddColumn(Markup.Escape(_labels.ChangesBefore)).AddColumn(Markup.Escape(_labels.ChangesAfter));
                // Every property: changed ones with both sides, an added or removed object's with its one side.
                foreach (PropertyChange property in change.Kind == ChangeKind.Changed ? change.Properties : change.Values) {
                    string before = change.Kind == ChangeKind.Added ? "" : "[fail]" + Markup.Escape(property.BeforeText) + "[/]";
                    string after = change.Kind == ChangeKind.Removed ? "" : "[pass]" + Markup.Escape(property.AfterText) + "[/]";
                    table.AddRow(Markup.Escape(property.Path), before, after);
                }
                yield return table;
            }
        }

        private string Unified() {
            List<string> lines = new List<string>();
            foreach (ObjectChange change in Changes.Changes) {
                switch (change.Kind) {
                    case ChangeKind.Added:
                        lines.Add("[pass]+ " + Markup.Escape(change.Key) + "[/]" + Note(change));
                        break;
                    case ChangeKind.Removed:
                        lines.Add("[fail]- " + Markup.Escape(change.Key) + "[/]" + Note(change));
                        break;
                    case ChangeKind.Changed:
                        lines.Add("[warn]~ " + Markup.Escape(change.Key) + "[/]");
                        foreach (PropertyChange property in change.Properties) {
                            string before = property.BeforeText;
                            string after = property.AfterText;
                            if (before.IndexOf('\n') >= 0 || after.IndexOf('\n') >= 0) {
                                lines.Add("  [muted]" + Markup.Escape(property.Path) + ":[/]");
                                lines.AddRange(TextDiff(before, after).Select(line => "  " + DiffMarkup(line)));
                            } else {
                                lines.Add("  [fail]- " + Markup.Escape(property.Path) + ": " + Markup.Escape(before) + "[/]");
                                lines.Add("  [pass]+ " + Markup.Escape(property.Path) + ": " + Markup.Escape(after) + "[/]");
                            }
                        }
                        break;
                }
            }
            return string.Join("\n", lines);
        }

        private static string DiffMarkup(DiffLine line) {
            switch (line.Kind) {
                case ChangeKind.Added:
                    return "[pass]+ " + Markup.Escape(line.Text) + "[/]";
                case ChangeKind.Removed:
                    return "[fail]- " + Markup.Escape(line.Text) + "[/]";
                default:
                    return "[muted]  " + Markup.Escape(line.Text) + "[/]";
            }
        }

        private bool IsLong(object? value) {
            string text = ObjectDiff.Text(value);
            return text.IndexOf('\n') >= 0 || DisplayWidth.Measure(text) > LongValue;
        }

        private static string Marker(ChangeKind kind) {
            switch (kind) {
                case ChangeKind.Added:
                    return "[pass]+[/]";
                case ChangeKind.Removed:
                    return "[fail]-[/]";
                case ChangeKind.Changed:
                    return "[warn]~[/]";
                default:
                    return "[muted]=[/]";
            }
        }

        private static string Tone(ChangeKind kind, string markup) {
            switch (kind) {
                case ChangeKind.Added:
                    return "[pass]" + markup + "[/]";
                case ChangeKind.Removed:
                    return "[fail]" + markup + "[/]";
                case ChangeKind.Changed:
                    return "[warn]" + markup + "[/]";
                default:
                    return markup;
            }
        }

        private static string Describe(ObjectChange change) => change.Summary ?? Summarize(change.Kind == ChangeKind.Added ? change.After : change.Before);

        private string Note(ObjectChange change) {
            string text = Describe(change);
            return text.Length == 0 ? string.Empty : "  [muted]" + Markup.Escape(Clip(text)) + "[/]";
        }

        private static string Summarize(object? value) => value == null || ObjectInspectorScalar(value) ? ObjectDiff.Text(value) : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

        private static bool ObjectInspectorScalar(object value) => Data.ObjectInspector.IsScalarValue(value);

        private string Clip(string text) => TextWrapper.Truncate(text.Replace("\r\n", " ").Replace('\n', ' '), LongValue);
    }
}
