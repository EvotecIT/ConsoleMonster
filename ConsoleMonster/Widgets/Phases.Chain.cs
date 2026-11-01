using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Phases {
        /// <summary>
        /// Draws the phases as one line that folds to fit, for a table cell, a status-list detail or a header:
        /// <c>✓ Fetch › ● Build 40% › ○ Test</c>. When the line is too wide, phases around the current one (the first failed,
        /// running or pending phase) fold into <c>✓ +2 earlier</c> and <c>○ +3 more</c>, then into plain counts, and finally the
        /// current phase drops its detail. At very narrow widths, surrounding counts are omitted before the current
        /// phase is shortened. Other failures can be represented by a folded group's status. Items and the step counter are not shown in this form.
        /// </summary>
        public bool Compact { get; set; }

        private int ChainIndex() {
            int index = _phases.FindIndex(phase => phase.Status == StatusKind.Fail);
            if (index < 0) { index = _phases.FindIndex(phase => phase.Status == StatusKind.Running); }
            if (index < 0) { index = _phases.FindIndex(phase => phase.Status == StatusKind.Pending); }
            return index < 0 ? _phases.Count - 1 : index;
        }

        private StyledLine ChainLine(int width, RenderContext context) {
            if (_phases.Count == 0) { return StyledLine.Empty; }
            int current = ChainIndex();
            // Most readable first: every phase, then neighbours folded with words, then counts, then no current detail.
            for (int level = 0; level < 4; level++) {
                StyledLine line = Chain(current, level, context);
                if (line.Width <= width) { return line; }
            }
            // Keep the current phase ahead of surrounding counts when the whole chain no longer fits.
            StyledLine chip = Chip(new StatusItem(_phases[current].Key, _phases[current].Label, _phases[current].Status), context);
            return TextWrapper.Truncate(chip, width, context.Ellipsis);
        }

        private StyledLine Chain(int current, int level, RenderContext context) {
            List<StyledLine> parts = new List<StyledLine>();
            if (level == 0) {
                foreach (StatusItem phase in _phases) { parts.Add(Chip(phase, context)); }
            } else {
                AddGroup(parts, 0, current, level, false, context);
                StyledLine chip = level == 3 ? Chip(new StatusItem(_phases[current].Key, _phases[current].Label, _phases[current].Status), context) : Chip(_phases[current], context);
                parts.Add(chip);
                AddGroup(parts, current + 1, _phases.Count, level, true, context);
            }
            List<TextSpan> spans = new List<TextSpan>();
            Style separator = context.Style(ThemeToken.Muted);
            string chevron = " " + context.Glyphs.Get(Glyph.Chevron) + " ";
            for (int i = 0; i < parts.Count; i++) {
                if (i > 0) { spans.Add(new TextSpan(chevron, separator)); }
                spans.AddRange(parts[i]);
            }
            return new StyledLine(spans);
        }

        // A run of phases before or after the current one; a single phase stays a chip until the counts stage.
        private void AddGroup(List<StyledLine> parts, int start, int end, int level, bool after, RenderContext context) {
            int count = end - start;
            if (count <= 0) { return; }
            if (count == 1 && level == 1) { parts.Add(Chip(_phases[start], context)); return; }
            StatusKind worst = _phases[start].Status;
            for (int i = start + 1; i < end; i++) {
                if (Rank(_phases[i].Status) < Rank(worst)) { worst = _phases[i].Status; }
            }
            string text = level >= 2
                ? count.ToString(CultureInfo.CurrentCulture)
                : WidgetLabels.Format(after ? context.Labels.MoreCount : context.Labels.EarlierCount, count);
            parts.Add(new StyledLine(new[] {
                new TextSpan(context.Glyphs.StatusMarker(worst), context.Style(worst)),
                new TextSpan(" " + text, context.Style(ThemeToken.Subtle))
            }));
        }
    }
}
