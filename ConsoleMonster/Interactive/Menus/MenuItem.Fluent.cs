using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>Invariant-preserving overloads alongside generated menu-item property methods.</summary>
    public static partial class MenuItemFluent {
        /// <summary>Sets a computed badge.</summary>
        /// <param name="badge">Returns the badge text.</param>
        /// <param name="item">The menu item.</param>
        /// <returns>The item.</returns>
        public static MenuItem Badge(this MenuItem item, Func<string?> badge) {
            item.Badge = badge == null ? null : () => badge() is string s && s.Length > 0 ? RichText.Plain(s) : null;
            return item;
        }

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

        /// <summary>Loads the choices of a selection item asynchronously instead of listing them up front.</summary>
        /// <param name="loader">Returns the choices.</param>
        /// <param name="multiple">Whether several values can be picked.</param>
        /// <param name="item">The menu item.</param>
        /// <returns>The item.</returns>
        public static MenuItem LoadChoices(this MenuItem item, Func<MenuContext, CancellationToken, Task<IEnumerable<Choice>>> loader, bool multiple = true) {
            item.LoadChoices = loader;
            item.Kind = multiple ? MenuItemKind.MultiSelect : MenuItemKind.Select;
            return item;
        }

    }
}
