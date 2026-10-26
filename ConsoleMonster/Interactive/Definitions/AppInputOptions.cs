using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Shared construction vocabulary for persistent application controls.</summary>
    public abstract class AppInputOptions {
        /// <summary>The unique application-state key.</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>The visible label; null uses the key.</summary>
        public string? Label { get; set; }
        /// <summary>Disables editing and keyboard focus.</summary>
        public bool Disabled { get; set; }
        /// <summary>Keyboard focus order; equal values preserve declaration order.</summary>
        public int TabIndex { get; set; }
        /// <summary>Initial validation text.</summary>
        public string? ValidationMessage { get; set; }
        /// <summary>Runs after a committed value change on the application input thread.</summary>
        public EventHandler<AppValueChangedEventArgs>? OnChange { get; set; }
    }
}
