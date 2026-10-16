namespace ConsoleMonster.Capabilities {
    /// <summary>
    /// The terminal or host that was detected. Informational; decisions are carried by <see cref="TerminalProfile"/>.
    /// </summary>
    public enum TerminalKind {
        /// <summary>Not identified.</summary>
        Unknown = 0,

        /// <summary>Windows Terminal (<c>WT_SESSION</c>).</summary>
        WindowsTerminal = 1,

        /// <summary>The classic Windows console host.</summary>
        Conhost = 2,

        /// <summary>The Visual Studio Code integrated terminal.</summary>
        VisualStudioCode = 3,

        /// <summary>An xterm-compatible terminal identified through <c>TERM</c>/<c>TERM_PROGRAM</c>.</summary>
        Xterm = 4,

        /// <summary>The PowerShell ISE, which is not a console.</summary>
        PowerShellIse = 5,

        /// <summary>Output is redirected to a file, pipe or log.</summary>
        Redirected = 6
    }
}
