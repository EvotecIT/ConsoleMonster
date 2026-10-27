using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Nested activities with progress, the way <c>Write-Progress</c> describes work: each activity has an id, an optional
    /// parent, a status, a percentage (or none) and an estimate of the time left. Children are indented under their
    /// parent; completed activities disappear. Empty, it takes no space, so it can sit in any layout and appear only while
    /// something reports progress.
    /// <para>Events: <c>update</c> with key <c>"id"</c> or <c>"id/parentId"</c>, text = activity, state = status, value =
    /// percent (negative for unknown) and total = seconds remaining (negative for unknown); <c>complete</c> with the key.</para>
    /// </summary>
    public sealed class ActivityList : LiveWidget, Output.ITaskbarProgressSource {
        /// <summary>The target name the PowerShell progress bridge posts to.</summary>
        public const string ProgressTarget = "$progress";

        private readonly List<Activity> _activities = new List<Activity>();
        private int _titleWidth = 6;
        private int _idWidth;

        /// <summary>Creates an activity list.</summary>
        /// <param name="widgetId">The target name (default <see cref="ProgressTarget"/>).</param>
        public ActivityList(string widgetId = ProgressTarget) : base(widgetId) {
        }

        /// <summary>The most activities shown (default 8); more are summarised in one line.</summary>
        public int MaxVisible { get; set; } = 8;

        /// <summary>The number of activities in progress.</summary>
        public int Count => _activities.Count;

        /// <summary>Whether each row ends with the time since the activity started, right-aligned (default true).</summary>
        public bool ShowElapsed { get; set; } = true;

        /// <summary>Whether each row starts with the activity's id (<c>#3</c>), for parallel work (default false).</summary>
        public bool ShowIds { get; set; }

        /// <inheritdoc />
        public bool TryGetTaskbarProgress(out Output.TaskbarState state, out double fraction) {
            state = Output.TaskbarState.Clear;
            fraction = double.NaN;
            if (_activities.Count == 0) {
                return false;
            }
            double sum = 0;
            int counted = 0;
            HashSet<int> ids = new HashSet<int>();
            foreach (Activity activity in _activities) { ids.Add(activity.Id); }
            foreach (Activity activity in _activities) {
                if ((activity.Parent == -1 || !ids.Contains(activity.Parent)) && activity.Percent >= 0) {
                    sum += Math.Min(100, activity.Percent) / 100.0;
                    counted++;
                }
            }
            fraction = counted > 0 ? sum / counted : double.NaN;
            state = counted > 0 ? Output.TaskbarState.Normal : Output.TaskbarState.Indeterminate;
            return true;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent == null || liveEvent.Key == null) {
                return;
            }
            ParseKey(liveEvent.Key, out int id, out int parent);
            int index = _activities.FindIndex(a => a.Id == id);
            if (liveEvent.Operation == "complete") {
                if (index >= 0) {
                    RemoveWithChildren(id);
                }
                return;
            }
            bool patch = liveEvent.Operation == "label" || liveEvent.Operation == "detail" || liveEvent.Operation == "value" || liveEvent.Operation == "remaining" || liveEvent.Operation == "parent";
            if (liveEvent.Operation != "update" && !patch) {
                return;
            }
            // An event without a time stamp (no session) leaves the start unknown, so no elapsed time is shown.
            Activity activity = index >= 0 ? _activities[index] : new Activity(id) { Started = liveEvent.Time > 0 ? TimeSpan.FromMilliseconds(liveEvent.Time) : (TimeSpan?)null };
            if (patch) {
                switch (liveEvent.Operation) {
                    case "label": activity.Title = liveEvent.Text ?? string.Empty; break;
                    case "detail": activity.Status = liveEvent.Text; break;
                    case "value": activity.Percent = liveEvent.Value; break;
                    case "remaining": activity.SecondsLeft = liveEvent.Value; break;
                    case "parent": activity.Parent = (int)liveEvent.Value; break;
                }
            } else {
                activity.Parent = parent;
                activity.Title = string.IsNullOrEmpty(liveEvent.Text) ? activity.Title : liveEvent.Text!;
                activity.Status = liveEvent.Status;
                activity.Percent = liveEvent.Value;
                activity.SecondsLeft = liveEvent.Total;
            }
            if (index < 0) {
                _activities.Add(activity);
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent?.Key == null || liveEvent.Operation != "update" || liveEvent.Value < 0) {
                return null;
            }
            // Stream lines only at whole tens of percent, so CI logs stay short.
            int percent = (int)liveEvent.Value;
            return percent % 10 == 0 ? (liveEvent.Text ?? string.Empty) + " " + percent.ToString(CultureInfo.InvariantCulture) + "%" + (string.IsNullOrEmpty(liveEvent.Status) ? string.Empty : " " + liveEvent.Status) : null;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int rows = Math.Min(_activities.Count, MaxVisible) + (_activities.Count > MaxVisible ? 1 : 0);
            return new Size(constraints.MaxWidth, Math.Min(rows, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            List<KeyValuePair<Activity, int>> ordered = new List<KeyValuePair<Activity, int>>();
            Order(-1, 0, ordered, new HashSet<int>());
            foreach (Activity orphan in _activities) {
                if (!ordered.Exists(p => p.Key == orphan)) {
                    ordered.Add(new KeyValuePair<Activity, int>(orphan, 0));
                }
            }
            _titleWidth = 6;
            _idWidth = 0;
            foreach (KeyValuePair<Activity, int> pair in ordered) {
                _idWidth = Math.Max(_idWidth, pair.Key.Id.ToString(CultureInfo.InvariantCulture).Length + 2);
                _titleWidth = Math.Max(_titleWidth, DisplayWidth.Measure(pair.Key.Title) + (pair.Value * 2) + 1);
            }
            int y = 0;
            int shown = Math.Min(ordered.Count, Math.Min(MaxVisible, region.Height - (ordered.Count > MaxVisible ? 1 : 0)));
            for (int i = 0; i < shown && y < region.Height; i++, y++) {
                DrawActivity(region, y, ordered[i].Key, ordered[i].Value, context);
            }
            if (ordered.Count > shown && y < region.Height) {
                region.Write(0, y, Localization.WidgetLabels.Format(context.Labels.MoreCount, (ordered.Count - shown).ToString(context.Culture)), context.Style(ThemeToken.Muted));
            }
        }

        private void DrawActivity(Region region, int y, Activity activity, int depth, RenderContext context) {
            int indent = Math.Min(depth * 2, Math.Max(0, region.Width / 3));
            int width = region.Width - indent;
            if (width < 8) {
                return;
            }
            // An activity at 100 % that was never marked complete is done: a check mark, not a spinner.
            bool done = activity.Percent >= 100;
            // Work in progress spins, with or without a percentage.
            string glyph = done ? context.Glyphs.StatusSymbol(StatusKind.Pass) : context.Spinner(activity.Id);
            region.Write(indent, y, glyph, context.Style(done ? ThemeToken.Pass : ThemeToken.Running));
            // The elapsed time sits at the right edge (when the activity's start is known); the rest of the row is laid out before it.
            string elapsed = ShowElapsed && activity.Started.HasValue && context.Elapsed > activity.Started.Value ? NumberFormat.Duration(context.Elapsed - activity.Started.Value) : string.Empty;
            int elapsedWidth = elapsed.Length > 0 ? DisplayWidth.Measure(elapsed) + 1 : 0;
            if (elapsed.Length > 0 && region.Width - elapsedWidth > indent + 8) {
                region.Write(region.Width - elapsedWidth + 1, y, elapsed, context.Style(ThemeToken.Muted));
                region = region.Sub(0, 0, region.Width - elapsedWidth, region.Height);
                width = region.Width - indent;
            }
            int titleX = indent + 2;
            if (ShowIds) {
                // Ids share one width (the widest), so titles and bars stay in line between #9 and #10.
                string idText = ("#" + activity.Id.ToString(CultureInfo.InvariantCulture)).PadRight(_idWidth - 1) + " ";
                region.Write(titleX, y, idText, context.Style(ThemeToken.Muted));
                titleX += _idWidth;
            }
            string tail = activity.Status ?? string.Empty;
            if (activity.SecondsLeft >= 0) {
                string left = "~" + Localization.WidgetLabels.Format(context.Labels.TimeLeft, NumberFormat.Remaining(TimeSpan.FromSeconds(activity.SecondsLeft)));
                tail = tail.Length == 0 ? left : tail + " " + Interactive.InteractiveGlyphs.For(context).Separator + " " + left;
            }
            // The title column ends at the same place for every depth, so bars line up under each other.
            int titleWidth = Math.Max(4, Math.Max(6, Math.Min(Math.Min(32, region.Width / 3), _titleWidth)) - indent - (ShowIds ? _idWidth : 0));
            if (activity.Percent < 0) {
                // Without a percentage there is no bar: the title takes the width the status leaves.
                int tailWidth = tail.Length > 0 ? DisplayWidth.Measure(tail) + 1 : 0;
                titleWidth = Math.Max(titleWidth, Math.Min(DisplayWidth.Measure(activity.Title), region.Width - titleX - 1 - tailWidth));
            }
            region.Write(titleX, y, TextWrapper.Truncate(activity.Title, titleWidth, context.Ellipsis), context.Style(depth == 0 ? ThemeToken.Highlight : ThemeToken.Text));
            int x = titleX + titleWidth + 1;
            int barWidth = Math.Max(0, Math.Min(30, (width - titleWidth - 3) / 3));
            if (activity.Percent >= 0 && barWidth >= 4) {
                BarDrawer.Draw(region, x, y, barWidth, Math.Min(1, activity.Percent / 100.0), context.Style(ThemeToken.Running), context, true, BarShape.Line);
                x += barWidth + 1;
                string percent = ((int)Math.Min(100, activity.Percent)).ToString(context.Culture) + "%";
                region.Write(x, y, percent.PadLeft(4), context.Style(ThemeToken.Text));
                x += 5;
            }
            if (tail.Length > 0 && x < region.Width - 1) {
                region.Write(x, y, TextWrapper.Truncate(tail, region.Width - x, context.Ellipsis), context.Style(ThemeToken.Muted));
            }
        }


        private void Order(int parent, int depth, List<KeyValuePair<Activity, int>> ordered, HashSet<int> seen) {
            foreach (Activity a in _activities) {
                if (a.Parent == parent && seen.Add(a.Id)) {
                    ordered.Add(new KeyValuePair<Activity, int>(a, depth));
                    Order(a.Id, depth + 1, ordered, seen);
                }
            }
        }

        private void RemoveWithChildren(int id) {
            _activities.RemoveAll(a => a.Id == id);
            foreach (Activity child in _activities.FindAll(a => a.Parent == id)) {
                RemoveWithChildren(child.Id);
            }
        }

        private static void ParseKey(string key, out int id, out int parent) {
            parent = -1;
            int slash = key.IndexOf('/');
            string idText = slash >= 0 ? key.Substring(0, slash) : key;
            if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)) {
                id = idText.GetHashCode();
            }
            if (slash >= 0 && int.TryParse(key.Substring(slash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p)) {
                parent = p;
            }
        }

        private sealed class Activity {
            public Activity(int id) {
                Id = id;
                Title = string.Empty;
            }

            public int Id { get; }

            public int Parent { get; set; } = -1;

            public string Title { get; set; }

            public string? Status { get; set; }

            public double Percent { get; set; } = -1;

            public double SecondsLeft { get; set; } = -1;

            public TimeSpan? Started { get; set; }
        }
    }
}
