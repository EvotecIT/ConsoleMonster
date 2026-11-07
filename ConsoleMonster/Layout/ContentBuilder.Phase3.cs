using System;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Layout {
    public sealed partial class ContentBuilder {
        /// <summary>Adds a configured grid definition through its shared factory.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder DataGrid(Action<DataGridInputOptions> configure) { var definition = Ui.DataGrid(); configure(definition); return DataGrid(definition); }
        /// <summary>Adds a materialized grid definition.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder DataGrid(DataGridInputOptions definition) => Add(definition.Build());
        /// <summary>Adds a configured explorable chart through its shared factory.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder ChartView(Action<ChartViewInputOptions> configure) { var definition = Ui.ChartView(); configure(definition); return ChartView(definition); }
        /// <summary>Adds a materialized explorable chart.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder ChartView(ChartViewInputOptions definition) => Add(definition.Build());
        /// <summary>Adds a configured tree definition.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder TreeSelect(Action<TreeSelectInputOptions> configure) { var definition = Ui.TreeSelect(); configure(definition); return TreeSelect(definition); }
        /// <summary>Adds a materialized tree definition.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder TreeSelect(TreeSelectInputOptions definition) => Add(definition.Build());
        /// <summary>Adds a configured date-picker definition.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder DatePicker(Action<DatePickerInputOptions> configure) { var definition = Ui.DatePicker(); configure(definition); return DatePicker(definition); }
        /// <summary>Adds a materialized date-picker definition.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder DatePicker(DatePickerInputOptions definition) => Add(definition.Build());
        /// <summary>Adds configured retained tabs.</summary><param name="configure">Tab settings.</param><returns>This builder.</returns>
        public ContentBuilder Tabs(Action<Tabs>? configure = null) { var tabs = Ui.Tabs(); configure?.Invoke(tabs); return Add(tabs); }
        /// <summary>Adds retained tabs.</summary><param name="tabs">The tabs.</param><returns>This builder.</returns>
        public ContentBuilder Tabs(Tabs tabs) => Add(tabs);
        /// <summary>Adds a configured collapsible section.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder Collapsible(Action<CollapsibleInputOptions> configure) { var definition = Ui.Collapsible(); configure(definition); return Collapsible(definition); }
        /// <summary>Adds a materialized collapsible section.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Collapsible(CollapsibleInputOptions definition) => Add(definition.Build());
        /// <summary>Adds a configured outline.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder Outline(Action<OutlineInputOptions> configure) { var definition = Ui.Outline(); configure(definition); return Outline(definition); }
        /// <summary>Adds a materialized outline.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Outline(OutlineInputOptions definition) => Add(definition.Build());
        /// <summary>Adds a configured breadcrumb.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder Breadcrumb(Action<BreadcrumbInputOptions> configure) { var definition = Ui.Breadcrumb(); configure(definition); return Breadcrumb(definition); }
        /// <summary>Adds a materialized breadcrumb.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder Breadcrumb(BreadcrumbInputOptions definition) => Add(definition.Build());
        /// <summary>Adds a configured accordion.</summary><param name="configure">Group settings.</param><returns>This builder.</returns>
        public ContentBuilder Accordion(Action<Accordion>? configure = null) { var accordion = Ui.Accordion(); configure?.Invoke(accordion); return Add(accordion); }
        /// <summary>Adds a configured app log with a find bar.</summary><param name="configure">Viewport settings.</param><returns>This builder.</returns>
        public ContentBuilder LogViewport(Action<LogViewport> configure) { var log = Ui.LogViewport(); configure(log); return Add(log); }
        /// <summary>Adds an app log with a find bar.</summary><param name="log">The viewport.</param><returns>This builder.</returns>
        public ContentBuilder LogViewport(LogViewport log) => Add(log);
        /// <summary>Adds a configured summary frame.</summary><param name="configure">Frame settings.</param><returns>This builder.</returns>
        public ContentBuilder DashboardFrame(Action<Frames.DashboardFrame> configure) { var frame = Ui.DashboardFrame(); configure(frame); return Add(frame); }
        /// <summary>Adds a summary frame.</summary><param name="frame">The frame.</param><returns>This builder.</returns>
        public ContentBuilder DashboardFrame(Frames.DashboardFrame frame) => Add(frame);
        /// <summary>Adds an accordion.</summary><param name="accordion">The group.</param><returns>This builder.</returns>
        public ContentBuilder Accordion(Accordion accordion) => Add(accordion);
        /// <summary>Adds a configured splitter.</summary><param name="configure">Splitter settings.</param><returns>This builder.</returns>
        public ContentBuilder Splitter(Action<Splitter>? configure = null) { var splitter = Ui.Splitter(); configure?.Invoke(splitter); return Add(splitter); }
        /// <summary>Adds a splitter.</summary><param name="splitter">The splitter.</param><returns>This builder.</returns>
        public ContentBuilder Splitter(Splitter splitter) => Add(splitter);
    }
}
