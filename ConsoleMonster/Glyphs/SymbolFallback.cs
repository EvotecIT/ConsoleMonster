using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Glyphs {
    /// <summary>
    /// What emoji and special symbols become on glyph tiers that cannot show them: ✅ is √ on the Basic tier and [OK] on
    /// ASCII, ⚠️ is !, ❌ is × or x, ➜ is → or ->. Applied to text that goes through markup or rich text (widget labels,
    /// titles, table cells, log lines) before it is measured, so widths stay right. The Full tier shows the text as written.
    /// Apps add or change entries with <see cref="Register"/>.
    /// </summary>
    public static partial class SymbolFallback {
        private static readonly object Gate = new object();
        private static Dictionary<string, string[]> _map = Build();

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
        /// Replaces complete emoji graphemes for a tier, including unknown emoji. Narrow replacements keep a separator
        /// before adjacent text. Existing whitespace is retained without adding a second separator.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <returns>The text for that tier.</returns>
        public static string Apply(string text, GlyphTier tier) => Apply(text, tier, DisplayWidth.Profile);

        /// <summary>Applies prose fallbacks using the destination's width model.</summary>
        /// <param name="text">The text.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <param name="widthProfile">The destination width model.</param>
        /// <returns>The text with complete fallbacks and readable separators.</returns>
        public static string Apply(string text, GlyphTier tier, TerminalWidthProfile widthProfile) {
            if (widthProfile == null) throw new ArgumentNullException(nameof(widthProfile));
            if (!Enabled || tier == GlyphTier.Full || string.IsNullOrEmpty(text) || !NeedsWork(text)) {
                return text;
            }
            Dictionary<string, string[]> map = _map;
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (TextCluster cluster in DisplayWidth.Clusters(text, widthProfile)) {
                sb.Append(ReplaceCluster(text, cluster, tier, map, widthProfile));
            }
            string result = sb.ToString();
            return string.Equals(result, text, StringComparison.Ordinal) ? text : result;
        }

        /// <summary>Applies the fallback to every span of styled text.</summary>
        /// <param name="text">The styled text.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <returns>The text for that tier (the same instance when nothing changed).</returns>
        public static StyledText Apply(StyledText text, GlyphTier tier) => Apply(text, tier, DisplayWidth.Profile);

        /// <summary>Applies fallbacks to styled text using the destination's width model, retaining unchanged span styles.</summary>
        /// <param name="text">The styled text.</param>
        /// <param name="tier">The glyph tier.</param>
        /// <param name="widthProfile">The destination width model.</param>
        /// <returns>The text for that tier.</returns>
        public static StyledText Apply(StyledText text, GlyphTier tier, TerminalWidthProfile widthProfile) {
            if (widthProfile == null) throw new ArgumentNullException(nameof(widthProfile));
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            if (!Enabled || tier == GlyphTier.Full) {
                return text;
            }
            bool changed = false;
            List<StyledLine> lines = new List<StyledLine>(text.Lines.Count);
            foreach (StyledLine line in text.Lines) {
                string original = line.PlainText;
                string replacedLine = Apply(original, tier, widthProfile);
                if (string.Equals(original, replacedLine, StringComparison.Ordinal)) {
                    lines.Add(line);
                    continue;
                }
                changed = true;
                List<TextSpan> spans = new List<TextSpan>();
                StringBuilder run = new StringBuilder();
                int spanIndex = 0;
                int spanEnd = line[0].Text.Length;
                Style style = line[0].Style;
                void Append(string value, Style nextStyle) {
                    if (!style.Equals(nextStyle) && run.Length > 0) {
                        spans.Add(new TextSpan(run.ToString(), style));
                        run.Clear();
                    }
                    style = nextStyle;
                    run.Append(value);
                }
                Dictionary<string, string[]> map = _map;
                foreach (TextCluster cluster in DisplayWidth.Clusters(original, widthProfile)) {
                    while (cluster.Start >= spanEnd && spanIndex + 1 < line.Count) spanEnd += line[++spanIndex].Text.Length;
                    string replacement = ReplaceCluster(original, cluster, tier, map, widthProfile);
                    if (!string.Equals(replacement, original.Substring(cluster.Start, cluster.Length), StringComparison.Ordinal)) {
                        Append(replacement, line[spanIndex].Style);
                        continue;
                    }
                    // Only replaced graphemes take their first span's style. Unchanged graphemes may contain
                    // differently styled combining characters, so retain every original slice.
                    int offset = cluster.Start;
                    int end = offset + cluster.Length;
                    while (offset < end) {
                        while (offset >= spanEnd && spanIndex + 1 < line.Count) spanEnd += line[++spanIndex].Text.Length;
                        int length = Math.Min(end, spanEnd) - offset;
                        Append(original.Substring(offset, length), line[spanIndex].Style);
                        offset += length;
                    }
                }
                if (run.Length > 0) spans.Add(new TextSpan(run.ToString(), style));
                lines.Add(new StyledLine(spans));
            }
            return changed ? new StyledText(lines) : text;
        }

        // Plain ASCII cannot contain a built-in fallback or an emoji presentation selector.
        private static bool NeedsWork(string text) {
            foreach (char c in text) {
                if (c > '~') {
                    return true;
                }
            }
            return false;
        }

        private static string Strip(string symbol) => symbol.Replace("️", string.Empty).Replace("︎", string.Empty);

        private static string ReplaceCluster(string text, TextCluster cluster, GlyphTier tier, Dictionary<string, string[]> map, TerminalWidthProfile profile) {
            string original = text.Substring(cluster.Start, cluster.Length);
            if (original.IndexOf('\uFE0E') >= 0) return original;
            string key = Strip(original);
            string replacement;
            if (map.TryGetValue(key, out string[]? entry)) replacement = Safe(entry[tier == GlyphTier.Basic ? 0 : 1], tier);
            else if (IsEmoji(original)) replacement = "*";
            else return key;
            int end = cluster.Start + cluster.Length;
            // Explicitly mapped pictographs also need separation when written without VS16 (e.g. ℹ, 🌡, ⛓).
            if (IsEmoji(original) || IsPictographic(original)) {
                int padding = cluster.Width - DisplayWidth.Measure(replacement, profile);
                if (end < text.Length && !char.IsWhiteSpace(text[end])) padding = Math.Max(1, padding);
                else if (end < text.Length) padding = 0;
                if (padding > 0) replacement += new string(' ', padding);
            }
            return replacement;
        }

        private static string Safe(string value, GlyphTier tier) {
            if (tier == GlyphTier.Ascii) {
                foreach (char c in value) if (c < ' ' || c > '~') return "*";
            }
            return value;
        }

        private static bool IsEmoji(string value) {
            if (value.IndexOf('\uFE0E') >= 0) return false;
            for (int i = 0; i < value.Length; i++) {
                int scalar = value[i];
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                    scalar = char.ConvertToUtf32(value[i], value[++i]);
                if (scalar == 0x20E3 || scalar >= 0x1F1E6 && scalar <= 0x1F1FF
                    || UnicodeWidthTables.Contains(UnicodeWidthTables.EmojiPresentation, scalar)
                    || (value.IndexOf('\uFE0F') >= 0 || value.IndexOf('\u200D') >= 0)
                        && UnicodeWidthTables.Contains(UnicodeWidthTables.ExtendedPictographic, scalar)) return true;
            }
            return false;
        }

        private static bool IsPictographic(string value) {
            int scalar = char.IsHighSurrogate(value[0]) && value.Length > 1 && char.IsLowSurrogate(value[1])
                ? char.ConvertToUtf32(value[0], value[1]) : value[0];
            return UnicodeWidthTables.Contains(UnicodeWidthTables.ExtendedPictographic, scalar);
        }

        private static Dictionary<string, string[]> Build() {
            Dictionary<string, string[]> map = new Dictionary<string, string[]>(StringComparer.Ordinal);
            void Add(string basic, string ascii, params string[] symbols) {
                foreach (string symbol in symbols) {
                    map[Strip(symbol)] = new[] { basic, ascii };
                }
            }
            void Cell(string basic, string ascii, string cell, params string[] symbols) {
                foreach (string symbol in symbols) map[Strip(symbol)] = new[] { basic, ascii, cell };
            }
            Cell("√", "[OK]", "+", "✅", "☑️", "✔️", "✔", "💚", "🧡");
            Add("√", "+", "✓", "👍");
            Add("×", "x", "❌", "✖️", "✗", "✘", "❎");
            Add("!", "!", "⚠️", "⚠", "❗", "❕", "‼️");
            Add("?", "?", "❓", "❔", "⁉️", "⁉", "🕵️");
            Add("!", "!", "😠", "🤯");
            Add("×", "x", "💔", "💀", "⛔");
            Add("i", "i", "💙");
            Add("~", "~", "🧪", "🔄", "⟲", "⛓", "⛓️‍💥");
            Add("·", "z", "💤");
            Add("↑", "^", "🆙");
            Add("■", "x", "🛑");
            Add("•", "*", "📌", "📛");
            Add("|", "|", "🚦");
            Add("*", "*", "🌡", "🌶");
            Add("→", ">", "🔚");
            Add("A", "A", "△");
            Add("a", "a", "▽");
            Cell("→", "->", ">", "➜", "➡️", "→", "👉", "▶️", "▶");
            Cell("←", "<-", "<", "⬅️", "←", "👈", "◀️", "◀");
            Add("↑", "^", "⬆️", "↑");
            Add("↓", "v", "⬇️", "↓");
            Add("i", "i", "ℹ️", "ℹ");
            Add("*", "*", "⭐", "★", "✨", "🌟", "🔥", "💥");
            Cell("»", ">>", ">", "🚀");
            Cell("…", "...", ".", "⏳", "⌛", "🕐", "⏱️");
            Add("•", "*", "🟢", "🟡", "🔴", "🔵", "🟠", "🟣", "⚪", "⚫");
            // Typographic characters the Basic tier can show stay; ASCII gets plain equivalents.
            Add("…", "...", "…");
            Add("•", "*", "•");
            Add("●", "*", "●");
            Add("○", "o", "○", "◯");
            Add("—", "-", "—");
            Add("–", "-", "–");
            Add("*", "*", "💡");
            Add("#", "#", "🔒", "🔐");
            Add("-", "-", "🔓");
            Add("+", "+", "➕");
            Add("-", "-", "➖");
            Add("=", "=", "🟰");
            Add("@", "@", "📧", "✉️");
            Cell("§", "[doc]", "#", "📄", "📝", "📋");
            Cell("■", "[dir]", "#", "📁", "📂");
            Cell("♦", "<>", "*", "💎");
            Add("+", "+", "🙂", "😀", "😊", "☺");
            Add("-", "-", "🙁", "😞", "👎", "☹");
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
