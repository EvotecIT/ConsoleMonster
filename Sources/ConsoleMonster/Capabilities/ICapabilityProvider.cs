namespace ConsoleMonster.Capabilities {
    /// <summary>
    /// Produces a <see cref="TerminalProfile"/>. The built-in provider detects the real terminal;
    /// tests and hosts supply fixed profiles.
    /// </summary>
    public interface ICapabilityProvider {
        /// <summary>Detects (or returns) the terminal profile.</summary>
        /// <returns>The profile to render with.</returns>
        TerminalProfile Detect();
    }
}
