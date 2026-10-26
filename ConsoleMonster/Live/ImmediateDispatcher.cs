using System;

namespace ConsoleMonster.Live {
    /// <summary>
    /// Applies events to one target immediately on the calling thread. Used when a widget is updated without a live
    /// session (building a report, tests). Not thread-safe, by design: without a session there is no render thread.
    /// </summary>
    public sealed class ImmediateDispatcher : IWidgetUpdateDispatcher {
        private readonly ILiveTarget _target;

        /// <summary>Creates a dispatcher for one target.</summary>
        /// <param name="target">The target.</param>
        public ImmediateDispatcher(ILiveTarget target) {
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <inheritdoc />
        public void Update(string widgetId, WidgetUpdate update) {
            if (!string.Equals(widgetId, _target.WidgetId, StringComparison.Ordinal)) {
                throw new ConsoleUpdateException("ConsoleMonsterUnknownWidget", "Unknown WidgetId '" + widgetId + "'. Available target: " + _target.WidgetId + ".");
            }
            if (update == null) { throw new ArgumentNullException(nameof(update)); }
            // Materialize and validate every event before the first mutation, as a live session does.
            foreach (LiveEvent item in ConsoleUpdate.Events(_target, update)) { Post(item); }
        }

        /// <inheritdoc />
        public void Post(LiveEvent liveEvent) {
            if (liveEvent == null) {
                throw new ArgumentNullException(nameof(liveEvent));
            }
            _target.Apply(liveEvent);
        }
    }
}
