using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    public sealed class ChartFactoryTests {
        [Theory]
        [InlineData(ChartType.Bar, typeof(BarChart))]
        [InlineData(ChartType.Stacked, typeof(StackedBar))]
        [InlineData(ChartType.Line, typeof(LineChart))]
        [InlineData(ChartType.Scatter, typeof(LineChart))]
        [InlineData(ChartType.Histogram, typeof(Histogram))]
        [InlineData(ChartType.Sparkline, typeof(Sparkline))]
        public void KindSelectsTheConcreteWidget(ChartType type, Type expected) {
            var options = new ChartOptions { Type = type };
            if (type == ChartType.Histogram) { options.Edges = new[] { 0.0, 1.0, 2.0 }; }
            Assert.IsType(expected, Chart.Create(options));
        }

        [Fact]
        public void PresentationAndRetentionOptionsApplyOnlyToTheirChartKinds() {
            var bars = Assert.IsType<BarChart>(Chart.Create(new ChartOptions { Type = ChartType.Bar, ShowValues = false, Signed = true }));
            Assert.False(bars.ShowValues);
            Assert.True(bars.Signed);
            var stacked = Assert.IsType<StackedBar>(Chart.Create(new ChartOptions { Type = ChartType.Stacked, ShowLegend = false, MaxLegendColumns = 2 }));
            Assert.False(stacked.ShowLegend);
            Assert.Equal(2, stacked.MaxLegendColumns);
            Assert.Equal(7, Assert.IsType<LineChart>(Chart.Create(new ChartOptions { Type = ChartType.Line, Capacity = 7 })).Capacity);
            Assert.Equal(9, Assert.IsType<Sparkline>(Chart.Create(new ChartOptions { Type = ChartType.Sparkline, Capacity = 9 })).Capacity);
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Line, ShowValues = false }));
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Bar, ShowLegend = false }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create(new ChartOptions { Type = ChartType.Line, Capacity = 0 }));
        }

        [Fact]
        public void RejectsMismatchedOptionsEvenWhenTheirValueIsZeroOrFalse() {
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Bar, ShowLatest = false }));
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Stacked, Maximum = 0 }));
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Line, Warning = 0 }));
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Histogram }));
        }

        [Fact]
        public void ParentResolvesAutomaticSeriesRolesAndKeepsExplicitRoles() {
            var options = new ChartOptions { Type = ChartType.Line };
            options.Series.Add(new ChartSeries("failed"));
            options.Series.Add(new ChartSeries("custom", ThemeToken.Accent));
            var chart = Assert.IsType<LineChart>(Chart.Create(options));
            Assert.Equal(ThemeToken.Fail, chart.Series[0].Role);
            Assert.Equal(ThemeToken.Accent, chart.Series[1].Role);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void NonfiniteDataAndUnorderedEdgesFailBeforeAChartIsReturned(double invalid) {
            var bars = new ChartOptions { Type = ChartType.Bar };
            bars.Items.Add(new ChartItem("Invalid", invalid));
            Assert.Throws<ArgumentException>(() => Chart.Create(bars));
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Sparkline, Values = new[] { invalid } }));
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Histogram, Edges = new[] { 0.0, invalid } }));
            Assert.Throws<ArgumentException>(() => Chart.Create(new ChartOptions { Type = ChartType.Histogram, Edges = new[] { 0.0, 2.0, 1.0 } }));
        }

        [Fact]
        public void InitializerAndFluentOptionsUseTheSameDataAndDefaults() {
            var initializer = new ChartOptions { Type = ChartType.Sparkline, Title = "CPU", Height = 2, Role = ThemeToken.Accent, Values = new[] { 0.0, 0.5, 1.0 } };
            ChartOptions fluent = new ChartOptions().Type(ChartType.Sparkline).Title("CPU").Height(2).Role(ThemeToken.Accent).Values(new[] { 0.0, 0.5, 1.0 });
            var render = new global::ConsoleMonster.Export.ExportOptions { Width = 60 };
            Assert.Equal(global::ConsoleMonster.Export.ConsoleExporter.ToText(Chart.Create(initializer), render), global::ConsoleMonster.Export.ConsoleExporter.ToText(Chart.Create(fluent), render));
            Assert.Equal(1, Assert.IsType<Sparkline>(Chart.Create(new ChartOptions { Type = ChartType.Sparkline })).Height);
            Assert.Equal(8, Assert.IsType<LineChart>(Chart.Create(new ChartOptions { Type = ChartType.Line })).Height);
        }
    }
}
