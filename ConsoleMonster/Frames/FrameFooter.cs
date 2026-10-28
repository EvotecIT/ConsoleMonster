using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Frames;

/// <summary>How the footer tells the reader to see the full objects.</summary>
public enum FrameHint {
    /// <summary><c>pipe to Format-Table for every row</c>, for a batch of rows.</summary>
    EveryRow,
    /// <summary><c>pipe to Format-List for every property</c>, for one summary object.</summary>
    EveryProperty,
    /// <summary>No hint, for frames that do not come from pipeline objects.</summary>
    None
}

/// <summary>The legend: only the symbols the frame used, in an aligned grid.</summary>
public sealed class FrameLegend {
    private readonly List<string> _entries = new List<string>();
    private readonly List<(HealthState State, string Word)> _states = new List<(HealthState, string)>();
    /// <summary>Adds a state entry, such as <c>× failing</c>.</summary>
    /// <param name="state">The state.</param><param name="word">The literal word.</param>
    public void Add(HealthState state, string word) => _states.Add((state, word ?? string.Empty));
    /// <summary>Adds an entry for a symbol that is not a state, such as <c>· no partnership</c>.</summary>
    /// <param name="symbol">The literal symbol.</param><param name="word">The literal word.</param>
    public void Add(string symbol, string word) => _entries.Add("[subtle]" + Markup.Escape(symbol ?? string.Empty) + "[/] [muted]" + Markup.Escape(word ?? string.Empty) + "[/]");
    /// <summary>Whether the legend has entries.</summary>
    public bool IsEmpty => _entries.Count == 0 && _states.Count == 0;

    internal Legend Build(RenderContext context) {
        List<string> entries = new List<string>();
        foreach ((HealthState state, string word) in _states) entries.Add("[" + FrameText.Tag(state) + "]" + FrameText.Glyph(state, context) + "[/] [muted]" + Markup.Escape(word) + "[/]");
        entries.AddRange(_entries);
        return new Legend(entries.ToArray());
    }
}

/// <summary>The last line: what was counted, then how to see every row.</summary>
public sealed class FrameFooter {
    /// <summary>Literal composition counts (<c>56 naming-context links</c>). Empty entries are skipped.</summary>
    public IList<string> Counts { get; } = new List<string>();
    /// <summary>The hint; every row by default.</summary>
    public FrameHint Hint { get; set; } = FrameHint.EveryRow;

    internal FactFlow Build() {
        List<string> parts = new List<string>();
        foreach (string count in Counts) { if (!string.IsNullOrWhiteSpace(count)) parts.Add(Markup.Escape(count)); }
        if (Hint == FrameHint.EveryRow) parts.Add("pipe to Format-Table for every row");
        else if (Hint == FrameHint.EveryProperty) parts.Add("pipe to Format-List for every property");
        return new FactFlow(parts) { Role = Styling.ThemeToken.Muted };
    }
}
