using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Builds rich text from the shared text parameters of New-ConsoleText, Write-ConsoleText and Write-ConsoleStatus:
    /// Write-Color style pieces and colour arrays, markup, decorations, a link, a theme role and a gradient.
    /// </summary>
    internal static class PSRichText {
        /// <summary>Parses colour arguments: ConsoleColor values or names (PowerShell mapping), theme roles, #RRGGBB, rgb(r,g,b), r,g,b.</summary>
        internal static RichColor[] Colors(object[]? values, string parameter) {
            if (values == null || values.Length == 0) {
                return Array.Empty<RichColor>();
            }
            List<RichColor> colors = new List<RichColor>(values.Length);
            foreach (object? raw in values) {
                object? value = PSText.Unwrap(raw);
                switch (value) {
                    case null:
                        colors.Add(RichColor.Default);
                        break;
                    case RichColor rich:
                        colors.Add(rich);
                        break;
                    case ConsoleColor console:
                        colors.Add(RichColor.FromConsoleColor(console));
                        break;
                    case ThemeToken role:
                        colors.Add(RichColor.FromRole(role));
                        break;
                    case Color color:
                        colors.Add(RichColor.From(color));
                        break;
                    default:
                        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                        if (!RichColor.TryParse(text, out RichColor parsed)) {
                            throw new PSArgumentException("-" + parameter + ": '" + text + "' is not a colour. Use a ConsoleColor name (Red, DarkCyan...), a theme role (Pass, Fail, Warn, Info, Muted, Accent), #RRGGBB, rgb(r,g,b) or r,g,b.");
                        }
                        colors.Add(parsed);
                        break;
                }
            }
            return colors.ToArray();
        }

        /// <summary>Parses decoration names (Bold, Dim, Italic, Underline, Strikethrough, Invert).</summary>
        internal static TextDecoration Decorations(string[]? styles) {
            TextDecoration result = TextDecoration.None;
            foreach (string style in styles ?? Array.Empty<string>()) {
                if (!RichStyle.TryParseDecoration(style, out TextDecoration decoration)) {
                    throw new PSArgumentException("-Style: '" + style + "' is not a style. Use Bold, Dim, Italic, Underline, Strikethrough or Invert.");
                }
                result |= decoration;
            }
            return result;
        }

        /// <summary>
        /// Builds rich text. Pieces are literal text (or rich text, whose own colours win); piece i gets colour i and
        /// background i, and pieces beyond the colours get the first ones, as Write-Color does. Markup follows the pieces.
        /// A gradient (two colours) recolours the whole text.
        /// </summary>
        internal static RichText Build(object[]? text, object[]? color, object[]? background, string? markup, string[]? style, string? link, ThemeToken? role, object[]? gradient) {
            RichColor[] foregrounds = Colors(color, "Color");
            RichColor[] backgrounds = Colors(background, "BackgroundColor");
            TextDecoration decoration = Decorations(style);
            RichColor roleColor = role.HasValue ? RichColor.FromRole(role.Value) : RichColor.Default;
            List<RichText> parts = new List<RichText>();
            List<object?> pieces = new List<object?>();
            foreach (object? piece in text ?? Array.Empty<object>()) {
                pieces.Add(PSText.Unwrap(piece));
            }
            parts.Add(RichText.FromPieces(pieces, foregrounds, backgrounds, new RichStyle(roleColor, RichColor.Default, decoration, link)));
            if (!string.IsNullOrEmpty(markup)) {
                try {
                    parts.Add(RichText.FromMarkup(markup, new RichStyle(roleColor, RichColor.Default, decoration, link)));
                } catch (ArgumentException ex) {
                    throw new PSArgumentException("-Markup: " + ex.Message);
                }
            }
            RichText result = RichText.Concat(parts.ToArray());
            if (gradient != null && gradient.Length > 0) {
                RichColor[] stops = Colors(gradient, "Gradient");
                if (stops.Length != 2) {
                    throw new PSArgumentException("-Gradient takes two colours: the first and the last.");
                }
                RichStyle gradientStyle = new RichStyle(RichColor.Default, backgrounds.Length > 0 ? backgrounds[0] : RichColor.Default, decoration, link);
                result = RichText.Gradient(result.PlainText, stops[0], stops[1], gradientStyle, Terminal.Theme);
            }
            return result;
        }

    }
}
