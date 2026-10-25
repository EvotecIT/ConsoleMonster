using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A one-line activity indicator for work without a known total: <c>⠹ Resolving DNS records  zone 4 of ?  3.2s</c>.
    /// While running it animates with the frame counter (braille in Full, <c>| / - \</c> in Basic and ASCII); once finished
    /// it shows the status marker in the status tone and the final duration.
    /// Live operations: <c>label</c> (Text), <c>detail</c> (Text), <c>state</c> (State, optional Text as the detail),
    /// <c>start</c> (restarts the clock and returns to running).
    /// </summary>
    public sealed class Spinner : LiveWidget, ILiveSummary, Output.ITaskbarProgressSource {
        /// <summary>Creates a spinner.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="widgetId">The live name (default <c>spinner</c>).</param>
        public Spinner(string label = "", string widgetId = "spinner") : base(widgetId) {
            Label = label ?? string.Empty;
        }

        /// <summary>The label (markup).</summary>
        public string Label { get; set; }

        /// <summary>Muted detail shown after the label, for example the current item.</summary>
        public string? Detail { get; set; }

        /// <summary>The status; anything other than Running or Pending replaces the animation with the status marker.</summary>
        public StatusKind Status { get; set; } = StatusKind.Running;

        /// <summary>Whether the elapsed time is shown on the right (default true).</summary>
        public bool ShowElapsed { get; set; } = true;

        /// <summary>
        /// When the current run started on the session clock, or null for "when the session started". Set by the
        /// <c>start</c> operation and by the first running <c>state</c> event.
        /// </summary>
        public TimeSpan? Started { get; set; }

        /// <summary>When the spinner finished on the session clock, or null while running.</summary>
        public TimeSpan? Finished { get; set; }

        /// <summary>Whether the spinner reached a final status.</summary>
        public bool IsFinished => Status != StatusKind.Running && Status != StatusKind.Pending;

        /// <summary>Returns a handle that updates this spinner through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public SpinnerHandle Handle(ILiveDispatcher dispatcher) => new SpinnerHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public bool TryGetTaskbarProgress(out Output.TaskbarState state, out double fraction) {
            fraction = double.NaN;
            state = Status == StatusKind.Running ? Output.TaskbarState.Indeterminate : Status == StatusKind.Fail ? Output.TaskbarState.Error : Output.TaskbarState.Clear;
            return state != Output.TaskbarState.Clear;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            TimeSpan now = TimeSpan.FromMilliseconds(liveEvent.Time);
            switch (liveEvent.Operation) {
                case "label":
                    Label = liveEvent.Text ?? string.Empty;
                    break;
                case "detail":
                    Detail = liveEvent.Text;
                    break;
                case "start":
                    Status = StatusKind.Running;
                    Started = now;
                    Finished = null;
                    if (!string.IsNullOrEmpty(liveEvent.Text)) {
                        Label = liveEvent.Text!;
                    }
                    break;
                case "state":
                    if (!StatusKinds.TryParse(liveEvent.Status, out StatusKind status)) {
                        break;
                    }
                    Status = status;
                    if (!string.IsNullOrEmpty(liveEvent.Text)) {
                        Detail = liveEvent.Text;
                    }
                    if (IsFinished) {
                        Finished = now;
                    } else {
                        Finished = null;
                        if (status == StatusKind.Running && !Started.HasValue) {
                            Started = now;
                        }
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            string label = Markup.Strip(Label);
            if (liveEvent.Operation == "start") {
                return label + " " + Localization.WidgetLabels.Current.Started;
            }
            if (liveEvent.Operation == "state" && IsFinished) {
                string detail = string.IsNullOrEmpty(Detail) ? string.Empty : ": " + Detail;
                return label + " " + Status.ToWord() + detail + " " + Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.TookDuration, NumberFormat.Duration(Duration(TimeSpan.Zero)));
            }
            return null;
        }

        /// <inheritdoc />
        public string? GetSummaryLine() {
            if (IsFinished) {
                return null;
            }
            string detail = string.IsNullOrEmpty(Detail) ? string.Empty : " (" + Detail + ")";
            return Markup.Strip(Label) + detail;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int width = MarkerWidth(context) + 1 + context.Markup(Label).Lines[0].Width;
            if (!string.IsNullOrEmpty(Detail)) {
                width += 2 + DisplayWidth.Measure(Detail!);
            }
            string elapsed = ElapsedText(context);
            if (elapsed.Length > 0) {
                width += 2 + DisplayWidth.Measure(elapsed);
            }
            return new Size(Math.Min(width, constraints.MaxWidth), Math.Min(1, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0 || region.Width <= 0) {
                return;
            }
            string marker = IsFinished || Status == StatusKind.Pending ? context.Glyphs.StatusMarker(Status) : Frame(context);
            ThemeToken markerToken = IsFinished ? Status.ToToken() : ThemeToken.Accent;
            region.Write(0, 0, marker, context.Style(markerToken));

            Style muted = context.Style(ThemeToken.Muted);
            string elapsed = ElapsedText(context);
            int elapsedWidth = DisplayWidth.Measure(elapsed);
            int right = elapsedWidth > 0 && elapsedWidth + 4 < region.Width ? region.Width - elapsedWidth : region.Width;
            if (right < region.Width) {
                region.Write(right, 0, elapsed, muted);
            }

            int x = MarkerWidth(context) + 1;
            int room = Math.Max(0, right - x - (right < region.Width ? 2 : 0));
            StyledLine label = TextWrapper.Truncate(context.Markup(Label).Lines[0], room, context.Ellipsis);
            region.Write(x, 0, label);
            x += label.Width;
            if (!string.IsNullOrEmpty(Detail) && room - label.Width > 4) {
                region.Write(x + 2, 0, TextWrapper.Truncate(Detail!, room - label.Width - 2, context.Ellipsis), muted);
            }
        }

        private string ElapsedText(RenderContext context) {
            if (!ShowElapsed) {
                return string.Empty;
            }
            TimeSpan duration = Duration(context.Elapsed);
            return duration > TimeSpan.Zero ? NumberFormat.Duration(duration) : string.Empty;
        }

        private TimeSpan Duration(TimeSpan now) {
            TimeSpan start = Started ?? TimeSpan.Zero;
            TimeSpan end = Finished ?? now;
            return end > start ? end - start : TimeSpan.Zero;
        }

        // The label column is the same whether running or finished (and across spinners stacked in rows), so it never
        // jumps when the animation is replaced by a status marker such as "[PASS]".
        private static int MarkerWidth(RenderContext context) {
            int width = 0;
            foreach (string frame in context.Glyphs.SpinnerFrames) {
                width = Math.Max(width, DisplayWidth.Measure(frame));
            }
            foreach (StatusKind status in FinalStatuses) {
                width = Math.Max(width, DisplayWidth.Measure(context.Glyphs.StatusMarker(status)));
            }
            return width;
        }

        private static readonly StatusKind[] FinalStatuses = { StatusKind.Pending, StatusKind.Pass, StatusKind.Fail, StatusKind.Warn, StatusKind.Skip };

        private static string Frame(RenderContext context) {
            return context.Spinner();
        }
    }
}
