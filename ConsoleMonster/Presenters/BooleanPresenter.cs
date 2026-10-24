using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A boolean as symbol and word: <c>✓ yes</c> / <c>– no</c> (<c>yes</c> / <c>no</c> on the ASCII tier). True takes
    /// the pass tone and false is muted, unless the presenter is created as a problem flag (true is bad).
    /// </summary>
    public sealed class BooleanPresenter : ICellPresenter {
        /// <summary>Creates a boolean presenter.</summary>
        /// <param name="trueText">The word for true (default <c>yes</c>).</param>
        /// <param name="falseText">The word for false (default <c>no</c>).</param>
        /// <param name="trueIsProblem">Whether true is a problem (for example <c>PasswordNeverExpires</c>): true takes the warning tone.</param>
        public BooleanPresenter(string trueText = "yes", string falseText = "no", bool trueIsProblem = false) {
            TrueText = trueText;
            FalseText = falseText;
            TrueIsProblem = trueIsProblem;
        }

        /// <summary>The word for true.</summary>
        public string TrueText { get; }

        /// <summary>The word for false.</summary>
        public string FalseText { get; }

        /// <summary>Whether true is a problem.</summary>
        public bool TrueIsProblem { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            bool? flag = value as bool?;
            if (flag == null && value is string text && bool.TryParse(text, out bool parsed)) {
                flag = parsed;
            }
            if (flag == null) {
                return new TextPresenter().Present(value, context);
            }
            bool ascii = context.Render.Glyphs.Tier == GlyphTier.Ascii;
            ThemeToken tone = flag.Value ? (TrueIsProblem ? ThemeToken.Warn : ThemeToken.Pass) : ThemeToken.Muted;
            string symbol = flag.Value ? (TrueIsProblem ? context.Render.Glyphs.StatusMarker(StatusKind.Warn) : context.Render.Glyphs.StatusMarker(StatusKind.Pass)) : context.Render.Glyphs.Get(Glyphs.Glyph.Skip);
            Style style = context.Render.Style(tone);
            StyledLine line = ascii
                ? StyledLine.From(flag.Value ? TrueText : FalseText, style)
                : new StyledLine(new[] { new TextSpan(symbol + " ", style), new TextSpan(flag.Value ? TrueText : FalseText, style) });
            return new PresentedValue(line) { Role = tone, Number = flag.Value ? 1 : 0 };
        }
    }
}
