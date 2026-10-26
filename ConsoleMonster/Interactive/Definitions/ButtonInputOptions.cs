using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Constructs an action button with the same callback names as New-ConsoleButton.</summary>
    public sealed class ButtonInputOptions : AppInputOptions {
        /// <summary>An in-app command shared with shortcuts and the palette. Mutually exclusive with OnClick.</summary>
        public AppCommand? Command { get; set; }
        /// <summary>Runs once per activation, after releasing the control's state lock.</summary>
        public EventHandler? OnClick { get; set; }
    }
}
