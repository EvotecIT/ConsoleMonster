namespace ConsoleMonster.Glyphs {
    /// <summary>Semantic glyph names resolved per tier by an <see cref="IGlyphSet"/>.</summary>
    public enum Glyph {
        /// <summary>Success mark (✓ / √ / +).</summary>
        Check = 0,

        /// <summary>Failure mark (✗ / × / x).</summary>
        Cross,

        /// <summary>Warning mark (▲ / ! / !).</summary>
        Warning,

        /// <summary>Information mark.</summary>
        Info,

        /// <summary>Skipped mark (○ / ○ / -).</summary>
        Skip,

        /// <summary>Running mark (● / ► / *).</summary>
        Running,

        /// <summary>Pending mark (· / · / .).</summary>
        Pending,

        /// <summary>List bullet.</summary>
        Bullet,

        /// <summary>Right arrow (→ / → / &gt;).</summary>
        ArrowRight,

        /// <summary>Up arrow.</summary>
        ArrowUp,

        /// <summary>Down arrow.</summary>
        ArrowDown,

        /// <summary>Ellipsis (… / … / ...).</summary>
        Ellipsis,

        /// <summary>Middle-dot separator (· / · / -).</summary>
        Dot,

        /// <summary>Vertical separator for status bars (│ / │ / |).</summary>
        Separator,

        /// <summary>Chevron used between phases (› / &gt; / &gt;).</summary>
        Chevron,

        /// <summary>Full block (█ / █ / #).</summary>
        BlockFull,

        /// <summary>Light shade (░ / ░ / .).</summary>
        ShadeLight,

        /// <summary>Medium shade (▒ / ▒ / :).</summary>
        ShadeMedium,

        /// <summary>Dark shade (▓ / ▓ / #).</summary>
        ShadeDark,

        /// <summary>Upper half block (▀ / ▀ / ").</summary>
        HalfUpper,

        /// <summary>Lower half block (▄ / ▄ / _).</summary>
        HalfLower,

        /// <summary>Tree branch prefix (├── / |-- ).</summary>
        TreeBranch,

        /// <summary>Tree last-branch prefix (└── / `-- ).</summary>
        TreeLast,

        /// <summary>Tree continuation prefix (│   / |   ).</summary>
        TreeVertical,

        /// <summary>Tree blank prefix.</summary>
        TreeSpace,

        /// <summary>Horizontal rule line (─ / ─ / -).</summary>
        RuleLine,

        /// <summary>Empty progress track when colour is unavailable (░ / ░ / -).</summary>
        BarTrack
    }
}
