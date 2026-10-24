using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster {
    /// <summary>
    /// Factories that let a layout read like the screen, for <c>using static ConsoleMonster.Ui;</c>:
    /// <code>
    /// Terminal.Write(Rows(
    ///     Columns(
    ///         Panel("Servers", Table(servers).Column(s => s.Name, c => c.WithIsKey()).WithTotals()),
    ///         Panel("Summary", KeyValue(("Servers", 3), ("Failed", 1)))),
    ///     Text("[muted]updated 10:02[/]")));
    /// </code>
    /// Each one only creates the widget it names (the same object model as <c>new Panel(...)</c>), so the result can be
    /// configured further with object initializers or the generated <c>With...</c> methods. For layouts whose live widgets
    /// are needed as typed handles, <see cref="Building.DashboardBuilder"/> hands them back through <c>out</c> parameters.
    /// </summary>
    public static class Ui {
        /// <summary>Children stacked top to bottom (<see cref="Layout.Rows"/>).</summary>
        /// <param name="children">The children; wrap one in <see cref="Fill"/> or <see cref="Fixed"/> to size it.</param>
        /// <returns>The rows.</returns>
        public static Rows Rows(params IWidget[] children) => new Rows(children);

        /// <summary>Children side by side, sharing the width equally unless sized (<see cref="Layout.Columns"/>).</summary>
        /// <param name="children">The children; wrap one in <see cref="Fill"/> or <see cref="Fixed"/> to size it.</param>
        /// <returns>The columns.</returns>
        public static Columns Columns(params IWidget[] children) => new Columns(children);

        /// <summary>Children in a grid with a number of columns, filled row by row (<see cref="Layout.Grid"/>).</summary>
        /// <param name="columns">The number of columns.</param>
        /// <param name="children">The children.</param>
        /// <returns>The grid.</returns>
        public static Grid Grid(int columns, params IWidget[] children) => new Grid(columns, children);

        /// <summary>A titled panel around content (<see cref="Layout.Panel"/>).</summary>
        /// <param name="title">The title (markup).</param>
        /// <param name="content">The content.</param>
        /// <returns>The panel.</returns>
        public static Panel Panel(string title, IWidget content) => new Panel(content, title);

        /// <summary>A child that takes a share of the remaining height (in rows) or width (in columns).</summary>
        /// <param name="widget">The child.</param>
        /// <param name="weight">Its share relative to other filling children (default 1).</param>
        /// <returns>The sized child.</returns>
        public static Sized Fill(IWidget widget, double weight = 1) => new Sized(widget, LayoutSize.Fraction(weight));

        /// <summary>A child with a fixed height (in rows) or width (in columns).</summary>
        /// <param name="widget">The child.</param>
        /// <param name="cells">The size in rows or cells.</param>
        /// <returns>The sized child.</returns>
        public static Sized Fixed(IWidget widget, int cells) => new Sized(widget, LayoutSize.Fixed(cells));

        /// <summary>An empty table, to fill with columns and rows.</summary>
        /// <returns>The table.</returns>
        public static Table Table() => new Table();

        /// <summary>A table whose rows are read from typed items (<see cref="Widgets.Table.For{T}(IEnumerable{T})"/>).</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items.</param>
        /// <returns>The table; add columns with <c>Column(s =&gt; s.Name)</c>.</returns>
        public static Table<T> Table<
#if NET8_0_OR_GREATER
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEnumerable<T> items) => global::ConsoleMonster.Widgets.Table.For(items);

        /// <summary>Key/value pairs, values formatted by their type (<see cref="KeyValueList"/>).</summary>
        /// <param name="items">The pairs, in order.</param>
        /// <returns>The list.</returns>
        public static KeyValueList KeyValue(params (string Key, object? Value)[] items) {
            KeyValueList list = new KeyValueList();
            foreach ((string key, object? value) in items ?? new (string, object?)[0]) {
                list.AddValue(key, value);
            }
            return list;
        }

        /// <summary>Text with markup (<see cref="TextBlock"/>); <c>[bold]</c>, <c>[pass]</c>, <c>[muted]</c> and the other tags apply.</summary>
        /// <param name="markup">The markup.</param>
        /// <returns>The text block.</returns>
        public static TextBlock Text(string markup) => new TextBlock(markup);

        /// <summary>A horizontal rule with an optional title (<see cref="Widgets.Rule"/>).</summary>
        /// <param name="title">The title (markup), or null.</param>
        /// <returns>The rule.</returns>
        public static Rule Rule(string? title = null) => new Rule(title);

        /// <summary>A sparkline of values (<see cref="Widgets.Sparkline"/>).</summary>
        /// <param name="values">The values, oldest first.</param>
        /// <returns>The sparkline.</returns>
        public static Sparkline Sparkline(IEnumerable<double> values) => new Sparkline().AddRange(values ?? throw new ArgumentNullException(nameof(values)));

        /// <summary>Progress lanes, one per key (<see cref="Widgets.Lanes"/>), each waiting to start.</summary>
        /// <param name="keys">The lane keys (also their labels).</param>
        /// <returns>The lanes.</returns>
        public static Lanes Lanes(params string[] keys) {
            Lanes lanes = new Lanes();
            foreach (string key in keys ?? new string[0]) {
                lanes.Add(key);
            }
            return lanes;
        }

        /// <summary>A progress bar (<see cref="ProgressBar"/>).</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="value">The current value.</param>
        /// <param name="total">The total (0 for an unknown total).</param>
        /// <returns>The progress bar.</returns>
        public static ProgressBar ProgressBar(string label, double value = 0, double total = 100) => new ProgressBar(label) { Value = value, Total = total };

        /// <summary>A list of labelled states (<see cref="StatusList"/>).</summary>
        /// <param name="items">The labels and their states, in order.</param>
        /// <returns>The status list.</returns>
        public static StatusList StatusList(params (string Label, StatusKind Status)[] items) {
            StatusList list = new StatusList();
            foreach ((string label, StatusKind status) in items ?? new (string, StatusKind)[0]) {
                list.Add(label, status);
            }
            return list;
        }

        /// <summary>A tree with a root label and nodes (<see cref="Widgets.Tree"/>).</summary>
        /// <param name="root">The root label (markup), or null to show the nodes as top level.</param>
        /// <param name="nodes">The nodes under the root.</param>
        /// <returns>The tree.</returns>
        public static Tree Tree(string? root, params TreeNode[] nodes) {
            Tree tree = new Tree(root);
            foreach (TreeNode node in nodes ?? new TreeNode[0]) {
                tree.Root.Add(node);
            }
            return tree;
        }

        /// <summary>A tree node with children (<see cref="TreeNode"/>).</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="children">The child nodes.</param>
        /// <returns>The node.</returns>
        public static TreeNode Node(string label, params TreeNode[] children) => Node(label, null, children);

        /// <summary>A tree node with a state marker and children (<see cref="TreeNode"/>).</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="status">The state shown before the label, or null.</param>
        /// <param name="children">The child nodes.</param>
        /// <returns>The node.</returns>
        public static TreeNode Node(string label, StatusKind? status, params TreeNode[] children) {
            TreeNode node = new TreeNode(label, status);
            foreach (TreeNode child in children ?? new TreeNode[0]) {
                node.Add(child);
            }
            return node;
        }

        /// <summary>A dashboard shell (header, body and key footer) for a live session (<see cref="Widgets.Dashboard"/>).</summary>
        /// <param name="title">The title (markup).</param>
        /// <param name="body">The body.</param>
        /// <returns>The dashboard.</returns>
        public static Dashboard Dashboard(string title, IWidget body) => new Dashboard(title, body);
    }
}
