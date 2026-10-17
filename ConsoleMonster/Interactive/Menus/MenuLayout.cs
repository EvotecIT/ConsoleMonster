using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Interactive {
    /// <summary>How a menu is laid out.</summary>
    public enum MenuLayout {
        /// <summary>Shell for menus with content (forms, selections, pages, submenus), Classic otherwise.</summary>
        Auto,

        /// <summary>One centred list, for small tools.</summary>
        Classic,

        /// <summary>A title bar with the breadcrumb, a sidebar, a content pane and a key footer, for apps.</summary>
        Shell,

        /// <summary>A list in the scrollback that does not take over the screen, for scripts.</summary>
        Inline
    }
}
