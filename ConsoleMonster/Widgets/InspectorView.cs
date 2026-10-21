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
        private Localization.WidgetLabels? _labels;

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
        public Size Measure(Constraints constraints, RenderContext context) => View(context).Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => View(context).Render(region, context);

        private RecordView View(RenderContext context) {
            if (_view == null || !ReferenceEquals(_labels, context.Labels)) {
                _labels = context.Labels;
                _view = Build(context.Labels);
            }
            return _view;
        }

        private RecordView Build(Localization.WidgetLabels labels) {
            RecordView view = ShowHeader ? new RecordView(Markup.Escape(Title ?? Root.TypeName)) { Presentation = Presentation, Summary = SummaryOf(Root, labels) } : new RecordView { Presentation = Presentation };
            switch (Root) {
                case InspectObject obj:
                    AddMembers(view, obj.Members, 0, labels);
                    break;
                case InspectCollection collection:
                    AddCollectionBody(view, collection, 0, labels);
                    break;
                default:
                    view.AddFieldAt(0, "Value", CellFor(Root));
                    break;
            }
            return view;
        }

        private void AddMembers(RecordView view, IReadOnlyList<InspectNode> members, int level, Localization.WidgetLabels labels) {
            foreach (InspectNode member in members) {
                string name = member.Name ?? string.Empty;
                switch (member) {
                    case InspectObject obj:
                        view.AddHeading(Markup.Escape(name), level, null, obj.TypeName);
                        AddMembers(view, obj.Members, level + 1, labels);
                        break;
                    case InspectCollection collection:
                        view.AddHeading(Markup.Escape(name), level, null, SummaryOf(collection, labels));
                        AddCollectionBody(view, collection, level + 1, labels);
                        break;
                    default:
                        view.AddFieldAt(level, name, CellFor(member));
                        break;
                }
            }
        }

        private void AddCollectionBody(RecordView view, InspectCollection collection, int level, Localization.WidgetLabels labels) {
            if (collection.Columns != null && collection.Columns.Count > 0) {
                view.AddWidgetAt(level, TableOf(collection, labels));
            } else {
                int index = 0;
                foreach (InspectNode item in collection.Items) {
                    string label = "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                    if (item is InspectObject obj) {
                        view.AddHeading(Markup.Escape(label), level, null, obj.TypeName);
                        AddMembers(view, obj.Members, level + 1, labels);
                    } else if (item is InspectCollection inner) {
                        view.AddHeading(Markup.Escape(label), level, null, SummaryOf(inner, labels));
                        AddCollectionBody(view, inner, level + 1, labels);
                    } else {
                        view.AddFieldAt(level, label, CellFor(item));
                    }
                    index++;
                }
            }
            string? more = MoreText(collection, labels);
            if (more != null) {
                view.AddFieldAt(level, string.Empty, new TableCell("[muted]" + Markup.Escape(more) + "[/]"));
            }
        }

        private Table TableOf(InspectCollection collection, Localization.WidgetLabels labels) {
            Table table = new Table { Border = TableBorder.Minimal, RecordViewRows = 0, Presentation = Presentation };
            foreach (string column in collection.Columns!) {
                table.AddColumn(new TableColumn(Markup.Escape(column)) { Key = column });
            }
            foreach (InspectNode item in collection.Items) {
                object?[] values = new object?[collection.Columns.Count];
                if (item is InspectObject obj) {
                    for (int c = 0; c < collection.Columns.Count; c++) {
                        values[c] = CellValue(Find(obj, collection.Columns[c]), labels);
                    }
                }
                table.AddValues(values);
            }
            if (collection.HiddenColumns > 0) {
                table.Caption = Localization.WidgetLabels.Format(labels.MoreColumns, collection.HiddenColumns.ToString(CultureInfo.InvariantCulture));
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
        private static object? CellValue(InspectNode? node, Localization.WidgetLabels labels) {
            switch (node) {
                case null:
                    return null;
                case InspectScalar scalar:
                    return Clip(scalar.Value);
                case InspectCollection collection:
                    return SummaryOf(collection, labels);
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

        private static string? SummaryOf(InspectNode node, Localization.WidgetLabels labels) {
            if (node is InspectCollection collection) {
                int count = collection.Count ?? collection.Items.Count;
                string text = Localization.WidgetLabels.Format(count == 1 ? labels.OneItem : labels.ManyItems, count.ToString("N0", CultureInfo.InvariantCulture));
                return collection.Count == null && collection.Omitted != 0 ? text + "+" : text;
            }
            if (node is InspectObject obj) {
                return Localization.WidgetLabels.Format(labels.Members, obj.Members.Count.ToString("N0", CultureInfo.InvariantCulture));
            }
            return null;
        }

        private static string? MoreText(InspectCollection collection, Localization.WidgetLabels labels) {
            if (collection.Omitted > 0) {
                return Localization.WidgetLabels.Format(labels.MoreCount, collection.Omitted.ToString("N0", CultureInfo.InvariantCulture));
            }
            return collection.Omitted < 0 ? labels.MoreUnknown : null;
        }
    }
}
