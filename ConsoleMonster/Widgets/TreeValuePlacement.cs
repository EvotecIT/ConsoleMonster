namespace ConsoleMonster.Widgets;

/// <summary>Where aligned tree value columns begin.</summary>
public enum TreeValuePlacement {
    /// <summary>Keep the original layout that uses the available right edge.</summary>
    Right,
    /// <summary>Start values two cells after the widest bounded label that has values.</summary>
    AfterLabel
}
