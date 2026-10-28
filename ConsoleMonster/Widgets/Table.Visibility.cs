using System;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Widgets;

public partial class Table {
    /// <summary>Whether a named column is displayed at the destination width, including below-row detail or record layouts.</summary>
    /// <param name="name">The column key or unstyled header.</param><param name="width">Available cells.</param>
    /// <param name="context">The destination render context.</param><returns>False when the column is absent or hidden.</returns>
    public bool DisplaysColumn(string name, int width, RenderContext context) {
        Prepare();
        int index = -1;
        for (int i = 0; i < _columns.Count; i++) if (string.Equals(_columns[i].Id, name, StringComparison.Ordinal)) { index = i; break; }
        if (index < 0) return false;
        TableLayout layout = ComputeLayout(Math.Max(1, width), context);
        return layout.Record != null || layout.Bands != null || layout.Mode != TableMode.Grid
            || Array.IndexOf(layout.Visible, index) >= 0 || layout.Below.Contains(index);
    }
}
