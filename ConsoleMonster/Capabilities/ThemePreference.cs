namespace ConsoleMonster.Capabilities {

    /// <summary>Specifies how a terminal theme should choose its light or dark palette.</summary>
    public enum ThemePreference {
        /// <summary>Choose based on automatic terminal background detection.</summary>
        Auto,
        /// <summary>Prefer a light theme.</summary>
        Light,
        /// <summary>Prefer a dark theme.</summary>
        Dark
    }
}
