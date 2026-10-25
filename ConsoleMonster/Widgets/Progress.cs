using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    /// <summary>The shared constructor for known-total and indeterminate progress indicators.</summary>
    public static class Progress {
        /// <summary>Creates a progress widget, rejecting configuration for the other indicator kind.</summary>
        /// <param name="options">The indicator kind and presentation options.</param>
        /// <returns>A ProgressBar or Spinner.</returns>
        public static IWidget Create(ProgressOptions? options = null) {
            ProgressOptions o = options ?? new ProgressOptions();
            string supported = "Indeterminate WidgetId Label Status " + (o.Indeterminate ? "Detail ShowElapsed" : "Value Total Shape Columns CountColumn ShowCount LabelWidth RateWindow");
            var allowed = new HashSet<string>(supported.Split(' '), StringComparer.Ordinal);
            foreach (string field in o.Bound) {
                if (!allowed.Contains(field)) { throw new ArgumentException((o.Indeterminate ? "Indeterminate progress" : "ProgressBar") + " does not support " + field + ". Supported options: " + supported + "."); }
            }
            if (o.Indeterminate) { return new Spinner(o.Label, o.WidgetId ?? "spinner") { Detail = o.Detail, Status = o.Status, ShowElapsed = o.ShowElapsed }; }
            if (!NumberFormat.IsFinite(o.Value) || !NumberFormat.IsFinite(o.Total) || o.Total < 0) { throw new ArgumentOutOfRangeException(nameof(options), "Value and Total must be finite; Total cannot be negative."); }
            if (o.LabelWidth < 0) { throw new ArgumentOutOfRangeException(nameof(o.LabelWidth)); }
            var bar = new ProgressBar(o.Label, o.WidgetId ?? "progress") {
                Value = o.Value, Total = o.Total, Status = o.Status, Shape = o.Shape, Columns = o.Columns,
                ShowCount = o.ShowCount, LabelWidth = o.LabelWidth, RateWindow = o.RateWindow
            };
            if (o.CountColumn != null) { bar.CountColumn = o.CountColumn; }
            return bar;
        }
    }
}
