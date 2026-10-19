using System;
using System.Globalization;
using System.Management.Automation.Host;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// What Format-ConsoleTable, Format-ConsoleList, Format-ConsoleTree, Format-ConsoleJson, Format-ConsoleCode and
    /// Format-ConsoleError write to the pipeline in PowerShell 7.2 and later: the rendered view, drawn in colour when it
    /// reaches the console (through ConsoleMonster's format view) and as plain text when it is captured (Out-String,
    /// Out-File, <c>&gt;</c>, a variable shown later), as PowerShell's $PSStyle.OutputRendering decides.
    /// </summary>
    public sealed class ConsoleRendering {
        /// <summary>Creates a rendering.</summary>
        /// <param name="widget">The view.</param>
        /// <param name="width">The width it was asked for (-Width), or 0 for the console's.</param>
        /// <param name="culture">The culture it was asked for (-Culture), or null.</param>
        public ConsoleRendering(IWidget widget, int width = 0, CultureInfo? culture = null) {
            Widget = widget ?? throw new ArgumentNullException(nameof(widget));
            Width = Math.Max(0, width);
            Culture = culture;
        }

        /// <summary>The view.</summary>
        public IWidget Widget { get; }

        /// <summary>The width it was asked for (-Width), or 0 for the console's.</summary>
        public int Width { get; }

        /// <summary>The culture it was asked for (-Culture), or null for the current one.</summary>
        public CultureInfo? Culture { get; }

        /// <summary>The view as plain text (no colour), as wide as asked for or the console.</summary>
        /// <param name="host">The host whose width to use when no width was asked for, or null.</param>
        /// <returns>The text.</returns>
        public string ToText(PSHost? host = null) => WidgetFormatter.Render(Widget, host, "PlainText", Width, Culture);

        /// <inheritdoc />
        public override string ToString() => ToText();
    }
}
