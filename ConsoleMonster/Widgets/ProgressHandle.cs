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
        /// <param name="total">The total, or 0 to keep it.</param>
        public void Set(double value, double total = 0) => Post("set", value: value, total: total);

        /// <summary>Adds to the value.</summary>
        /// <param name="amount">The amount (default 1).</param>
        public void Advance(double amount = 1) => Post("advance", value: amount);

        /// <summary>Changes the label.</summary>
        /// <param name="label">The label (markup).</param>
        public void Label(string label) => Post("label", text: label);

        /// <summary>Sets the final status (Pass, Fail, Warn).</summary>
        /// <param name="status">The status.</param>
        public void Complete(StatusKind status = StatusKind.Pass) => Post("state", state: status.ToString());
    }
}
