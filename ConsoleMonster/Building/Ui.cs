using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster.Interactive;

namespace ConsoleMonster {
    /// <summary>The fluent entry point: empty widgets, named configuration and Content builders. Initializers use the same object model.</summary>
    public static partial class Ui {
        /// <summary>Empty vertical composition.</summary>
        /// <returns>The empty vertical composition.</returns>
        public static Rows Rows() => new Rows();

        /// <summary>Empty horizontal composition.</summary>
        /// <returns>The empty horizontal composition.</returns>
        public static Columns Columns() => new Columns();

        /// <summary>Empty layout grid; configure Columns before adding cells.</summary>
        /// <returns>The empty layout grid; configure Columns before adding cells.</returns>
        public static ConsoleMonster.Layout.Grid LayoutGrid() => new ConsoleMonster.Layout.Grid(2);

        /// <summary>Empty panel; configure Title and Content in the chain.</summary>
        /// <returns>The empty panel; configure Title and Content in the chain.</returns>
        public static Panel Panel() => new Panel();

        /// <summary>Empty overlay composition.</summary>
        /// <returns>The empty overlay composition.</returns>
        public static Stack Stack() => new Stack();

        /// <summary>Empty aligned container.</summary>
        /// <returns>The empty aligned container.</returns>
        public static Align Align() => new Align();

        /// <summary>Empty padded container.</summary>
        /// <returns>The empty padded container.</returns>
        public static Padding Padding() => new Padding();

        /// <summary>Empty scrolling container.</summary>
        /// <returns>The empty scrolling container.</returns>
        public static ScrollView ScrollView() => new ScrollView();

        /// <summary>Literal text; use Ui.Markup(markup) for explicit markup.</summary>
        /// <param name="text">The primary input.</param>
        /// <returns>The literal text; use Ui.Markup(markup) for explicit markup.</returns>
        public static TextBlock Text(string text = "") => TextBlock.Plain(text);

        /// <summary>Markup text, matching New-ConsoleText -Markup.</summary>
        /// <param name="markup">The markup input.</param><returns>The markup text widget.</returns>
        public static TextBlock Markup(string markup) => new TextBlock(markup);

        /// <summary>Horizontal rule with an optional primary title.</summary>
        /// <param name="title">The primary input.</param>
        /// <returns>The horizontal rule with an optional primary title.</returns>
        public static Rule Rule(string? title = null) => new Rule(title);

        /// <summary>Empty table; add columns and rows in the chain.</summary>
        /// <returns>The empty table; add columns and rows in the chain.</returns>
        public static Table Table() => new Table();

    }
}
