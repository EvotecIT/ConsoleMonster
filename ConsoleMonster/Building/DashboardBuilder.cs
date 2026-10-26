using ConsoleMonster.Widgets;

namespace ConsoleMonster.Building {
    /// <summary>
    /// Fluent builder for a <see cref="Dashboard"/>: header, a vertical body built like <see cref="RowsBuilder"/>, footer.
    /// <code>
    /// Dashboard dashboard = new DashboardBuilder("Build")
    ///     .Subtitle("ConsoleMonster 2.0.0")
    ///     .Add(new Phases(), out Phases phases)
    ///     .Columns(c => c.Panel("Steps", new StatusList(), out StatusList steps, 1.4).Panel("Module", new KeyValueList(), out KeyValueList info))
    ///     .Panel("Output", new LogRegion(), out LogRegion log, fill: true)
    ///     .BuildDashboard();
    /// </code>
    /// </summary>
    public sealed class DashboardBuilder : RowsBuilderBase<DashboardBuilder> {
        private string _title;
        private string? _subtitle;
        private string? _footer;

        /// <summary>Creates a builder.</summary>
        /// <param name="title">The dashboard title (markup).</param>
        public DashboardBuilder(string title) {
            _title = title ?? string.Empty;
        }

        /// <summary>Sets the dashboard title before building.</summary>
        /// <param name="title">The title, including markup.</param><returns>This builder.</returns>
        public DashboardBuilder Title(string title) { _title = title ?? string.Empty; return this; }

        /// <summary>Sets the subtitle.</summary>
        /// <param name="subtitle">The subtitle (markup).</param>
        /// <returns>This builder.</returns>
        public DashboardBuilder Subtitle(string subtitle) {
            _subtitle = subtitle;
            return this;
        }

        /// <summary>Sets custom footer text; <c>null</c> uses the dashboard's default key hints.</summary>
        /// <param name="footer">The footer (markup).</param>
        /// <returns>This builder.</returns>
        public DashboardBuilder Footer(string? footer) {
            _footer = footer;
            return this;
        }

        /// <summary>Builds the dashboard.</summary>
        /// <param name="widgetId">The live name of the dashboard (default <c>dashboard</c>).</param>
        /// <returns>The dashboard.</returns>
        public Dashboard BuildDashboard(string widgetId = "dashboard") {
            return new Dashboard(_title, Build(), widgetId) { Subtitle = _subtitle, Footer = _footer };
        }
    }
}
