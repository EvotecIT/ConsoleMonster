namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Turns a raw value into what a cell shows. Presenters are shared by tables, record cards, record views, key/value
    /// lists and trees, and must stay readable without colour: status, booleans and tone rules always carry a symbol or a
    /// word, never colour alone.
    /// </summary>
    public interface ICellPresenter {
        /// <summary>Presents a value.</summary>
        /// <param name="value">The raw value (may be null).</param>
        /// <param name="context">Culture, clock, time zone, column statistics and the render context.</param>
        /// <returns>The presented value.</returns>
        PresentedValue Present(object? value, PresenterContext context);
    }
}
