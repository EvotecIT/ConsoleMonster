using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>A composition of <see cref="Table"/> with a dedicated marker column and whole-row roles.</summary>
    public sealed class RoleTable : IWidget, IContainer {
        /// <summary>Creates a table, prepending a marker column without changing the supplied column definitions.</summary>
        /// <param name="columns">The data columns; the first is the key unless another column explicitly is.</param>
        public RoleTable(IEnumerable<TableColumn> columns) {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            Table = new Table();
            Table.AddColumn(new TableColumn(string.Empty) { Width = 2, Priority = int.MaxValue });
            List<TableColumn> supplied = new List<TableColumn>(columns);
            bool hasKey = supplied.Exists(column => column != null && column.IsKey);
            for (int i = 0; i < supplied.Count; i++) {
                TableColumn source = supplied[i] ?? throw new ArgumentException("Columns cannot be null.", nameof(columns));
                TableColumn copy = CopyColumn(source);
                if (!hasKey && i == 0) copy.IsKey = true;
                Table.AddColumn(copy);
            }
        }

        /// <summary>The composed table, for ordinary border, header, card, grouping and summary options.</summary>
        public Table Table { get; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new IWidget[] { Table };

        /// <summary>Adds data cells after the marker column, retaining their raw values and original instances.</summary>
        /// <param name="role">The row role, or null for an ordinary unmarked row.</param>
        /// <param name="cells">The data cells, excluding the marker column.</param>
        /// <returns>This widget.</returns>
        public RoleTable AddRow(TableRowRole? role, params TableCell[] cells) {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            TableCell[] row = new TableCell[cells.Length + 1];
            row[0] = TableCell.FromValue(new MarkerWidget(role));
            Array.Copy(cells, 0, row, 1, cells.Length);
            if (role == null) Table.AddRow(row); else Table.AddRow(role, row);
            return this;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Table.Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Table.Render(region, context);

        internal static TableColumn CopyColumn(TableColumn source) {
            TableColumn copy = new TableColumn(source.Header) {
                Key = source.Key, Alignment = source.Alignment, Overflow = source.Overflow, MaxLines = source.MaxLines,
                Priority = source.Priority, IsKey = source.IsKey, Flexible = source.Flexible, MinWidth = source.MinWidth,
                MaxWidth = source.MaxWidth, Width = source.Width, Presenter = source.Presenter, Aggregate = source.Aggregate
            };
            copy.HeaderAlignment = source.HeaderAlignment; copy.Gap = source.Gap; copy.ShortHeader = source.ShortHeader;
            copy.Placement = source.Placement; copy.BelowWidth = source.BelowWidth; copy.DimRepeats = source.DimRepeats;
            copy.UnknownText = source.UnknownText; copy.NotApplicableText = source.NotApplicableText;
            foreach (var rule in source.RoleRules) copy.RoleRules.Add(rule);
            return copy;
        }

        private sealed class MarkerWidget : IWidget {
            private readonly TableRowRole? _role;
            internal MarkerWidget(TableRowRole? role) { _role = role; }
            private string Text(RenderContext context) => (context.Glyphs.Tier == GlyphTier.Ascii ? _role?.AsciiMarker ?? _role?.Marker : _role?.Marker) ?? string.Empty;
            public Size Measure(Constraints constraints, RenderContext context) => new Size(Math.Min(DisplayWidth.Measure(Text(context)), constraints.MaxWidth), Math.Min(1, constraints.MaxHeight));
            public void Render(Region region, RenderContext context) => region.Write(0, 0, Text(context), _role?.MarkerTone is Styling.ThemeToken tone ? context.Style(tone) : _role?.Style(context) ?? Styling.Style.Plain);
        }
    }
}
