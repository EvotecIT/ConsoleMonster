using System;
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
    public sealed class ProgressBar : LiveWidget {
        /// <summary>Creates a progress bar.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="name">The live name (default <c>progress</c>).</param>
        public ProgressBar(string label = "", string name = "progress") : base(name) {
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

        /// <summary>Returns a handle that updates this bar through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public ProgressHandle Handle(ILiveDispatcher dispatcher) => new ProgressHandle(dispatcher, Name);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
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
                    if (StatusKinds.TryParse(liveEvent.State, out StatusKind status)) {
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
            ThemeToken token = Status == StatusKind.Running || Status == StatusKind.Pending ? ThemeToken.Accent : Status.ToToken();
            BarDrawer.Draw(region, barX, 0, barWidth, Fraction, context.Style(token), context, true, Shape);
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
