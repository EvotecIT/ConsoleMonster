using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Renders widgets for PowerShell's formatting system (ConsoleMonster.format.ps1xml): a widget or a
    /// <see cref="ConsoleRendering"/> that reaches Out-Default draws itself instead of listing its properties.
    /// <para>Colour follows PowerShell: with $PSStyle.OutputRendering Host (the default in 7.2+) the text carries colour, and
    /// PowerShell removes it for Out-String, Out-File and redirection; PlainText and Windows PowerShell 5.1 (no $PSStyle) get
    /// plain text, since 5.1 would keep escape codes in captured output. Format-List * still lists the properties.</para>
    /// </summary>
    public static class WidgetFormatter {
        /// <summary>The text for a widget or a rendering (called by the format view).</summary>
        /// <param name="value">A widget, a <see cref="ConsoleRendering"/>, a PowerShell object around one, or a deserialized rendering.</param>
        /// <param name="host">The host (for the width), or null.</param>
        /// <param name="outputRendering">$PSStyle.OutputRendering as text (Host, Ansi, PlainText), or empty for 5.1.</param>
        /// <returns>The rendered text.</returns>
        public static string Format(object? value, PSHost? host, string? outputRendering) {
            object? raw = value is PSObject wrapped ? wrapped.BaseObject : value;
            switch (raw) {
                case ConsoleRendering rendering:
                    return Render(rendering.Widget, host, rendering.DisplayOnly && string.IsNullOrEmpty(outputRendering) ? "Host" : outputRendering, rendering.Width, rendering.Culture);
                case IWidget widget:
                    return Render(widget, host, outputRendering, 0, null);
                default:
                    // A deserialized rendering carries its plain text as its ToString.
                    return (value is PSObject other ? other.ToString() : Convert.ToString(raw, CultureInfo.CurrentCulture)) ?? string.Empty;
            }
        }

        /// <summary>A widget as text for a host and an output rendering mode.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="host">The host (for the width), or null.</param>
        /// <param name="outputRendering">Host, Ansi or PlainText; empty or null means plain.</param>
        /// <param name="width">An explicit positive width, or zero for the current target/terminal, falling back to 100 cells.</param>
        /// <param name="culture">A culture, or null.</param>
        /// <returns>The text, without a trailing line break.</returns>
        public static string Render(IWidget widget, PSHost? host, string? outputRendering, int width, CultureInfo? culture) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            TerminalProfile profile = Terminal.GetCurrentProfile();
            if (culture != null) {
                profile = profile.WithCulture(culture);
            }
            int columns = Terminal.ResolveWidth(width);
            profile = profile.WithSize(columns, profile.Height);
            if (profile.Mode == OutputMode.Interactive) {
                profile = profile.WithMode(OutputMode.Stream);
            }
            bool colour = profile.ColorDepth != ColorDepth.None
                && (string.Equals(outputRendering, "Ansi", StringComparison.OrdinalIgnoreCase)
                    || (string.Equals(outputRendering, "Host", StringComparison.OrdinalIgnoreCase) && !Console.IsOutputRedirected));
            string text = colour
                ? Terminal.RenderEncoded(widget, profile)
                : Terminal.RenderText(widget, profile.WithColorDepth(ColorDepth.None));
            return text.Replace("\r\n", "\n").TrimEnd('\n');
        }

        // The visible width minus one: a line exactly as wide as the window would wrap on some consoles.
        internal static int HostWidth(PSHost? host) {
            try {
                int width = host?.UI?.RawUI?.WindowSize.Width ?? 0;
                if (width <= 0) {
                    width = host?.UI?.RawUI?.BufferSize.Width ?? 0;
                }
                return width > 1 ? width - 1 : 0;
            } catch (Exception) {
                return 0;
            }
        }
    }
}
