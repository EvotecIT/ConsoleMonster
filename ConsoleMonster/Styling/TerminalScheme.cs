using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Styling {
    /// <summary>An immutable terminal scheme: sixteen ANSI colours and its default foreground and background.</summary>
    public sealed partial class TerminalScheme {
        /// <summary>Creates a scheme from RGB colours in ANSI index order (black through bright white).</summary>
        public TerminalScheme(string name, IReadOnlyList<Color> palette, Color foreground, Color background,
            Color? selection = null, Color? cursor = null, Color? signatureAccent = null) {
            if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl)) { throw new ArgumentException("A scheme needs a printable name.", nameof(name)); }
            if (palette == null) { throw new ArgumentNullException(nameof(palette)); }
            if (palette.Count != 16) { throw new ArgumentException("A scheme needs exactly sixteen palette colours.", nameof(palette)); }
            Color[] copy = palette.ToArray();
            foreach (Color color in copy) { RequireRgb(color); }
            RequireRgb(foreground); RequireRgb(background);
            if (selection.HasValue) { RequireRgb(selection.Value); }
            if (cursor.HasValue) { RequireRgb(cursor.Value); }
            if (signatureAccent.HasValue) { RequireRgb(signatureAccent.Value); }
            Name = name; Palette = Array.AsReadOnly(copy); Foreground = foreground; Background = background;
            Selection = selection; Cursor = cursor; SignatureAccent = signatureAccent;
        }

        /// <summary>The name supplied by the scheme.</summary>
        public string Name { get; }
        /// <summary>The sixteen RGB colours, indexed in ANSI order.</summary>
        public IReadOnlyList<Color> Palette { get; }
        /// <summary>The terminal's default foreground.</summary>
        public Color Foreground { get; }
        /// <summary>The terminal's default background.</summary>
        public Color Background { get; }
        /// <summary>The optional selection background.</summary>
        public Color? Selection { get; }
        /// <summary>The optional cursor colour.</summary>
        public Color? Cursor { get; }
        /// <summary>An optional scheme-defining accent used in place of ANSI blue.</summary>
        public Color? SignatureAccent { get; }

        private static void RequireRgb(Color color) {
            if (color.Kind != ColorKind.Rgb) { throw new ArgumentException("Scheme colours must be concrete RGB values."); }
        }
    }
}
