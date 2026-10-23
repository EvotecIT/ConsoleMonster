namespace ConsoleMonster.Export {
    /// <summary>Generic vector title bars for exported frames and recordings.</summary>
    public enum WindowChromeStyle {
        /// <summary>No title bar or window frame.</summary>
        None = 0,
        /// <summary>A 28 pixel title bar with three coloured dots.</summary>
        MacOS = 1,
        /// <summary>A 32 pixel tab bar with a prompt and line controls.</summary>
        Windows = 2,
        /// <summary>A 36 pixel header with a bold title and a round close control.</summary>
        Linux = 3
    }
}
