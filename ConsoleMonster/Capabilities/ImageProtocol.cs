namespace ConsoleMonster.Capabilities {

    /// <summary>
    /// How an inline image can be drawn. <see cref="TerminalProfile.ImageProtocol"/> reports the protocol an output may use:
    /// redirected, CI, plain, no-colour and 16-colour output always get <see cref="None"/>.
    /// </summary>
    public enum ImageProtocol {

        /// <summary>No pixels: a text placeholder such as <c>[image: chart.png 560x272]</c> (no colour, redirected or CI output).</summary>
        None = 0,

        /// <summary>Half blocks (▀) with a foreground and a background colour per cell, on any 256-colour or truecolor terminal.</summary>
        HalfBlock = 1,

        /// <summary>DEC Sixel graphics (Windows Terminal 1.22 and later), drawn on Windows Terminal's 10×20-pixel sixel cell.</summary>
        Sixel = 2,

        /// <summary>The iTerm2 inline image protocol, <c>OSC 1337 File</c> (iTerm2, WezTerm).</summary>
        ITerm2 = 3,

        /// <summary>The Kitty graphics protocol (Kitty, Ghostty).</summary>
        Kitty = 4
    }
}
