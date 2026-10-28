using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Glyphs {
    /// <summary>
    /// Resolves semantic glyphs for one tier. Implement it for a custom set, or wrap a built-in one.
    /// </summary>
    public interface IGlyphSet {
        /// <summary>The tier this set targets.</summary>
        GlyphTier Tier { get; }

        /// <summary>Whether braille patterns (U+2800-U+28FF) render reliably.</summary>
        bool SupportsBraille { get; }

        /// <summary>Whether half blocks (▀▄) render reliably.</summary>
        bool SupportsHalfBlocks { get; }

        /// <summary>Spinner animation frames.</summary>
        IReadOnlyList<string> SpinnerFrames { get; }

        /// <summary>Vertical levels from empty (index 0) to full (last index), used by sparklines.</summary>
        IReadOnlyList<string> VerticalRamp { get; }

        /// <summary>Returns a semantic glyph.</summary>
        /// <param name="glyph">The glyph name.</param>
        /// <returns>The text (may be wider than one cell, for example tree prefixes or ASCII ellipsis).</returns>
        string Get(Glyph glyph);

        /// <summary>Returns border glyphs, falling back to a style the tier can show.</summary>
        /// <param name="style">The requested style.</param>
        /// <returns>The border glyphs actually used.</returns>
        BoxGlyphs GetBox(BoxStyle style);

        /// <summary>
        /// The marker shown for a status. Full and Basic return one symbol; Ascii returns a bracketed word such as
        /// <c>[OK]</c> so status stays readable without colour.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns>The marker text.</returns>
        string StatusMarker(StatusKind status);

        /// <summary>A partial horizontal block for <paramref name="eighths"/> of a cell (1-8), or empty when the tier cannot show it.</summary>
        /// <param name="eighths">Filled eighths, 1-8.</param>
        /// <returns>The glyph, or an empty string.</returns>
        string HorizontalFill(int eighths);

        /// <summary>A partial vertical block for <paramref name="eighths"/> of a cell (1-8), or empty when the tier cannot show it.</summary>
        /// <param name="eighths">Filled eighths, 1-8.</param>
        /// <returns>The glyph, or an empty string.</returns>
        string VerticalFill(int eighths);
    }
}
