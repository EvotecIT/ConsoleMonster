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
            object[] pieces = text ?? Array.Empty<object>();
            for (int i = 0; i < pieces.Length; i++) {
                RichStyle pieceStyle = new RichStyle(Pick(foregrounds, i, roleColor), Pick(backgrounds, i, RichColor.Default), decoration, link);
                object? piece = PSText.Unwrap(pieces[i]);
                if (piece is RichText rich) {
                    parts.Add(rich.WithBaseStyle(pieceStyle));
                } else {
                    parts.Add(RichText.Plain(Convert.ToString(piece, CultureInfo.InvariantCulture) ?? string.Empty, pieceStyle));
                }
            }
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
                RichStyle gradientStyle = new RichStyle(RichColor.Default, Pick(backgrounds, 0, RichColor.Default), decoration, link);
                result = RichText.Gradient(result.PlainText, stops[0], stops[1], gradientStyle, Terminal.Theme);
            }
            return result;
        }

        /// <summary>
        /// Splits rich text at a separator, keeping each piece's styles (Write-ConsoleText -SplitLines). Markup spans are
        /// split on their visible text only when they contain no tags.
        /// </summary>
        internal static List<RichText> Split(RichText text, string separator) {
            List<RichText> result = new List<RichText>();
            List<RichSegment> current = new List<RichSegment>();
            foreach (RichSegment segment in text.Segments) {
                bool splittable = !segment.IsMarkup || segment.Text.IndexOf('[') < 0;
                if (!splittable || segment.Text.IndexOf(separator, StringComparison.Ordinal) < 0) {
                    current.Add(segment);
                    continue;
                }
                string[] pieces = segment.Text.Split(new[] { separator }, StringSplitOptions.None);
                for (int i = 0; i < pieces.Length; i++) {
                    if (i > 0) {
                        result.Add(new RichText(current));
                        current = new List<RichSegment>();
                    }
                    if (pieces[i].Length > 0) {
                        current.Add(new RichSegment(pieces[i], segment.Style, segment.IsMarkup));
                    }
                }
            }
            result.Add(new RichText(current));
            return result;
        }

        private static RichColor Pick(RichColor[] colors, int index, RichColor fallback) {
            if (colors.Length == 0) {
                return fallback;
            }
            return index < colors.Length ? colors[index] : colors[0];
        }
    }
}
