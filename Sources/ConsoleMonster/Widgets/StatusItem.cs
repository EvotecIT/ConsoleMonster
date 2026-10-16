using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>One row of a <see cref="StatusList"/>.</summary>
    public sealed class StatusItem {
        /// <summary>Creates an item.</summary>
        /// <param name="key">Unique key.</param>
        /// <param name="label">Label (markup).</param>
        /// <param name="status">Status.</param>
        /// <param name="detail">Detail shown right-aligned and muted (duration, count), or <c>null</c>.</param>
        public StatusItem(string key, string label, StatusKind status, string? detail = null) {
            Key = key;
            Label = label;
            Status = status;
            Detail = detail;
        }

        /// <summary>Unique key.</summary>
        public string Key { get; }

        /// <summary>Label (markup).</summary>
        public string Label { get; set; }

        /// <summary>Status.</summary>
        public StatusKind Status { get; set; }

        /// <summary>Right-aligned detail.</summary>
        public string? Detail { get; set; }
    }
}
