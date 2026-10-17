using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// The style of a rich-text span: foreground and background (<see cref="RichColor"/>: explicit colours, PowerShell
    /// ConsoleColors or theme roles), decorations (bold, dim, italic, underline, strikethrough, invert) and an optional
    /// hyperlink. Colours are resolved and down-mapped per colour tier when the text is rendered.
    /// </summary>
    public readonly struct RichStyle : IEquatable<RichStyle> {
        /// <summary>Creates a style.</summary>
        /// <param name="foreground">The foreground colour.</param>
        /// <param name="background">The background colour.</param>
        /// <param name="decoration">Decorations.</param>
        /// <param name="link">A hyperlink target (OSC 8 where supported), or null.</param>
        public RichStyle(RichColor foreground = default, RichColor background = default, TextDecoration decoration = TextDecoration.None, string? link = null) {
            Foreground = foreground;
            Background = background;
            Decoration = decoration;
            Link = string.IsNullOrEmpty(link) ? null : link;
        }

        /// <summary>No styling.</summary>
        public static RichStyle Plain => default;

        /// <summary>The foreground colour.</summary>
        public RichColor Foreground { get; }

        /// <summary>The background colour.</summary>
        public RichColor Background { get; }

        /// <summary>Decorations.</summary>
        public TextDecoration Decoration { get; }

        /// <summary>The hyperlink target, or null.</summary>
        public string? Link { get; }

        /// <summary>Whether the style changes nothing.</summary>
        public bool IsPlain => Foreground.IsDefault && Background.IsDefault && Decoration == TextDecoration.None && Link == null;

        /// <summary>Returns a copy with another foreground.</summary>
        /// <param name="color">The colour.</param>
        /// <returns>The copy.</returns>
        public RichStyle WithForeground(RichColor color) => new RichStyle(color, Background, Decoration, Link);

        /// <summary>Returns a copy with another background.</summary>
        /// <param name="color">The colour.</param>
        /// <returns>The copy.</returns>
        public RichStyle WithBackground(RichColor color) => new RichStyle(Foreground, color, Decoration, Link);

        /// <summary>Returns a copy with added decorations.</summary>
        /// <param name="decoration">The decorations.</param>
        /// <returns>The copy.</returns>
        public RichStyle With(TextDecoration decoration) => new RichStyle(Foreground, Background, Decoration | decoration, Link);

        /// <summary>Returns a copy with a hyperlink (null removes it).</summary>
        /// <param name="link">The target.</param>
        /// <returns>The copy.</returns>
        public RichStyle WithLink(string? link) => new RichStyle(Foreground, Background, Decoration, link);

        /// <summary>
        /// Parses decorations from names separated by commas or spaces: Bold, Dim, Italic, Underline, Strikethrough (Strike),
        /// Invert (Reverse), and None.
        /// </summary>
        /// <param name="text">The names.</param>
        /// <param name="decoration">The decorations.</param>
        /// <returns>True when every name is known.</returns>
        public static bool TryParseDecoration(string? text, out TextDecoration decoration) {
            decoration = TextDecoration.None;
            foreach (string part in (text ?? string.Empty).Split(new[] { ',', ' ', '|', '+' }, StringSplitOptions.RemoveEmptyEntries)) {
                switch (part.Trim().ToLowerInvariant()) {
                    case "none":
                    case "plain":
                        break;
                    case "bold":
                        decoration |= TextDecoration.Bold;
                        break;
                    case "dim":
                    case "faint":
                        decoration |= TextDecoration.Dim;
                        break;
                    case "italic":
                        decoration |= TextDecoration.Italic;
                        break;
                    case "underline":
                        decoration |= TextDecoration.Underline;
                        break;
                    case "strike":
                    case "strikethrough":
                        decoration |= TextDecoration.Strikethrough;
                        break;
                    case "invert":
                    case "inverse":
                    case "reverse":
                        decoration |= TextDecoration.Reverse;
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        /// <summary>The style as a markup tag body (<c>bold lime on navy link=https://...</c>), or empty when plain.</summary>
        /// <returns>The specification.</returns>
        public string ToMarkup() {
            List<string> words = new List<string>();
            if ((Decoration & TextDecoration.Bold) != 0) {
                words.Add("bold");
            }
            if ((Decoration & TextDecoration.Dim) != 0) {
                words.Add("dim");
            }
            if ((Decoration & TextDecoration.Italic) != 0) {
                words.Add("italic");
            }
            if ((Decoration & TextDecoration.Underline) != 0) {
                words.Add("underline");
            }
            if ((Decoration & TextDecoration.Strikethrough) != 0) {
                words.Add("strikethrough");
            }
            if ((Decoration & TextDecoration.Reverse) != 0) {
                words.Add("reverse");
            }
            if (!Foreground.IsDefault) {
                words.Add(Foreground.ToMarkup());
            }
            if (!Background.IsDefault) {
                words.Add("on");
                words.Add(Background.ToMarkup());
            }
            if (Link != null) {
                words.Add("link=" + EncodeLink(Link));
            }
            return string.Join(" ", words.ToArray());
        }

        // Markup tags cannot hold whitespace, brackets or parentheses; percent-encoding keeps the URL valid.
        private static string EncodeLink(string link) {
            StringBuilder sb = new StringBuilder(link.Length);
            foreach (char c in link) {
                switch (c) {
                    case ' ':
                        sb.Append("%20");
                        break;
                    case '[':
                        sb.Append("%5B");
                        break;
                    case ']':
                        sb.Append("%5D");
                        break;
                    case '(':
                        sb.Append("%28");
                        break;
                    case ')':
                        sb.Append("%29");
                        break;
                    default:
                        if (c >= 0x20 && c != 0x7F) {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <inheritdoc />
        public bool Equals(RichStyle other) => Foreground.Equals(other.Foreground) && Background.Equals(other.Background) && Decoration == other.Decoration && string.Equals(Link, other.Link, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RichStyle other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (Foreground.GetHashCode() * 397) ^ (Background.GetHashCode() * 31) ^ (int)Decoration ^ (Link == null ? 0 : StringComparer.Ordinal.GetHashCode(Link));

        /// <summary>Equality.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>True when equal.</returns>
        public static bool operator ==(RichStyle left, RichStyle right) => left.Equals(right);

        /// <summary>Inequality.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>True when different.</returns>
        public static bool operator !=(RichStyle left, RichStyle right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => IsPlain ? "plain" : ToMarkup();
    }
}
