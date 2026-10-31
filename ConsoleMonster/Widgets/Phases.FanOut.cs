using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Live;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Phases {
        private readonly Dictionary<string, List<StatusItem>> _phaseItems = new Dictionary<string, List<StatusItem>>(StringComparer.Ordinal);
        private int _fanOut = 5;

        // Status order for choosing which items stay visible and for summarising folded ones: problems first.
        private static readonly StatusKind[] Urgency = { StatusKind.Fail, StatusKind.Warn, StatusKind.Running, StatusKind.Pass, StatusKind.Skip, StatusKind.Info, StatusKind.Pending };

        /// <summary>
        /// Rows the current phase fans out to under the strip (default 5; zero hides items). The current phase is the
        /// first running one, otherwise the first failed one. When it has more items than rows, failed, warning and running
        /// items stay visible and the last row folds the rest into <c>+N more</c> with a count per status.
        /// </summary>
        public int FanOut {
            get => _fanOut;
            set { if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); } _fanOut = value; }
        }

        /// <summary>Adds an item to a phase (a user, file or server it is working on) or updates the item's status.</summary>
        /// <param name="phase">The key of an existing phase.</param>
        /// <param name="key">The item key, unique within the phase (also the label when <paramref name="label"/> is null).</param>
        /// <param name="status">The item status.</param>
        /// <param name="label">Label (markup), or null to keep the existing one.</param>
        /// <param name="detail">Detail (duration, percentage), or null to keep the existing one.</param>
        /// <returns>This instance.</returns>
        /// <exception cref="ArgumentException">The phase has not been added.</exception>
        public Phases SetItem(string phase, string key, StatusKind status = StatusKind.Pending, string? label = null, string? detail = null) {
            if (phase == null || !_byKey.ContainsKey(phase)) { throw new ArgumentException("Add the phase '" + phase + "' before its items.", nameof(phase)); }
            if (string.IsNullOrEmpty(key)) { throw new ArgumentException("An item needs a key.", nameof(key)); }
            SetItemCore(phase, key, status, label, detail);
            return this;
        }

        private void SetItemCore(string phase, string key, StatusKind? status, string? label, string? detail) {
            if (!_phaseItems.TryGetValue(phase, out List<StatusItem>? items)) {
                items = new List<StatusItem>();
                _phaseItems[phase] = items;
            }
            StatusItem? item = items.Find(existing => existing.Key == key);
            if (item == null) {
                items.Add(new StatusItem(key, string.IsNullOrEmpty(label) ? Markup.Escape(key) : label!, status ?? StatusKind.Pending, string.IsNullOrEmpty(detail) ? null : detail));
                return;
            }
            if (status.HasValue) { item.Status = status.Value; }
            if (!string.IsNullOrEmpty(label)) { item.Label = label!; }
            if (!string.IsNullOrEmpty(detail)) { item.Detail = detail; }
        }

        // Live item events address "phase␟item"; the unit separator cannot appear in a typed key.
        internal const char ItemSeparator = '\u001f';

        private bool ApplyItem(LiveEvent liveEvent) {
            int split = liveEvent.Key!.IndexOf(ItemSeparator);
            if (split <= 0 || split == liveEvent.Key.Length - 1) { return false; }
            string phase = liveEvent.Key.Substring(0, split);
            string key = liveEvent.Key.Substring(split + 1);
            if (!_byKey.ContainsKey(phase)) { return true; }
            if (liveEvent.Operation == "item") {
                StatusKind? status = StatusKinds.TryParse(liveEvent.Status, out StatusKind parsed) ? parsed : (StatusKind?)null;
                SetItemCore(phase, key, status, liveEvent.Text, null);
            } else {
                SetItemCore(phase, key, null, null, liveEvent.Text);
            }
            return true;
        }

        internal bool HasItems => _phaseItems.Count > 0;

        private List<StatusItem>? FanOutItems(out int phaseIndex) {
            phaseIndex = _phases.FindIndex(phase => phase.Status == StatusKind.Running);
            if (phaseIndex < 0) { phaseIndex = _phases.FindIndex(phase => phase.Status == StatusKind.Fail); }
            if (phaseIndex < 0 || _fanOut == 0) { return null; }
            return _phaseItems.TryGetValue(_phases[phaseIndex].Key, out List<StatusItem>? items) && items.Count > 0 ? items : null;
        }

        private static int Rank(StatusKind status) {
            int index = Array.IndexOf(Urgency, status);
            return index < 0 ? Urgency.Length : index;
        }

        // Rows under the current phase: items in their own order, then "+N more" for the ones that did not fit.
        private List<StyledLine> FanOutLines(List<StatusItem> items, int width, RenderContext context) {
            List<StatusItem> shown = items;
            List<StatusItem> hidden = new List<StatusItem>();
            if (items.Count > _fanOut) {
                // Keep the most urgent items, shown in their original order.
                HashSet<StatusItem> keep = new HashSet<StatusItem>(items.Select((item, index) => (item, index))
                    .OrderBy(pair => Rank(pair.item.Status)).ThenBy(pair => pair.index).Take(_fanOut - 1).Select(pair => pair.item));
                shown = items.FindAll(keep.Contains);
                hidden = items.FindAll(item => !keep.Contains(item));
            }
            IGlyphSet glyphs = context.Glyphs;
            Style branch = context.Style(ThemeToken.Subtle);
            List<StyledLine> lines = new List<StyledLine>();
            for (int i = 0; i < shown.Count; i++) {
                bool last = i == shown.Count - 1 && hidden.Count == 0;
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(glyphs.Get(last ? Glyph.TreeLast : Glyph.TreeBranch), branch) };
                spans.AddRange(Chip(shown[i], context, animate: false));
                lines.Add(TextWrapper.Truncate(new StyledLine(spans), width, context.Ellipsis));
            }
            if (hidden.Count > 0) {
                List<TextSpan> spans = new List<TextSpan> {
                    new TextSpan(glyphs.Get(Glyph.TreeLast), branch),
                    new TextSpan(WidgetLabels.Format(context.Labels.MoreCount, hidden.Count), context.Style(ThemeToken.Subtle))
                };
                string separator = " " + glyphs.Get(Glyph.Dot) + " ";
                foreach (StatusKind status in Urgency) {
                    int count = hidden.FindAll(item => item.Status == status).Count;
                    if (count == 0) { continue; }
                    spans.Add(new TextSpan(separator, branch));
                    spans.Add(new TextSpan(glyphs.StatusMarker(status) + " " + count.ToString(System.Globalization.CultureInfo.CurrentCulture), context.Style(status)));
                }
                lines.Add(TextWrapper.Truncate(new StyledLine(spans), width, context.Ellipsis));
            }
            return lines;
        }
    }
}
