using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using static ConsoleMonster.PowerShell.Display.ConsoleViewDocument;

namespace ConsoleMonster.PowerShell.Display;

internal sealed class DeclarativeView {
    private readonly IDictionary _data;
    private readonly List<Column> _columns = new List<Column>();
    private readonly List<IDictionary> _tones = new List<IDictionary>();
    internal DeclarativeView(IDictionary data) {
        _data = data;
        TypeName = String(data, "TypeName") ?? throw new ArgumentException("Each view needs TypeName.");
        ViewId = String(data, "ViewId") ?? String(data, "Group") ?? TypeName;
        if (string.IsNullOrWhiteSpace(ViewId)) { throw new ArgumentException("ViewId cannot be empty."); }
        string batch = String(data, "Batch") ?? "Single";
        if (!Enum.TryParse(batch, true, out DefaultDisplayBatch parsed) || !Enum.IsDefined(typeof(DefaultDisplayBatch), parsed)) { throw new ArgumentException("Unknown Batch."); }
        Batch = parsed;
        ScopeKeys = Sequence(data["ScopeKey"] ?? data["GroupBy"]).Cast<object>().Select(PropertyName).ToArray();
        Kind = String(data, "View") ?? "Table";
        if (!new[] { "Table", "Cards", "Inspector", "List", "Tree", "Auto", "Record" }.Contains(Kind, StringComparer.OrdinalIgnoreCase)) { throw new ArgumentException("The data-only adapter supports Table, Cards, Inspector, List, Record, Auto and Tree views."); }
        BuilderName = String(data, "Builder");
        foreach (string key in new[] { "Children", "Id", "ParentId", "Cycle", "Repeat" }) { if (data[key] != null) { PropertyName(data[key]!); } }
        foreach (string key in new[] { "Title", "Summary" }) { if (data[key] != null) { ValidateTemplate(String(data, key)!); } }
        if (data["Budget"] != null && !(data["Budget"] is IDictionary)) { throw new ArgumentException("Budget must be a hashtable."); }
        IDictionary limits = data["Budget"] as IDictionary ?? new Hashtable();
        Keys(limits, "Records", "Bytes", "Nodes", "Depth", "Children");
        Budget = new DefaultDisplayBudget(Number(limits, "Records", 2000), Number(limits, "Bytes", 16 * 1024 * 1024), Number(limits, "Nodes", 400), Number(limits, "Depth", 3), Number(limits, "Children", 25));
        string over = String(data, "OverBudget") ?? "Table";
        if (over != "Table" && over != "Native") { throw new ArgumentException("OverBudget must be Table or Native."); }
        foreach (object value in Sequence(data["Property"])) {
            if (value is string name) { _columns.Add(new Column(PropertyName(name), name, 0, null)); }
            else if (value is IDictionary column) {
                Keys(column, "Name", "Label", "Expression", "Property", "Priority", "Presenter");
                string property = PropertyName(column["Expression"] ?? column["Property"] ?? column["Name"] ?? throw new ArgumentException("A column requires a property name."));
                string label = String(column, "Label") ?? String(column, "Name") ?? property;
                _columns.Add(new Column(property, label, Number(column, "Priority", 0), String(column, "Presenter")));
            } else { throw new ArgumentException("Properties must be names or column data hashtables."); }
        }
        if (data["Priority"] != null) {
            if (!(data["Priority"] is IDictionary priorities)) { throw new ArgumentException("Priority must map property names to integers."); }
            foreach (DictionaryEntry priority in priorities) { PropertyName(priority.Key); Convert.ToInt32(priority.Value, CultureInfo.InvariantCulture); }
        }
        foreach (object value in Sequence(data["Tones"])) {
            if (!(value is IDictionary tone)) { throw new ArgumentException("Each tone must be a data hashtable."); }
            Keys(tone, "Property", "Pass", "Warn", "Fail", "Info", "Muted", "When");
            PropertyName(tone["Property"] ?? throw new ArgumentException("A tone requires Property."));
            if (tone["When"] != null && !(tone["When"] is IDictionary)) { throw new ArgumentException("Tone When must be a property/value map."); }
            if (tone["When"] is IDictionary when) { foreach (DictionaryEntry entry in when) { PropertyName(entry.Key); Scalar(entry.Value); } }
            foreach (string role in new[] { "Pass", "Warn", "Fail", "Info", "Muted" }) { if (tone.Contains(role)) { Scalar(tone[role]); } }
            _tones.Add(tone);
        }
    }
    internal string TypeName { get; }
    internal string ViewId { get; }
    internal string Kind { get; }
    internal DefaultDisplayBatch Batch { get; }
    internal IReadOnlyList<string> ScopeKeys { get; }
    internal DefaultDisplayBudget Budget { get; }
    internal string? BuilderName { get; }
    internal IWidget Build(IReadOnlyList<object> items, DefaultDisplayContext context) {
        if (items.Count == 0) { return TextBlock.Plain(string.Empty); }
        List<Column> columns = Columns(items[0]);
        if (Kind.Equals("Tree", StringComparison.OrdinalIgnoreCase)) {
            try { return BuildTree(items, context, columns); }
            catch (TreeBudgetExceededException) when (String(_data, "OverBudget") != "Native") {
                Table fallback = BuildTable(items, context, columns);
                fallback.Caption = Markup.Escape(items.Count.ToString(context.Culture) + " top-level rows · tree budget exceeded");
                return fallback;
            }
        }
        if (Kind.Equals("Inspector", StringComparison.OrdinalIgnoreCase) || Kind.Equals("List", StringComparison.OrdinalIgnoreCase) || Kind.Equals("Record", StringComparison.OrdinalIgnoreCase)) {
            RecordView record = new RecordView(Template(String(_data, "Title"), items[0], context)) { Summary = Template(String(_data, "Summary"), items[0], context) };
            foreach (object item in items) { foreach (Column column in columns) { record.AddField(Markup.Escape(column.Label), Cell(item, column, context), column.Presenter); } }
            return record;
        }
        return BuildTable(items, context, columns);
    }
    private Table BuildTable(IReadOnlyList<object> items, DefaultDisplayContext context, List<Column> columns) {
        Table table = new Table {
            Title = Template(String(_data, "Title"), items[0], context), Caption = Template(String(_data, "Summary"), items[0], context),
            AlwaysCards = Kind.Equals("Cards", StringComparison.OrdinalIgnoreCase), CardsBelow = 60, StableWidths = true,
            RecordViewRows = 0, Presentation = new PresentationOptions { Culture = context.Culture }
        };
        foreach (Column column in columns) {
            int priority = _data["Priority"] is IDictionary priorities && priorities.Contains(column.Name) ? Convert.ToInt32(priorities[column.Name], CultureInfo.InvariantCulture) : column.Priority;
            table.AddColumn(new TableColumn(Markup.Escape(column.Label)) { Key = column.Name, Priority = priority, Presenter = column.Presenter });
        }
        foreach (object item in items) { table.AddRow(columns.Select(column => Cell(item, column, context)).ToArray()); }
        return table;
    }
    private IWidget BuildTree(IReadOnlyList<object> items, DefaultDisplayContext context, List<Column> columns) {
        string labelProperty = columns.Count > 0 ? columns[0].Name : "Name";
        string? idProperty = String(_data, "Id");
        string? parentProperty = String(_data, "ParentId");
        string? childProperty = String(_data, "Children");
        if (childProperty == null && (idProperty == null || parentProperty == null)) { throw new InvalidOperationException("A declarative tree requires Children or Id and ParentId."); }
        List<object> roots = new List<object>();
        Dictionary<string, List<object>> children = new Dictionary<string, List<object>>(StringComparer.Ordinal);
        if (childProperty == null) {
            HashSet<string> ids = new HashSet<string>(items.Select(item => DisplayData.Text(DisplayData.Read(item, idProperty!))), StringComparer.Ordinal);
            foreach (object item in items) {
                string parent = DisplayData.Text(DisplayData.Read(item, parentProperty!));
                if (string.IsNullOrEmpty(parent) || !ids.Contains(parent)) { roots.Add(item); }
                else {
                    if (!children.TryGetValue(parent, out var list)) { list = new List<object>(); children.Add(parent, list); }
                    list.Add(item);
                }
            }
            if (roots.Count == 0 && items.Count > 0) { roots.Add(items[0]); }
        } else { roots.AddRange(items); }
        // Do not portray a large or disconnected graph as complete. The complete top-level table remains honest.
        if (items.Count > Budget.Nodes) {
            if (String(_data, "OverBudget") == "Native") { throw new InvalidOperationException("Tree node budget exceeded."); }
            Table fallback = BuildTable(items, context, columns);
            fallback.Caption = Markup.Escape(items.Count.ToString(context.Culture) + " rows · tree budget exceeded");
            return fallback;
        }
        Tree tree = new Tree(Template(String(_data, "Title"), items[0], context));
        int nodes = 0;
        HashSet<object> path = new HashSet<object>(DisplayData.ReferenceComparer.Instance);
        HashSet<object> visited = new HashSet<object>(DisplayData.ReferenceComparer.Instance);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        Action<TreeNode, object, int> append = null!;
        append = (parent, item, depth) => {
            if (++nodes > Budget.Nodes) { throw new TreeBudgetExceededException(); }
            string identity = idProperty == null ? string.Empty : DisplayData.Text(DisplayData.Read(item, idProperty));
            TreeNode node = parent.Add(Markup.Escape(DisplayData.Text(DisplayData.Read(item, labelProperty), context.Culture)));
            visited.Add(item);
            string? cycle = String(_data, "Cycle");
            string? repeat = String(_data, "Repeat");
            if (path.Contains(item) || (cycle != null && DisplayData.Read(item, cycle) is bool cyclic && cyclic)) { node.Reference = TreeReferenceKind.Cycle; return; }
            if ((identity.Length > 0 && !seen.Add(identity)) || (repeat != null && DisplayData.Read(item, repeat) is bool repeated && repeated)) { node.Reference = TreeReferenceKind.Repeat; return; }
            List<object> next;
            if (childProperty != null) {
                object? materialized = DisplayData.Read(item, childProperty);
                if (materialized == null) { return; }
                if (!(materialized is Array array)) { throw new InvalidOperationException("Declarative children must be materialized arrays; use a compiled builder for another collection contract."); }
                next = array.Cast<object>().ToList();
            } else { next = children.TryGetValue(identity, out var list) ? list : new List<object>(); }
            if (depth >= Budget.Depth) { if (next.Count > 0) { node.Reference = TreeReferenceKind.Collapsed; node.ReferenceSummary = next.Count.ToString(context.Culture) + " children · depth limit"; } return; }
            path.Add(item);
            foreach (object child in next.Take(Budget.Children)) { append(node, child, depth + 1); }
            path.Remove(item);
            if (next.Count > Budget.Children) { node.Detail = "+" + (next.Count - Budget.Children).ToString(context.Culture) + " more · pipe to Format-Table for every row"; }
        };
        foreach (object root in roots) { append(tree.Root, root, 0); }
        // A separate cyclic component has no root. Keep it visible rather than silently dropping its records.
        if (childProperty == null) { foreach (object item in items) { if (!visited.Contains(item)) { append(tree.Root, item, 0); } } }
        return tree;
    }
    private List<Column> Columns(object first) {
        if (_columns.Count > 0) { return _columns; }
        return first.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0).Select(property => property.Name)
            .Concat(first.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Select(field => field.Name))
            .Distinct(StringComparer.Ordinal).Select(name => new Column(name, name, 0, null)).ToList();
    }
    private TableCell Cell(object item, Column column, DefaultDisplayContext context) {
        object? value = DisplayData.Read(item, column.Name);
        foreach (IDictionary tone in _tones) {
            if (!string.Equals(String(tone, "Property"), column.Name, StringComparison.Ordinal)) { continue; }
            if (tone["When"] is IDictionary when && when.Cast<DictionaryEntry>().Any(entry => !Equal(DisplayData.Read(item, (string)entry.Key), entry.Value))) { continue; }
            foreach (string role in new[] { "Fail", "Warn", "Pass", "Info", "Muted" }) {
                if (!tone.Contains(role) || !Equal(value, tone[role])) { continue; }
                if (role == "Muted") { return new TableCell("[muted]" + Markup.Escape(DisplayData.Text(value, context.Culture)) + "[/]"); }
                StatusKind status = role == "Fail" ? StatusKind.Fail : role == "Warn" ? StatusKind.Warn : role == "Pass" ? StatusKind.Pass : StatusKind.Info;
                return TableCell.ForStatus(status, DisplayData.Text(value, context.Culture));
            }
        }
        return TableCell.FromValue(value == null || DisplayData.IsScalar(value) ? value : DisplayData.Text(value, context.Culture));
    }
    private static bool Equal(object? left, object? right) => string.Equals(DisplayData.Text(left), DisplayData.Text(right), StringComparison.OrdinalIgnoreCase);
    private static void Scalar(object? value) { if (value != null && !DisplayData.IsScalar(value)) { throw new ArgumentException("Tone comparisons must be scalar data."); } }
    private static string PropertyName(object value) {
        if (!(value is string name) || !Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$")) { throw new ArgumentException("A simple property name is required."); }
        return name;
    }
    private static void ValidateTemplate(string template) {
        string plain = Regex.Replace(template, "\\{[A-Za-z_][A-Za-z0-9_]*\\}", string.Empty);
        if (plain.IndexOfAny(new[] { '{', '}' }) >= 0) { throw new ArgumentException("Templates allow only {PropertyName} substitutions."); }
    }
    private static string? Template(string? template, object item, DefaultDisplayContext context) => template == null ? null
        : Markup.Escape(Regex.Replace(template, "\\{([A-Za-z_][A-Za-z0-9_]*)\\}", match => DisplayData.Text(DisplayData.Read(item, match.Groups[1].Value), context.Culture)));
    private sealed class Column {
        internal Column(string name, string label, int priority, string? presenter) {
            Name = name; Label = label; Priority = priority;
            if (presenter != null) {
                // Only named core presenters, never a type name, executable expression or script.
                System.Reflection.PropertyInfo? property = typeof(CellPresenters).GetProperty(presenter, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);
                Presenter = property?.GetValue(null, null) as ICellPresenter ?? throw new ArgumentException("Unknown core presenter: " + presenter);
            }
        }
        internal string Name { get; }
        internal string Label { get; }
        internal int Priority { get; }
        internal ICellPresenter? Presenter { get; }
    }
    private sealed class TreeBudgetExceededException : InvalidOperationException {
        internal TreeBudgetExceededException() : base("Tree node budget exceeded; use native formatting.") { }
    }
}
