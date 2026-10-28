using System;
using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// Everything a widget needs besides its own state: the terminal profile, the theme, the glyph set and the
    /// animation clock. Resolved theme styles and parsed markup are cached per context.
    /// </summary>
    public sealed class RenderContext {
        private readonly Style[] _tokenStyles;
        private readonly Dictionary<string, StyledText> _markupCache = new Dictionary<string, StyledText>(StringComparer.Ordinal);

        /// <summary>Creates a context.</summary>
        /// <param name="profile">The terminal profile.</param>
        /// <param name="theme">The theme.</param>
        /// <param name="glyphs">The glyph set; defaults to the built-in set for the profile's tier.</param>
        /// <param name="tick">Animation frame counter (spinners advance with it).</param>
        /// <param name="elapsed">Time since the session started (for clocks).</param>
        public RenderContext(TerminalProfile profile, ITheme theme, IGlyphSet? glyphs = null, long tick = 0, TimeSpan elapsed = default) {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Theme = theme ?? throw new ArgumentNullException(nameof(theme));
            Glyphs = glyphs ?? GlyphSet.For(profile.Glyphs);
            Tick = tick;
            Elapsed = elapsed;
            _tokenStyles = new Style[(int)ThemeToken.Gap + 1];
            for (int i = 0; i < _tokenStyles.Length; i++) {
                _tokenStyles[i] = theme.Resolve((ThemeToken)i, profile.ColorDepth);
            }
        }

        /// <summary>The terminal profile.</summary>
        public TerminalProfile Profile { get; }

        /// <summary>The theme.</summary>
        public ITheme Theme { get; }

        /// <summary>The glyph set.</summary>
        public IGlyphSet Glyphs { get; }

        /// <summary>The colour depth being rendered.</summary>
        public ColorDepth Depth => Profile.ColorDepth;

        /// <summary>Whether any colour is available.</summary>
        public bool HasColor => Profile.ColorDepth != ColorDepth.None;

        /// <summary>The culture for numbers and dates: <see cref="TerminalProfile.Culture"/>, or the current culture when that is null.</summary>
        public System.Globalization.CultureInfo Culture => Profile.Culture ?? System.Globalization.CultureInfo.CurrentCulture;

        /// <summary>The built-in widget words for <see cref="TerminalProfile.Language"/> (or the default language).</summary>
        public Localization.WidgetLabels Labels => _labels ?? (_labels = Localization.WidgetLabels.For(Profile.Language));

        private Localization.WidgetLabels? _labels;

        /// <summary>Animation frame counter.</summary>
        public long Tick { get; }

        /// <summary>Time since the session started.</summary>
        public TimeSpan Elapsed { get; }

        /// <summary>The ellipsis for this glyph tier.</summary>
        public string Ellipsis => Glyphs.Get(Glyph.Ellipsis);

        /// <summary>Returns the resolved style of a theme token.</summary>
        /// <param name="token">The token.</param>
        /// <returns>The style for the current colour depth.</returns>
        public Style Style(ThemeToken token) => _tokenStyles[(int)token];

        /// <summary>Returns the style for a status.</summary>
        /// <param name="status">The status.</param>
        /// <returns>The style.</returns>
        public Style Style(StatusKind status) => _tokenStyles[(int)status.ToToken()];

        /// <summary>Returns the chart series style for an index (wraps after six).</summary>
        /// <param name="index">Zero-based series index.</param>
        /// <returns>The style.</returns>
        public Style Series(int index) => _tokenStyles[(int)ThemeToken.Series1 + (Math.Abs(index) % 6)];

        /// <summary>Parses markup with this context's theme and colour depth (cached).</summary>
        /// <param name="markup">The markup.</param>
        /// <returns>The styled text.</returns>
        public StyledText Markup(string markup) {
            if (markup == null) {
                return StyledText.FromPlain(string.Empty);
            }
            if (!_markupCache.TryGetValue(markup, out StyledText? text)) {
                text = global::ConsoleMonster.Glyphs.SymbolFallback.Apply(Text.Markup.Parse(markup, Theme, Depth), Glyphs.Tier, Profile.WidthProfile);
                if (_markupCache.Count < 4096) {
                    _markupCache[markup] = text;
                }
            }
            return text;
        }

        /// <summary>Returns a context for the next animation frame, keeping the caches.</summary>
        /// <param name="tick">The new tick.</param>
        /// <param name="elapsed">The new elapsed time.</param>
        /// <returns>A context sharing profile, theme and glyphs.</returns>
        public RenderContext WithClock(long tick, TimeSpan elapsed) {
            RenderContext next = new RenderContext(Profile, Theme, Glyphs, tick, elapsed, _tokenStyles, _markupCache);
            return next;
        }

        private RenderContext(TerminalProfile profile, ITheme theme, IGlyphSet glyphs, long tick, TimeSpan elapsed, Style[] tokenStyles, Dictionary<string, StyledText> markupCache) {
            Profile = profile;
            Theme = theme;
            Glyphs = glyphs;
            Tick = tick;
            Elapsed = elapsed;
            _tokenStyles = tokenStyles;
            _markupCache = markupCache;
        }
    }
}
