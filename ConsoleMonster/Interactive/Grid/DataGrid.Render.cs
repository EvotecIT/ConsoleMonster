using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    public sealed partial class DataGrid {
        private Rect _bodyBounds;
        private List<int> _rowLines = new List<int>();
        private List<int> _rowEnds = new List<int>();
        private int _positionLine = -1;
        internal bool ShowPosition { get; set; }
        internal bool StatusPresenters { get; set; }

        /// <inheritdoc />
        protected override void RenderContent(Region region, RenderContext context) => RenderRows(region, context);

        internal void RenderRows(Region region, RenderContext context) {
            _bodyBounds = region.Bounds;
            if (region.Height <= 0 || region.Width <= 0) { return; }
            int count = Math.Max(1, region.Height - 2);
            bool footer = ShowPosition && (_top > 0 || _visible.Count > count) || _quickSearch.Length > 0;
            if (footer) { count = Math.Max(1, count - 1); }
            _pageRows = Math.Max(1, count - 1);
            if (_cursor < _top) { _top = _cursor; }
            else if (_cursor >= _top + count) { _top = _cursor - count + 1; }
            _top = Math.Max(0, Math.Min(_top, Math.Max(0, _visible.Count - count)));

            // The table negotiates columns over at most 128 sample rows plus this page. Pending sample
            // cells stay placeholders: only visible cells start work. No full-list presentation or layout.
            int sample = _visible.Count <= 128 ? 0 : Math.Min(128, _visible.Count);
            Table table = BuildPage(context, sample, count);
            int start = sample == 0 ? _top : sample;
            CellBuffer buffer = table.RenderWindow(region.Width, start, count, context, out int head, out _rowLines, out string? hidden);
            _rowEnds = new List<int>(_rowLines.Count);
            for (int i = 0; i < _rowLines.Count; i++) { _rowEnds.Add(i + 1 < _rowLines.Count ? _rowLines[i + 1] : buffer.Height); }
            _positionLine = -1;
            region.Buffer.Blit(buffer, 0, 0, Math.Min(region.Width, buffer.Width), Math.Min(region.Height, buffer.Height), region.Bounds.X, region.Bounds.Y);
            if (_visible.Count == 0) {
                region.Write(0, Math.Min(region.Height - 1, head), PromptLabels.For(context.Profile.Language).NoMatches, context.Style(ThemeToken.Muted));
                return;
            }
            int focused = _cursor - _top;
            if (Focused && focused >= 0 && focused < _rowLines.Count && _rowLines[focused] < region.Height) {
                Style focus = context.Style(ThemeToken.Focus);
                int y = _rowLines[focused];
                for (int x = 0; x < region.Width; x++) {
                    Cell cell = region.Get(x, y);
                    region.SetStyle(x, y, context.HasColor ? cell.Style.WithBackgroundColor(focus.BackgroundColor).With(TextDecoration.Bold) : cell.Style.With(TextDecoration.Reverse));
                }
            }
            string? position = footer ? _quickSearch.Length > 0 ? "Search: " + _quickSearch
                : (_top + 1).ToString(Culture) + (context.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? "-" : "–")
                + Math.Min(_top + _rowLines.Count, _visible.Count).ToString(Culture) + " of " + _visible.Count.ToString(Culture) : null;
            string? note = position == null ? hidden : position + (hidden == null ? string.Empty : " · " + hidden);
            if (note != null) {
                _positionLine = region.Height - 1;
                string text = TextWrapper.Truncate(note, region.Width, context.Ellipsis);
                region.Write(Math.Max(0, region.Width - DisplayWidth.Measure(text)), region.Height - 1, text, context.Style(ThemeToken.Muted));
            }
        }

        private Table BuildPage(RenderContext context, int sample, int count) {
            var table = new Table { Border = TableBorder.Minimal, CardsBelow = 0, RecordViewRows = 0, Presentation = _grid.Presentation, StableWidths = false };
            bool marks = _grid.OutputMode == GridSelection.Multiple;
            if (marks) { table.AddColumn(new TableColumn(StatusPresenters ? context.Glyphs.Get(Glyph.Check) : " ") { IsKey = true, Width = 1, Alignment = HorizontalAlignment.Left }); }
            List<int> shown = ShownColumns();
            foreach (int c in shown) {
                TableColumn source = _grid.Columns[c];
                var column = new TableColumn(source.Header) {
                    Key = source.Key, Alignment = source.Alignment,
                    Presenter = source.Presenter ?? (StatusPresenters && _rows.Count > 0 ? CellPresenters.ForStatus(source.Key ?? Markup.Strip(source.Header), Value(_rows[0], c)) : null),
                    Priority = source.Priority, IsKey = source.IsKey, Flexible = source.Flexible, Weight = source.Weight,
                    MinWidth = source.MinWidth, MaxWidth = source.MaxWidth, Width = source.Width,
                    Overflow = ColumnOverflow.Truncate, MaxLines = 1
                };
                foreach (RoleRule rule in source.RoleRules) { column.RoleRules.Add(rule); }
                table.AddColumn(column);
            }
            string check = Markup.Escape(context.Glyphs.Get(Glyph.Check));
            void Add(int visibleIndex, bool resolve) {
                GridRow row = _rows[_visible[visibleIndex]];
                var cells = new List<TableCell>();
                if (marks) { cells.Add(new TableCell(_marked.Contains(row.Id) ? "[accent bold]" + check + "[/]" : string.Empty)); }
                foreach (int c in shown) {
                    object? value = c < row.Values.Count ? row.Values[c] : null;
                    if (value is DataGridCell lazy) {
                        if (resolve) { _cells.Add(lazy); value = lazy.Resolve(_lifetime.Token, _cellChanged); }
                        else { value = lazy.Peek(); }
                    }
                    cells.Add(TableCell.FromValue(value));
                }
                table.AddRow(cells.ToArray());
            }
            if (sample == 0) {
                for (int i = 0; i < _visible.Count; i++) { Add(i, i >= _top && i < _top + count); }
            } else {
                for (int i = 0; i < sample; i++) { Add(i, false); }
                for (int i = _top; i < Math.Min(_visible.Count, _top + count); i++) { Add(i, true); }
            }
            return table;
        }

        /// <inheritdoc />
        protected override bool HandlePointerCore(InputEvent input, RenderContext context) {
            if (input.MouseAction == MouseAction.Up) { _dragAnchor = -1; }
            if (input.Column < _bodyBounds.X || input.Column >= _bodyBounds.Right || input.Row < _bodyBounds.Y || input.Row >= _bodyBounds.Bottom) { return false; }
            if (input.MouseAction == MouseAction.Wheel) { _cursor = Clamp(_cursor - Math.Sign(input.WheelDelta) * 3); return true; }
            int relative = input.Row - _bodyBounds.Y;
            int hit = _rowLines.FindLastIndex(y => y <= relative);
            if (hit < 0 || hit + _top >= _visible.Count || relative >= _rowEnds[hit] || relative == _positionLine) { return false; }
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                _cursor = hit + _top; _dragAnchor = _cursor;
                if ((input.Modifiers & KeyModifiers.Control) != 0 && _grid.OutputMode == GridSelection.Multiple) {
                    object id = _rows[_visible[_cursor]].Id;
                    if (!_marked.Remove(id)) { _marked.Add(id); }
                    _markedSelection = null;
                }
                return true;
            }
            if (input.MouseAction == MouseAction.Move && _dragAnchor >= 0 && (input.HeldButtons & 1) != 0) {
                _cursor = hit + _top;
                if (_grid.OutputMode == GridSelection.Multiple) {
                    for (int i = Math.Min(_dragAnchor, _cursor); i <= Math.Max(_dragAnchor, _cursor); i++) { _marked.Add(_rows[_visible[i]].Id); }
                    _markedSelection = null;
                }
                return true;
            }
            return false;
        }

        internal override void CancelPointer() => _dragAnchor = -1;
        internal bool Pointer(InputEvent input, RenderContext context) => HandleInput(input, context);
    }
}
