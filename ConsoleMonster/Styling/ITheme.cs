using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Maps semantic tokens to styles per colour tier. Implement it for brand themes, or start from
    /// <see cref="Theme.Dark"/> and override tokens with <see cref="Theme.With(ThemeToken, ThemeStyle)"/>.
    /// </summary>
    public interface ITheme {
        /// <summary>The theme name.</summary>
        string Name { get; }

        /// <summary>Resolves a token to a concrete style for a colour depth.</summary>
        /// <param name="token">The token.</param>
        /// <param name="depth">The colour depth being rendered.</param>
        /// <returns>The style.</returns>
        Style Resolve(ThemeToken token, ColorDepth depth);

        /// <summary>Resolves a token by name (case-insensitive), as used in markup such as <c>[pass]</c>.</summary>
        /// <param name="name">Token name, for example <c>pass</c> or <c>muted</c>.</param>
        /// <param name="depth">The colour depth being rendered.</param>
        /// <param name="style">The resolved style.</param>
        /// <returns>Whether the name is a token of this theme.</returns>
        bool TryResolve(string name, ColorDepth depth, out Style style);
    }
}
