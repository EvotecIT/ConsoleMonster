namespace ConsoleMonster.Styling {
    /// <summary>
    /// Semantic style tokens. Widgets only ask for tokens, never for concrete colours, so a theme
    /// (dark, light, high contrast, a brand theme) restyles everything and maps cleanly to every colour tier.
    /// Markup can use the token names directly, for example <c>[pass]OK[/]</c> or <c>[muted]12:00[/]</c>.
    /// </summary>
    public enum ThemeToken {
        /// <summary>Body text.</summary>
        Text = 0,

        /// <summary>Secondary text: timestamps, units, hints.</summary>
        Muted,

        /// <summary>Very low emphasis: empty bar tracks, guides, separators.</summary>
        Subtle,

        /// <summary>The primary accent: titles, active items, the main series.</summary>
        Accent,

        /// <summary>A secondary accent hue.</summary>
        Accent2,

        /// <summary>Success / passed.</summary>
        Pass,

        /// <summary>Failure / error.</summary>
        Fail,

        /// <summary>Warning.</summary>
        Warn,

        /// <summary>Informational.</summary>
        Info,

        /// <summary>Skipped / not applicable.</summary>
        Skip,

        /// <summary>In progress.</summary>
        Running,

        /// <summary>Queued / not started.</summary>
        Pending,

        /// <summary>Panel and table borders.</summary>
        Border,

        /// <summary>Panel titles.</summary>
        Title,

        /// <summary>Table and section headers.</summary>
        Header,

        /// <summary>Emphasised values (key figures).</summary>
        Highlight,

        /// <summary>Header and footer bars (carries a background).</summary>
        Surface,

        /// <summary>Accent text on <see cref="Surface"/>.</summary>
        SurfaceAccent,

        /// <summary>Chart series 1.</summary>
        Series1,

        /// <summary>Chart series 2.</summary>
        Series2,

        /// <summary>Chart series 3.</summary>
        Series3,

        /// <summary>Chart series 4.</summary>
        Series4,

        /// <summary>Chart series 5.</summary>
        Series5,

        /// <summary>Chart series 6.</summary>
        Series6
    }
}
