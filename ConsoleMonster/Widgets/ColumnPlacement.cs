namespace ConsoleMonster.Widgets;

/// <summary>Where a column's evidence appears when the grid is narrow.</summary>
public enum ColumnPlacement {
    /// <summary>Keep ordinary grid allocation and hidden-column reporting.</summary>
    Inline,
    /// <summary>Move evidence below its own row when narrow or hidden, rather than dropping it.</summary>
    Below
}
