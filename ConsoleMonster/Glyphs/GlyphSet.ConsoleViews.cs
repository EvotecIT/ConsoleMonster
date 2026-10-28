using System.Collections.Generic;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Glyphs {
    public sealed partial class GlyphSet {
        /// <summary>Braille, eighth blocks and rounded corners. Needs Cascadia, JetBrains Mono, DejaVu, Menlo or similar.</summary>
        internal static GlyphSet ConsoleViewFull { get; } = new GlyphSet(
            GlyphTier.Full,
            new[] {
                "✓", "×", "▴", "•", "–", "•", "·", "•", "→", "↑", "↓", "…", "·", "│", "›",
                "█", "░", "▒", "▓", "▀", "▄", "├── ", "└── ", "│   ", "    ", "─", "░", "∞", "↑", "▸", "↔", "▶", "■", "←", "−", "≈", "—", "·", "├─→", "└─→"
            },
            new Dictionary<BoxStyle, BoxGlyphs> {
                [BoxStyle.Rounded] = new BoxGlyphs(BoxStyle.Rounded, "╭─╮│╰╯├┤┬┴┼"),
                [BoxStyle.Single] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Double] = new BoxGlyphs(BoxStyle.Double, "╔═╗║╚╝╠╣╦╩╬"),
                [BoxStyle.Heavy] = new BoxGlyphs(BoxStyle.Heavy, "┏━┓┃┗┛┣┫┳┻╋"),
                [BoxStyle.Ascii] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.None] = new BoxGlyphs(BoxStyle.None, "           ")
            },
            new[] { "·", "•", "✓", "×", "▴", "–", "•", "◌", "·" },
            new[] { "▏", "▎", "▍", "▌", "▋", "▊", "▉", "█" },
            new[] { "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" },
            new[] { " ", "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" },
            new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" });

        /// <summary>Box drawing, shades and half blocks from the WGL4 repertoire (Consolas, Lucida Console).</summary>
        internal static GlyphSet ConsoleViewBasic { get; } = new GlyphSet(
            GlyphTier.Basic,
            new[] {
                "√", "×", "!", "•", "–", "•", "·", "•", "→", "↑", "↓", "…", "·", "│", "›",
                "█", "░", "▒", "▓", "▀", "▄", "├── ", "└── ", "│   ", "    ", "─", "░", "∞", "↑", "►", "↔", "►", "■", "←", "-", "≈", "—", "·", "├─→", "└─→"
            },
            new Dictionary<BoxStyle, BoxGlyphs> {
                [BoxStyle.Rounded] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Single] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Double] = new BoxGlyphs(BoxStyle.Double, "╔═╗║╚╝╠╣╦╩╬"),
                [BoxStyle.Heavy] = new BoxGlyphs(BoxStyle.Single, "┌─┐│└┘├┤┬┴┼"),
                [BoxStyle.Ascii] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.None] = new BoxGlyphs(BoxStyle.None, "           ")
            },
            new[] { "·", "•", "√", "×", "!", "–", "•", "○", "·" },
            new[] { "", "", "", "▌", "▌", "▌", "▌", "█" },
            new[] { "", "", "", "▄", "▄", "▄", "▄", "█" },
            new[] { " ", "░", "▒", "▓", "█" },
            new[] { "|", "/", "-", "\\" });

        /// <summary>Printable ASCII only; statuses become bracketed words.</summary>
        internal static GlyphSet ConsoleViewAscii { get; } = new GlyphSet(
            GlyphTier.Ascii,
            new[] {
                "+", "x", "!", "i", "-", "*", ".", "*", ">", "^", "v", "...", "-", "|", ">",
                "#", ".", ":", "#", "\"", "_", "|-- ", "`-- ", "|   ", "    ", "-", ".", "@", "^", ">", "<>", ">", "#", "<-", "-", "about", "\\", ".", "|->", "`->"
            },
            new Dictionary<BoxStyle, BoxGlyphs> {
                [BoxStyle.Rounded] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.Single] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.Double] = new BoxGlyphs(BoxStyle.Ascii, "+=+|+++++++"),
                [BoxStyle.Heavy] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.Ascii] = new BoxGlyphs(BoxStyle.Ascii, "+-+|+++++++"),
                [BoxStyle.None] = new BoxGlyphs(BoxStyle.None, "           ")
            },
            new[] { ".", "i", "+", "x", "!", "-", "i", "o", "." },
            new[] { "", "", "", "", "", "", "", "#" },
            new[] { "", "", "", ".", ".", ".", ".", "#" },
            new[] { " ", ".", ":", "-", "=", "+", "*", "#", "@" },
            new[] { "|", "/", "-", "\\" });
    }
}
