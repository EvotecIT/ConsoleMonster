using System;
using ConsoleMonster.Glyphs;

namespace ConsoleMonster.Styling {
    /// <summary>How statuses are marked in status columns, lists, headers and messages.</summary>
    public enum StatusMarkerStyle {
        /// <summary>Symbols: ✓ ✗ ▴ (bracketed words at the ASCII glyph tier).</summary>
        Symbols = 0,
        /// <summary>Bracketed words at every glyph tier: [OK], [FAIL], [WARN]. Easier for screen readers and small symbols.</summary>
        Words
    }

    /// <summary>The page background a theme is made for.</summary>
    public enum ThemeBackground {
        /// <summary>A dark background.</summary>
        Dark = 0,
        /// <summary>A light background.</summary>
        Light
    }

    /// <summary>
    /// The style part of a theme, beside its colours: the frame drawn around panels, tables and inputs, how statuses are
    /// marked, and the background the theme is made for. A widget that names its own frame (a <see cref="BoxStyle"/> other
    /// than <see cref="BoxStyle.Rounded"/>) keeps it; the Border and StatusMarkers settings override the theme's choice.
    /// </summary>
    public sealed class ThemeAppearance : IEquatable<ThemeAppearance> {
        /// <summary>Creates a style.</summary>
        /// <param name="border">The frame for panels, tables and inputs that use the default (rounded) frame.</param>
        /// <param name="statusMarkers">How statuses are marked.</param>
        /// <param name="background">The background the theme is made for.</param>
        public ThemeAppearance(BoxStyle border = BoxStyle.Rounded, StatusMarkerStyle statusMarkers = StatusMarkerStyle.Symbols, ThemeBackground background = ThemeBackground.Dark) {
            if (border == BoxStyle.None) { throw new ArgumentException("A theme's frame cannot be None; widgets choose to draw no frame themselves.", nameof(border)); }
            Border = border;
            StatusMarkers = statusMarkers;
            Background = background;
        }

        /// <summary>Rounded frames, symbols and a dark background.</summary>
        public static ThemeAppearance Default { get; } = new ThemeAppearance();

        /// <summary>The frame for panels, tables and inputs that use the default (rounded) frame.</summary>
        public BoxStyle Border { get; }

        /// <summary>How statuses are marked.</summary>
        public StatusMarkerStyle StatusMarkers { get; }

        /// <summary>The background the theme is made for; exports use it for the page.</summary>
        public ThemeBackground Background { get; }

        /// <summary>Whether frames and markers are the defaults (the background may differ).</summary>
        internal bool DrawsAsDefault => Border == BoxStyle.Rounded && StatusMarkers == StatusMarkerStyle.Symbols;

        /// <inheritdoc />
        public bool Equals(ThemeAppearance? other) =>
            other != null && Border == other.Border && StatusMarkers == other.StatusMarkers && Background == other.Background;

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as ThemeAppearance);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Border * 31 + (int)StatusMarkers) * 31 + (int)Background;
    }
}
