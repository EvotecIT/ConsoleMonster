namespace ConsoleMonster.Input {
    /// <summary>Temporarily gives a leased console back to signal-driven work, then resumes raw editing.</summary>
    internal interface IConsoleInputMode {
        /// <summary>Restores the original mode while the owning input loop is suspended.</summary>
        void Suspend();

        /// <summary>Discards keys typed during work and resumes the leased input mode.</summary>
        void Resume();
    }
}
