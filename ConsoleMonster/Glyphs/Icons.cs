namespace ConsoleMonster.Glyphs {
    /// <summary>
    /// The icon catalog keys ConsoleMonster draws, resolved per glyph tier by
    /// <see cref="GlyphSetExtensions.Icon(IGlyphSet, Icon)"/>: a shape that reads well in modern fonts (Full), a WGL4
    /// fallback for Consolas and Lucida Console (Basic), and a printable ASCII character (Ascii). Every icon is one cell
    /// wide in every tier. Other engines bind the same keys to their own artwork; a key ConsoleMonster does not know
    /// (including one from another pack, such as <c>tabler:server</c>) draws the plain item mark.
    /// </summary>
    public static class Icons {
        /// <summary>A plain item mark (• / • / *): <c>item</c>.</summary>
        public static Icon Item => Icon.Named("item");

        /// <summary>Targets: servers, computers or other things work runs on (● / ● / o): <c>targets</c>.</summary>
        public static Icon Targets => Icon.Named("targets");

        /// <summary>Tasks: what to run (■ / ■ / #): <c>tasks</c>.</summary>
        public static Icon Tasks => Icon.Named("tasks");

        /// <summary>Settings and options (◆ / ♦ / *): <c>settings</c>.</summary>
        public static Icon Settings => Icon.Named("settings");

        /// <summary>Favourites and saved presets (★ / ☼ / *): <c>favorite</c>.</summary>
        public static Icon Favorite => Icon.Named("favorite");

        /// <summary>Run, start or review and run (► / ► / &gt;): <c>run</c>.</summary>
        public static Icon Run => Icon.Named("run");

        /// <summary>Back or previous (◄ / ◄ / &lt;): <c>back</c>.</summary>
        public static Icon Back => Icon.Named("back");

        /// <summary>Lists, history and logs (≡ / ≡ / =): <c>list</c>.</summary>
        public static Icon List => Icon.Named("list");

        /// <summary>Help (? / ? / ?): <c>help</c>.</summary>
        public static Icon Help => Icon.Named("help");

        /// <summary>Something that needs attention (▲ / ! / !), the warn status symbol: <c>warning</c>.</summary>
        public static Icon Warning => Icon.Named("warning");

        /// <summary>Done or verified (✓ / √ / +), the pass status symbol: <c>done</c>.</summary>
        public static Icon Done => Icon.Named("done");

        /// <summary>Exit or close (× / × / x): <c>exit</c>.</summary>
        public static Icon Exit => Icon.Named("exit");

        /// <summary>Every key in this catalog, in the order above.</summary>
        public static System.Collections.Generic.IReadOnlyList<Icon> All { get; } = new[] { Item, Targets, Tasks, Settings, Favorite, Run, Back, List, Help, Warning, Done, Exit };
    }
}
