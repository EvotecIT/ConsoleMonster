namespace ConsoleMonster.Glyphs {
    /// <summary>The characters that draw one border style.</summary>
    public sealed class BoxGlyphs {
        /// <summary>Creates a border glyph set from an 11-character string.</summary>
        /// <param name="style">The style these glyphs draw.</param>
        /// <param name="chars">
        /// Exactly 11 characters in this order: top-left, horizontal, top-right, vertical, bottom-left, bottom-right,
        /// left joint (├), right joint (┤), top joint (┬), bottom joint (┴), cross (┼).
        /// </param>
        public BoxGlyphs(BoxStyle style, string chars) {
            if (chars == null || chars.Length != 11) {
                throw new System.ArgumentException("Exactly 11 characters are required.", nameof(chars));
            }
            Style = style;
            TopLeft = chars[0].ToString();
            Horizontal = chars[1].ToString();
            TopRight = chars[2].ToString();
            Vertical = chars[3].ToString();
            BottomLeft = chars[4].ToString();
            BottomRight = chars[5].ToString();
            LeftJoint = chars[6].ToString();
            RightJoint = chars[7].ToString();
            TopJoint = chars[8].ToString();
            BottomJoint = chars[9].ToString();
            Cross = chars[10].ToString();
        }

        /// <summary>The style actually drawn (after tier fallback).</summary>
        public BoxStyle Style { get; }

        /// <summary>Top-left corner.</summary>
        public string TopLeft { get; }

        /// <summary>Horizontal edge.</summary>
        public string Horizontal { get; }

        /// <summary>Top-right corner.</summary>
        public string TopRight { get; }

        /// <summary>Vertical edge.</summary>
        public string Vertical { get; }

        /// <summary>Bottom-left corner.</summary>
        public string BottomLeft { get; }

        /// <summary>Bottom-right corner.</summary>
        public string BottomRight { get; }

        /// <summary>Joint on the left edge (├).</summary>
        public string LeftJoint { get; }

        /// <summary>Joint on the right edge (┤).</summary>
        public string RightJoint { get; }

        /// <summary>Joint on the top edge (┬).</summary>
        public string TopJoint { get; }

        /// <summary>Joint on the bottom edge (┴).</summary>
        public string BottomJoint { get; }

        /// <summary>Inner cross (┼).</summary>
        public string Cross { get; }

        /// <summary>Whether the border is invisible (<see cref="BoxStyle.None"/>).</summary>
        public bool IsInvisible => Style == BoxStyle.None;
    }
}
