using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Data;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Views {
    /// <summary>The view <see cref="ConsoleView"/> picks for some data.</summary>
    public enum ConsoleViewKind {
        /// <summary>Nothing to show.</summary>
        Empty,

        /// <summary>Plain text: strings or scalar values.</summary>
        Text,

        /// <summary>Highlighted code: strings in a detected language (JSON, PowerShell, XML, YAML, C#...).</summary>
        Code,

        /// <summary>One object (or objects of different shapes) as an inspector.</summary>
        Inspector,

        /// <summary>Objects of one shape as a table (cards when the window is narrow).</summary>
        Table,

        /// <summary>Objects of one shape as cards, one per object.</summary>
        Cards,

        /// <summary>Hierarchical objects (a child collection of the same type) as a tree.</summary>
        Tree,

        /// <summary>A numeric series as a bar chart.</summary>
        Chart,

        /// <summary>The interactive grid (filter, sort, select).</summary>
        Grid
    }

    /// <summary>Options for <see cref="ConsoleView"/>.</summary>
    public sealed class ConsoleViewOptions {
        /// <summary>A view to use instead of the automatic choice.</summary>
        public ConsoleViewKind? View { get; set; }

        /// <summary>The member that holds each object's children, for a tree (found automatically when null).</summary>
        public string? ChildProperty { get; set; }

        /// <summary>Whether objects should become a chart (label from the first text member, value from the first number).</summary>
        public bool Chart { get; set; }

        /// <summary>Member sources tried before reflection (for example PowerShell objects).</summary>
        public IList<IMemberSource> Sources { get; } = new List<IMemberSource>();

        /// <summary>The members to show as columns, or null for all public members.</summary>
        public IList<string>? Columns { get; set; }

        /// <summary>A title for the view (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>The deepest level a tree or inspector shows (default 3).</summary>
        public int Depth { get; set; } = 3;
    }

    /// <summary>
    /// Picks the best view for data and builds it: one object becomes an inspector, objects of one shape a table (cards
    /// when narrow), hierarchical objects a tree, a numeric series a chart, strings highlighted code when a language is
    /// detected and text otherwise. Columns get presenters from their member names and types
    /// (<see cref="CellPresenters.ForMember"/>). <c>Show-Console</c> is the PowerShell front end.
    /// </summary>
    public static class ConsoleView {
        private static readonly IMemberSource Reflection = new ReflectionMemberSource();

        /// <summary>Builds the automatic view for a value: a sequence is shown as its items, anything else as one item.</summary>
        /// <param name="value">The value.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The widget.</returns>
        public static IWidget Auto(object? value, ConsoleViewOptions? options = null) {
            List<object?> items = new List<object?>();
            if (value is IEnumerable sequence && !(value is string) && !(value is IDictionary)) {
                foreach (object? item in sequence) {
                    items.Add(item);
                }
            } else if (value != null) {
                items.Add(value);
            }
            return Auto(items, options);
        }

        /// <summary>Builds the automatic (or requested) view for some items.</summary>
        /// <param name="items">The items.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The widget.</returns>
        public static IWidget Auto(IReadOnlyList<object?> items, ConsoleViewOptions? options = null) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            options ??= new ConsoleViewOptions();
            switch (Decide(items, options)) {
                case ConsoleViewKind.Empty:
                    return new TextBlock(string.Empty);
                case ConsoleViewKind.Text:
                    return new TextBlock(Markup.Escape(string.Join("\n", items.Select(item => Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty))));
                case ConsoleViewKind.Code:
                    return CodeView.For(string.Join("\n", items.Select(item => Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty)), "auto");
                case ConsoleViewKind.Chart:
                    return BuildChart(items, options);
                case ConsoleViewKind.Tree:
                    return BuildTree(items, options);
                case ConsoleViewKind.Inspector:
                    return BuildInspector(items, options);
                case ConsoleViewKind.Cards:
                    Table cards = BuildTable(items, options);
                    cards.AlwaysCards = true;
                    return cards;
                default:
                    return BuildTable(items, options);
            }
        }

        /// <summary>The view the data gets (see the class remarks): <see cref="ConsoleViewOptions.View"/> wins when set.</summary>
        /// <param name="items">The items.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The view.</returns>
        public static ConsoleViewKind Decide(IReadOnlyList<object?> items, ConsoleViewOptions? options = null) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            options ??= new ConsoleViewOptions();
            if (options.View.HasValue) {
                return options.View.Value;
            }
            List<object> present = items.Where(item => item != null).Select(item => item!).ToList();
            if (present.Count == 0) {
                return ConsoleViewKind.Empty;
            }
            if (present.All(item => item is string)) {
                string text = string.Join("\n", present.Cast<string>());
                ITextHighlighter? highlighter = Highlighters.Detect(text);
                return highlighter != null && !(highlighter is SmartHighlighter) ? ConsoleViewKind.Code : ConsoleViewKind.Text;
            }
            if (present.All(IsNumber)) {
                return present.Count > 1 ? ConsoleViewKind.Chart : ConsoleViewKind.Text;
            }
            if (present.All(IsScalar)) {
                return ConsoleViewKind.Text;
            }
            if (options.Chart) {
                return ConsoleViewKind.Chart;
            }
            string? childProperty = options.ChildProperty ?? FindChildProperty(present[0], options);
            if (childProperty != null && options.ChildProperty != null) {
                return ConsoleViewKind.Tree;
            }
            if (present.Count == 1) {
                return childProperty != null ? ConsoleViewKind.Tree : ConsoleViewKind.Inspector;
            }
            string type = TypeName(present[0], options);
            if (present.Any(item => !string.Equals(TypeName(item, options), type, StringComparison.Ordinal))) {
                return ConsoleViewKind.Inspector;
            }
            return childProperty != null ? ConsoleViewKind.Tree : ConsoleViewKind.Table;
        }

        /// <summary>A table of the items' members, with presenters chosen by member name and type.</summary>
        /// <param name="items">The items.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The table.</returns>
        public static Table BuildTable(IReadOnlyList<object?> items, ConsoleViewOptions? options = null) {
            options ??= new ConsoleViewOptions();
            List<object> present = items.Where(item => item != null).Select(item => item!).ToList();
            List<string> columns = options.Columns != null ? new List<string>(options.Columns) : new List<string>();
            Dictionary<string, Type?> types = new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);
            foreach (object item in present) {
                foreach (InspectMember member in Members(item, options)) {
                    if (options.Columns == null && !columns.Contains(member.Name, StringComparer.OrdinalIgnoreCase)) {
                        columns.Add(member.Name);
                    }
                    if (!types.TryGetValue(member.Name, out Type? known) || known == null) {
                        types[member.Name] = SafeRead(member)?.GetType();
                    }
                }
            }
            Table table = new Table { Title = options.Title };
            foreach (string column in columns) {
                types.TryGetValue(column, out Type? type);
                table.AddColumn(new TableColumn(Markup.Escape(column)) { Presenter = CellPresenters.ForMember(column, type) });
            }
            foreach (object item in present) {
                Dictionary<string, object?> values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (InspectMember member in Members(item, options)) {
                    values[member.Name] = SafeRead(member);
                }
                table.AddValues(columns.Select(column => values.TryGetValue(column, out object? value) ? Plain(value) : null).ToArray());
            }
            return table;
        }

        /// <summary>A tree of the items and their children (<see cref="ConsoleViewOptions.ChildProperty"/> or found automatically).</summary>
        /// <param name="items">The items.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The tree.</returns>
        public static Tree BuildTree(IReadOnlyList<object?> items, ConsoleViewOptions? options = null) {
            options ??= new ConsoleViewOptions();
            Tree tree = new Tree(options.Title);
            List<object> present = items.Where(item => item != null).Select(item => item!).ToList();
            string? child = options.ChildProperty ?? (present.Count > 0 ? FindChildProperty(present[0], options) : null);
            foreach (object item in present) {
                AddNode(tree.Root, item, child, options, 0);
            }
            return tree;
        }

        /// <summary>A bar chart: numbers as they are, or objects as label (first text member) and value (first number member).</summary>
        /// <param name="items">The items.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The chart.</returns>
        public static BarChart BuildChart(IReadOnlyList<object?> items, ConsoleViewOptions? options = null) {
            options ??= new ConsoleViewOptions();
            BarChart chart = new BarChart();
            int index = 0;
            foreach (object? item in items) {
                index++;
                if (item == null) {
                    continue;
                }
                if (IsNumber(item)) {
                    chart.Add(index.ToString(CultureInfo.InvariantCulture), Convert.ToDouble(item, CultureInfo.InvariantCulture));
                    continue;
                }
                string? label = null;
                double? value = null;
                foreach (InspectMember member in Members(item, options)) {
                    object? read = SafeRead(member);
                    if (label == null && read is string text) {
                        label = text;
                    } else if (value == null && read != null && IsNumber(read)) {
                        value = Convert.ToDouble(read, CultureInfo.InvariantCulture);
                    }
                }
                if (value.HasValue) {
                    chart.Add(label ?? index.ToString(CultureInfo.InvariantCulture), value.Value);
                }
            }
            return chart;
        }

        private static IWidget BuildInspector(IReadOnlyList<object?> items, ConsoleViewOptions options) {
            InspectorOptions inspect = new InspectorOptions { MaxDepth = options.Depth };
            foreach (IMemberSource source in options.Sources) {
                inspect.Sources.Add(source);
            }
            List<IWidget> views = new List<IWidget>();
            foreach (object? item in items) {
                if (item != null) {
#pragma warning disable IL2026
                    views.Add(new InspectorView(ObjectInspector.Inspect(item, inspect), options.Title));
#pragma warning restore IL2026
                }
            }
            return views.Count == 1 ? views[0] : new Rows(views.ToArray()) { Gap = 1 };
        }

        private static void AddNode(TreeNode parent, object item, string? child, ConsoleViewOptions options, int depth) {
            List<InspectMember> members = Members(item, options).ToList();
            string label = Label(item, members, child);
            TreeNode node = parent.Add(Markup.Escape(label));
            if (child == null || depth + 1 >= options.Depth + 8) {
                return;
            }
            InspectMember? children = members.FirstOrDefault(member => string.Equals(member.Name, child, StringComparison.OrdinalIgnoreCase));
            if (children != null && SafeRead(children) is IEnumerable sequence && !(sequence is string)) {
                foreach (object? grandchild in sequence) {
                    if (grandchild != null) {
                        AddNode(node, grandchild, child, options, depth + 1);
                    }
                }
            }
        }

        // The label of a tree node: a Name-like member when there is one, otherwise the first text member, otherwise the value.
        private static string Label(object item, List<InspectMember> members, string? child) {
            foreach (string preferred in new[] { "Name", "Title", "DisplayName", "Label", "Key", "Id" }) {
                InspectMember? member = members.FirstOrDefault(m => string.Equals(m.Name, preferred, StringComparison.OrdinalIgnoreCase));
                if (member != null && SafeRead(member) is object value && IsScalar(value)) {
                    return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
                }
            }
            foreach (InspectMember member in members) {
                if (!string.Equals(member.Name, child, StringComparison.OrdinalIgnoreCase) && SafeRead(member) is string text) {
                    return text;
                }
            }
            return Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        // A member holding a collection whose items have the same type as the object: its children.
        private static string? FindChildProperty(object item, ConsoleViewOptions options) {
            if (IsScalar(item) || item is IEnumerable) {
                return null;
            }
            string type = TypeName(item, options);
            foreach (InspectMember member in Members(item, options)) {
                if (SafeRead(member) is IEnumerable sequence && !(sequence is string) && !(sequence is IDictionary)) {
                    foreach (object? first in sequence) {
                        if (first != null && string.Equals(TypeName(first, options), type, StringComparison.Ordinal)) {
                            return member.Name;
                        }
                        break;
                    }
                }
            }
            return null;
        }

        private static IEnumerable<InspectMember> Members(object item, ConsoleViewOptions options) {
            foreach (IMemberSource source in options.Sources) {
                if (source.CanRead(item)) {
                    return source.GetMembers(item);
                }
            }
            return Reflection.GetMembers(item);
        }

        private static string TypeName(object item, ConsoleViewOptions options) {
            foreach (IMemberSource source in options.Sources) {
                if (source.CanRead(item)) {
                    return source.GetTypeName(item);
                }
            }
            return item.GetType().FullName ?? item.GetType().Name;
        }


        private static object? SafeRead(InspectMember member) {
            try {
                return member.Value;
            } catch (Exception ex) when (!(ex is OutOfMemoryException)) {
                return null;
            }
        }

        // Nested objects in a table cell show as their text (a type name for most objects), not as a nested structure.
        private static object? Plain(object? value) {
            if (value == null || IsScalar(value) || value is Text.RichText) {
                return value;
            }
            if (value is IEnumerable sequence && !(value is string)) {
                List<string> parts = new List<string>();
                foreach (object? item in sequence) {
                    if (parts.Count == 5) {
                        parts.Add("…");
                        break;
                    }
                    parts.Add(Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty);
                }
                return "{" + string.Join(", ", parts) + "}";
            }
            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }

        private static bool IsNumber(object value) => value is int || value is long || value is double || value is float || value is decimal
            || value is short || value is byte || value is uint || value is ulong || value is ushort || value is sbyte;

        private static bool IsScalar(object value) => ObjectInspector.IsScalarValue(value);
    }
}
