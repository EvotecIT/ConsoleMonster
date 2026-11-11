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
        /// items have priority for the available rows. The last row folds the remaining items into <c>+N more</c> with a count per status.
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
        /// <exception cref="ArgumentException">The phase has not been added, or the key is empty or contains U+001F.</exception>
        public Phases SetItem(string phase, string key, StatusKind status = StatusKind.Pending, string? label = null, string? detail = null) {
            if (phase == null || !_byKey.ContainsKey(phase)) { throw new ArgumentException("Add the phase '" + phase + "' before its items.", nameof(phase)); }
            if (string.IsNullOrEmpty(key)) { throw new ArgumentException("An item needs a key.", nameof(key)); }
            CheckKey(key, nameof(key));
            SetItemCore(phase, key, status, label, detail);
            return this;
        }

        /// <summary>
        /// Adds a sub-item under an item of a phase (a package under the stage that signs it) or updates its status. The
        /// step ledger lists sub-items under their item and folds a finished item's sub-items into a "done/total" count
        /// when it is short of space; the fan-out and the compact chain show items only.
        /// </summary>
        /// <param name="phase">The key of an existing phase.</param>
        /// <param name="item">The key of an existing item of that phase.</param>
        /// <param name="key">The sub-item key, unique within the item (also the label when <paramref name="label"/> is null).</param>
        /// <param name="status">The sub-item status.</param>
        /// <param name="label">Label (markup), or null to keep the existing one.</param>
        /// <param name="detail">Detail, or null to keep the existing one.</param>
        /// <returns>This instance.</returns>
        /// <exception cref="ArgumentException">The phase or the item has not been added, or the key is empty or contains U+001F.</exception>
        public Phases SetSubItem(string phase, string item, string key, StatusKind status = StatusKind.Pending, string? label = null, string? detail = null) {
            if (phase == null || item == null || FindItem(new[] { phase, item }) == null) { throw new ArgumentException("Add the item '" + item + "' of phase '" + phase + "' before its sub-items.", nameof(item)); }
            if (string.IsNullOrEmpty(key)) { throw new ArgumentException("A sub-item needs a key.", nameof(key)); }
            CheckKey(key, nameof(key));
            SetItemCore(phase + ItemSeparator + item, key, status, label, detail);
            return this;
        }

        // Items are listed by their owner's path: a phase key for items, "phase␟item" for sub-items.
        private void SetItemCore(string owner, string key, StatusKind? status, string? label, string? detail, bool replaceDetail = false) {
            if (!_phaseItems.TryGetValue(owner, out List<StatusItem>? items)) {
                items = new List<StatusItem>();
                _phaseItems[owner] = items;
            }
            StatusItem? item = items.Find(existing => existing.Key == key);
            if (item == null) {
                items.Add(new StatusItem(key, string.IsNullOrEmpty(label) ? Markup.Escape(key) : label!, status ?? StatusKind.Pending, string.IsNullOrEmpty(detail) ? null : detail));
                return;
            }
            if (status.HasValue) { item.Status = status.Value; }
            if (!string.IsNullOrEmpty(label)) { item.Label = label!; }
            if (replaceDetail || !string.IsNullOrEmpty(detail)) { item.Detail = detail; }
        }

        // Live item events address "phase␟item" or "phase␟item␟sub-item", so no phase, item or sub-item key may contain the
        // unit separator: "p␟a" as a phase would share item lists and timings with item "a" of phase "p".
        internal const char ItemSeparator = '\u001f';

        private static void CheckKey(string key, string name) {
            if (key != null && key.IndexOf(ItemSeparator) >= 0) { throw new ArgumentException("A phase or item key cannot contain U+001F, which separates phase and item keys.", name); }
        }

        private bool ApplyItem(LiveEvent liveEvent) {
            string[] path = liveEvent.Key!.Split(ItemSeparator);
            if (path.Length < 2 || path.Length > 3 || Array.Exists(path, part => part.Length == 0)) { return false; }
            if (!_byKey.ContainsKey(path[0])) { return true; }
            string owner = path[0];
            if (path.Length == 3) {
                // A sub-item needs its item first.
                if (FindItem(new[] { path[0], path[1] }) == null) { return true; }
                owner = path[0] + ItemSeparator + path[1];
            }
            string key = path[path.Length - 1];
            if (liveEvent.Operation == "item") {
                StatusKind? status = StatusKinds.TryParse(liveEvent.Status, out StatusKind parsed) ? parsed : (StatusKind?)null;
                SetItemCore(owner, key, status, liveEvent.Text, null);
                if (status.HasValue) { Track(liveEvent.Key, status.Value, liveEvent); }
            } else {
                SetItemCore(owner, key, null, null, liveEvent.Text, replaceDetail: true);
            }
            return true;
        }

        // The item or sub-item a path ("phase", "item"[, "sub-item"]) names, or null.
        private StatusItem? FindItem(string[] path) {
            string owner = path[0];
            StatusItem? found = null;
            for (int i = 1; i < path.Length; i++) {
                if (!_phaseItems.TryGetValue(owner, out List<StatusItem>? items)) { return null; }
                found = items.Find(item => item.Key == path[i]);
                if (found == null) { return null; }
                owner += ItemSeparator + path[i];
            }
            return found;
        }

        // When rows started (session time) and how long finished rows took, by path ("phase" or "phase␟item[␟sub-item]").
        private readonly Dictionary<string, TimeSpan> _took = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

        // Records a row's timing from its state events: a running row starts its clock once, a finished row keeps the time
        // it took (Value in milliseconds when the event gives it, otherwise measured from the start). Returns that time.
        private TimeSpan? Track(string path, StatusKind status, LiveEvent liveEvent) {
            TimeSpan now = TimeSpan.FromMilliseconds(liveEvent.Time);
            if (status == StatusKind.Running) {
                if (!_started.ContainsKey(path)) { _started[path] = now; }
                _took.Remove(path);
                return null;
            }
            TimeSpan? took = null;
            if (status != StatusKind.Pending && liveEvent.Value > 0) {
                took = TimeSpan.FromMilliseconds(liveEvent.Value);
            } else if (status != StatusKind.Pending && _started.TryGetValue(path, out TimeSpan started)) {
                took = now - started < TimeSpan.Zero ? TimeSpan.Zero : now - started;
            }
            _started.Remove(path);
            if (took.HasValue) { _took[path] = took.Value; } else { _took.Remove(path); }
            return took;
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
