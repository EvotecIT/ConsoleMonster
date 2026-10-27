using System;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Layout {
    public sealed partial class ContentBuilder {
        /// <summary>Adds a configured grid definition through its shared factory.</summary><param name="configure">Definition settings.</param><returns>This builder.</returns>
        public ContentBuilder DataGrid(Action<DataGridInputOptions> configure) { var definition = Ui.DataGrid(); configure(definition); return DataGrid(definition); }
        /// <summary>Adds a materialized grid definition.</summary><param name="definition">The definition.</param><returns>This builder.</returns>
        public ContentBuilder DataGrid(DataGridInputOptions definition) => Add(definition.Build());
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
    }
}
