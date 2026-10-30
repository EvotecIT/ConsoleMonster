using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>Shows the value as plain text (brackets are literal, never markup).</summary>
    public sealed class TextPresenter : ICellPresenter {
        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            // Collections, dictionaries, data rows, XML and JSON values and secure strings are written by the shared value
            // rules (joined items, Key=Value pairs, titles, masked secrets) instead of their .NET type name.
            string raw = value != null && !(value is string) && (value is System.Collections.IEnumerable || value is System.Security.SecureString || Values.ValueShaper.IsStructural(value))
                ? Values.ValueShaper.Text(value, new Values.ShapeOptions { UseReflection = false, Culture = context.Culture })
                : PresenterValues.ToText(value, context.Culture);
            string text = raw.Replace("\r\n", " ").Replace('\n', ' ').Replace('\t', ' ');
            return new PresentedValue(StyledLine.From(text, context.Render.Style(ThemeToken.Text))) { IsEmpty = text.Length == 0 };
        }
    }
}
