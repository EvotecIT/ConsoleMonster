using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A table cell: markup text, optionally a status (marker and word coloured by the status).</summary>
    public sealed class TableCell {
        /// <summary>Creates a cell from markup.</summary>
        /// <param name="markup">The markup.</param>
        public TableCell(string markup) {
            Markup = markup ?? string.Empty;
        }

        /// <summary>The markup.</summary>
        public string Markup { get; }

        /// <summary>The status, when this is a status cell.</summary>
        public StatusKind? Status { get; private set; }

        /// <summary>Creates a status cell: the marker and the text take the status colour; this is the row's one colour signal.</summary>
        /// <param name="status">The status.</param>
        /// <param name="text">The text (plain), or <c>null</c> for the status word.</param>
        /// <returns>The cell.</returns>
        public static TableCell ForStatus(StatusKind status, string? text = null) {
            return new TableCell(Text.Markup.Escape(text ?? status.ToWord())) { Status = status };
        }

        /// <summary>Converts a string (markup) to a cell.</summary>
        /// <param name="markup">The markup.</param>
        public static implicit operator TableCell(string markup) => new TableCell(markup);
    }
}
