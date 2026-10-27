using System;
using System.Threading.Tasks;
using ConsoleMonster.Export;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    /// <summary>The fluent entry point configures the same objects and preserves kind and identity at materialization.</summary>
    public sealed class UiFactoryTests {
        [Fact]
        public void RootAndNestedFactoryChainsRenderTheInitializerModel() {
            var expected = new Rows(new Panel(new Rows(TextBlock.Plain("literal [text]"), new Rule("Build")), "Status"), new Table().AddColumn("Name").AddRow("API")) { Gap = 1 };
            var actual = Ui.Rows().Gap(1).Content(content => {
                content.Panel(panel => panel.Title("Status").Content(children => {
                    children.Text("literal [text]");
                    children.Rule("Build");
                }));
                content.Table(table => table.AddColumn("Name").AddRow("API"));
            });
            Assert.Equal(ConsoleExporter.ToText(expected), ConsoleExporter.ToText(actual));
            Assert.Equal(ConsoleExporter.ToText(new TextBlock("[pass]Ready[/]")), ConsoleExporter.ToText(Ui.Text().Content("[pass]Ready[/]").Markup()));
        }

        [Fact]
        public async Task ConfigureOperationsUseFreshDefaultsBeforeRunningAndPropagateConfigurationErrors() {
            int calls = 0;
            var result = await ConsoleMonster.Live.ConsoleRun.ForEachAsync(new[] { 1, 2 }, (int item, System.Threading.CancellationToken _) => Task.FromResult(item * 2), options => {
                calls++;
                Assert.Equal(Math.Max(1, Environment.ProcessorCount), options.ThrottleLimit);
                options.ThrottleLimit(2).Title("Configured").Live(false).Configure(request => request.Print = _ => { });
            });
            Assert.Equal(1, calls);
            Assert.Equal(2, result.Count);
            Assert.Throws<ArgumentNullException>(() => Ui.Rows().CreateSession((Action<ConsoleMonster.Live.LiveSessionOptions>)null!));
            Assert.Throws<InvalidOperationException>(() => Ui.Grid().Show(_ => throw new InvalidOperationException("before input")));
            Assert.Equal("literal [bold]text[/]", ConsoleExporter.ToText(Ui.Text("literal [bold]text[/]")).Trim());
            Assert.Equal("text", ConsoleExporter.ToText(Ui.Markup("[bold]text[/]")).Trim());
            Assert.Equal(ConsoleExporter.ToText(new Rows(new TextBlock("[bold]text[/]"))), ConsoleExporter.ToText(Ui.Rows().Content(content => content.Markup("[bold]text[/]"))));
            Assert.False(Ui.Markup("[bold]text[/]").Markup(false).IsMarkup);
        }

        [Fact]
        public void PolymorphicDefinitionsUseTheSharedFactoriesAndValidateRequestedHandles() {
            var line = Ui.Chart().Type(ChartType.Line).WidgetId("throughput").Build<LineChart>();
            Assert.Equal("throughput", line.WidgetId);
            var spinner = Ui.Progress().Indeterminate().WidgetId("wait").Label("Waiting").Build<Spinner>();
            Assert.Equal("Waiting", spinner.Label);
            Assert.Throws<ArgumentException>(() => Ui.Progress().Indeterminate().Build<ProgressBar>());
            Assert.Throws<ArgumentException>(() => Ui.Chart().Type(ChartType.Bar).Build<Histogram>());
            Assert.Throws<ArgumentException>(() => Ui.Chart().Type(ChartType.Histogram).Edges(new double[] { 1, 0 }).Build());
        }

        [Fact]
        public void ControlAndCommandIdentityIsFixedWhenTheDefinitionBuilds() {
            var input = Ui.TextInput().Key("query").Value("initial");
            var text = Assert.IsType<TextInput>(input.Build());
            input.Key("other").Value("changed");
            Assert.Equal("query", text.Key);
            Assert.Equal("initial", text.Value);
            var definition = Ui.Command().Key("run").Label("Run").Configure(command => command.Action = (_, __) => Task.CompletedTask);
            AppCommand action = definition.Build();
            definition.Key("other").Label("Changed");
            Assert.Equal("run", action.Key);
            Assert.Equal("Run", action.Label);
            var button = Ui.Button().Key("button").Command(action).Build();
            Assert.Same(action, button.Command);
            Assert.Equal("run", Ui.Button().Command(action).Build().Key);
            var model = new ConsoleMonster.Dates.CalendarData();
            Assert.Same(model, Ui.Calendar().Marks(model).Data);
            Assert.Equal("Run", button.Label);
            Assert.Throws<ArgumentException>(() => Ui.Command().Build());
        }

        [Fact]
        public void ConstructedPeriodAndLayoutSettingsRetainTheirOriginalValidation() {
            var calendar = Ui.Calendar().Year(2026).Month(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.Month(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.Year(0));
            Assert.Equal(2026, calendar.Year);
            Assert.Equal(10, calendar.Month);
            Assert.Equal(2, Ui.LayoutGrid().Columns);
            Assert.Throws<InvalidOperationException>(() => Ui.TypedConfirmPrompt().Title("Remove?").Default(true).Show(new InteractiveOptions { Mode = InteractionMode.None }));
            var grid = Ui.LayoutGrid().Columns(0);
            Assert.Equal(1, grid.Columns);
        }
    }
}
