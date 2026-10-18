namespace ConsoleMonster.Capabilities {

    /// <summary>
    /// Colour fidelity a terminal supports. Themes map their semantic tokens down to the detected depth.
    /// </summary>
    public enum ColorDepth {

        /// <summary>No colour at all (NO_COLOR, plain mode, logs that must stay clean).</summary>
        None = 0,

        /// <summary>The 16 standard ANSI colours (Windows Server 2016 conhost, most CI logs).</summary>
        Ansi16 = 1,

        /// <summary>The xterm 256-colour palette.</summary>
        Ansi256 = 2,

        /// <summary>24-bit RGB colour (Windows 10 1703+, Windows Terminal, VS Code, modern terminals).</summary>
        TrueColor = 3
    }
}
