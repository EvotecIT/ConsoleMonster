using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A status as symbol, word and tone: <c>✓ PASS</c>, <c>✗ FAIL</c>, <c>▴ WARN</c>; <c>[FAIL]</c> on the ASCII tier. Accepts
    /// <see cref="StatusKind"/> values and status words (pass, passed, ok, fail, failed, error, warn, warning, skip...).
    /// Values that are not statuses are shown as text.
    /// </summary>
    public sealed class StatusPresenter : ICellPresenter {
        /// <summary>Creates a status presenter.</summary>
        /// <param name="showWord">Whether the word follows the symbol (default true).</param>
        public StatusPresenter(bool showWord = true) {
            ShowWord = showWord;
        }

        /// <summary>Whether the word follows the symbol.</summary>
        public bool ShowWord { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            StatusKind status;
            if (value is StatusKind kind) {
                status = kind;
            } else if (!StatusKinds.TryParse(PresenterValues.ToText(value, context.Culture), out status)) {
                PresentedValue text = new TextPresenter().Present(value, context);
                return text;
            }
            Style style = context.Render.Style(status);
            string marker = context.Render.Glyphs.StatusMarker(status);
            List<TextSpan> spans = new List<TextSpan> { new TextSpan(marker, style) };
            if (ShowWord && context.Render.Glyphs.Tier != GlyphTier.Ascii) {
                spans.Add(new TextSpan(" " + context.Render.Labels.StatusWord(status), style));
            }
            return new PresentedValue(new StyledLine(spans)) { Tone = status.ToToken() };
        }
    }
}
