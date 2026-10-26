using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Typed table configuration shared by construction and display; only bound overrides replace registered-view defaults.</summary>
    public abstract class TableConfigurationCmdlet : PSCmdlet {
        /// <summary><para type="description">Optional title indentation in cells.</para></summary>
        public virtual int? TitleIndent { get; set; }

        /// <summary><para type="description">Wraps column headings.</para></summary>
        public virtual SwitchParameter WrapHeaders { get; set; }

        /// <summary><para type="description">Renders consecutive grids when all columns cannot fit.</para></summary>
        public virtual SwitchParameter KeepAllColumns { get; set; }

        /// <summary><para type="description">Renders group headings as full-width rows.</para></summary>
        public virtual SwitchParameter GroupHeadersAsRows { get; set; }

        /// <summary><para type="description">Uses labelled records at or below this row count; zero disables.</para></summary>
        public virtual int RecordViewRows { get; set; } = 1;

        /// <summary><para type="description">Repeats the full key in record and card layouts.</para></summary>
        public virtual SwitchParameter ShowKeyField { get; set; }

        /// <summary><para type="description">Suppresses the default-on hidden-column footer.</para></summary>
        public virtual SwitchParameter NoHiddenColumns { get; set; }

        /// <summary><para type="description">Allows column widths to shrink between renders.</para></summary>
        public virtual SwitchParameter NoStableWidths { get; set; }

        /// <summary><para type="description">Suppresses the default-on header row.</para></summary>
        public virtual SwitchParameter NoHeader { get; set; }

        /// <summary><para type="description">Uses record cards at every width.</para></summary>
        public virtual SwitchParameter AlwaysCards { get; set; }

        /// <summary><para type="description">Optional per-row titles for record and card views.</para></summary>
        public virtual string[]? RowTitles { get; set; }

        /// <summary><para type="description">Shows group subtotals.</para></summary>
        public virtual SwitchParameter ShowSubtotals { get; set; }

        /// <summary><para type="description">Text for the totals row.</para></summary>
        public virtual string? TotalLabel { get; set; }

        /// <summary><para type="description">Maximum wrapped lines per value; zero has no limit.</para></summary>
        public virtual int WrapLines { get; set; } = 3;

        /// <summary><para type="description">Total count when the input contains a subset.</para></summary>
        public virtual int? TotalRowCount { get; set; }

        /// <summary><para type="description">Typed culture, clock and time-zone configuration for values.</para></summary>
        public virtual ConsoleMonster.Presenters.PresentationOptions? Presentation { get; set; }

        /// <summary>Applies explicitly bound presentation overrides without replacing registered-view defaults.</summary>
        /// <param name="table">The constructed table.</param>
        protected void ApplyTableConfiguration(Table table) {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(TitleIndent))) { table.TitleIndent = TitleIndent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(WrapHeaders))) { table.WrapHeaders = WrapHeaders.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(KeepAllColumns))) { table.KeepAllColumns = KeepAllColumns.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(GroupHeadersAsRows))) { table.GroupHeadersAsRows = GroupHeadersAsRows.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RecordViewRows))) { table.RecordViewRows = RecordViewRows; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ShowKeyField))) { table.ShowKeyField = ShowKeyField.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoHiddenColumns))) { table.ShowHiddenColumns = !NoHiddenColumns.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoStableWidths))) { table.StableWidths = !NoStableWidths.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoHeader))) { table.ShowHeader = !NoHeader.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(AlwaysCards))) { table.AlwaysCards = AlwaysCards.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RowTitles))) { table.RowTitles = RowTitles; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ShowSubtotals))) { table.ShowSubtotals = ShowSubtotals.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(TotalLabel))) { table.TotalLabel = TotalLabel; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(WrapLines))) { table.WrapLines = WrapLines; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(TotalRowCount))) { table.TotalRowCount = TotalRowCount; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Presentation))) { table.Presentation = Presentation!; }
        }
    }
}
