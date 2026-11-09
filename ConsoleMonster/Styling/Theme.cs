using System;
using System.Collections.Generic;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// The built-in <see cref="ITheme"/> implementation: a table of <see cref="ThemeStyle"/> per token.
    /// Themes are immutable; <see cref="With(ThemeToken, ThemeStyle)"/> returns a modified copy.
    /// </summary>
    public sealed partial class Theme : ITheme {
        private static readonly Dictionary<string, ThemeToken> TokenNames = BuildTokenNames();
        private readonly ThemeStyle[] _styles;

        /// <summary>Creates a theme from a complete token table.</summary>
        /// <param name="name">The theme name.</param>
        /// <param name="styles">Styles per token; tokens without an entry render with the terminal default.</param>
        public Theme(string name, IReadOnlyDictionary<ThemeToken, ThemeStyle> styles) : this(name, styles, null) { }

        /// <summary>Creates a theme from a complete token table and a style.</summary>
        /// <param name="name">The theme name.</param>
        /// <param name="styles">Styles per token; tokens without an entry render with the terminal default.</param>
        /// <param name="appearance">Frames, status markers and background; null uses <see cref="ThemeAppearance.Default"/>.</param>
        public Theme(string name, IReadOnlyDictionary<ThemeToken, ThemeStyle> styles, ThemeAppearance? appearance) {
            Appearance = appearance ?? ThemeAppearance.Default;
            if (styles == null) {
                throw new ArgumentNullException(nameof(styles));
            }
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _styles = new ThemeStyle[TokenCount];
            foreach (KeyValuePair<ThemeToken, ThemeStyle> pair in styles) {
                _styles[(int)pair.Key] = pair.Value;
            }
        }

        private Theme(string name, ThemeStyle[] styles, ThemeAppearance appearance) {
            Name = name;
            _styles = styles;
            Appearance = appearance;
        }

        private static int TokenCount => (int)ThemeToken.Gap + 1;

        /// <summary>Enumerates the canonical token table without runtime enum-array generation, including on NativeAOT.</summary>
        internal static IEnumerable<ThemeToken> AllTokens() {
            for (int index = 0; index < TokenCount; index++) { yield return (ThemeToken)index; }
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>The style beside the colours: frames, status markers and the background the theme is made for.</summary>
        public ThemeAppearance Appearance { get; }

        /// <summary>Returns a copy of the theme with another style.</summary>
        /// <param name="appearance">The style.</param>
        /// <returns>A new theme.</returns>
        public Theme WithAppearance(ThemeAppearance appearance) =>
            new Theme(Name, (ThemeStyle[])_styles.Clone(), appearance ?? throw new ArgumentNullException(nameof(appearance)));

        /// <summary>Returns the definition of a token.</summary>
        /// <param name="token">The token.</param>
        /// <returns>Its theme style.</returns>
        public ThemeStyle Get(ThemeToken token) => _styles[(int)token];

        /// <inheritdoc />
        public Style Resolve(ThemeToken token, ColorDepth depth) => _styles[(int)token].Resolve(depth);

        /// <inheritdoc />
        public bool TryResolve(string name, ColorDepth depth, out Style style) {
            if (name != null && TryParseToken(name, out ThemeToken token)) {
                style = Resolve(token, depth);
                return true;
            }
            style = Style.Plain;
            return false;
        }

        /// <summary>Returns a copy of the theme with one token replaced.</summary>
        /// <param name="token">The token.</param>
        /// <param name="style">The new definition.</param>
        /// <returns>A new theme.</returns>
        public Theme With(ThemeToken token, ThemeStyle style) {
            ThemeStyle[] copy = (ThemeStyle[])_styles.Clone();
            copy[(int)token] = style;
            return new Theme(Name, copy, Appearance);
        }

        /// <summary>Returns a copy of the theme under a new name.</summary>
        /// <param name="name">The name.</param>
        /// <returns>A new theme.</returns>
        public Theme Rename(string name) => new Theme(name ?? throw new ArgumentNullException(nameof(name)), (ThemeStyle[])_styles.Clone(), Appearance);

        /// <summary>Parses a token name case-insensitively (<c>pass</c>, <c>Muted</c>, <c>series3</c>).</summary>
        /// <param name="name">The name.</param>
        /// <param name="token">The token.</param>
        /// <returns>Whether the name is a known token.</returns>
        public static bool TryParseToken(string name, out ThemeToken token) {
            return TokenNames.TryGetValue(name.Trim(), out token);
        }

        /// <summary>Returns a built-in theme by name (Dark, Light, HighContrast, ColorBlindSafe or ColorBlindSafeLight), or <see cref="Dark"/>.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The theme.</returns>
        public static Theme FromName(string? name) {
            switch ((name ?? string.Empty).Trim().ToLowerInvariant()) {
                case "light":
                    return Light;
                case "highcontrast":
                case "high-contrast":
                case "contrast":
                    return HighContrast;
                case "colorblindsafe":
                case "color-blind-safe":
                case "colorblindsafedark":
                case "color-blind-safe-dark":
                    return ColorBlindSafe;
                case "colorblindsafelight":
                case "color-blind-safe-light":
                    return ColorBlindSafeLight;
                default:
                    return Dark;
            }
        }

        private static Dictionary<string, ThemeToken> BuildTokenNames() {
            Dictionary<string, ThemeToken> names = new Dictionary<string, ThemeToken>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < TokenCount; i++) {
                ThemeToken token = (ThemeToken)i;
                names[token.ToString()] = token;
            }
            return names;
        }
    }
}
