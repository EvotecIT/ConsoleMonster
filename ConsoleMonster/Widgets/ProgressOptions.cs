using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>Shared construction options for known-total and indeterminate progress indicators.</summary>
    public sealed class ProgressOptions {
        private readonly HashSet<string> _bound = new HashSet<string>(StringComparer.Ordinal);
        internal IEnumerable<string> Bound => _bound;
        private bool _indeterminate = false;
        /// <summary>Creates an indicator for work without a known total.</summary>
        public bool Indeterminate { get => _indeterminate; set { _indeterminate = value; _bound.Add(nameof(Indeterminate)); } }
        private string? _widgetId;
        /// <summary>The live identifier; omitted uses progress or spinner.</summary>
        public string? WidgetId { get => _widgetId; set { _widgetId = value; _bound.Add(nameof(WidgetId)); } }
        private string _label = string.Empty;
        /// <summary>The label, including supported markup.</summary>
        public string Label { get => _label; set { _label = value; _bound.Add(nameof(Label)); } }
        private string? _detail;
        /// <summary>The indeterminate indicator detail.</summary>
        public string? Detail { get => _detail; set { _detail = value; _bound.Add(nameof(Detail)); } }
        private StatusKind _status = StatusKind.Running;
        /// <summary>The initial state.</summary>
        public StatusKind Status { get => _status; set { _status = value; _bound.Add(nameof(Status)); } }
        private double _value;
        /// <summary>The initial completed value.</summary>
        public double Value { get => _value; set { _value = value; _bound.Add(nameof(Value)); } }
        private double _total = 100;
        /// <summary>The planned total.</summary>
        public double Total { get => _total; set { _total = value; _bound.Add(nameof(Total)); } }
        private BarShape _shape = BarShape.Line;
        /// <summary>The bar shape.</summary>
        public BarShape Shape { get => _shape; set { _shape = value; _bound.Add(nameof(Shape)); } }
        private IList<ProgressColumn>? _columns;
        /// <summary>The ordered progress columns.</summary>
        public IList<ProgressColumn>? Columns { get => _columns; set { _columns = value; _bound.Add(nameof(Columns)); } }
        private ColumnFormat? _countColumn;
        /// <summary>The numeric count format.</summary>
        public ColumnFormat? CountColumn { get => _countColumn; set { _countColumn = value; _bound.Add(nameof(CountColumn)); } }
        private bool _showCount = true;
        /// <summary>Whether the default bar layout shows its count.</summary>
        public bool ShowCount { get => _showCount; set { _showCount = value; _bound.Add(nameof(ShowCount)); } }
        private int? _labelWidth;
        /// <summary>The fixed label width, when supplied.</summary>
        public int? LabelWidth { get => _labelWidth; set { _labelWidth = value; _bound.Add(nameof(LabelWidth)); } }
        private TimeSpan _rateWindow = TimeSpan.FromSeconds(10);
        /// <summary>The smoothing interval for rate and remaining time.</summary>
        public TimeSpan RateWindow { get => _rateWindow; set { _rateWindow = value; _bound.Add(nameof(RateWindow)); } }
        private bool _showElapsed = true;
        /// <summary>Whether an indeterminate indicator shows elapsed time.</summary>
        public bool ShowElapsed { get => _showElapsed; set { _showElapsed = value; _bound.Add(nameof(ShowElapsed)); } }
    }
}
