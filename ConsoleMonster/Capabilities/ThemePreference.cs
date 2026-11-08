namespace ConsoleMonster.Capabilities {

    /// <summary>Specifies how a terminal theme should choose its light or dark palette.</summary>
    public enum ThemePreference {
        /// <summary>Choose based on automatic terminal background detection.</summary>
        Auto,
        /// <summary>Prefer a light theme.</summary>
        Light,
        /// <summary>Prefer a dark theme.</summary>
        Dark,
        /// <summary>Use the high contrast palette.</summary>
        HighContrast,
        /// <summary>Use the colour-blind-safe palette, in its light or dark variant from the detected background.</summary>
        ColorBlind,
        /// <summary>Use the light-background colour-blind-safe palette.</summary>
        ColorBlindLight,
        /// <summary>Use the dark-background colour-blind-safe palette.</summary>
        ColorBlindDark
    }
}
