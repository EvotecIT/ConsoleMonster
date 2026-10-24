using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A labelled progress bar with percentage and count: <c>Build  ████████▌░░░░  64%  32/50</c>.
    /// Live operations: <c>set</c> (Value, Total), <c>advance</c> (Value = amount), <c>label</c> (Text), <c>state</c> (State).
    /// </summary>
    public sealed class ProgressBar : LiveWidget, ILiveSummary, Output.ITaskbarProgressSource {
        /// <summary>Creates a progress bar.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="widgetId">The live name (default <c>progress</c>).</param>
        public ProgressBar(string label = "", string widgetId = "progress") : base(widgetId) {
            Label = label ?? string.Empty;
        }

        /// <summary>The label (markup).</summary>
        public string Label { get; set; }

        /// <summary>The current value.</summary>
        public double Value { get; set; }

        /// <summary>The total (default 100).</summary>
        public double Total { get; set; } = 100;

        /// <summary>The status; Pass/Fail/Warn change the bar colour once finished.</summary>
        public StatusKind Status { get; set; } = StatusKind.Running;

        /// <summary>Whether the <c>value/total</c> count is shown (default true).</summary>
        public bool ShowCount { get; set; } = true;

        /// <summary>The bar shape: <see cref="BarShape.Line"/> (default, a thin stroke) or <see cref="BarShape.Block"/> (solid blocks).</summary>
        public BarShape Shape { get; set; } = BarShape.Line;

        /// <summary>
        /// Layout of the value in the <c>value/total</c> count (default right-aligned). The value reserves the width of the
        /// total, so the bar keeps its length as the value grows from 9 to 10.
        /// </summary>
        public ColumnFormat CountColumn {
            get => _countColumn.Format;
            set => _countColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly StatColumn _countColumn = new StatColumn(new ColumnFormat());

        /// <summary>A fixed label column width, or <c>null</c> to fit the label.</summary>
        public int? LabelWidth { get; set; }

        /// <summary>The fraction complete (0-1).</summary>
        public double Fraction => Total <= 0 ? 0 : Math.Max(0, Math.Min(1, Value / Total));

        /// <summary>
        /// The columns of the bar's row (see <see cref="ProgressColumns"/>: description, bar, percent, count, elapsed,
        /// remaining time, rate, custom...), or null (default) for the classic label, bar, percentage and count. With columns
        /// set, the stream heartbeat line summarises the bar through them.
        /// </summary>
        public IList<ProgressColumn>? Columns { get; set; }

        /// <summary>The moving window that smooths the rate and the remaining time (default 10 seconds).</summary>
        public TimeSpan RateWindow {
            get => _meter.Window;
            set => _meter.Window = value;
        }

        private readonly RateMeter _meter = new RateMeter();
        private TimeSpan? _started;
        private TimeSpan? _finished;
        private TimeSpan _lastEventTime;

        /// <inheritdoc />
        public string? GetSummaryLine() {
            if (Columns == null || Columns.Count == 0) {
                return null;
            }
            RenderContext context = new RenderContext(Capabilities.TerminalProfile.Fixed(200, 1, Capabilities.ColorDepth.None, Capabilities.GlyphTier.Ascii, Capabilities.OutputMode.Stream), Theme.Dark, null, 0, _lastEventTime);
            return ProgressLayout.Summarize(Columns, Row(_lastEventTime), context);
        }

        /// <inheritdoc />
        public bool TryGetTaskbarProgress(out Output.TaskbarState state, out double fraction) {
            fraction = Total > 0 ? Fraction : double.NaN;
            switch (Status) {
                case StatusKind.Fail:
                    state = Output.TaskbarState.Error;
                    return true;
                case StatusKind.Warn:
                    state = Output.TaskbarState.Paused;
                    return true;
                case StatusKind.Running:
                    state = Total > 0 ? Output.TaskbarState.Normal : Output.TaskbarState.Indeterminate;
                    return true;
                default:
                    state = Output.TaskbarState.Clear;
                    return false;
            }
        }

        private ProgressRow Row(TimeSpan now) {
            string label = Markup.Strip(Label);
            StatusKind status = Status == StatusKind.Running && !_started.HasValue && Value <= 0 ? StatusKind.Pending : Status;
            return new ProgressRow(WidgetId, label, RichText.FromMarkup(Label), null, null, status, Value, Total, 0, 0, 0, 0, _started, _finished, now,
                status == StatusKind.Running ? _meter.Rate(now) : 0, 0);
        }

        /// <summary>Returns a handle that updates this bar through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public ProgressHandle Handle(ILiveDispatcher dispatcher) => new ProgressHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            TimeSpan now = TimeSpan.FromMilliseconds(liveEvent.Time);
            if (now > _lastEventTime) {
                _lastEventTime = now;
            }
            if ((liveEvent.Operation == "set" || liveEvent.Operation == "advance") && !_started.HasValue) {
                _started = now;
                _meter.Record(now, Value);
            }
            ApplyOperation(liveEvent);
            if (liveEvent.Operation == "set" || liveEvent.Operation == "advance") {
                _meter.Record(now, Value);
            } else if (liveEvent.Operation == "state" && Status != StatusKind.Running && Status != StatusKind.Pending && !_finished.HasValue) {
                _finished = now;
            }
        }

        private void ApplyOperation(LiveEvent liveEvent) {
            switch (liveEvent.Operation) {
                case "set":
                    Value = liveEvent.Value;
                    if (liveEvent.Total > 0) {
                        Total = liveEvent.Total;
                    }
                    break;
                case "advance":
                    Value += liveEvent.Value == 0 ? 1 : liveEvent.Value;
                    break;
                case "label":
                    Label = liveEvent.Text ?? string.Empty;
                    break;
                case "state":
                    if (StatusKinds.TryParse(liveEvent.Status, out StatusKind status)) {
                        Status = status;
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if (liveEvent.Operation == "state" && Status != StatusKind.Running && Status != StatusKind.Pending) {
                string count = ShowCount ? " " + NumberFormat.Compact(Value) + "/" + NumberFormat.Compact(Total) : string.Empty;
                return Markup.Strip(Label) + " " + Status.ToWord() + " " + NumberFormat.Percent(Fraction) + count;
            }
            return null;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0) {
                return;
            }
            if (Columns != null && Columns.Count > 0) {
                ProgressRow row = Row(context.Elapsed > _lastEventTime ? context.Elapsed : _lastEventTime);
                ProgressRow[] rows = { row };
                ProgressLayout.Draw(region, 0, Columns, ProgressLayout.Arrange(Columns, rows, region.Width, context), row, context);
                return;
            }
            StyledLine label = context.Markup(Label).Lines[0];
            int labelWidth = LabelWidth ?? label.Width;
            string tail = Tail();
            int tailWidth = DisplayWidth.Measure(tail);
            int barX = labelWidth > 0 ? labelWidth + 2 : 0;
            int barWidth = region.Width - barX - tailWidth - 2;
            if (barWidth < 4) {
                labelWidth = Math.Max(0, region.Width - tailWidth - 8);
                barX = labelWidth > 0 ? labelWidth + 2 : 0;
                barWidth = Math.Max(0, region.Width - barX - tailWidth - 2);
            }
            if (labelWidth > 0) {
                region.Write(0, 0, TextWrapper.Truncate(label, labelWidth, context.Ellipsis));
            }
            ThemeToken role = Status == StatusKind.Running || Status == StatusKind.Pending ? ThemeToken.Accent : Status.ToToken();
            BarDrawer.Draw(region, barX, 0, barWidth, Fraction, context.Style(role), context, true, Shape);
            region.Write(region.Width - tailWidth, 0, tail, context.Style(ThemeToken.Muted));
        }

        private string Tail() {
            string percent = NumberFormat.Percent(Fraction).PadLeft(4);
            if (!ShowCount) {
                return percent;
            }
            string value = NumberFormat.Compact(Value);
            string total = NumberFormat.Compact(Total);
            int width = _countColumn.Resolve(DisplayWidth.Measure(value), 0, DisplayWidth.Measure(total));
            return percent + "  " + StatColumn.Pad(value, width, CountColumn.Alignment) + "/" + total;
        }
    }
}
