using System;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>Turns Write-Host text with console colours into markup that keeps those colours.</summary>
    internal static class HostColors {
        public static string Markup(string? text, ConsoleColor? foreground, ConsoleColor? background) {
            RichColor[]? fg = foreground.HasValue ? new[] { RichColor.FromConsoleColor(foreground.Value) } : null;
            RichColor[]? bg = background.HasValue ? new[] { RichColor.FromConsoleColor(background.Value) } : null;
            return RichText.FromPieces(new[] { text ?? string.Empty }, fg, bg).ToMarkup();
        }
    }
}
