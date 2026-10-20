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

        /// <summary>Creates a pair whose value is formatted by a presenter (sizes, durations, dates, statuses...).</summary>
        /// <param name="key">The key (plain text).</param>
        /// <param name="value">The raw value.</param>
        /// <param name="presenter">The presenter, or null for the one the key and value suggest.</param>
        /// <param name="status">An optional status marker.</param>
        /// <returns>The pair, for <see cref="KeyValueList.Add(KeyValueItem)"/>.</returns>
        public static KeyValueItem ForValue(string key, object? value, Presenters.ICellPresenter? presenter = null, StatusKind? status = null) {
            return new KeyValueItem(key, string.Empty, status) {
                Cell = TableCell.FromValue(value),
                Presenter = presenter ?? Presenters.CellPresenters.ForLabel(key, value)
            };
        }

        /// <summary>The key.</summary>
        public string Key { get; }

        /// <summary>The value (markup).</summary>
        public string Value { get; }

        /// <summary>An optional status marker.</summary>
        public StatusKind? Status { get; }

        /// <summary>A raw value shown through <see cref="Presenter"/> instead of <see cref="Value"/>, or null.</summary>
        public TableCell? Cell { get; internal set; }

        /// <summary>The presenter for <see cref="Cell"/>, or null for automatic.</summary>
        public Presenters.ICellPresenter? Presenter { get; internal set; }

        /// <summary>Tone rules for <see cref="Cell"/>, or null.</summary>
        public System.Collections.Generic.IReadOnlyList<Presenters.ToneRule>? Rules { get; internal set; }
    }
}
