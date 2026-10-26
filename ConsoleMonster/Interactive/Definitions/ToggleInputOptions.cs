namespace ConsoleMonster.Interactive {
    /// <summary>Constructs a Boolean application input.</summary>
    public sealed class ToggleInputOptions : AppInputOptions {
        /// <summary>The initial Boolean value.</summary>
        public bool Value { get; set; }
        /// <summary>Text for the checked value; null uses the localized Yes label.</summary>
        public string? OnText { get; set; }
        /// <summary>Text for the unchecked value; null uses the localized No label.</summary>
        public string? OffText { get; set; }
    }
}
