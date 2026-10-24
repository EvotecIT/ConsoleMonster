using System;
using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    // Colour presence must be retained: an explicit default overrides an inherited colour.
    internal readonly struct MarkupStyle {
        private readonly bool _foreground;
        private readonly bool _background;
        internal MarkupStyle(Style style, bool foreground, bool background, bool linkFromText = false) {
            Style = style;
            _foreground = foreground;
            _background = background;
            LinkFromText = linkFromText;
        }

        internal Style Style { get; }

        // A bare [link] tag: the text it encloses is the link target.
        internal bool LinkFromText { get; }

        internal Style Apply(Style parent) => new Style(
            _foreground ? Style.Color : parent.Color,
            _background ? Style.BackgroundColor : parent.BackgroundColor,
            parent.Decoration | Style.Decoration,
            Style.Link ?? parent.Link);

        internal static bool TryParse(string? specification, ITheme? theme, ColorDepth depth, out MarkupStyle result) {
            result = default;
            if (string.IsNullOrWhiteSpace(specification) || specification!.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0) {
                return false;
            }
            List<string>? words = Tokenize(specification);
            if (words == null || words.Count == 0) {
                return false;
            }
            Style style = Style.Plain;
            bool foreground = false;
            bool background = false;
            bool linkFromText = false;
            for (int i = 0; i < words.Count; i++) {
                string word = words[i];
                string lower = word.ToLowerInvariant();
                if (lower == "on") {
                    if (++i >= words.Count) {
                        return false;
                    }
                    if (Color.TryParse(words[i], out Color bg)) {
                        style = style.WithBackgroundColor(bg);
                    } else if ((theme ?? Theme.Dark).TryResolve(words[i], depth, out Style role) && !role.Color.IsDefault) {
                        // A theme role as the background ("on pass", "on muted") uses that role's colour.
                        style = style.WithBackgroundColor(role.Color);
                    } else {
                        return false;
                    }
                    background = true;
                    continue;
                }
                TextDecoration decoration;
                switch (lower) {
                    case "bold":
                        decoration = TextDecoration.Bold;
                        break;
                    case "dim":
                        decoration = TextDecoration.Dim;
                        break;
                    case "italic":
                        decoration = TextDecoration.Italic;
                        break;
                    case "underline":
                        decoration = TextDecoration.Underline;
                        break;
                    case "strikethrough":
                        decoration = TextDecoration.Strikethrough;
                        break;
                    case "reverse":
                    case "invert":
                        decoration = TextDecoration.Reverse;
                        break;
                    case "blink":
                    case "slowblink":
                    case "rapidblink":
                    case "conceal":
                        continue;
                    case "link":
                        linkFromText = true;
                        continue;
                    default:
                        if (lower.StartsWith("link=", StringComparison.Ordinal)) {
                            if (word.Length == 5) {
                                return false;
                            }
                            // The target as written (case kept); OSC 8 output is limited to terminals that support it.
                            style = style.WithLink(word.Substring(5));
                            continue;
                        }
                        if (Color.TryParse(word, out Color color)) {
                            style = style.WithColor(color);
                            foreground = true;
                        } else if ((theme ?? Theme.Dark).TryResolve(word, depth, out Style token)) {
                            style = style.Combine(token);
                            foreground |= !token.Color.IsDefault;
                            background |= !token.BackgroundColor.IsDefault;
                        } else {
                            return false;
                        }
                        continue;
                }
                style = style.With(decoration);
            }
            result = new MarkupStyle(style.Downsample(depth), foreground, background, linkFromText && style.Link == null);
            return true;
        }

        private static List<string>? Tokenize(string specification) {
            List<string> words = new List<string>();
            int start = -1;
            bool parenthesis = false;
            for (int i = 0; i < specification.Length; i++) {
                char c = specification[i];
                if (char.IsWhiteSpace(c) && !parenthesis) {
                    if (start >= 0) {
                        words.Add(specification.Substring(start, i - start));
                        start = -1;
                    }
                    continue;
                }
                if (start < 0) {
                    start = i;
                }
                if (c == '(') {
                    if (parenthesis) {
                        return null;
                    }
                    parenthesis = true;
                } else if (c == ')') {
                    if (!parenthesis) {
                        return null;
                    }
                    parenthesis = false;
                }
            }
            if (parenthesis) {
                return null;
            }
            if (start >= 0) {
                words.Add(specification.Substring(start));
            }
            return words;
        }
    }
}
