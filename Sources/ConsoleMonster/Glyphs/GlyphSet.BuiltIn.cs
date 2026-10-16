using System.Collections.Generic;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Glyphs {
    public sealed partial class GlyphSet {
        /// <summary>Braille, eighth blocks and rounded corners. Needs Cascadia, JetBrains Mono, DejaVu, Menlo or similar.</summary>
        public static GlyphSet Full { get; } = new GlyphSet(
            GlyphTier.Full,
            new[] {
                "✓", "✗", "▲", "◆", "–", "●", "○", "•", "→", "↑", "↓", "…", "·", "│", "›",
                "█", "░", "▒", "▓", "▀", "▄", "├── ", "└── ", "│   ", "    ", "─", "░"
            },
            new Dictionary<BoxStyle, BoxGlyphs> {
                [BoxStyle.Rounded] = new BoxGlyphs(BoxStyle.Rounded, "╭─╮│╰╯├┤┬┴┼"),
                [BoxStyle.Single] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Double] = new BoxGlyphs(BoxStyle.Double, "╔═╗║╚╝╠╣╦╩╬"),
                [BoxStyle.Heavy] = new BoxGlyphs(BoxStyle.Heavy, "┏━┓┃┗┛┣┫┳┻╋"),
                [BoxStyle.Ascii] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.None] = new BoxGlyphs(BoxStyle.None, "           ")
            },
            new[] { "○", "●", "✓", "✗", "▲", "–", "◆" },
            new[] { "▏", "▎", "▍", "▌", "▋", "▊", "▉", "█" },
            new[] { "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" },
            new[] { " ", "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" },
            new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" });

        /// <summary>Box drawing, shades and half blocks from the WGL4 repertoire (Consolas, Lucida Console).</summary>
        public static GlyphSet Basic { get; } = new GlyphSet(
            GlyphTier.Basic,
            new[] {
                "√", "×", "!", "♦", "–", "►", "○", "•", "→", "↑", "↓", "…", "·", "│", "›",
                "█", "░", "▒", "▓", "▀", "▄", "├── ", "└── ", "│   ", "    ", "─", "░"
            },
            new Dictionary<BoxStyle, BoxGlyphs> {
                [BoxStyle.Rounded] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Single] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Double] = new BoxGlyphs(BoxStyle.Double, "╔═╗║╚╝╠╣╦╩╬"),
                [BoxStyle.Heavy] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Ascii] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.None] = new BoxGlyphs(BoxStyle.None, "           ")
            },
            new[] { "○", "►", "√", "×", "!", "–", "♦" },
            new[] { "", "", "", "▌", "▌", "▌", "▌", "█" },
            new[] { "", "", "", "▄", "▄", "▄", "▄", "█" },
            new[] { " ", "░", "▒", "▓", "█" },
            new[] { "|", "/", "-", "\\" });

        /// <summary>Printable ASCII only; statuses become bracketed words.</summary>
        public static GlyphSet Ascii { get; } = new GlyphSet(
            GlyphTier.Ascii,
            new[] {
                "+", "x", "!", "i", "-", "*", ".", "*", ">", "^", "v", "...", "-", "|", ">",
                "#", ".", ":", "#", "\"", "_", "|-- ", "`-- ", "|   ", "    ", "-", "-"
            },
            new Dictionary<BoxStyle, BoxGlyphs> {
                [BoxStyle.Rounded] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.Single] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.Double] = new BoxGlyphs(BoxStyle.Ascii, "+=+|+++++++"),
                [BoxStyle.Heavy] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.Ascii] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.None] = new BoxGlyphs(BoxStyle.None, "           ")
            },
            new[] { "[WAIT]", "[BUSY]", "[PASS]", "[FAIL]", "[WARN]", "[SKIP]", "[INFO]" },
            new[] { "", "", "", "", "", "", "", "#" },
            new[] { "", "", "", ".", ".", ".", ".", "#" },
            new[] { " ", ".", ":", "-", "=", "+", "*", "#", "@" },
            new[] { "|", "/", "-", "\\" });
    }
}
