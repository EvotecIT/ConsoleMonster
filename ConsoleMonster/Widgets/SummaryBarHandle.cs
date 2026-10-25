using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe updates for a <see cref="SummaryBar"/> through its session's dispatcher.</summary>
    public sealed class SummaryBarHandle : LiveHandle {
        /// <summary>Creates a summary-bar handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="widgetId">The summary bar's name.</param>
        public SummaryBarHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) { }

        /// <summary>Sets progress; a kept value uses the preceding queued update, not a producer's stale copy.</summary>
        /// <param name="value">Items completed, or null to leave the value unchanged.</param>
        /// <param name="total">Total items, or null to leave it unchanged.</param>
        public void SetProgress(double? value = null, double? total = null) {
            var update = new WidgetUpdate();
            if (value.HasValue) { update.Value = value.Value; }
            if (total.HasValue) { update.Total = total.Value; }
            Update(update);
        }

        /// <summary>Adds or replaces a named counter in apply order.</summary>
        /// <param name="label">The nonempty counter label, optionally containing a {0} count placeholder.</param>
        /// <param name="count">The count.</param>
        /// <param name="status">The status for its marker and tone, or null to retain the existing status.</param>
        public void SetCounter(string label, int count, StatusKind? status = null) {
            var update = new WidgetUpdate { Counter = new System.Collections.Generic.Dictionary<string, int> { [label] = count } };
            if (status.HasValue) { update.Status = status.Value; }
            Update(update);
        }
    }
}
