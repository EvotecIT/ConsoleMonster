using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Content shown in a menu's content area or on its own: a submenu list, a form, a selection, a page, a result. The
    /// host draws the frame (title bar, sidebar, footer); the pane draws its content and declares its keys.
    /// </summary>
    internal interface IPane {
        string Title { get; }

        /// <summary>Muted text beside the title (a page counter, a selection count).</summary>
        string? Caption { get; }

        IWidget Build(ViewFrame frame, bool focused);

        void AddBindings(List<KeyBinding> bindings, ViewFrame frame);

        bool HandleUnbound(InputEvent input);

        bool Poll(TimeSpan now);

        bool IsAnimating { get; }

        /// <summary>Whether leaving would lose changes.</summary>
        bool IsDirty { get; }

        /// <summary>Esc inside the pane: closes an editor or goes back a page. Returns false when the host should go back.</summary>
        bool Back();

        /// <summary>Breadcrumb parts below the pane's title (an open editor, the form page).</summary>
        IReadOnlyList<string> Crumbs { get; }
    }
}
