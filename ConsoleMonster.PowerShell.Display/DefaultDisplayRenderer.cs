using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Prepares a complete bounded frame before any external host write.</summary>
internal static class DefaultDisplayRenderer {
    /// <summary>Fully materializes rendering; a builder or widget failure emits no host output.</summary>
    public static string Prepare(IWidget widget, TerminalProfile profile, long maximumBytes = 16 * 1024 * 1024) {
        if (widget == null) { throw new ArgumentNullException(nameof(widget)); }
        if (profile == null) { throw new ArgumentNullException(nameof(profile)); }
        if (maximumBytes < 1 || maximumBytes > 16 * 1024 * 1024) { throw new ArgumentOutOfRangeException(nameof(maximumBytes)); }
        string encoded = Terminal.RenderEncoded(Widgets.ConsoleViewStyle.Wrap(widget), profile);
        if ((long)encoded.Length * 2 > maximumBytes) { throw new InvalidOperationException("The rendered view exceeds its output budget."); }
        return encoded;
    }
}
