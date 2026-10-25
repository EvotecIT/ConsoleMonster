using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Invariant-preserving overloads alongside generated menu-item property methods.</summary>
    public static partial class MenuItemFluent {
        /// <summary>Makes the item open a submenu built in place.</summary>
        /// <param name="configure">Adds the submenu's items.</param>
        /// <param name="item">The menu item.</param>
        /// <returns>The item.</returns>
        public static MenuItem Submenu(this MenuItem item, Action<Menu> configure) {
            Menu menu = new Menu(item.Label);
            configure?.Invoke(menu);
            return item.Submenu(menu);
        }

        /// <summary>Makes the item open a form.</summary>
        /// <param name="form">The form.</param>
        /// <param name="submitted">Called when it is submitted.</param>
        /// <param name="item">The menu item.</param>
        /// <returns>The item.</returns>
        public static MenuItem Form(this MenuItem item, Form form, Action<MenuContext, FormResult>? submitted = null) {
            item.Form = form;
            item.OnSubmit = submitted;
            item.Kind = MenuItemKind.Form;
            return item;
        }

    }
}
