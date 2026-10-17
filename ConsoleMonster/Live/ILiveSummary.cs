namespace ConsoleMonster.Live {
    /// <summary>Provides a concise periodic status for stream and plain outputs.</summary>
    public interface ILiveSummary {
        /// <summary>Returns the current status, or null to omit this target from the heartbeat.</summary>
        /// <returns>The plain-text status line.</returns>
        string? GetSummaryLine();
    }
}
