using System.Collections.Generic;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Shows text values as markup (<c>[pass]OK[/] [muted]3s[/]</c>) and <see cref="RichText"/> with its own styles. Data is
    /// literal everywhere else, so this presenter is the opt-in (column option <c>Markup = $true</c> or
    /// <c>Presenter = 'Markup'</c>). Invalid markup is shown literally; line breaks become spaces in one cell line.
    /// </summary>
    public sealed class MarkupPresenter : ICellPresenter {
        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            if (value is RichText rich) {
                return FromStyledText(rich.ToStyledText(context.Render), rich.PlainText.Length == 0);
            }
            string text = PresenterValues.ToText(value, context.Culture);
            if (!Markup.Validate(text, out _)) {
                return new TextPresenter().Present(value, context);
            }
            return FromStyledText(context.Render.Markup(text), text.Length == 0);
        }

        /// <summary>One cell line from styled text (lines joined with a space).</summary>
        /// <param name="text">The styled text.</param>
        /// <param name="empty">Whether the value counts as empty.</param>
        /// <returns>The presented value.</returns>
        internal static PresentedValue FromStyledText(StyledText text, bool empty) {
            StyledLine line;
            if (text.Lines.Count == 1) {
                line = text.Lines[0];
            } else {
                List<TextSpan> spans = new List<TextSpan>();
                for (int i = 0; i < text.Lines.Count; i++) {
                    if (i > 0) {
                        spans.Add(new TextSpan(" ", Styling.Style.Plain));
                    }
                    spans.AddRange(text.Lines[i]);
                }
                line = new StyledLine(spans);
            }
            return new PresentedValue(line) { IsEmpty = empty };
        }
    }
}
