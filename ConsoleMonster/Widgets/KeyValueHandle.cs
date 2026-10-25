using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="KeyValueList"/>.</summary>
    public sealed class KeyValueHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="widgetId">The list name.</param>
        public KeyValueHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) {
        }

        /// <summary>Adds or replaces a pair.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value (markup).</param>
        /// <param name="status">An optional status.</param>
        public void Set(string key, string value, StatusKind? status = null) {
            var update = new WidgetUpdate { Key = key, Value = ConsoleMonster.Text.RichText.FromMarkup(value) };
            if (status.HasValue) { update.Status = status.Value; }
            Update(update);
        }
    }
}
