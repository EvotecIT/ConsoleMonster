using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster.Interactive;

namespace ConsoleMonster {
    /// <summary>Passive widgets and validated construction definitions. Typed renderer factories retain C# handles; Chart and Progress expose the shared polymorphic definitions.</summary>
    public static partial class Ui {
        /// <summary>Empty ActivityList widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty ActivityList widget; the optional primary input is its live identity.</returns>
        public static ActivityList ActivityList(string widgetId = ConsoleMonster.Widgets.ActivityList.ProgressTarget) => new ActivityList(widgetId);

        /// <summary>Empty CellMap widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty CellMap widget; the optional primary input is its live identity.</returns>
        public static CellMap CellMap(string widgetId = "cells") => new CellMap(widgetId);

        /// <summary>Empty KeyValue widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty KeyValue widget; the optional primary input is its live identity.</returns>
        public static KeyValueList KeyValue(string widgetId = "keyvalue") => new KeyValueList(widgetId);

        /// <summary>Empty Lanes widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty Lanes widget; the optional primary input is its live identity.</returns>
        public static Lanes Lanes(string widgetId = "lanes") => new Lanes(widgetId);

        /// <summary>Empty LogRegion widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty LogRegion widget; the optional primary input is its live identity.</returns>
        public static LogRegion LogRegion(string widgetId = "log") => new LogRegion(widgetId);

        /// <summary>Empty Phases widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty Phases widget; the optional primary input is its live identity.</returns>
        public static Phases Phases(string widgetId = "phases") => new Phases(widgetId);

        /// <summary>Empty SummaryBar widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty SummaryBar widget; the optional primary input is its live identity.</returns>
        public static SummaryBar SummaryBar(string widgetId = "summary") => new SummaryBar(widgetId);

        /// <summary>Empty Region widget; the optional primary input is its live identity.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty Region widget; the optional primary input is its live identity.</returns>
        public static RegionWidget Region(string widgetId = "region") => new RegionWidget(widgetId);

        /// <summary>Empty dashboard; configure Title and Content in the chain.</summary>
        /// <param name="widgetId">The primary input.</param>
        /// <returns>The empty dashboard; configure Title and Content in the chain.</returns>
        public static Dashboard Dashboard(string widgetId = "dashboard") => new Dashboard(widgetId: widgetId);

        /// <summary>Empty object tree; configure Title in the chain.</summary>
        /// <returns>The empty object tree; configure Title in the chain.</returns>
        public static Tree Tree() => new Tree();

        /// <summary>Empty header bar.</summary>
        /// <returns>The empty header bar.</returns>
        public static HeaderBar HeaderBar() => new HeaderBar(string.Empty);

        /// <summary>Empty footer bar.</summary>
        /// <returns>The empty footer bar.</returns>
        public static FooterBar FooterBar() => new FooterBar();

        /// <summary>Empty logo; configure Title and Subtitle in the chain.</summary>
        /// <returns>The empty logo; configure Title and Subtitle in the chain.</returns>
        public static Logo Logo() => new Logo(string.Empty);

        /// <summary>Empty status list.</summary><param name="widgetId">The optional primary live identity.</param><returns>The status list.</returns>
        public static StatusList StatusList(string widgetId = "status") => new StatusList(widgetId);

        /// <summary>Empty flow; add nodes, edges and branches in the chain.</summary>
        /// <param name="widgetId">The optional primary live identity.</param>
        /// <returns>The empty flow; add nodes, edges and branches in the chain.</returns>
        public static Flow Flow(string widgetId = "flow") => new Flow(widgetId);

        /// <summary>Calendar view; configure its date, view and marks in the chain.</summary>
        /// <returns>The calendar view; configure its date, view and marks in the chain.</returns>
        public static Calendar Calendar() => new Calendar(DateTime.Today.Year, DateTime.Today.Month);

        /// <summary>Numeric chart definition; Build delegates to Chart.Create.</summary>
        /// <returns>The numeric chart definition; Build delegates to Chart.Create.</returns>
        public static ChartOptions Chart() => new ChartOptions();

        /// <summary>Progress definition; Build delegates to Progress.Create.</summary>
        /// <returns>The progress definition; Build delegates to Progress.Create.</returns>
        public static ProgressOptions Progress() => new ProgressOptions();

        /// <summary>Column presentation format.</summary>
        /// <returns>The column presentation format.</returns>
        public static ColumnFormat ColumnFormat() => new ColumnFormat();

    }
}
