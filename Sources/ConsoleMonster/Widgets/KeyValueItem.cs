using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>One pair in a <see cref="KeyValueList"/>.</summary>
    public sealed class KeyValueItem {
        /// <summary>Creates a pair.</summary>
        /// <param name="key">The key (plain text).</param>
        /// <param name="value">The value (markup).</param>
        /// <param name="status">An optional status marker.</param>
        public KeyValueItem(string key, string value, StatusKind? status = null) {
            Key = key;
            Value = value;
            Status = status;
        }

        /// <summary>The key.</summary>
        public string Key { get; }

        /// <summary>The value (markup).</summary>
        public string Value { get; }

        /// <summary>An optional status marker.</summary>
        public StatusKind? Status { get; }
    }
}
