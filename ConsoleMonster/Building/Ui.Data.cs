using System;
using System.Collections.Generic;
using ConsoleMonster.Building;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster {
    public static partial class Ui {
        /// <summary>A table over a primary sequence, preserving row identity and typed column selectors.</summary>
        /// <typeparam name="T">The row type.</typeparam><param name="items">The primary row sequence.</param><returns>The typed table.</returns>
        public static Table<T> Table<
#if NET8_0_OR_GREATER
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEnumerable<T> items) => Widgets.Table.For(items);


        /// <summary>Creates an empty review model for a typed question workflow.</summary><returns>The review model.</returns>
        public static ReviewModel Review() => new ReviewModel();

        /// <summary>Creates a rich-text composer.</summary><returns>The composer.</returns>
        public static RichTextBuilder RichText() => new RichTextBuilder();

        /// <summary>Creates the C# dashboard composition helper.</summary><returns>The empty helper.</returns>
        public static DashboardBuilder DashboardLayout() => new DashboardBuilder(string.Empty);
    }
}
