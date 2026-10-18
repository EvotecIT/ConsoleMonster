using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;

namespace ConsoleMonster.Glyphs {
    /// <summary>
    /// What emoji and special symbols become on glyph tiers that cannot show them: ✅ is √ on the Basic tier and [OK] on
    /// ASCII, ⚠️ is !, ❌ is × or x, ➜ is → or ->. Applied to text that goes through markup or rich text (widget labels,
    /// titles, table cells, log lines) before it is measured, so widths stay right. The Full tier shows the text as written.
    /// Apps add or change entries with <see cref="Register"/>.
    /// </summary>
    public static class SymbolFallback {
        private static readonly object Gate = new object();
        private static Dictionary<string, string[]> _map = Build();
        private static int _longest = 2;

        /// <summary>Whether the fallback is applied (default true).</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>Adds or replaces the fallback for a symbol.</summary>
        /// <param name="symbol">The symbol as written (an emoji with or without its variation selector).</param>
        /// <param name="basic">What the Basic tier shows.</param>
        /// <param name="ascii">What the ASCII tier shows.</param>
        public static void Register(string symbol, string basic, string ascii) {
            if (string.IsNullOrEmpty(symbol)) {
                throw new ArgumentException("A symbol is required.", nameof(symbol));
            }
            lock (Gate) {
                Dictionary<string, string[]> map = new Dictionary<string, string[]>(_map, StringComparer.Ordinal);
                map[Strip(symbol)] = new[] { basic ?? string.Empty, ascii ?? string.Empty };
                _map = map;
                _longest = Math.Max(_longest, Strip(symbol).Length);
            }
        }

        /// <summary>The fallback of a symbol for a tier.</summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <param name="fallback">The text to show instead (the symbol itself on the Full tier).</param>
        /// <returns>Whether the symbol has an entry.</returns>
        public static bool TryGet(string symbol, GlyphTier tier, out string fallback) {
            fallback = symbol;
            if (symbol == null || !_map.TryGetValue(Strip(symbol), out string[]? entry)) {
                return false;
            }
            fallback = tier == GlyphTier.Full ? symbol : tier == GlyphTier.Basic ? entry[0] : entry[1];
            return true;
        }

        /// <summary>
        /// Replaces the known symbols in a text for a tier. On the Basic and ASCII tiers it also drops emoji variation
        /// selectors and joiners that would be left alone.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <returns>The text for that tier.</returns>
        public static string Apply(string text, GlyphTier tier) {
            if (!Enabled || tier == GlyphTier.Full || string.IsNullOrEmpty(text) || !NeedsWork(text)) {
                return text;
            }
            Dictionary<string, string[]> map = _map;
            int index = tier == GlyphTier.Basic ? 0 : 1;
            StringBuilder sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length) {
                bool replaced = false;
                for (int length = Math.Min(_longest, text.Length - i); length >= 1; length--) {
                    if (map.TryGetValue(text.Substring(i, length), out string[]? entry)) {
                        sb.Append(entry[index]);
                        i += length;
                        replaced = true;
                        break;
                    }
                }
                if (replaced) {
                    continue;
                }
                char c = text[i];
                if (c != '️' && c != '‍') {
                    sb.Append(c);
                }
                i++;
            }
            string result = sb.ToString();
            return string.Equals(result, text, StringComparison.Ordinal) ? text : result;
        }

        /// <summary>Applies the fallback to every span of styled text.</summary>
        /// <param name="text">The styled text.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <returns>The text for that tier (the same instance when nothing changed).</returns>
        public static StyledText Apply(StyledText text, GlyphTier tier) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            if (!Enabled || tier == GlyphTier.Full) {
                return text;
            }
            bool changed = false;
            List<StyledLine> lines = new List<StyledLine>(text.Lines.Count);
            foreach (StyledLine line in text.Lines) {
                List<TextSpan> spans = new List<TextSpan>();
                foreach (TextSpan span in line) {
                    string replaced = Apply(span.Text, tier);
                    changed |= !ReferenceEquals(replaced, span.Text);
                    spans.Add(ReferenceEquals(replaced, span.Text) ? span : new TextSpan(replaced, span.Style));
                }
                lines.Add(new StyledLine(spans));
            }
            return changed ? new StyledText(lines) : text;
        }

        // Only text with characters outside Latin-1 (or the selectors) can hold an entry.
        private static bool NeedsWork(string text) {
            foreach (char c in text) {
                if (c > 'ÿ') {
                    return true;
                }
            }
            return false;
        }

        private static string Strip(string symbol) => symbol.Replace("️", string.Empty);

        private static Dictionary<string, string[]> Build() {
            Dictionary<string, string[]> map = new Dictionary<string, string[]>(StringComparer.Ordinal);
            void Add(string basic, string ascii, params string[] symbols) {
                foreach (string symbol in symbols) {
                    map[Strip(symbol)] = new[] { basic, ascii };
                }
            }
            Add("√", "[OK]", "✅", "☑️", "✔️", "✔");
            Add("√", "v", "✓");
            Add("×", "x", "❌", "✖️", "✗", "✘", "❎");
            Add("!", "!", "⚠️", "⚠", "❗", "❕", "‼️");
            Add("?", "?", "❓", "❔");
            Add("→", "->", "➜", "➡️", "→", "👉", "▶️", "▶");
            Add("←", "<-", "⬅️", "←", "👈", "◀️", "◀");
            Add("↑", "^", "⬆️", "↑");
            Add("↓", "v", "⬇️", "↓");
            Add("i", "i", "ℹ️", "ℹ");
            Add("*", "*", "⭐", "★", "✨", "🌟", "🔥", "💥");
            Add("»", ">>", "🚀");
            Add("…", "...", "⏳", "⌛", "🕐", "⏱️");
            Add("•", "*", "🟢", "🟡", "🔴", "🔵", "🟠", "🟣", "⚪", "⚫");
            // Typographic characters the Basic tier can show stay; ASCII gets plain equivalents.
            Add("…", "...", "…");
            Add("•", "*", "•");
            Add("●", "*", "●");
            Add("○", "o", "○", "◯");
            Add("—", "-", "—");
            Add("–", "-", "–");
            Add("!", "!", "💡");
            Add("#", "#", "🔒", "🔐");
            Add("-", "-", "🔓");
            Add("+", "+", "➕");
            Add("-", "-", "➖");
            Add("=", "=", "🟰");
            Add("@", "@", "📧", "✉️");
            Add("§", "[doc]", "📄", "📝", "📋");
            Add("■", "[dir]", "📁", "📂");
            Add("♦", "<>", "💎");
            Add("☺", ":)", "🙂", "😀", "😊", "👍");
            Add("☹", ":(", "🙁", "😞", "👎");
            Add("“", "\"", "“");
            Add("”", "\"", "”");
            Add("„", "\"", "„");
            Add("‘", "'", "‘");
            Add("’", "'", "’");
            return map;
        }
    }

    /// <summary>
    /// <c>:shortcode:</c> names for emoji (<c>:white_check_mark:</c> is ✅, <c>:warning:</c> is ⚠️, <c>:rocket:</c> is 🚀).
    /// Used by markup when <see cref="Markup.Shortcodes"/> is on and by the emoji presenter; apps add names with
    /// <see cref="Register"/>. Unknown names stay as written.
    /// </summary>
    public static class Shortcodes {
        private static readonly object Gate = new object();
        private static Dictionary<string, string> _codes = Build();

        /// <summary>Adds or replaces a shortcode.</summary>
        /// <param name="name">The name without colons, for example <c>deploy</c>.</param>
        /// <param name="text">The text it stands for.</param>
        public static void Register(string name, string text) {
            if (string.IsNullOrEmpty(name) || name.IndexOf(':') >= 0) {
                throw new ArgumentException("A shortcode name is required and has no colons.", nameof(name));
            }
            lock (Gate) {
                _codes = new Dictionary<string, string>(_codes, StringComparer.OrdinalIgnoreCase) { [name] = text ?? string.Empty };
            }
        }

        /// <summary>The text of a shortcode.</summary>
        /// <param name="name">The name without colons.</param>
        /// <param name="text">The text.</param>
        /// <returns>Whether the name is known.</returns>
        public static bool TryGet(string name, out string text) {
            if (name != null && _codes.TryGetValue(name, out string? found)) {
                text = found;
                return true;
            }
            text = string.Empty;
            return false;
        }

        /// <summary>Replaces every known <c>:name:</c> in a text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The text with shortcodes replaced.</returns>
        public static string Replace(string text) {
            if (string.IsNullOrEmpty(text) || text.IndexOf(':') < 0) {
                return text;
            }
            StringBuilder sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length) {
                if (text[i] == ':') {
                    int end = text.IndexOf(':', i + 1);
                    if (end > i + 1 && IsName(text, i + 1, end) && TryGet(text.Substring(i + 1, end - i - 1), out string emoji)) {
                        sb.Append(emoji);
                        i = end + 1;
                        continue;
                    }
                }
                sb.Append(text[i]);
                i++;
            }
            string result = sb.ToString();
            return string.Equals(result, text, StringComparison.Ordinal) ? text : result;
        }

        private static bool IsName(string text, int start, int end) {
            for (int i = start; i < end; i++) {
                char c = text[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '+')) {
                    return false;
                }
            }
            return true;
        }

        private static Dictionary<string, string> Build() {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["white_check_mark"] = "✅", ["heavy_check_mark"] = "✔️", ["check"] = "✔️", ["x"] = "❌", ["cross_mark"] = "❌",
                ["warning"] = "⚠️", ["exclamation"] = "❗", ["question"] = "❓", ["information_source"] = "ℹ️", ["info"] = "ℹ️",
                ["rocket"] = "🚀", ["fire"] = "🔥", ["star"] = "⭐", ["sparkles"] = "✨", ["bulb"] = "💡", ["hourglass"] = "⌛",
                ["hourglass_flowing_sand"] = "⏳", ["lock"] = "🔒", ["unlock"] = "🔓", ["arrow_right"] = "➡️", ["arrow_left"] = "⬅️",
                ["arrow_up"] = "⬆️", ["arrow_down"] = "⬇️", ["point_right"] = "👉", ["green_circle"] = "🟢", ["yellow_circle"] = "🟡",
                ["red_circle"] = "🔴", ["blue_circle"] = "🔵", ["thumbsup"] = "👍", ["+1"] = "👍", ["thumbsdown"] = "👎", ["-1"] = "👎",
                ["smile"] = "😀", ["slightly_smiling_face"] = "🙂", ["memo"] = "📝", ["page_facing_up"] = "📄", ["clipboard"] = "📋",
                ["file_folder"] = "📁", ["open_file_folder"] = "📂", ["email"] = "📧", ["gem"] = "💎", ["boom"] = "💥",
                ["heavy_plus_sign"] = "➕", ["heavy_minus_sign"] = "➖", ["stopwatch"] = "⏱️"
            };
        }
    }
}
