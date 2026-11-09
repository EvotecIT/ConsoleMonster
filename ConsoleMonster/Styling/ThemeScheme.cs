using System;

namespace ConsoleMonster.Styling {
    /// <summary>A named catalogue palette and the labels used by theme lists and galleries.</summary>
    public sealed class ThemeScheme {
        private readonly Lazy<Theme> _faithful;
        private readonly Lazy<Theme> _accessible;

        internal ThemeScheme(string name, string family, ThemeBackground variant, string displayName, TerminalScheme scheme) {
            Name = name; Family = family; Variant = variant; DisplayName = displayName; Scheme = scheme;
            _faithful = new Lazy<Theme>(() => Theme.FromScheme(Scheme));
            _accessible = new Lazy<Theme>(() => Theme.FromScheme(Scheme, accessible: true));
        }

        internal Theme Derive(bool accessible) => (accessible ? _accessible : _faithful).Value;

        /// <summary>The explicit variant name accepted by Theme.FromName, settings and BaseScheme theme files.</summary>
        public string Name { get; }
        /// <summary>The family name. Choosing a family selects its variant for the detected terminal background.</summary>
        public string Family { get; }
        /// <summary>The background this palette is designed for.</summary>
        public ThemeBackground Variant { get; }
        /// <summary>The upstream name to show in a gallery; use Name when storing an explicit choice.</summary>
        public string DisplayName { get; }
        /// <summary>The immutable upstream palette for faithful or accessible derivation with Theme.FromScheme.</summary>
        public TerminalScheme Scheme { get; }
    }
}
