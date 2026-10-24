using System;

namespace ConsoleMonster.Live {
    /// <summary>
    /// One update for a live widget, addressed by the widget's name. Every live change travels as a LiveEvent, so
    /// updates from any thread can be queued, coalesced, recorded as JSON lines and replayed without reflection.
    /// </summary>
    public sealed class LiveEvent {
        /// <summary>Creates an event.</summary>
        /// <param name="widgetId">The name of the target widget.</param>
        /// <param name="operation">The operation, defined by the target (for example <c>advance</c>, <c>add</c>, <c>write</c>).</param>
        /// <param name="key">The item key inside the target (lane, phase, row), when the operation needs one.</param>
        /// <param name="text">Free text (log line, detail, label).</param>
        /// <param name="status">A state word (pass, fail, running, info, warn...).</param>
        /// <param name="value">A numeric value (increment, sample, current value).</param>
        /// <param name="total">A second numeric value (maximum, total, duration).</param>
        /// <param name="time">Milliseconds since the session started; the session stamps it when 0.</param>
        /// <param name="isMarkup">Whether <paramref name="text"/> is markup (rich text) rather than literal text.</param>
        public LiveEvent(string widgetId, string operation, string? key = null, string? text = null, string? status = null, double value = 0, double total = 0, long time = 0, bool isMarkup = false) {
            WidgetId = widgetId ?? throw new ArgumentNullException(nameof(widgetId));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Key = key;
            Text = text;
            Status = status;
            Value = value;
            Total = total;
            Time = time;
            IsMarkup = isMarkup;
        }

        /// <summary>
        /// Whether <see cref="Text"/> is markup (rich text, for example from <c>New-ConsoleText</c>) rather than literal text.
        /// Targets that show text literally (lane labels and detail, log lines) render it with its styles when set.
        /// </summary>
        public bool IsMarkup { get; }

        /// <summary>The name of the target widget.</summary>
        public string WidgetId { get; }

        /// <summary>The operation.</summary>
        public string Operation { get; }

        /// <summary>The item key inside the target.</summary>
        public string? Key { get; }

        /// <summary>Free text.</summary>
        public string? Text { get; }

        /// <summary>A state word.</summary>
        public string? Status { get; }

        /// <summary>A numeric value.</summary>
        public double Value { get; }

        /// <summary>A second numeric value.</summary>
        public double Total { get; }

        /// <summary>Milliseconds since the session started.</summary>
        public long Time { get; }

        /// <summary>Returns a copy stamped with a time.</summary>
        /// <param name="time">Milliseconds since the session started.</param>
        /// <returns>The stamped event.</returns>
        public LiveEvent WithTime(long time) => new LiveEvent(WidgetId, Operation, Key, Text, Status, Value, Total, time, IsMarkup) { Payload = Payload };

        /// <summary>
        /// An in-process object for the target (a replacement widget for a <see cref="Widgets.RegionWidget"/>, row values for a
        /// <see cref="Widgets.LiveTable"/>, or an <see cref="Layout.IWidget"/> to append through a log target), or null.
        /// It is not recorded: a replayed recording sees only the text fields.
        /// </summary>
        public object? Payload { get; set; }

        /// <inheritdoc />
        public override string ToString() => Time + "ms " + WidgetId + "." + Operation + (Key == null ? string.Empty : "[" + Key + "]");
    }
}
