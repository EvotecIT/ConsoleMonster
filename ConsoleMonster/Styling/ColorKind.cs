namespace ConsoleMonster.Styling {
    /// <summary>How a <see cref="Color"/> value is expressed.</summary>
    public enum ColorKind : byte {
        /// <summary>The terminal's default foreground or background.</summary>
        Default = 0,

        /// <summary>One of the 16 standard ANSI colours (index 0-15).</summary>
        Ansi16 = 1,

        /// <summary>One of the xterm 256 palette entries (index 0-255).</summary>
        Ansi256 = 2,

        /// <summary>A 24-bit RGB colour.</summary>
        Rgb = 3
    }
}
