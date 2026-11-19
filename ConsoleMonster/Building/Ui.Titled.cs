using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster {
    public static partial class Ui {
        /// <summary>Creates a panel with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the panel, or null.</param>
        /// <returns>The panel.</returns>
        public static Panel Panel(string title, Action<Panel>? configure = null) => Configured(new Panel(null, title), configure);

        /// <summary>Creates a table with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the table, or null.</param>
        /// <returns>The table.</returns>
        public static Table Table(string title, Action<Table>? configure = null) => Configured(new Table { Title = title }, configure);

        /// <summary>Creates a tree with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the tree, or null.</param>
        /// <returns>The tree.</returns>
        public static Tree Tree(string title, Action<Tree>? configure = null) => Configured(new Tree(title), configure);

        /// <summary>Creates a header bar with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the header, or null.</param>
        /// <returns>The header.</returns>
        public static HeaderBar HeaderBar(string title, Action<HeaderBar>? configure = null) => Configured(new HeaderBar(title), configure);

        /// <summary>Creates a menu with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the menu, or null.</param>
        /// <returns>The menu.</returns>
        public static ConsoleMonster.Interactive.Menu Menu(string title, Action<ConsoleMonster.Interactive.Menu>? configure = null) => Configured(new ConsoleMonster.Interactive.Menu(title), configure);

        /// <summary>Creates a form with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the form, or null.</param>
        /// <returns>The form.</returns>
        public static ConsoleMonster.Interactive.Form Form(string title, Action<ConsoleMonster.Interactive.Form>? configure = null) => Configured(new ConsoleMonster.Interactive.Form(title), configure);

        /// <summary>Creates a chart definition with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the chart, or null.</param>
        /// <returns>The chart.</returns>
        public static ChartOptions Chart(string title, Action<ChartOptions>? configure = null) => Configured(new ChartOptions { Title = title }, configure);

        /// <summary>Creates a diagram with its title and, optionally, configures it.</summary>
        /// <param name="title">The title.</param>
        /// <param name="configure">Configures the diagram, or null.</param>
        /// <returns>The diagram.</returns>
        public static Diagram Diagram(string title, Action<Diagram>? configure = null) => Configured(new Diagram(title), configure);

        private static T Configured<T>(T model, Action<T>? configure) {
            configure?.Invoke(model);
            return model;
        }
    }
}
