using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>What a menu item does when opened.</summary>
    public enum MenuItemKind {
        /// <summary>Runs an action.</summary>
        Action,

        /// <summary>Opens a submenu.</summary>
        Submenu,

        /// <summary>Opens a form.</summary>
        Form,

        /// <summary>Picks one value (kept on the item).</summary>
        Select,

        /// <summary>Picks several values (kept on the item).</summary>
        MultiSelect,

        /// <summary>Shows content.</summary>
        Page,

        /// <summary>Shows a review of the app's current choices with the equivalent command, and runs it.</summary>
        Review,

        /// <summary>Lists the saved presets of the menu (apply, delete).</summary>
        Presets,

        /// <summary>A divider with an optional heading.</summary>
        Separator,

        /// <summary>Leaves the menu.</summary>
        Exit
    }
}
