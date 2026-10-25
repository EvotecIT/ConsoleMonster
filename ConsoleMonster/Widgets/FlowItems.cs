using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A labelled hop in a linear flow. Labels are literal text.</summary>
    /// <param name="Key">Stable key for live updates.</param>
    /// <param name="Label">Visible label.</param>
    /// <param name="Status">Optional status, shown by glyph and colour.</param>
    public sealed record FlowNode(string Key, string Label, StatusKind? Status = null);

    /// <summary>A directed connection between adjacent flow nodes.</summary>
    /// <param name="Key">Stable key for live updates.</param>
    /// <param name="From">Source node key.</param>
    /// <param name="To">Next node key.</param>
    /// <param name="Label">Optional literal edge label.</param>
    /// <param name="Status">Optional status.</param>
    /// <param name="Cut">Whether the connection is severed.</param>
    public sealed record FlowEdge(string Key, string From, string To, string? Label = null, StatusKind? Status = null, bool Cut = false);

    /// <summary>The flow's single side branch; a null destination ends below its source.</summary>
    /// <param name="From">Source node key.</param>
    /// <param name="Label">Literal reason or alternative path label.</param>
    /// <param name="To">Optional later node to return to.</param>
    /// <param name="Status">Optional status.</param>
    public sealed record FlowBranch(string From, string Label, string? To = null, StatusKind? Status = null);
}
