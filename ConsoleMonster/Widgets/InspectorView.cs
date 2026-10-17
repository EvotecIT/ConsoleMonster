using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Shows an arbitrary object (POCO, dictionary, PSObject through a member source, JSON on .NET 8) as a record view:
    /// scalar members as labelled values through the automatic presenter, nested objects as indented sections, uniform
    /// collections as nested tables (columns inferred from the elements), other collections as lists, with
    /// <c>+N more</c>, cycle and depth-limit notes. The object is inspected once, when the view is created.
    /// </summary>
    public sealed class InspectorView : IWidget {
        private const int MaxText = 500;
        private RecordView? _view;

        /// <summary>Inspects a value and creates the view.</summary>
        /// <param name="value">The value.</param>
        /// <param name="options">Depth, item and column limits, filters and member sources, or null for defaults.</param>
        /// <param name="title">The title, or null for the value's type name.</param>
#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Inspecting arbitrary objects reflects over public properties unless options supply an IMemberSource or disable reflection.")]
#endif
        public InspectorView(object? value, InspectorOptions? options = null, string? title = null)
            : this(ObjectInspector.Inspect(value, options), title) {
        }

        /// <summary>Creates the view from an inspection tree.</summary>
        /// <param name="root">The inspection root.</param>
        /// <param name="title">The title, or null for the root's type name.</param>
        public InspectorView(InspectNode root, string? title = null) {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Title = title;
        }

        /// <summary>The inspection root.</summary>
        public InspectNode Root { get; }

        /// <summary>The title, or null for the root's type name.</summary>
        public string? Title { get; }

        /// <summary>Whether the title and summary line is shown (default true; nested inspectors turn it off).</summary>
        public bool ShowHeader { get; set; } = true;

        /// <summary>Culture, clock and time zone for values.</summary>
        public Presenters.PresentationOptions Presentation { get; set; } = new Presenters.PresentationOptions();

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => View.Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => View.Render(region, context);

        private RecordView View => _view ?? (_view = Build());

        private RecordView Build() {
            RecordView view = ShowHeader ? new RecordView(Markup.Escape(Title ?? Root.TypeName)) { Presentation = Presentation, Summary = SummaryOf(Root) } : new RecordView { Presentation = Presentation };
            switch (Root) {
                case InspectObject obj:
                    AddMembers(view, obj.Members, 0);
                    break;
                case InspectCollection collection:
                    AddCollectionBody(view, collection, 0);
                    break;
                default:
                    view.AddFieldAt(0, "Value", CellFor(Root));
                    break;
            }
            return view;
        }

        private void AddMembers(RecordView view, IReadOnlyList<InspectNode> members, int level) {
            foreach (InspectNode member in members) {
                string name = member.Name ?? string.Empty;
                switch (member) {
                    case InspectObject obj:
                        view.AddHeading(Markup.Escape(name), level, null, obj.TypeName);
                        AddMembers(view, obj.Members, level + 1);
                        break;
                    case InspectCollection collection:
                        view.AddHeading(Markup.Escape(name), level, null, SummaryOf(collection));
                        AddCollectionBody(view, collection, level + 1);
                        break;
                    default:
                        view.AddFieldAt(level, name, CellFor(member));
                        break;
                }
            }
        }

        private void AddCollectionBody(RecordView view, InspectCollection collection, int level) {
            if (collection.Columns != null && collection.Columns.Count > 0) {
                view.AddWidgetAt(level, TableOf(collection));
            } else {
                int index = 0;
                foreach (InspectNode item in collection.Items) {
                    string label = "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                    if (item is InspectObject obj) {
                        view.AddHeading(Markup.Escape(label), level, null, obj.TypeName);
                        AddMembers(view, obj.Members, level + 1);
                    } else if (item is InspectCollection inner) {
                        view.AddHeading(Markup.Escape(label), level, null, SummaryOf(inner));
                        AddCollectionBody(view, inner, level + 1);
                    } else {
                        view.AddFieldAt(level, label, CellFor(item));
                    }
                    index++;
                }
            }
            string? more = MoreText(collection);
            if (more != null) {
                view.AddFieldAt(level, string.Empty, new TableCell("[muted]" + Markup.Escape(more) + "[/]"));
            }
        }

        private Table TableOf(InspectCollection collection) {
            Table table = new Table { Border = TableBorder.Minimal, RecordViewRows = 0, Presentation = Presentation };
            foreach (string column in collection.Columns!) {
                table.AddColumn(new TableColumn(Markup.Escape(column)) { Key = column });
            }
            foreach (InspectNode item in collection.Items) {
                object?[] values = new object?[collection.Columns.Count];
                if (item is InspectObject obj) {
                    for (int c = 0; c < collection.Columns.Count; c++) {
                        values[c] = CellValue(Find(obj, collection.Columns[c]));
                    }
                }
                table.AddValues(values);
            }
            if (collection.HiddenColumns > 0) {
                table.Caption = Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.MoreColumns, collection.HiddenColumns.ToString(CultureInfo.InvariantCulture));
            }
            return table;
        }

        private static InspectNode? Find(InspectObject obj, string name) {
            foreach (InspectNode member in obj.Members) {
                if (string.Equals(member.Name, name, StringComparison.Ordinal)) {
                    return member;
                }
            }
            return null;
        }

        // Cells of a nested table: scalars as values; anything deeper as a short summary.
        private static object? CellValue(InspectNode? node) {
            switch (node) {
                case null:
                    return null;
                case InspectScalar scalar:
                    return Clip(scalar.Value);
                case InspectCollection collection:
                    return SummaryOf(collection);
                default:
                    return node.TypeName;
            }
        }

        private static TableCell CellFor(InspectNode node) {
            switch (node) {
                case InspectScalar scalar:
                    return TableCell.FromValue(Clip(scalar.Value));
                case InspectCycle cycle:
                    return new TableCell("[muted](cycle: " + Markup.Escape(cycle.TargetPath) + ")[/]");
                case InspectLimit limit:
                    return new TableCell("[muted]" + Markup.Escape(limit.Summary) + " (depth limit)[/]");
                case InspectError error:
                    return new TableCell("[warn]error: " + Markup.Escape(error.Message) + "[/]");
                default:
                    return new TableCell(Markup.Escape(node.TypeName));
            }
        }

        private static object? Clip(object? value) {
            if (value is byte[] bytes) {
                return bytes.Length.ToString("N0", CultureInfo.InvariantCulture) + " bytes";
            }
            if (value is string text && text.Length > MaxText) {
                return text.Substring(0, MaxText) + "… (" + text.Length.ToString("N0", CultureInfo.InvariantCulture) + " chars)";
            }
            return value;
        }

        private static string? SummaryOf(InspectNode node) {
            if (node is InspectCollection collection) {
                int count = collection.Count ?? collection.Items.Count;
                string text = Localization.WidgetLabels.Format(count == 1 ? Localization.WidgetLabels.Current.OneItem : Localization.WidgetLabels.Current.ManyItems, count.ToString("N0", CultureInfo.InvariantCulture));
                return collection.Count == null && collection.Omitted != 0 ? text + "+" : text;
            }
            if (node is InspectObject obj) {
                return Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.Members, obj.Members.Count.ToString("N0", CultureInfo.InvariantCulture));
            }
            return null;
        }

        private static string? MoreText(InspectCollection collection) {
            if (collection.Omitted > 0) {
                return Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.MoreCount, collection.Omitted.ToString("N0", CultureInfo.InvariantCulture));
            }
            return collection.Omitted < 0 ? Localization.WidgetLabels.Current.MoreUnknown : null;
        }
    }
}
