using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Constructs an action button with the same callback names as New-ConsoleButton.</summary>
    public sealed class ButtonInputOptions : AppInputOptions {
        /// <summary>Runs once per activation, after releasing the control's state lock.</summary>
        public EventHandler? OnClick { get; set; }
    }
}
