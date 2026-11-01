using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A table cell: either markup text (optionally with a status marker), or a raw value that the column's presenter
    /// formats (numbers, bytes, dates, statuses, data bars...). Raw values also feed totals and tone rules.
    /// </summary>
    public sealed class TableCell {
        /// <summary>Creates a cell from markup.</summary>
        /// <param name="markup">The markup.</param>
        public TableCell(string markup) {
            Markup = markup ?? string.Empty;
        }

        private TableCell(object? value, bool raw) {
            Markup = string.Empty;
            Value = value;
            HasValue = raw;
        }

        /// <summary>The markup (empty for value cells).</summary>
        public string Markup { get; }

        /// <summary>The status, when this is a status cell.</summary>
        public StatusKind? Status { get; private set; }

        /// <summary>The assessment severity when this cell uses the assessment scale.</summary>
        public Severity? AssessmentSeverity { get; private set; }

        /// <summary>The raw value of a value cell, or null.</summary>
        public object? Value { get; }

        /// <summary>Whether this is a value cell (formatted by the column's presenter).</summary>
        public bool HasValue { get; }

        internal TableRowRole? RowRole { get; private set; }

        /// <summary>Markup holding ConsoleMonster's own placeholders, resolved for the glyph tier where the cell is drawn, or null.</summary>
        internal string? PlaceholderMarkup { get; private set; }

        internal static TableCell WithPlaceholders(string markup) =>
            new TableCell(Values.Placeholders.Resolve(markup, Capabilities.GlyphTier.Full)) { PlaceholderMarkup = markup };

        /// <summary>Returns a copy with a cell tone, retaining the raw value, status and existing emphasis.</summary>
        /// <param name="tone">The theme role supplied by the caller; it is not inferred from the numeric value.</param>
        /// <returns>A new cell; this cell is unchanged.</returns>
        public TableCell WithTone(ThemeToken tone) => WithRowRole(new TableRowRole {
            Tone = tone, Bold = RowRole?.Bold ?? false, Muted = RowRole?.Muted ?? false,
            Marker = RowRole?.Marker, AsciiMarker = RowRole?.AsciiMarker
        });

        internal TableCell WithRowRole(TableRowRole role) {
            TableCell copy = HasValue ? FromValue(Value) : new TableCell(Markup) { PlaceholderMarkup = PlaceholderMarkup };
            copy.Status = Status;
            copy.AssessmentSeverity = AssessmentSeverity;
            copy.RowRole = role;
            return copy;
        }

        /// <summary>Creates a value cell: the column's presenter formats it, and totals and tone rules read it.</summary>
        /// <param name="value">The raw value (null is an empty cell).</param>
        /// <returns>The cell.</returns>
        public static TableCell FromValue(object? value) => new TableCell(value, true);

        /// <summary>Creates a status cell: the marker and the text take the status colour; this is the row's one colour signal.</summary>
        /// <param name="status">The status.</param>
        /// <param name="text">The text (plain), or <c>null</c> for the status word.</param>
        /// <returns>The cell.</returns>
        public static TableCell ForStatus(StatusKind status, string? text = null) {
            return new TableCell(Text.Markup.Escape(text ?? status.ToWord())) { Status = status };
        }

        /// <summary>Creates a severity cell whose glyph is chosen before destination measurement.</summary>
        public static TableCell ForSeverity(Severity severity, string? text = null) =>
            new TableCell(Text.Markup.Escape(text ?? StatusScale.Word(severity))) { AssessmentSeverity = severity };

        /// <summary>Converts a string (markup) to a cell.</summary>
        /// <param name="markup">The markup.</param>
        public static implicit operator TableCell(string markup) => new TableCell(markup);
    }
}
