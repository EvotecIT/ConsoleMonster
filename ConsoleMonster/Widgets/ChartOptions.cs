using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>The numeric chart representation.</summary>
    public enum ChartType {
        /// <summary>One horizontal bar per labelled value.</summary>
        Bar,
        /// <summary>One proportional bar with a legend.</summary>
        Stacked,
        /// <summary>Ordered values joined by lines.</summary>
        Line,
        /// <summary>Ordered values shown as points.</summary>
        Scatter,
        /// <summary>Values counted into explicit buckets.</summary>
        Histogram,
        /// <summary>A compact recent-value trend.</summary>
        Sparkline
    }

    /// <summary>Presence-aware numeric chart configuration, shared by the C# factory and PowerShell.</summary>
    public sealed class ChartOptions {
        private readonly HashSet<string> _bound = new HashSet<string>(StringComparer.Ordinal);
        internal IEnumerable<string> Bound => _bound;
        private ChartType _type = ChartType.Bar;
        /// <summary>The numeric chart kind.</summary>
        public ChartType Type { get => _type; set { _type = value; _bound.Add(nameof(Type)); } }
        private string? _widgetId;
        /// <summary>The live identifier; omitted uses the chart kind.</summary>
        public string? WidgetId { get => _widgetId; set { _widgetId = value; _bound.Add(nameof(WidgetId)); } }
        private string? _unit;
        /// <summary>The unit on numeric labels.</summary>
        public string? Unit { get => _unit; set { _unit = value; _bound.Add(nameof(Unit)); } }
        private string? _label;
        /// <summary>The sparkline label.</summary>
        public string? Label { get => _label; set { _label = value; _bound.Add(nameof(Label)); } }
        private int? _height;
        /// <summary>The plot height; omitted uses the selected kind default.</summary>
        public int? Height { get => _height; set { _height = value; _bound.Add(nameof(Height)); } }
        private double? _minimum;
        /// <summary>The lower numeric limit.</summary>
        public double? Minimum { get => _minimum; set { _minimum = value; _bound.Add(nameof(Minimum)); } }
        private double? _maximum;
        /// <summary>The upper numeric limit.</summary>
        public double? Maximum { get => _maximum; set { _maximum = value; _bound.Add(nameof(Maximum)); } }
        private ThemeToken? _role;
        /// <summary>The chart color role.</summary>
        public ThemeToken? Role { get => _role; set { _role = value; _bound.Add(nameof(Role)); } }
        private BarShape _shape = BarShape.Block;
        /// <summary>The bar shape.</summary>
        public BarShape Shape { get => _shape; set { _shape = value; _bound.Add(nameof(Shape)); } }
        private double? _warning;
        /// <summary>The warning threshold.</summary>
        public double? Warning { get => _warning; set { _warning = value; _bound.Add(nameof(Warning)); } }
        private double? _critical;
        /// <summary>The critical threshold.</summary>
        public double? Critical { get => _critical; set { _critical = value; _bound.Add(nameof(Critical)); } }
        private bool _showLatest = true;
        /// <summary>Whether the latest sparkline value is shown.</summary>
        public bool ShowLatest { get => _showLatest; set { _showLatest = value; _bound.Add(nameof(ShowLatest)); } }
        private double[]? _edges;
        /// <summary>Strictly ascending histogram bucket edges.</summary>
        public double[]? Edges { get => _edges; set { _edges = value; _bound.Add(nameof(Edges)); } }
        private double[]? _values;
        /// <summary>Initial numeric samples.</summary>
        public double[]? Values { get => _values; set { _values = value; _bound.Add(nameof(Values)); } }
        private ColumnFormat? _valueColumn;
        /// <summary>The bar numeric-column format.</summary>
        public ColumnFormat? ValueColumn { get => _valueColumn; set { _valueColumn = value; _bound.Add(nameof(ValueColumn)); } }
        private ColumnFormat? _countColumn;
        /// <summary>The stacked numeric-column format.</summary>
        public ColumnFormat? CountColumn { get => _countColumn; set { _countColumn = value; _bound.Add(nameof(CountColumn)); } }
        private ColumnFormat? _percentColumn;
        /// <summary>The stacked percent-column format.</summary>
        public ColumnFormat? PercentColumn { get => _percentColumn; set { _percentColumn = value; _bound.Add(nameof(PercentColumn)); } }
        private ColumnFormat? _labelColumn;
        /// <summary>The stacked legend-label format.</summary>
        public ColumnFormat? LabelColumn { get => _labelColumn; set { _labelColumn = value; _bound.Add(nameof(LabelColumn)); } }
        private bool _showValues = true;
        /// <summary>Whether Bar charts show each numeric value.</summary>
        public bool ShowValues { get => _showValues; set { _showValues = value; _bound.Add(nameof(ShowValues)); } }
        private bool _signed;
        /// <summary>Whether Bar charts reserve space on both sides of zero.</summary>
        public bool Signed { get => _signed; set { _signed = value; _bound.Add(nameof(Signed)); } }
        private bool _showLegend = true;
        /// <summary>Whether Stacked charts show their legend.</summary>
        public bool ShowLegend { get => _showLegend; set { _showLegend = value; _bound.Add(nameof(ShowLegend)); } }
        private int _maxLegendColumns;
        /// <summary>Maximum Stacked legend columns; zero fits the available width.</summary>
        public int MaxLegendColumns { get => _maxLegendColumns; set { _maxLegendColumns = value; _bound.Add(nameof(MaxLegendColumns)); } }
        private int? _capacity;
        /// <summary>Retained Line, Scatter or Sparkline samples; null keeps that kind's default.</summary>
        public int? Capacity { get => _capacity; set { _capacity = value; _bound.Add(nameof(Capacity)); } }

        /// <summary>Labelled bars or stacked segments.</summary>
        public IList<ChartItem> Items { get; } = new List<ChartItem>();
        /// <summary>Typed line/scatter series, retaining per-series colors.</summary>
        public IList<ChartSeries> Series { get; } = new List<ChartSeries>();
    }
}
