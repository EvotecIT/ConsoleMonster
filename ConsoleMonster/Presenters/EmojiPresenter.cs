using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Text with <c>:shortcode:</c> emoji (<c>:rocket: Deploy</c> shows 🚀 Deploy), turned into Basic or ASCII equivalents on
    /// glyph tiers that cannot show emoji (see <see cref="SymbolFallback"/>). Opt in per column, since colons in ordinary
    /// data must stay as they are.
    /// </summary>
    public sealed class EmojiPresenter : ICellPresenter {
        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            string text = PresenterValues.ToText(value, context.Culture).Replace("\r\n", " ").Replace('\n', ' ').Replace('\t', ' ');
            text = SymbolFallback.Apply(Shortcodes.Replace(text), context.Render.Glyphs.Tier, context.Render.Profile.WidthProfile);
            return new PresentedValue(StyledLine.From(text, context.Render.Style(ThemeToken.Text))) { IsEmpty = text.Length == 0 };
        }
    }
}
