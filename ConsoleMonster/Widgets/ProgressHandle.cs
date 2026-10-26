using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="ProgressBar"/>.</summary>
    public sealed class ProgressHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="widgetId">The bar name.</param>
        public ProgressHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) {
        }

        /// <summary>Sets the value (and optionally the total).</summary>
        /// <param name="value">The value.</param>
        /// <param name="total">The total, or null to keep it; zero explicitly clears an unknown total.</param>
        public void Set(double value, double? total = null) {
            var update = new WidgetUpdate { Value = value };
            if (total.HasValue) { update.Total = total.Value; }
            Update(update);
        }

        /// <summary>Adds to the value.</summary>
        /// <param name="amount">The amount (default 1).</param>
        public void Advance(double amount = 1) => Update(new WidgetUpdate { Increment = amount });

        /// <summary>Changes the label.</summary>
        /// <param name="label">The label (markup).</param>
        public void Label(string label) => Update(new WidgetUpdate { Label = label });

        /// <summary>Sets the final status (Pass, Fail, Warn).</summary>
        /// <param name="status">The status.</param>
        public void Complete(StatusKind status = StatusKind.Pass) => Update(new WidgetUpdate { Status = status });
    }
}
