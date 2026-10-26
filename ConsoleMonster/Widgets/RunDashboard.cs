using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>A height-bounded view of a declared run: totals, active work, queued work, groups and pinned problems.</summary>
    public sealed partial class RunDashboard : IWidget, IContainer {
        private readonly RunTracker _tracker;
        /// <summary>Creates a dashboard over a tracker with declared <see cref="RunItem"/> metadata.</summary>
        /// <param name="tracker">The shared run state.</param>
        public RunDashboard(RunTracker tracker) { _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker)); }
        /// <summary>The run name, shown as literal text.</summary>
        public string Title { get; set; } = "Run";
        /// <summary>Whether group rows may use spare dashboard space.</summary>
        public bool ShowGroups { get; set; } = true;
        /// <summary>Whether the key hints are visible.</summary>
        public bool ShowKeys { get; set; }
        /// <summary>Whether the key help is expanded.</summary>
        public bool ShowHelp { get; set; }
        /// <summary>Whether completion lines are temporarily held.</summary>
        public bool LogPaused { get; set; }
        /// <summary>Whether only problems are appended to the log.</summary>
        public bool ProblemsOnly { get; set; }
        /// <summary>The optional throughput trend beside the rate.</summary>
        public bool ShowTrend { get; set; }
        /// <summary>Captured output and completion events. Entries stream to scrollback while its retained history stays bounded.</summary>
        public LogRegion Log { get; } = new LogRegion("run-log") { ShowElapsed = false, Scrollback = true, Capacity = 1000 };
        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new IWidget[] { Log };
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, 20));
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Height == 0 || region.Width == 0) { return; }
            if (region.Height >= 4) {
                Line(new string(context.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? '-' : '─', region.Width), ThemeToken.Muted).Render(region.Sub(0, 0, region.Width, 1), context);
                region = region.Sub(0, 1, region.Width, region.Height - 1);
            }
            RunSnapshot snapshot = _tracker.Snapshot();
            List<IWidget> rows = Header(snapshot, context, region.Width);
            RunItemSnapshot[] problems = snapshot.Items.Where(i => i.Status == StatusKind.Fail || i.Status == StatusKind.Warn).OrderBy(i => i.CompletionOrder).ToArray();
            RunItemSnapshot[] running = snapshot.Items.Where(i => i.Status == StatusKind.Running).ToArray();
            RunItemSnapshot[] next = snapshot.Items.Where(i => i.Status == StatusKind.Pending).ToArray();
            if (region.Height <= 3) {
                string totals = snapshot.Done + "/" + snapshot.Total + " · " + CountMarkup(StatusKind.Fail, snapshot.Failed, snapshot.Failed + " fail") + " · " + CountMarkup(StatusKind.Warn, snapshot.Warned, snapshot.Warned + " warn");
                if (region.Height == 1) { new TextBlock(totals) { Overflow = TextOverflow.Ellipsis }.Render(region, context); return; }
                new TextBlock(Markup.Escape(Title) + " · " + totals) { Overflow = TextOverflow.Ellipsis }.Render(region.Sub(0, 0, region.Width, 1), context);
                if (region.Height == 3) { Line(running.Length + " running · " + next.Length + " queued", ThemeToken.Muted).Render(region.Sub(0, 1, region.Width, 1), context); }
                IWidget last = problems.Length > 0 ? Problem(problems[problems.Length - 1], context) : Line("Problems · 0", ThemeToken.Muted);
                last.Render(region.Sub(0, region.Height - 1, region.Width, 1), context);
                return;
            }
            if (region.Height < 7) {
                List<IWidget> compact = new List<IWidget> {
                    Label(Title + " · " + snapshot.Done + "/" + snapshot.Total + " · " + (snapshot.Total > 0 ? 100 * snapshot.Done / snapshot.Total : 100) + "%"),
                    Counts(snapshot, context, compact: true, includeGlyphs: false),
                    Line(running.Length + " running · " + next.Length + " queued · ETA " + (snapshot.Remaining.HasValue ? NumberFormat.Duration(snapshot.Remaining.Value) : "--"), ThemeToken.Muted),
                    Label("Problems · " + problems.Length)
                };
                if (problems.Length > 0) {
                    if (region.Height == 4) { compact[3] = Problem(problems[problems.Length - 1], context); }
                    else { compact.Add(Problem(problems[problems.Length - 1], context)); }
                }
                for (int i = 0; i < Math.Min(region.Height, compact.Count); i++) { compact[i].Render(region.Sub(0, i, region.Width, 1), context); }
                return;
            }
            int problemRows = Math.Min(Math.Max(1, region.Height - 5 - (ShowKeys ? 1 : 0)), problems.Length == 0 ? 1 : Math.Min(3, problems.Length + 1));
            // Reserve totals and pinned problems first. Group details can trade queued preview rows for available height.
            int spare = Math.Max(0, region.Height - rows.Count - problemRows - 2 - (ShowKeys ? 1 : 0));
            int columns = region.Width >= 100 ? 2 : 1;
            int needed = (running.Length + columns - 1) / columns;
            int activeRows = spare >= needed ? needed : 0;
            spare -= activeRows;
            IGrouping<string, RunItemSnapshot>[] groups = snapshot.Items.Where(i => i.Item.Group.Length > 0).GroupBy(i => i.Item.Group).ToArray();
            int groupRows = ShowGroups && groups.Length > 0 && spare > 0 ? (spare >= groups.Length + 1 ? groups.Length + 1 : 1) : 0;
            int nextRows = Math.Min(3, Math.Min(next.Length, Math.Max(0, spare - groupRows)));
            spare -= nextRows;
            rows.Add(Label("Running now · " + running.Length + " workers"));
            for (int i = 0; i < activeRows; i++) {
                string spin = context.Glyphs.SpinnerFrames[(int)(context.Tick % context.Glyphs.SpinnerFrames.Count)];
                rows.Add(new ActiveRow(running.Skip(i * columns).Take(columns).ToArray(), spin));
            }
            rows.Add(Label("Up next · " + next.Length + " queued"));
            foreach (RunItemSnapshot item in next.Take(nextRows)) { rows.Add(Line("  " + ItemName(item), ThemeToken.Muted)); }
            if (nextRows > 0 && next.Length > nextRows) { rows[rows.Count - 1] = Line("  " + ItemName(next[nextRows - 1]) + " · + " + (next.Length - nextRows) + " queued", ThemeToken.Muted); }
            if (groupRows > 0) {
                if (groupRows > 1) {
                    rows.Add(Label("Groups"));
                    foreach (IGrouping<string, RunItemSnapshot> group in groups) { rows.Add(new GroupRow(group.Key, group.ToArray())); }
                } else { rows.Add(Label("Groups · " + groups.Length + " categories")); }
            }
            int hidden = Math.Max(0, problems.Length - (problemRows - 1));
            List<IWidget> pinned = new List<IWidget> { Label("Problems so far · " + problems.Length + (hidden > 0 ? " · + " + hidden + " more" : "")) };
            int visible = Math.Max(0, problemRows - 1);
            foreach (RunItemSnapshot item in problems.Skip(Math.Max(0, problems.Length - visible))) {
                pinned.Add(Problem(item, context));
            }
            int y = 0;
            int beforePinned = Math.Max(0, region.Height - pinned.Count - (ShowKeys && region.Height > 7 ? 1 : 0));
            foreach (IWidget row in rows.Take(beforePinned)) { row.Render(region.Sub(0, y++, region.Width, 1), context); }
            foreach (IWidget row in pinned) { if (y < region.Height) { row.Render(region.Sub(0, y++, region.Width, 1), context); } }
            if (ShowKeys && y < region.Height) {
                string keys = ShowHelp ? "p pause/resume log · f problems only · g groups · ? help" : "p log" + (LogPaused ? " paused" : "") + " · f " + (ProblemsOnly ? "problems" : "all") + " · g groups · ? help";
                Line(keys, ThemeToken.Muted).Render(region.Sub(0, y, region.Width, 1), context);
            }
        }

        private List<IWidget> Header(RunSnapshot snapshot, RenderContext context, int width) {
            string eta = snapshot.Remaining.HasValue ? NumberFormat.Duration(snapshot.Remaining.Value) : "--";
            List<IWidget> rows = new List<IWidget> {
                new HeaderRow(Title, snapshot),
                new RateRow(snapshot, "Elapsed " + NumberFormat.Duration(snapshot.Elapsed) + " · ETA " + eta + " · " + (snapshot.Rate * 60).ToString("0.0", CultureInfo.InvariantCulture) + "/min", ShowTrend),
                Counts(snapshot, context, width < 80)
            };
            return rows;
        }

        internal static IWidget Counts(RunSnapshot snapshot, RenderContext context, bool compact = false, bool includeGlyphs = true) {
            string text = string.Empty;
            foreach (var pair in new[] { (StatusKind.Pass, snapshot.Passed, "passed"), (StatusKind.Fail, snapshot.Failed, "failed"), (StatusKind.Warn, snapshot.Warned, "warnings"), (StatusKind.Skip, snapshot.Skipped, "skipped") }) {
                if (text.Length > 0) { text += " · "; }
                string label = compact ? (pair.Item1 == StatusKind.Warn ? "warn" : pair.Item1.ToString().ToLowerInvariant()) : pair.Item3;
                text += CountMarkup(pair.Item1, pair.Item2, (includeGlyphs ? context.Glyphs.StatusSymbol(pair.Item1) + " " : "") + pair.Item2 + " " + label);
            }
            return new TextBlock(text) { Overflow = TextOverflow.Ellipsis };
        }
        private static string CountMarkup(StatusKind state, int count, string text) => "[" + (count > 0 ? state.ToString().ToLowerInvariant() : "muted") + "]" + Markup.Escape(text) + "[/]";
        internal static IWidget Label(string text) => new TextBlock("[bold]" + Markup.Escape(text) + "[/]") { Role = ThemeToken.Muted, Overflow = TextOverflow.Ellipsis };
        internal static IWidget Line(string text, ThemeToken role = ThemeToken.Text) => new TextBlock(Markup.Escape(text)) { Role = role, Overflow = TextOverflow.Ellipsis };
        internal static string ItemName(RunItemSnapshot item) => item.Item.DisplayName;
        internal static IWidget Problem(RunItemSnapshot item, RenderContext context) => Line(context.Glyphs.StatusSymbol(item.Status) + " " + ItemName(item) + (item.Detail.Length > 0 ? ": " + item.Detail : ""), item.Status == StatusKind.Fail ? ThemeToken.Fail : ThemeToken.Warn);
    }
}
