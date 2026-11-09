using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    public sealed partial class Theme {
        /// <summary>The source scheme, when this theme was derived from a terminal scheme.</summary>
        public TerminalScheme? Scheme { get; private set; }
        /// <summary>Whether the source scheme uses the accessible derivation.</summary>
        public bool IsAccessible { get; private set; }
        /// <summary>Tokens whose colours were moved in OKLCH to meet contrast or colour-vision requirements.</summary>
        public IReadOnlyList<ThemeToken> AdjustedTokens { get; private set; } = Array.Empty<ThemeToken>();
        /// <summary>Whether this theme follows live terminal palette indexes.</summary>
        public bool FollowsTerminal { get; private set; }

        private Theme Copy(string name, ThemeStyle[] styles, ThemeAppearance appearance) => new Theme(name, styles, appearance) {
            Scheme = Scheme, IsAccessible = IsAccessible, AdjustedTokens = AdjustedTokens, FollowsTerminal = FollowsTerminal, _schemeOverrides = _schemeOverrides
        };

        internal Theme WithSchemeMetadata(Theme source) {
            Scheme = source.Scheme; IsAccessible = source.IsAccessible; AdjustedTokens = source.AdjustedTokens; FollowsTerminal = source.FollowsTerminal; _schemeOverrides = source._schemeOverrides;
            return this;
        }

        /// <summary>Derives semantic roles from one scheme. Faithful colours are chosen without adjustment; accessibility uses ThemeCheck's measurements.</summary>
        public static Theme FromScheme(TerminalScheme scheme, bool accessible = false) {
            if (scheme == null) { throw new ArgumentNullException(nameof(scheme)); }
            bool light = ThemeCheck.Contrast(scheme.Background, Color.FromHex("#000000")) > ThemeCheck.Contrast(scheme.Background, Color.FromHex("#ffffff"));
            ThemeColor Rgb(Color color) => new ThemeColor(color, color.Downsample(ColorDepth.Ansi16).Index);
            Color Role(int index) {
                int first = light ? index + 8 : index, second = light ? index : index + 8;
                if (ThemeCheck.Contrast(scheme.Palette[first], scheme.Background) >= 3) { return scheme.Palette[first]; }
                if (ThemeCheck.Contrast(scheme.Palette[second], scheme.Background) >= 3) { return scheme.Palette[second]; }
                return scheme.Foreground;
            }
            Color muted = new[] { scheme.Palette[8], scheme.Palette[7], scheme.Foreground }
                .FirstOrDefault(color => ThemeCheck.Contrast(color, scheme.Background) >= 3);
            if (muted.Kind != ColorKind.Rgb) { muted = scheme.Foreground; }
            ThemeColor foreground = Rgb(scheme.Foreground), background = Rgb(scheme.Background);
            ThemeColor blue = Rgb(Role(4)), red = Rgb(Role(1)), cyan = Rgb(Role(6));
            Color surfaceColor = Color.FromRgb(Mix(scheme.Background.R, scheme.Foreground.R), Mix(scheme.Background.G, scheme.Foreground.G), Mix(scheme.Background.B, scheme.Foreground.B));
            ThemeColor surface = Rgb(surfaceColor), selection = Rgb(scheme.Selection ?? Role(4));
            Dictionary<ThemeToken, ThemeStyle> styles = new Dictionary<ThemeToken, ThemeStyle>();
            foreach (ThemeToken token in AllTokens()) { styles[token] = new ThemeStyle(foreground); }
            foreach (ThemeToken token in new[] { ThemeToken.Title, ThemeToken.Header, ThemeToken.Strong }) { styles[token] = new ThemeStyle(foreground, null, TextDecoration.Bold); }
            foreach (ThemeToken token in new[] { ThemeToken.Muted, ThemeToken.Border, ThemeToken.Skip, ThemeToken.Pending, ThemeToken.Low }) { styles[token] = new ThemeStyle(Rgb(muted)); }
            styles[ThemeToken.Subtle] = new ThemeStyle(Rgb(scheme.Palette[8]), null, TextDecoration.Dim);
            styles[ThemeToken.Pass] = new ThemeStyle(Rgb(Role(2)));
            styles[ThemeToken.Warn] = new ThemeStyle(Rgb(Role(3)));
            styles[ThemeToken.Fail] = new ThemeStyle(red);
            styles[ThemeToken.Info] = styles[ThemeToken.Running] = new ThemeStyle(cyan);
            styles[ThemeToken.Accent] = new ThemeStyle(Rgb(scheme.SignatureAccent ?? Role(4)));
            styles[ThemeToken.Accent2] = new ThemeStyle(Rgb(Role(5)));
            styles[ThemeToken.Highlight] = styles[ThemeToken.Focus] = new ThemeStyle(foreground, selection);
            styles[ThemeToken.Surface] = new ThemeStyle(foreground, surface);
            styles[ThemeToken.SurfaceAccent] = new ThemeStyle(blue, surface);
            int[] series = { 4, 3, 5, 6, 2, 1 };
            for (int i = 0; i < series.Length; i++) { styles[ThemeToken.Series1 + i] = new ThemeStyle(Rgb(Role(series[i]))); }
            styles[ThemeToken.Critical] = new ThemeStyle(background, red);
            styles[ThemeToken.Gap] = new ThemeStyle(background);
            Theme theme = new Theme(scheme.Name + (accessible ? " · accessible" : ""), styles,
                new ThemeAppearance(background: light ? ThemeBackground.Light : ThemeBackground.Dark)) { Scheme = scheme, IsAccessible = accessible };
            return accessible ? AccessibleScheme(theme) : theme;
        }

        private static byte Mix(byte background, byte foreground) => (byte)Math.Round(background + (foreground - background) * 0.08);
    }
}
