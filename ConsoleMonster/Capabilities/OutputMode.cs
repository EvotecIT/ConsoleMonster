namespace ConsoleMonster.Capabilities {

    /// <summary>
    /// How output reaches the user. Every widget renders in every mode; authors never special-case it.
    /// </summary>
    public enum OutputMode {

        /// <summary>A real VT terminal: cursor movement, alternate screen and live redraw.</summary>
        Interactive = 0,

        /// <summary>Append-only lines (redirected output, CI logs, transcripts); colour only when the profile allows it.</summary>
        Stream = 1,

        /// <summary>Append-only ASCII lines without colour, readable by screen readers and in any log.</summary>
        Plain = 2
    }
}
