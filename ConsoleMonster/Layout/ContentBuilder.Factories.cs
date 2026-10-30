using System;
using System.Collections.Generic;
using ConsoleMonster.Widgets;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.Layout {
    public sealed partial class ContentBuilder {
        /// <summary>Adds a Rows configured with the same chain as Ui.Rows().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Rows(Action<Rows>? configure = null) { var widget = Ui.Rows(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Rows instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Rows(Rows widget) => Add(widget);

        /// <summary>Adds a Columns configured with the same chain as Ui.Columns().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Columns(Action<Columns>? configure = null) { var widget = Ui.Columns(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Columns instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Columns(Columns widget) => Add(widget);

        /// <summary>Adds a LayoutGrid configured with the same chain as Ui.LayoutGrid().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder LayoutGrid(Action<ConsoleMonster.Layout.LayoutGrid>? configure = null) { var widget = Ui.LayoutGrid(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured LayoutGrid instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder LayoutGrid(ConsoleMonster.Layout.LayoutGrid widget) => Add(widget);

        /// <summary>Adds a Panel configured with the same chain as Ui.Panel().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Panel(Action<Panel>? configure = null) { var widget = Ui.Panel(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Panel instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Panel(Panel widget) => Add(widget);

        /// <summary>Adds a Stack configured with the same chain as Ui.Stack().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Stack(Action<Stack>? configure = null) { var widget = Ui.Stack(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Stack instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Stack(Stack widget) => Add(widget);

        /// <summary>Adds a Align configured with the same chain as Ui.Align().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Align(Action<Align>? configure = null) { var widget = Ui.Align(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Align instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Align(Align widget) => Add(widget);

        /// <summary>Adds a Padding configured with the same chain as Ui.Padding().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Padding(Action<Padding>? configure = null) { var widget = Ui.Padding(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Padding instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Padding(Padding widget) => Add(widget);

        /// <summary>Adds a ScrollView configured with the same chain as Ui.ScrollView().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder ScrollView(Action<ScrollView>? configure = null) { var widget = Ui.ScrollView(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured ScrollView instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder ScrollView(ScrollView widget) => Add(widget);

        /// <summary>Adds a Text configured with the same chain as Ui.Text().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Text(Action<TextBlock>? configure = null) { var widget = Ui.Text(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Text instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Text(TextBlock widget) => Add(widget);

        /// <summary>Adds a Rule configured with the same chain as Ui.Rule().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Rule(Action<Rule>? configure = null) { var widget = Ui.Rule(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Rule instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Rule(Rule widget) => Add(widget);

        /// <summary>Adds a Table configured with the same chain as Ui.Table().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Table(Action<Table>? configure = null) { var widget = Ui.Table(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Table instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Table(Table widget) => Add(widget);

        /// <summary>Adds a ActivityList configured with the same chain as Ui.ActivityList().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder ActivityList(Action<ActivityList>? configure = null) { var widget = Ui.ActivityList(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured ActivityList instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder ActivityList(ActivityList widget) => Add(widget);

        /// <summary>Adds a CellMap configured with the same chain as Ui.CellMap().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder CellMap(Action<CellMap>? configure = null) { var widget = Ui.CellMap(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured CellMap instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder CellMap(CellMap widget) => Add(widget);

        /// <summary>Adds a KeyValue configured with the same chain as Ui.KeyValue().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder KeyValue(Action<KeyValueList>? configure = null) { var widget = Ui.KeyValue(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured KeyValue instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder KeyValue(KeyValueList widget) => Add(widget);

        /// <summary>Adds a Lanes configured with the same chain as Ui.Lanes().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Lanes(Action<Lanes>? configure = null) { var widget = Ui.Lanes(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Lanes instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Lanes(Lanes widget) => Add(widget);

        /// <summary>Adds a LogRegion configured with the same chain as Ui.LogRegion().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder LogRegion(Action<LogRegion>? configure = null) { var widget = Ui.LogRegion(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured LogRegion instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder LogRegion(LogRegion widget) => Add(widget);

        /// <summary>Adds a Phases configured with the same chain as Ui.Phases().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Phases(Action<Phases>? configure = null) { var widget = Ui.Phases(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Phases instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Phases(Phases widget) => Add(widget);

        /// <summary>Adds a SummaryBar configured with the same chain as Ui.SummaryBar().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder SummaryBar(Action<SummaryBar>? configure = null) { var widget = Ui.SummaryBar(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured SummaryBar instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder SummaryBar(SummaryBar widget) => Add(widget);

        /// <summary>Adds a Region configured with the same chain as Ui.Region().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Region(Action<RegionWidget>? configure = null) { var widget = Ui.Region(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Region instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Region(RegionWidget widget) => Add(widget);

        /// <summary>Adds a Dashboard configured with the same chain as Ui.Dashboard().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Dashboard(Action<Dashboard>? configure = null) { var widget = Ui.Dashboard(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Dashboard instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Dashboard(Dashboard widget) => Add(widget);

        /// <summary>Adds a Tree configured with the same chain as Ui.Tree().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Tree(Action<Tree>? configure = null) { var widget = Ui.Tree(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Tree instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Tree(Tree widget) => Add(widget);

        /// <summary>Adds a HeaderBar configured with the same chain as Ui.HeaderBar().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder HeaderBar(Action<HeaderBar>? configure = null) { var widget = Ui.HeaderBar(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured HeaderBar instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder HeaderBar(HeaderBar widget) => Add(widget);

        /// <summary>Adds a FooterBar configured with the same chain as Ui.FooterBar().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder FooterBar(Action<FooterBar>? configure = null) { var widget = Ui.FooterBar(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured FooterBar instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder FooterBar(FooterBar widget) => Add(widget);

        /// <summary>Adds a Logo configured with the same chain as Ui.Logo().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Logo(Action<Logo>? configure = null) { var widget = Ui.Logo(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Logo instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Logo(Logo widget) => Add(widget);

        /// <summary>Adds a status list using the Ui.StatusList chain.</summary><param name="configure">Configures the list.</param><returns>This builder.</returns>
        public ContentBuilder StatusList(Action<StatusList>? configure = null) { var widget = Ui.StatusList(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured status list.</summary><param name="widget">The list.</param><returns>This builder.</returns>
        public ContentBuilder StatusList(StatusList widget) => Add(widget);

        /// <summary>Adds a Flow configured with the same chain as Ui.Flow().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Flow(Action<Flow>? configure = null) { var widget = Ui.Flow(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Flow instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Flow(Flow widget) => Add(widget);

        /// <summary>Adds a Calendar configured with the same chain as Ui.Calendar().</summary>
        /// <param name="configure">Configures the empty widget before adding it.</param><returns>This builder.</returns>
        public ContentBuilder Calendar(Action<Calendar>? configure = null) { var widget = Ui.Calendar(); configure?.Invoke(widget); return Add(widget); }
        /// <summary>Adds a configured Calendar instance.</summary><param name="widget">The widget.</param><returns>This builder.</returns>
        public ContentBuilder Calendar(Calendar widget) => Add(widget);

        /// <summary>Configures and materializes Chart through its shared factory before adding it.</summary>
        /// <param name="configure">The definition configuration.</param><returns>This builder.</returns>
        public ContentBuilder Chart(Action<ChartOptions>? configure = null) { var options = Ui.Chart(); configure?.Invoke(options); return Add(options.Build()); }
        /// <summary>Adds a configured Chart definition.</summary><param name="options">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Chart(ChartOptions options) => Add(options.Build());

        /// <summary>Configures and materializes Progress through its shared factory before adding it.</summary>
        /// <param name="configure">The definition configuration.</param><returns>This builder.</returns>
        public ContentBuilder Progress(Action<ProgressOptions>? configure = null) { var options = Ui.Progress(); configure?.Invoke(options); return Add(options.Build()); }
        /// <summary>Adds a configured Progress definition.</summary><param name="options">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Progress(ProgressOptions options) => Add(options.Build());

        /// <summary>Configures and materializes Select through its shared factory before adding it.</summary>
        /// <param name="configure">The definition configuration.</param><returns>This builder.</returns>
        public ContentBuilder Select(Action<SelectInputOptions> configure) { var options = Ui.Select(); configure(options); return Add(options.Build()); }
        /// <summary>Adds a configured Select definition.</summary><param name="options">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Select(SelectInputOptions options) => Add(options.Build());

        /// <summary>Configures and materializes TextInput through its shared factory before adding it.</summary>
        /// <param name="configure">The definition configuration.</param><returns>This builder.</returns>
        public ContentBuilder TextInput(Action<TextInputOptions> configure) { var options = Ui.TextInput(); configure(options); return Add(options.Build()); }
        /// <summary>Adds a configured TextInput definition.</summary><param name="options">The definition.</param><returns>This builder.</returns>
        public ContentBuilder TextInput(TextInputOptions options) => Add(options.Build());

        /// <summary>Configures and materializes Button through its shared factory before adding it.</summary>
        /// <param name="configure">The definition configuration.</param><returns>This builder.</returns>
        public ContentBuilder Button(Action<ButtonInputOptions> configure) { var options = Ui.Button(); configure(options); return Add(options.Build()); }
        /// <summary>Adds a configured Button definition.</summary><param name="options">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Button(ButtonInputOptions options) => Add(options.Build());

        /// <summary>Configures and materializes Toggle through its shared factory before adding it.</summary>
        /// <param name="configure">The definition configuration.</param><returns>This builder.</returns>
        public ContentBuilder Toggle(Action<ToggleInputOptions> configure) { var options = Ui.Toggle(); configure(options); return Add(options.Build()); }
        /// <summary>Adds a configured Toggle definition.</summary><param name="options">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Toggle(ToggleInputOptions options) => Add(options.Build());

        /// <summary>Configures and materializes Binding through its shared factory before adding it.</summary>
        /// <param name="configure">The definition configuration.</param><returns>This builder.</returns>
        public ContentBuilder Binding(Action<BindingOptions> configure) { var options = Ui.Binding(); configure(options); return Add(options.Build()); }
        /// <summary>Adds a configured Binding definition.</summary><param name="options">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Binding(BindingOptions options) => Add(options.Build());

        /// <summary>Adds an already materialized Chart widget without rebuilding it.</summary><param name="widget">The configured widget.</param><returns>This builder.</returns>
        public ContentBuilder Chart(IWidget widget) => Add(widget);
        /// <summary>Adds an already materialized Progress widget without rebuilding it.</summary><param name="widget">The configured widget.</param><returns>This builder.</returns>
        public ContentBuilder Progress(IWidget widget) => Add(widget);
        /// <summary>Adds an already materialized Select widget without rebuilding it.</summary><param name="widget">The configured widget.</param><returns>This builder.</returns>
        public ContentBuilder Select(IWidget widget) => Add(widget);
        /// <summary>Adds an already materialized TextInput widget without rebuilding it.</summary><param name="widget">The configured widget.</param><returns>This builder.</returns>
        public ContentBuilder TextInput(IWidget widget) => Add(widget);
        /// <summary>Adds an already materialized Button widget without rebuilding it.</summary><param name="widget">The configured widget.</param><returns>This builder.</returns>
        public ContentBuilder Button(IWidget widget) => Add(widget);
        /// <summary>Adds an already materialized Toggle widget without rebuilding it.</summary><param name="widget">The configured widget.</param><returns>This builder.</returns>
        public ContentBuilder Toggle(IWidget widget) => Add(widget);
        /// <summary>Adds an already materialized Binding widget without rebuilding it.</summary><param name="widget">The configured widget.</param><returns>This builder.</returns>
        public ContentBuilder Binding(IWidget widget) => Add(widget);

        /// <summary>Adds literal text, matching Ui.Text and New-ConsoleText's primary text input.</summary>
        /// <param name="text">The literal text.</param><returns>This builder.</returns>
        public ContentBuilder Text(string text) => Add(Ui.Text(text));

        /// <summary>Adds markup text, matching New-ConsoleText -Markup.</summary>
        /// <param name="markup">The markup input.</param><returns>This builder.</returns>
        public ContentBuilder Markup(string markup) => Add(Ui.Markup(markup));
        /// <summary>Adds a titled or untitled rule, matching Ui.Rule.</summary>
        /// <param name="title">The optional primary title, or null.</param><returns>This builder.</returns>
        public ContentBuilder Rule(string? title) => Add(Ui.Rule(title));
        /// <summary>Adds a table over primary typed rows and configures its columns in one operation.</summary>
        /// <typeparam name="T">The row type.</typeparam><param name="items">The primary row sequence.</param><param name="configure">Configures typed columns.</param><returns>This builder.</returns>
        public ContentBuilder Table<
#if NET8_0_OR_GREATER
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEnumerable<T> items, Action<Table<T>>? configure = null) { var widget = Ui.Table(items); configure?.Invoke(widget); return Add(widget); }
    }
}
