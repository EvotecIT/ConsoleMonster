namespace ConsoleMonster.Glyphs {
    /// <summary>
    /// Semantic icon names for menu items and similar marks, resolved per glyph tier by
    /// <see cref="GlyphSetExtensions.Icon(IGlyphSet, GlyphIcon)"/>: a shape that reads well in modern fonts (Full), a
    /// WGL4 fallback for Consolas and Lucida Console (Basic), and a printable ASCII character (Ascii). Every icon is one
    /// cell wide in every tier.
    /// </summary>
    public enum GlyphIcon {
        /// <summary>A plain item mark (• / • / *).</summary>
        Item = 0,

        /// <summary>Targets: servers, computers or other things work runs on (● / ● / o).</summary>
        Targets,

        /// <summary>Tasks: what to run (■ / ■ / #).</summary>
        Tasks,

        /// <summary>Settings and options (◆ / ♦ / *).</summary>
        Settings,

        /// <summary>Favourites and saved presets (★ / ☼ / *).</summary>
        Favorite,

        /// <summary>Run, start or review and run (► / ► / &gt;).</summary>
        Run,

        /// <summary>Back or previous (◄ / ◄ / &lt;).</summary>
        Back,

        /// <summary>Lists, history and logs (≡ / ≡ / =).</summary>
        List,

        /// <summary>Help (? / ? / ?).</summary>
        Help,

        /// <summary>Something that needs attention (▴ / ! / !), the warn status symbol.</summary>
        Warning,

        /// <summary>Done or verified (✓ / √ / +), the pass status symbol.</summary>
        Done,

        /// <summary>Exit or close (× / × / x).</summary>
        Exit
    }
}
