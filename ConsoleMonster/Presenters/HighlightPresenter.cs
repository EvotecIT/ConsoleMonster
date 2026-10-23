using System.Collections.Generic;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Shows cell text highlighted by a language or the smart value highlighter (column option <c>Highlight = 'Smart'</c>
    /// or a language name such as <c>'Json'</c>). The text stays literal; line breaks become spaces in one cell line.
    /// </summary>
    public sealed class HighlightPresenter : ICellPresenter {
        /// <summary>Creates the presenter.</summary>
        /// <param name="highlighter">The highlighter.</param>
        public HighlightPresenter(ITextHighlighter highlighter) {
            Highlighter = highlighter ?? throw new System.ArgumentNullException(nameof(highlighter));
        }

        /// <summary>The highlighter.</summary>
        public ITextHighlighter Highlighter { get; }

        /// <summary>An optional theme tone for type tokens, for signatures whose types are secondary context.</summary>
        public Styling.ThemeToken? TypeTone { get; set; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            string text = PresenterValues.ToText(value, context.Culture);
            List<TextSpan> spans = new List<TextSpan>();
            List<StyledLine> lines = HighlightStyles.ToLines(text, Highlighter, context.Render, 0, TypeTone);
            for (int i = 0; i < lines.Count; i++) {
                if (i > 0) {
                    spans.Add(new TextSpan(" ", Styling.Style.Plain));
                }
                spans.AddRange(lines[i]);
            }
            return new PresentedValue(new StyledLine(spans)) { IsEmpty = text.Length == 0 };
        }
    }
}
