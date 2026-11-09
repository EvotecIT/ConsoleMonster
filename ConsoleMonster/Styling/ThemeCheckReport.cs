using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Styling {
    /// <summary>The accessibility rule measured by a theme check.</summary>
    public enum ThemeCheckKind {
        /// <summary>WCAG text contrast against the background actually used.</summary>
        Contrast,
        /// <summary>Status colour separation in normal and simulated colour vision.</summary>
        ColourVision,
        /// <summary>Status markers and decorations distinguish statuses without colour.</summary>
        Markers,
        /// <summary>A filled critical status has a background distinct from ordinary statuses.</summary>
        Fill
    }

    /// <summary>The outcome of an individual theme check.</summary>
    public enum ThemeCheckOutcome {
        /// <summary>The rule is satisfied.</summary>
        Pass,
        /// <summary>The rule is not satisfied.</summary>
        Fail,
        /// <summary>The colours or renderable markers needed to decide are unavailable.</summary>
        Unknown
    }

    /// <summary>One measurement, its required minimum and a plain-language verdict.</summary>
    public sealed class ThemeCheckResult {
        internal ThemeCheckResult(ThemeCheckKind check, ThemeToken[] tokens, ThemeCheckOutcome outcome, double? value, double? required, string detail, string? vision = null) {
            Check = check;
            Tokens = System.Array.AsReadOnly(tokens);
            Outcome = outcome;
            Value = value;
            Required = required;
            Detail = detail;
            Vision = vision;
        }
        /// <summary>The rule being checked.</summary>
        public ThemeCheckKind Check { get; }
        /// <summary>The token or pair of tokens measured.</summary>
        public IReadOnlyList<ThemeToken> Tokens { get; }
        /// <summary>Pass, Fail or Unknown; unknown is never evidence of a pass.</summary>
        public ThemeCheckOutcome Outcome { get; }
        /// <summary>The contrast ratio, CIELAB distance, or 1/0 for a structural rule; null when unknown.</summary>
        public double? Value { get; }
        /// <summary>The minimum acceptable value, when the rule has a measurement.</summary>
        public double? Required { get; }
        /// <summary>A one-line explanation of the outcome, including the simulation for colour-vision checks.</summary>
        public string Detail { get; }
        /// <summary>The vision condition (normal vision, protanopia, deuteranopia or tritanopia); null for other checks.</summary>
        public string? Vision { get; }
    }

    /// <summary>Accessibility measurements for a theme, independent of any report renderer.</summary>
    public sealed class ThemeCheckReport {
        internal ThemeCheckReport(string themeName, List<ThemeCheckResult> results) { ThemeName = themeName; Results = results.AsReadOnly(); }
        /// <summary>The name of the theme that was checked.</summary>
        public string ThemeName { get; }
        /// <summary>True only when every result passes. Unknown measurements prevent a confirmed pass.</summary>
        public bool Passed => Results.All(result => result.Outcome == ThemeCheckOutcome.Pass);
        /// <summary>Each rule, including failures and measurements that could not be made.</summary>
        public IReadOnlyList<ThemeCheckResult> Results { get; }
    }
}
