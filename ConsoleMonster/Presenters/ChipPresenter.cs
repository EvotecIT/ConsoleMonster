using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>Shows a literal category in a compact bracketed chip that remains readable without colour.</summary>
    public sealed class ChipPresenter : ICellPresenter {
        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            PresentedValue text = CellPresenters.Text.Present(value, context);
            if (text.IsEmpty) { return text; }
            return new PresentedValue(StyledLine.From("[" + text.Line.PlainText + "]", context.Render.Style(ThemeToken.SurfaceAccent)));
        }
    }
}
