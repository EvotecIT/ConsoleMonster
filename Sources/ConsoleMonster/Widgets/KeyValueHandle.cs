using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="KeyValueList"/>.</summary>
    public sealed class KeyValueHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="target">The list name.</param>
        public KeyValueHandle(ILiveDispatcher dispatcher, string target) : base(dispatcher, target) {
        }

        /// <summary>Adds or replaces a pair.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value (markup).</param>
        /// <param name="status">An optional status.</param>
        public void Set(string key, string value, StatusKind? status = null) {
            Post("set", key, value, status.HasValue ? status.Value.ToString() : null);
        }
    }
}
