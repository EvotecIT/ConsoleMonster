using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>How a form is shown on its own.</summary>
    public sealed class FormOptions {
        /// <summary>Interaction settings (mode, input, profile, theme, labels).</summary>
        public InteractiveOptions? Interactive { get; set; }

        /// <summary>The app name presets and remembered choices are stored under; null disables both.</summary>
        public string? AppName { get; set; }

        /// <summary>Whether the last submitted values are remembered and offered next time (opt-in).</summary>
        public bool RememberLastChoices { get; set; }

        /// <summary>The folder for the app's storage folder; null uses the per-user default.</summary>
        public string? StorageRoot { get; set; }

        /// <summary>Draws the form in the scrollback instead of on the alternate screen.</summary>
        public bool Inline { get; set; }

        /// <summary>A preset to apply before showing.</summary>
        public string? Preset { get; set; }

        internal ChoiceStore? Store() => string.IsNullOrEmpty(AppName) ? null : new ChoiceStore(AppName!, StorageRoot);
    }
}
