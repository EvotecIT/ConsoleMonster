using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>How a menu runs.</summary>
    public sealed class MenuOptions {
        /// <summary>The layout.</summary>
        public MenuLayout Layout { get; set; } = MenuLayout.Auto;

        /// <summary>Interaction settings (mode, input, profile, theme, labels).</summary>
        public InteractiveOptions? Interactive { get; set; }

        /// <summary>The app name presets and remembered choices are stored under; null uses the menu title.</summary>
        public string? AppName { get; set; }

        /// <summary>Whether forms and selections remember the last choices (opt-in, stored per user).</summary>
        public bool RememberLastChoices { get; set; }

        /// <summary>The folder for the app's storage folder; null uses the per-user default.</summary>
        public string? StorageRoot { get; set; }

        /// <summary>Whether leaving with unsaved changes asks first.</summary>
        public bool ConfirmExit { get; set; } = true;

        /// <summary>Whether the command palette is available.</summary>
        public bool CommandPalette { get; set; } = true;

        /// <summary>The narrowest window that still shows the sidebar.</summary>
        public int SidebarMinimumWidth { get; set; } = 84;
    }
}
