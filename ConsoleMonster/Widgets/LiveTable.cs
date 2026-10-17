using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A table whose rows change while a live session runs: rows have stable keys and are added, updated, removed or
    /// cleared through a <see cref="LiveTableHandle"/> (thread-safe, through the update queue). The <see cref="Table"/>
    /// defines the columns, presenters and options; its column widths only grow (<see cref="Widgets.Table.StableWidths"/>),
    /// so rows never jitter while values change. Optionally only the most recent <see cref="MaxRows"/> rows are kept, and
    /// rows can be kept sorted by a column.
    /// Live operations (Key = row key): <c>add</c> and <c>update</c> (Payload = the values; both insert or replace),
    /// <c>remove</c>, <c>clear</c>. A recording replays the tab-separated text form of the values.
    /// </summary>
    public sealed class LiveTable : LiveWidget {
        private readonly List<string> _order = new List<string>();
        private readonly Dictionary<string, object?[]> _rows = new Dictionary<string, object?[]>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _inserted = new Dictionary<string, long>(StringComparer.Ordinal);
        private long _sequence;
        private bool _dirty = true;

        /// <summary>Creates a live table.</summary>
        /// <param name="table">The table that defines the columns and options (its own rows are replaced).</param>
        /// <param name="name">The live name (default <c>table</c>).</param>
        public LiveTable(Table table, string name = "table") : base(name) {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            // A live table keeps its grid shape: no record view for a single row, no cards in narrow windows.
            Table.StableWidths = true;
            Table.RecordViewRows = 0;
            Table.CardsBelow = 0;
        }

        /// <summary>The table that defines the columns, presenters and options.</summary>
        public Table Table { get; }

        /// <summary>Keeps only the most recently added rows (0, the default, keeps them all): a bounded "recent N" window.</summary>
        public int MaxRows { get; set; }

        /// <summary>The column index to keep rows sorted by, or null (default) for insertion order.</summary>
        public int? SortColumn { get; set; }

        /// <summary>Whether <see cref="SortColumn"/> sorts from the largest value down.</summary>
        public bool SortDescending { get; set; }

        /// <summary>The row keys in display order.</summary>
        public IReadOnlyList<string> Keys {
            get {
                Rebuild();
                return _order.ToArray();
            }
        }

        /// <summary>A handle that changes rows through a dispatcher (a live session).</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public LiveTableHandle Handle(ILiveDispatcher dispatcher) => new LiveTableHandle(dispatcher, Name);

        /// <summary>Adds or replaces a row directly (not thread-safe; use a handle while a session runs).</summary>
        /// <param name="key">The row key.</param>
        /// <param name="values">The values, one per column.</param>
        public void Set(string key, params object?[] values) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if (!_rows.ContainsKey(key)) {
                _order.Add(key);
                _inserted[key] = ++_sequence;
            }
            _rows[key] = values ?? new object?[0];
            Trim();
            _dirty = true;
        }

        /// <summary>Removes a row directly (not thread-safe; use a handle while a session runs).</summary>
        /// <param name="key">The row key.</param>
        /// <returns>Whether the row existed.</returns>
        public bool Remove(string key) {
            if (key == null || !_rows.Remove(key)) {
                return false;
            }
            _order.Remove(key);
            _inserted.Remove(key);
            _dirty = true;
            return true;
        }

        /// <summary>Removes every row directly (column widths stay).</summary>
        public void Clear() {
            _rows.Clear();
            _order.Clear();
            _inserted.Clear();
            _dirty = true;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            switch (liveEvent.Operation) {
                case "add":
                case "update":
                    if (liveEvent.Key != null) {
                        Set(liveEvent.Key, Values(liveEvent));
                    }
                    break;
                case "remove":
                    if (liveEvent.Key != null) {
                        Remove(liveEvent.Key);
                    }
                    break;
                case "clear":
                    Clear();
                    break;
            }
        }

        /// <inheritdoc />
        public override string? Describe(LiveEvent liveEvent) {
            if ((liveEvent.Operation == "add" || liveEvent.Operation == "update") && liveEvent.Key != null && _rows.TryGetValue(liveEvent.Key, out object?[]? values)) {
                List<string> parts = new List<string>();
                foreach (object? value in values) {
                    parts.Add(value is RichText rich ? rich.PlainText : value is TableCell cell ? Markup.Strip(cell.Markup) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                }
                return string.Join("  ", parts.ToArray());
            }
            return null;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            Rebuild();
            return Table.Measure(constraints, context);
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            Rebuild();
            Table.Render(region, context);
        }

        /// <summary>The text form of row values that recordings keep (tab-separated plain text).</summary>
        /// <param name="values">The values.</param>
        /// <returns>The text.</returns>
        public static string TextOf(object?[] values) {
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            string[] parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++) {
                parts[i] = Convert.ToString(values[i], CultureInfo.InvariantCulture)?.Replace('\t', ' ') ?? string.Empty;
            }
            return string.Join("\t", parts);
        }

        private static object?[] Values(LiveEvent liveEvent) {
            if (liveEvent.Payload is object?[] values) {
                return values;
            }
            if (liveEvent.Payload is IEnumerable enumerable && !(liveEvent.Payload is string)) {
                List<object?> list = new List<object?>();
                foreach (object? value in enumerable) {
                    list.Add(value);
                }
                return list.ToArray();
            }
            return (liveEvent.Text ?? string.Empty).Split('\t');
        }

        // The bounded window keeps the most recently added rows.
        private void Trim() {
            if (MaxRows <= 0) {
                return;
            }
            while (_order.Count > MaxRows) {
                string oldest = _order[0];
                long first = _inserted[oldest];
                foreach (string key in _order) {
                    if (_inserted[key] < first) {
                        oldest = key;
                        first = _inserted[key];
                    }
                }
                Remove(oldest);
            }
        }

        private void Rebuild() {
            if (!_dirty) {
                return;
            }
            if (SortColumn.HasValue) {
                int column = SortColumn.Value;
                int direction = SortDescending ? -1 : 1;
                List<string> sorted = new List<string>(_order);
                sorted.Sort((a, b) => {
                    int compared = Compare(Cell(_rows[a], column), Cell(_rows[b], column));
                    return compared != 0 ? compared * direction : _inserted[a].CompareTo(_inserted[b]);
                });
                _order.Clear();
                _order.AddRange(sorted);
            }
            Table.ClearRows();
            foreach (string key in _order) {
                Table.AddValues(_rows[key]);
            }
            _dirty = false;
        }

        private static object? Cell(object?[] values, int column) => column >= 0 && column < values.Length ? values[column] : null;

        private static int Compare(object? a, object? b) {
            if (a == null || b == null) {
                return a == null ? (b == null ? 0 : -1) : 1;
            }
            if (a is IComparable comparable && a.GetType() == b.GetType()) {
                return comparable.CompareTo(b);
            }
            if (IsNumber(a) && IsNumber(b)) {
                return Convert.ToDouble(a, CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(b, CultureInfo.InvariantCulture));
            }
            return string.Compare(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumber(object value) => value is int || value is long || value is double || value is float || value is decimal || value is short || value is byte || value is uint || value is ulong;
    }

    /// <summary>Changes the rows of a <see cref="LiveTable"/> through the live session's update queue (thread-safe).</summary>
    public sealed class LiveTableHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher (a live session).</param>
        /// <param name="target">The live table name.</param>
        public LiveTableHandle(ILiveDispatcher dispatcher, string target) : base(dispatcher, target) {
        }

        /// <summary>Adds a row (or replaces the row with that key).</summary>
        /// <param name="key">The stable row key.</param>
        /// <param name="values">The values, one per column (raw values go through the column presenters).</param>
        public void AddRow(string key, params object?[] values) => Send("add", key, values);

        /// <summary>Replaces the values of the row with that key (or adds it).</summary>
        /// <param name="key">The row key.</param>
        /// <param name="values">The new values.</param>
        public void UpdateRow(string key, params object?[] values) => Send("update", key, values);

        /// <summary>Removes the row with that key.</summary>
        /// <param name="key">The row key.</param>
        public void RemoveRow(string key) => Post("remove", key ?? throw new ArgumentNullException(nameof(key)));

        /// <summary>Removes every row; the column widths stay.</summary>
        public void Clear() => Post("clear");

        private void Send(string operation, string key, object?[] values) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            object?[] copy = values == null ? new object?[0] : (object?[])values.Clone();
            PostPayload(operation, copy, key, LiveTable.TextOf(copy));
        }
    }
}
