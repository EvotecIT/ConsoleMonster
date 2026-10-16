using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>Shows the value as plain text (brackets are literal, never markup).</summary>
    public sealed class TextPresenter : ICellPresenter {
        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            string text = PresenterValues.ToText(value, context.Culture).Replace("\r\n", " ").Replace('\n', ' ').Replace('\t', ' ');
            return new PresentedValue(StyledLine.From(text, context.Render.Style(ThemeToken.Text))) { IsEmpty = text.Length == 0 };
        }
    }
}
