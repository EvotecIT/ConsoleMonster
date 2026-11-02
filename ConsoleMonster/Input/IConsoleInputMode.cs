namespace ConsoleMonster.Input {
    /// <summary>Temporarily gives a leased console back to signal-driven work, then resumes raw editing.</summary>
    internal interface IConsoleInputMode {
        /// <summary>Restores the original mode while the owning input loop is suspended.</summary>
        void Suspend();

        /// <summary>Reapplies a suspended mode after a nested reader temporarily changes native input.</summary>
        void RestoreSuspendedMode();

        /// <summary>Discards keys typed during work and resumes the leased input mode.</summary>
        void Resume();
    }
}
