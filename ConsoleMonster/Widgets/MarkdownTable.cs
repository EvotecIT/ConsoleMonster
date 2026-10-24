using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Builds native tables after resolving Markdown links for the target terminal.</summary>
    internal sealed class MarkdownTable : IWidget {
        private readonly RichText[] _headers;
        private readonly RichText[][] _rows;
        private readonly HorizontalAlignment?[]? _alignments;

        internal MarkdownTable(IReadOnlyList<RichText> headers, IEnumerable<IReadOnlyList<RichText>> rows,
            IReadOnlyList<HorizontalAlignment?>? alignments) {
            if (headers == null) {
                throw new ArgumentNullException(nameof(headers));
            }
            if (rows == null) {
                throw new ArgumentNullException(nameof(rows));
            }
            if (headers.Count == 0 || headers.Any(h => h == null)) {
                throw new ArgumentException("A table needs at least one non-null header cell.", nameof(headers));
            }
            _headers = headers.ToArray();
            _rows = rows.Select(row => row == null || row.Count != headers.Count || row.Any(c => c == null)
                ? throw new ArgumentException("Each row must match the header cell count and contain non-null cells.", nameof(rows))
                : row.ToArray()).ToArray();
            if (alignments != null && alignments.Count != headers.Count) {
                throw new ArgumentException("Alignments must match the header cell count.", nameof(alignments));
            }
            _alignments = alignments?.ToArray();
        }

        private Table Build(RenderContext context) {
            Table table = new Table { WrapLines = int.MaxValue };
            for (int c = 0; c < _headers.Length; c++) {
                table.AddColumn(new TableColumn(MarkdownText.Resolve(_headers[c], context)) {
                    Alignment = _alignments?[c],
                    MaxLines = TableColumn.Unlimited
                });
            }
            foreach (RichText[] row in _rows) {
                table.AddRow(row.Select(cell => new TableCell(MarkdownText.Resolve(cell, context).ToMarkup())).ToArray());
            }
            return table;
        }

        public Size Measure(Constraints constraints, RenderContext context) => Build(context).Measure(constraints, context);

        public void Render(Region region, RenderContext context) => Build(context).Render(region, context);
    }
}
