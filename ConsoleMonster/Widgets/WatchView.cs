using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The view of a command run again and again (<see cref="ConsoleWatch"/>, Watch-Console): a heading with the interval,
    /// the title, the run number and the time of the last run, then the latest output. With <see cref="Differences"/>, the
    /// cells that changed since the previous run are shown in reverse video, so changes stand out without colour.
    /// <para>Live operation: <c>update</c> (Payload = the new content widget, Value = the run number, Text = a status such
    /// as an error message, or null).</para>
    /// </summary>
    public sealed class WatchView : LiveWidget {
        private IWidget _content = new TextBlock(string.Empty);
        private string[,]? _previous;
        private string[,]? _rendered;
        private DateTimeOffset? _lastRun;

        /// <summary>Creates a watch view.</summary>
        /// <param name="name">The live name (default <c>watch</c>).</param>
        public WatchView(string name = "watch") : base(name) {
        }

        /// <summary>What is being watched, shown in the heading (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>The time between runs, shown in the heading.</summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>Whether cells that changed since the previous run are highlighted (reverse video).</summary>
        public bool Differences { get; set; }

        /// <summary>The number of the latest run (1 for the first).</summary>
        public int Iteration { get; private set; }

        /// <summary>A status shown after the heading (an error of the last run), or null.</summary>
        public string? Status { get; private set; }

        /// <summary>Shows the output of a new run; the previous output becomes the base for <see cref="Differences"/>.</summary>
        /// <param name="content">The new content.</param>
        /// <param name="iteration">The run number.</param>
        /// <param name="status">A status (an error message), or null.</param>
        /// <param name="time">The time of the run, or null for now.</param>
        /// <returns>This instance.</returns>
        public WatchView Update(IWidget content, int iteration, string? status = null, DateTimeOffset? time = null) {
            _previous = _rendered;
            _content = content ?? new TextBlock(string.Empty);
            Iteration = iteration;
            Status = status;
            _lastRun = time ?? DateTimeOffset.Now;
            return this;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent != null && liveEvent.Operation == "update" && liveEvent.Payload is IWidget content) {
                Update(content, (int)liveEvent.Value, liveEvent.Text);
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            Size inner = _content.Measure(new Constraints(constraints.MaxWidth, Math.Max(0, constraints.MaxHeight - 1)), context);
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, inner.Height + 1));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            region.Write(0, 0, TextWrapper.Truncate(Heading(context), region.Width, context.Ellipsis));
            if (region.Height <= 1) {
                return;
            }
            Region body = region.Sub(0, 1, region.Width, region.Height - 1);
            _content.Render(body, context);
            string[,] grid = new string[body.Width, body.Height];
            for (int y = 0; y < body.Height; y++) {
                for (int x = 0; x < body.Width; x++) {
                    Cell cell = body.Get(x, y);
                    grid[x, y] = cell.Glyph;
                    // A changed cell is marked, also one that turned blank (a value that disappeared).
                    if (Differences && _previous != null && !cell.IsContinuation && Changed(_previous, x, y, cell.Glyph)) {
                        body.SetStyle(x, y, cell.Style.With(TextDecoration.Reverse));
                    }
                }
            }
            _rendered = grid;
        }

        private static bool Changed(string[,] previous, int x, int y, string glyph) {
            string before = x < previous.GetLength(0) && y < previous.GetLength(1) ? previous[x, y] ?? " " : " ";
            return !string.Equals(before, glyph, StringComparison.Ordinal);
        }

        // "Every 2s: Get-Process   #3 · 14:05:33" and the status of the last run.
        private StyledLine Heading(RenderContext context) {
            Style muted = context.Style(ThemeToken.Muted);
            System.Collections.Generic.List<TextSpan> spans = new System.Collections.Generic.List<TextSpan> {
                new TextSpan(Localization.WidgetLabels.Format(context.Labels.WatchEvery, NumberFormat.Duration(Interval)), muted)
            };
            if (!string.IsNullOrEmpty(Title)) {
                spans.Add(new TextSpan(" "));
                spans.AddRange(context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Text)));
            }
            string separator = " " + Interactive.InteractiveGlyphs.For(context).Separator + " ";
            if (Iteration > 0) {
                spans.Add(new TextSpan("   #" + Iteration.ToString(CultureInfo.InvariantCulture), muted));
            }
            if (_lastRun.HasValue) {
                spans.Add(new TextSpan(separator + _lastRun.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture), muted));
            }
            if (!string.IsNullOrEmpty(Status)) {
                spans.Add(new TextSpan(separator + Status, context.Style(StatusKind.Fail)));
            }
            return new StyledLine(spans);
        }
    }
}
