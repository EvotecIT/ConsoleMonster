using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The content of one cell after presentation: its text lines and, for value cells, the presented value (which may carry
    /// a data bar, sparkline or progress graphic). Shared by tables, record cards and record views.
    /// </summary>
    internal sealed class CellContent {
        internal CellContent(List<StyledLine> lines, PresentedValue? presented, bool atomic = false, IWidget? widget = null) {
            Lines = lines;
            Presented = presented;
            Atomic = atomic;
            Widget = widget;
        }

        /// <summary>A short value that never breaks (a boolean, date, enum, duration, version or status word).</summary>
        internal bool Atomic { get; }

        internal List<StyledLine> Lines { get; }

        internal PresentedValue? Presented { get; }

        internal bool HasVisual => Presented != null && Presented.Visual != CellVisualKind.None;

        internal bool IsEmpty {
            get {
                if (Presented != null) {
                    return Presented.IsEmpty;
                }
                foreach (StyledLine line in Lines) {
                    if (line.Width > 0) {
                        return false;
                    }
                }
                return true;
            }
        }

        internal IWidget? Widget { get; }

        internal List<StyledLine> LinesAtWidth(int width, RenderContext context) =>
            Widget == null ? Lines : WidgetCellLayout.Lines(Widget, width, context);

        internal HorizontalAlignment? Alignment => Presented?.Alignment;

        internal int TextWidth {
            get {
                int width = 0;
                foreach (StyledLine line in Lines) {
                    width = Math.Max(width, line.Width);
                }
                return width;
            }
        }
    }

    /// <summary>Turns cells into content and draws visuals; one implementation for every widget that shows cells.</summary>
    internal static class CellRendering {
        /// <summary>Presents a cell: raw values through the column presenter and tone rules, markup as written, status cells with marker.</summary>
        internal static CellContent Content(TableCell cell, ICellPresenter? presenter, IReadOnlyList<RoleRule>? rules, PresenterContext context) {
            if (cell.HasValue && WidgetCellLayout.GetWidget(cell.Value) is IWidget widget) {
