using System;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Maps the shared <see cref="Tone"/> to theme tokens, and the domain facts (<see cref="StatusKind"/>,
    /// <see cref="Severity"/>) to the tone they are painted with. A named tone resolves to the theme token of that name
    /// (<c>accent</c>, <c>series2</c>, <c>gap</c>); an unknown name paints as neutral text.
    /// </summary>
    public static class Tones {
        /// <summary>The theme token a tone paints with, or null for <see cref="Tone.None"/>.</summary>
        /// <param name="tone">The tone.</param>
        /// <returns>The token.</returns>
        public static ThemeToken? ToToken(this Tone tone) {
            switch (tone.Name) {
                case "none": return null;
                case "neutral": return ThemeToken.Text;
                case "info": return ThemeToken.Info;
                case "success": return ThemeToken.Pass;
                case "warning": return ThemeToken.Warn;
                case "danger": return ThemeToken.Fail;
                case "muted": return ThemeToken.Muted;
                default: return Enum.TryParse(tone.Name, true, out ThemeToken token) && Enum.IsDefined(typeof(ThemeToken), token) ? token : ThemeToken.Text;
            }
        }

        /// <summary>The tone of a theme token: a built-in tone where one matches, otherwise the token by name.</summary>
        /// <param name="token">The token.</param>
        /// <returns>The tone.</returns>
        public static Tone ToTone(this ThemeToken token) {
            switch (token) {
                case ThemeToken.Text: return Tone.Neutral;
                case ThemeToken.Info: return Tone.Info;
                case ThemeToken.Pass: return Tone.Success;
                case ThemeToken.Warn: return Tone.Warning;
                case ThemeToken.Fail: return Tone.Danger;
                case ThemeToken.Muted: return Tone.Muted;
                default: return Tone.Named(token.ToString().ToLowerInvariant());
            }
        }

        /// <summary>The tone a status is painted with.</summary>
        /// <param name="status">The status.</param>
        /// <returns>The tone.</returns>
        public static Tone ToTone(this StatusKind status) {
            switch (status) {
                case StatusKind.Pass: return Tone.Success;
                case StatusKind.Fail: return Tone.Danger;
                case StatusKind.Warn: return Tone.Warning;
                case StatusKind.Info:
                case StatusKind.Running: return Tone.Info;
                case StatusKind.Pending:
                case StatusKind.Skip: return Tone.Muted;
                case StatusKind.None: return Tone.None;
                default: return Tone.Neutral;
            }
        }

        /// <summary>The tone an assessment outcome is painted with.</summary>
        /// <param name="severity">The outcome.</param>
        /// <returns>The tone.</returns>
        public static Tone ToTone(this Severity severity) {
            switch (severity) {
                case Severity.Critical:
                case Severity.High: return Tone.Danger;
                case Severity.Medium:
                case Severity.CouldNotEvaluate: return Tone.Warning;
                case Severity.Low:
                case Severity.Info: return Tone.Info;
                case Severity.Passed: return Tone.Success;
                case Severity.NotEvaluated: return Tone.Muted;
                default: return Tone.Neutral;
            }
        }
    }
}
