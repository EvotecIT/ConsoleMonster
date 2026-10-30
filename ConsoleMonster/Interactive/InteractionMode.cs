namespace ConsoleMonster.Interactive {
    /// <summary>How prompts, menus and forms talk to the user.</summary>
    public enum InteractionMode {
        /// <summary>Raw keys and in-place redraws: filtering lists, focus bars, the app shell.</summary>
        Rich,

        /// <summary>
        /// Line-based prompts: a question, then a typed answer. Used where raw keys or redraws are unavailable (PowerShell
        /// remoting, the ISE, consoles without VT).
        /// </summary>
        Line,

        /// <summary>
        /// Sequential plain prompts for screen readers and braille displays: numbered choices, no redraws, every state
        /// written as text (<c>CONSOLEMONSTER_ACCESSIBLE=1</c>).
        /// </summary>
        Accessible,

        /// <summary>No user is there to answer (CI, redirected input, <c>-NonInteractive</c>): prompts return their default or fail with a clear message.</summary>
        None,

        /// <summary>Render an initialized application or PowerShell prompt without reading input. A typed prompt's answer-returning Show method cannot return a snapshot; use its Snapshot method.</summary>
        Snapshot
    }
}
