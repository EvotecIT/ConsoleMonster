namespace ConsoleMonster.Views;

/// <summary>How much of a dashboard frame prints. PowerShell reads it from <c>$ConsoleViewLengthPreference</c>.</summary>
public enum ConsoleViewLength {
    /// <summary>Title, meters, attention (capped to the first screen) and the overview sections; no detail sections.</summary>
    Summary,
    /// <summary>The summary, then every detail section up to its row cap. The default.</summary>
    Standard,
    /// <summary>Everything, without caps.</summary>
    Full
}
