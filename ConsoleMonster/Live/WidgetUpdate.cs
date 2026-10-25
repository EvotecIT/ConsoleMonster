using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Live {
    /// <summary>A presence-aware update: omission, zero, null and empty text retain distinct meanings.</summary>
    public sealed class WidgetUpdate {
        private readonly HashSet<string> _bound = new HashSet<string>(StringComparer.Ordinal);
        internal IEnumerable<string> Bound => _bound;
        internal bool Has(string field) => _bound.Contains(field);
        private string? _key;
        /// <summary>The item key within the addressed widget.</summary>
        public string? Key {
            get => _key;
            set { _key = value; _bound.Add(nameof(Key)); }
        }
        private string? _parentKey;
        /// <summary>The parent activity key.</summary>
        public string? ParentKey {
            get => _parentKey;
            set { _parentKey = value; _bound.Add(nameof(ParentKey)); }
        }
        private string? _label;
        /// <summary>The replacement label; null explicitly clears it.</summary>
        public string? Label {
            get => _label;
            set { _label = value; _bound.Add(nameof(Label)); }
        }
        private string? _detail;
        /// <summary>The replacement detail; null explicitly clears it.</summary>
        public string? Detail {
            get => _detail;
            set { _detail = value; _bound.Add(nameof(Detail)); }
        }
        private object? _value;
        /// <summary>The absolute value, retaining its original scalar type.</summary>
        public object? Value {
            get => _value;
            set { _value = value; _bound.Add(nameof(Value)); }
        }
        private double _total;
        /// <summary>The planned total; zero represents an unknown total.</summary>
        public double Total {
            get => _total;
            set { _total = value; _bound.Add(nameof(Total)); }
        }
        private double _increment;
        /// <summary>The additive change, including zero.</summary>
        public double Increment {
            get => _increment;
            set { _increment = value; _bound.Add(nameof(Increment)); }
        }
        private StatusKind _status;
        /// <summary>The new state.</summary>
        public StatusKind Status {
            get => _status;
            set { _status = value; _bound.Add(nameof(Status)); }
        }
        private StatusKind _result;
        /// <summary>The result of completed lane steps.</summary>
        public StatusKind Result {
            get => _result;
            set { _result = value; _bound.Add(nameof(Result)); }
        }
        private bool _complete;
        /// <summary>Whether to complete an activity or lane.</summary>
        public bool Complete {
            get => _complete;
            set { _complete = value; _bound.Add(nameof(Complete)); }
        }
        private bool _restart;
        /// <summary>Whether to restart a spinner.</summary>
        public bool Restart {
            get => _restart;
            set { _restart = value; _bound.Add(nameof(Restart)); }
        }
        private TimeSpan? _remaining;
        /// <summary>The estimated time remaining on an activity.</summary>
        public TimeSpan? Remaining {
            get => _remaining;
            set { _remaining = value; _bound.Add(nameof(Remaining)); }
        }
        private IDictionary<string, int>? _counter;
        /// <summary>The summary counters, in enumeration order.</summary>
        public IDictionary<string, int>? Counter {
            get => _counter;
            set { _counter = value; _bound.Add(nameof(Counter)); }
        }
        private bool _append;
        /// <summary>Whether Value is a new numeric sample.</summary>
        public bool Append {
            get => _append;
            set { _append = value; _bound.Add(nameof(Append)); }
        }
        private ConsoleTableRow? _row;
        /// <summary>A keyed table row with typed cells.</summary>
        public ConsoleTableRow? Row {
            get => _row;
            set { _row = value; _bound.Add(nameof(Row)); }
        }
        private bool _removeRow;
        /// <summary>Whether to remove the row addressed by Key.</summary>
        public bool RemoveRow {
            get => _removeRow;
            set { _removeRow = value; _bound.Add(nameof(RemoveRow)); }
        }
        private bool _clearRows;
        /// <summary>Whether to clear all table rows.</summary>
        public bool ClearRows {
            get => _clearRows;
            set { _clearRows = value; _bound.Add(nameof(ClearRows)); }
        }
        private int[]? _index;
        /// <summary>The zero-based cell positions.</summary>
        public int[]? Index {
            get => _index;
            set { _index = value; _bound.Add(nameof(Index)); }
        }
        private bool _appendCells;
        /// <summary>Whether to append cells.</summary>
        public bool AppendCells {
            get => _appendCells;
            set { _appendCells = value; _bound.Add(nameof(AppendCells)); }
        }
        private int _count = 1;
        /// <summary>The number of appended cells; the default is one.</summary>
        public int Count {
            get => _count;
            set { _count = value; _bound.Add(nameof(Count)); }
        }
        private int _cellCount;
        /// <summary>The new number of cells.</summary>
        public int CellCount {
            get => _cellCount;
            set { _cellCount = value; _bound.Add(nameof(CellCount)); }
        }
        private IWidget? _content;
        /// <summary>The replacement region content; null empties it.</summary>
        public IWidget? Content {
            get => _content;
            set { _content = value; _bound.Add(nameof(Content)); }
        }
    }

    /// <summary>A stable row key and values passed through the live table column presenters.</summary>
    public sealed class ConsoleTableRow {
        /// <summary>The stable row key.</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>Typed cell values, in column order.</summary>
        public object?[] Values { get; set; } = Array.Empty<object?>();
    }

    /// <summary>An update to one item of a caller-managed run.</summary>
    public sealed class RunUpdate {
        /// <summary>The zero-based item position.</summary>
        public int Index { get; set; }
        /// <summary>Running, Pass, Fail, Warn or Skip.</summary>
        public StatusKind Status { get; set; }
        /// <summary>The current phase label.</summary>
        public string? Label { get; set; }
        /// <summary>The completion detail.</summary>
        public string? Detail { get; set; }
        /// <summary>An optional measured duration.</summary>
        public TimeSpan? Duration { get; set; }
    }
}
