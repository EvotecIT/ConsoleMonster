using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe updates for a <see cref="SummaryBar"/> through its session's dispatcher.</summary>
    public sealed class SummaryBarHandle : LiveHandle {
        /// <summary>Creates a summary-bar handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="target">The summary bar's name.</param>
        public SummaryBarHandle(ILiveDispatcher dispatcher, string target) : base(dispatcher, target) { }

        /// <summary>Sets progress; a kept value uses the preceding queued update, not a producer's stale copy.</summary>
        /// <param name="done">Items completed, or <see cref="SummaryBar.Keep"/> to leave the value unchanged.</param>
        /// <param name="total">Total items, or <see cref="SummaryBar.Keep"/> to leave it unchanged.</param>
        public void SetProgress(double done = SummaryBar.Keep, double total = SummaryBar.Keep) => Post("progress", value: done, total: total);

        /// <summary>Adds or replaces a named counter in apply order.</summary>
        /// <param name="label">The nonempty counter label, optionally containing a {0} count placeholder.</param>
        /// <param name="count">The count.</param>
        /// <param name="status">The status for its marker and tone, or null to retain the existing status.</param>
        public void SetCounter(string label, int count, StatusKind? status = null) => Post("counter", key: label, value: count, state: status?.ToString());
    }
}
