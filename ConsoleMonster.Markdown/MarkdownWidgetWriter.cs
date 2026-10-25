using ConsoleMonster.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleMonster.Layout;
using ConsoleMonster.Canvas;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Markdown {
    /// <summary>Serializes widget data using its existing presenters, without interpreting rendered box characters.</summary>
    internal static class MarkdownWidgetWriter {
        internal static string Write(IWidget widget, ExportOptions options) {
            if (widget == null) { throw new ArgumentNullException(nameof(widget)); }
            RenderContext context = ConsoleExporter.CreateContext(options, options.DarkTheme ?? Theme.Dark, options.Height > 0 ? options.Height : 24);
            StringBuilder output = new StringBuilder();
            Append(output, widget, context, options);
            return output.ToString();
        }

        private static void Append(StringBuilder output, IWidget widget, RenderContext context, ExportOptions options) {
            if (widget is CultureScope scope) {
                Append(output, scope.Child, new RenderContext(context.Profile.WithCulture(scope.Culture), context.Theme, context.Glyphs, context.Tick, context.Elapsed), options);
            } else if (widget is Table table) {
                Table(output, table, context);
            } else if (widget is RecordView record) {
                Record(output, record, context, options);
            } else if (widget is Tree tree) {
                var presenter = tree.Presentation.CreateContext(context);
                if (tree.ShowRoot) { Node(output, tree.Root, 0, presenter); }
                else { foreach (TreeNode node in tree.Root.Children) { Node(output, node, 0, presenter); } }
                output.Append('\n');
            } else if (widget is TextBlock text) {
                output.Append(Prose(text.IsMarkup ? Plain(text.Content, context) : text.Content)).Append("\n\n");
            } else if (widget is IContainer container && Transparent(widget)) {
                foreach (IWidget child in container.Children) { Append(output, child, context, options); }
            } else {
                ExportOptions effective = options.Clone();
                effective.Culture = context.Profile.Culture;
                output.Append(MarkdownFrameWriter.Write(ConsoleExporter.ToText(widget, effective))).Append('\n');
            }
        }

        // Only layout-only wrappers are transparent. Content-bearing or third-party containers retain their full frame.
        private static bool Transparent(IWidget widget) => widget is Rows || widget is Columns || widget is Grid || widget is Stack
            || widget is Align || widget is Padding || widget is Sized || widget is Prioritized || widget is RegionWidget;

        private static void Table(StringBuilder output, Table table, RenderContext context) {
            TableLayout layout = table.SemanticLayout(context);
            if (table.Columns.Count == 0) { return; }
            Heading(output, Plain(table.Title, context));
            string[] headers = table.Columns.Select(c => Escape(Plain(c.Header, context))).ToArray();
            string[] alignment = layout.Alignments.Select(a => a == HorizontalAlignment.Right ? "---:" : a == HorizontalAlignment.Center ? ":---:" : ":---").ToArray();
            void Header() { Row(output, headers, false); Row(output, alignment, false); }
            bool started = false;
            foreach (TableLayoutRow row in layout.Rows) {
                if (row.Kind == TableRowKind.Group) {
                    if (started) { output.Append('\n'); }
                    Heading(output, row.Spanning?.PlainText ?? string.Empty);
                    Header();
                    started = true;
                } else if (row.Cells != null) {
                    if (!started) { Header(); started = true; }
                    Row(output, row.Cells.Select((cell, c) => Cell(cell, row.Source >= 0 ? table.Rows[row.Source][c] : null, table.Columns[c].Presenter)).Select(Escape), row.Kind == TableRowKind.Subtotal || row.Kind == TableRowKind.Total);
                }
            }
            if (!started) { Header(); }
            if (layout.Total?.Cells != null) {
                if (layout.Rows.Any(row => row.Kind == TableRowKind.Group)) {
                    output.Append('\n');
                    Heading(output, table.TotalLabel ?? context.Labels.Total);
                    Header();
                }
                Row(output, layout.Total.Cells.Select(cell => Cell(cell)).Select(Escape), true);
            }
            output.Append('\n');
            if (!string.IsNullOrEmpty(table.Caption)) { output.Append(Prose(Plain(table.Caption, context))).Append("\n\n"); }
        }

        private static void Record(StringBuilder output, RecordView record, RenderContext context, ExportOptions options) {
            Heading(output, Plain(record.Title, context));
            if (!string.IsNullOrEmpty(record.Summary)) { output.Append(Prose(record.Summary!)).Append("\n\n"); }
            bool started = false;
            PresenterContext presenter = record.Presentation.CreateContext(context);
            foreach (RecordItem item in record.Items) {
                if (item.Kind == RecordItemKind.Heading) {
                    if (started) { output.Append('\n'); started = false; }
                    Heading(output, Plain(item.Text, context) + (string.IsNullOrEmpty(item.Note) ? string.Empty : " · " + item.Note));
                } else if (item.Kind == RecordItemKind.Widget && item.Widget != null) {
                    if (started) { output.Append('\n'); started = false; }
                    Append(output, item.Widget, context, options);
                } else if (item.Cell != null) {
                    CellContent value = CellRendering.Content(item.Cell, item.Presenter, item.Rules, presenter);
                    if (record.HideEmpty && value.IsEmpty) { continue; }
                    if (!started) { output.Append("| Property | Value |\n| :--- | :--- |\n"); started = true; }
                    Row(output, new[] { Escape(item.Text), Escape(Cell(value, item.Cell, item.Presenter)) }, false);
                }
            }
            output.Append('\n');
        }

        private static void Node(StringBuilder output, TreeNode node, int depth, PresenterContext presenter) {
            string label = node.IsScalarCollectionItem ? string.Empty : Plain(node.LabelFor(presenter.Render), presenter.Render);
            if (node.Status.HasValue) { label = presenter.Render.Glyphs.StatusMarker(node.Status.Value) + " " + label; }
            if (node.Value != null) { label += (label.Length > 0 ? ": " : string.Empty) + Cell(CellRendering.Content(node.Value, node.Presenter, null, presenter), node.Value, node.Presenter); }
            string? note = node.NoteFor(presenter.Render);
            if (!string.IsNullOrEmpty(note)) { label += " · " + note; }
            output.Append(' ', depth * 2).Append("- ").Append(Prose(label)).Append('\n');
            foreach (TreeNode child in node.Children) { Node(output, child, depth + 1, presenter); }
        }

        private static string Cell(CellContent cell, TableCell? source = null, ICellPresenter? presenter = null) =>
            source?.HasValue == true && source.Value is string raw && (presenter == null || presenter is TextPresenter || presenter is AutoPresenter)
                ? raw : string.Join("\n", cell.Lines.Select(line => line.PlainText));
        private static string Plain(string? markup, RenderContext context) => markup == null ? string.Empty : string.Join("\n", context.Markup(markup).Lines.Select(line => line.PlainText));
        private static void Heading(StringBuilder output, string text) { if (text.Length > 0) { output.Append("### ").Append(Prose(text)).Append("\n\n"); } }
        private static void Row(StringBuilder output, IEnumerable<string> cells, bool bold) => output.Append("| ").Append(string.Join(" | ", cells.Select(cell => bold && cell.Length > 0 ? "**" + cell + "**" : cell))).Append(" |\n");
        // Encode user HTML before adding our own line-break markup. Escape backslashes first for literal \| sequences.
        private static string Escape(string value) => Rendering.TextSanitizer.Clean(value, preserveLineBreaks: true).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\\", "\\\\").Replace("|", "\\|").Replace("*", "\\*").Replace("_", "\\_").Replace("`", "\\`")
            .Replace("[", "\\[").Replace("]", "\\]").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br>");
        private static string Prose(string value) {
            string text = Escape(value).Replace("#", "\\#").Replace("~", "\\~");
            int start = 0;
            while (start < text.Length && char.IsWhiteSpace(text[start])) { start++; }
            if (start < text.Length && "-+=".IndexOf(text[start]) >= 0) { return text.Insert(start, "\\"); }
            int end = start;
            while (end < text.Length && char.IsDigit(text[end])) { end++; }
            if (end > start && end < text.Length && (text[end] == '.' || text[end] == ')')) { return text.Insert(end, "\\"); }
            return text;
        }
    }
}
