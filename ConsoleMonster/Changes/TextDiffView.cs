using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Changes {
    /// <summary>
    /// A text diff in the object-preview pass/fail tones: side by side at 120 columns and above, inline below.
    /// Line numbers and minus/plus markers distinguish removals and additions without colour. Changed words are emphasised.
    /// </summary>
    public sealed class TextDiffView : IWidget {
        private readonly Dictionary<TextDiffLine, string> _highlighted = new Dictionary<TextDiffLine, string>();

        /// <summary>Creates a view of a reusable text comparison.</summary>
        /// <param name="comparison">The comparison, including context and source labels.</param>
        public TextDiffView(TextDiffResult comparison) {
            Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
            HighlightReplacements();
        }

        /// <summary>The immutable comparison rendered by this widget.</summary>
        public TextDiffResult Comparison { get; }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Build(constraints.MaxWidth, context).Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Build(region.Width, context).Render(region, context);

        private IWidget Build(int width, RenderContext context) {
            bool wide = width >= 120;
            Rows rows = new Rows();
            string left = "[header]" + Markup.Escape(Comparison.ReferenceLabel) + "[/]";
            string right = "[header]" + Markup.Escape(Comparison.DifferenceLabel) + "[/]";
            rows.Add(wide ? Pair(left, right) : new TextBlock(left + " -> " + right));
            rows.Add(new TextBlock("[pass]+" + Number(Comparison.Added) + " added[/]  [fail]-" + Number(Comparison.Removed) + " removed[/]"));
            int index = 0;
            foreach ((int Start, int End) range in Comparison.Ranges()) {
                Collapse(rows, index, range.Start, context);
                index = range.Start;
                while (index < range.End) {
                    TextDiffLine line = Comparison.Lines[index];
                    if (!wide || line.Kind == ChangeKind.Unchanged) {
                        rows.Add(wide ? Pair(Cell(line, true), Cell(line, false)) : new TextBlock(Inline(line)));
                        index++;
                        continue;
                    }
                    List<TextDiffLine> removed = new List<TextDiffLine>();
                    List<TextDiffLine> added = new List<TextDiffLine>();
                    while (index < range.End && Comparison.Lines[index].Kind != ChangeKind.Unchanged) {
                        line = Comparison.Lines[index++];
                        (line.Kind == ChangeKind.Removed ? removed : added).Add(line);
                    }
                    for (int i = 0; i < Math.Max(removed.Count, added.Count); i++) {
                        rows.Add(Pair(i < removed.Count ? Cell(removed[i], true) : string.Empty,
                            i < added.Count ? Cell(added[i], false) : string.Empty));
                    }
                }
            }
            Collapse(rows, index, Comparison.Lines.Count, context);
            if (Comparison.Lines.Count == 0) { rows.Add(new TextBlock("[muted]No changes[/]")); }
            return rows;
        }

        private void Collapse(Rows rows, int start, int end, RenderContext context) {
            if (end <= start) { return; }
            TextDiffLine first = Comparison.Lines[start];
            TextDiffLine last = Comparison.Lines[end - 1];
            string label = end - start == 1 ? " unchanged line (" : " unchanged lines (";
            rows.Add(new TextBlock("[muted]" + context.Ellipsis + " " + Number(end - start) + label +
                Number(first.ReferenceLineNumber) + "-" + Number(last.ReferenceLineNumber) + " / " +
                Number(first.DifferenceLineNumber) + "-" + Number(last.DifferenceLineNumber) + ")[/]"));
        }

        private string Cell(TextDiffLine line, bool reference) {
            int? number = reference ? line.ReferenceLineNumber : line.DifferenceLineNumber;
            return "[muted]" + Number(number).PadLeft(Digits()) + "[/] " + Marker(line) + " " + Content(line);
        }

        private string Inline(TextDiffLine line) => "[muted]" + Number(line.ReferenceLineNumber).PadLeft(Digits()) + " " +
            Number(line.DifferenceLineNumber).PadLeft(Digits()) + "[/] " + Marker(line) + " " + Content(line);

        private string Content(TextDiffLine line) {
            string text = _highlighted.TryGetValue(line, out string? highlighted) ? highlighted : Markup.Escape(line.Text);
            if (!line.HasLineEnding) { text += " [muted]\\ No newline at end of file[/]"; }
            return text;
        }

        private static string Marker(TextDiffLine line) => line.Kind == ChangeKind.Removed ? "[fail]-[/]" : line.Kind == ChangeKind.Added ? "[pass]+[/]" : " ";

        private int Digits() => Number(Comparison.Lines.Count).Length;

        private static string Number(int? number) => number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        private static IWidget Pair(string left, string right) => new Columns {
            CollapseBelow = 0,
            Gap = 1
        }.Add(new TextBlock(left)).Add(new TextBlock("[muted]|[/]"), LayoutSize.Fixed(1)).Add(new TextBlock(right));

        private void HighlightReplacements() {
            int index = 0;
            while (index < Comparison.Lines.Count) {
                if (Comparison.Lines[index].Kind == ChangeKind.Unchanged) { index++; continue; }
                List<TextDiffLine> removed = new List<TextDiffLine>();
                List<TextDiffLine> added = new List<TextDiffLine>();
                while (index < Comparison.Lines.Count && Comparison.Lines[index].Kind != ChangeKind.Unchanged) {
                    TextDiffLine line = Comparison.Lines[index++];
                    (line.Kind == ChangeKind.Removed ? removed : added).Add(line);
                }
                for (int i = 0; i < Math.Max(removed.Count, added.Count); i++) {
                    (string Before, string After) words = TextDiffWords.Highlight(i < removed.Count ? removed[i].Text : string.Empty,
                        i < added.Count ? added[i].Text : string.Empty);
                    if (i < removed.Count) { _highlighted.Add(removed[i], words.Before); }
                    if (i < added.Count) { _highlighted.Add(added[i], words.After); }
                }
            }
        }
    }
}
