namespace ConsoleMonster.Text {
    /// <summary>String helpers for markup, named as in Spectre.Console so ported code keeps its calls.</summary>
    public static class MarkupExtensions {
        /// <summary>Escapes brackets so the text is shown literally inside markup (see <see cref="Markup.Escape"/>).</summary>
        /// <param name="text">Plain text, or null.</param>
        /// <returns>Escaped text.</returns>
        public static string EscapeMarkup(this string? text) => Markup.Escape(text);

        /// <summary>Removes markup tags and resolves escapes (see <see cref="Markup.Strip"/>).</summary>
        /// <param name="markup">Markup, or null.</param>
        /// <returns>Plain text.</returns>
        public static string RemoveMarkup(this string? markup) => Markup.Strip(markup);
    }
}
