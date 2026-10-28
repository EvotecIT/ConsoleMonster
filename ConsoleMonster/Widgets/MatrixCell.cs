using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A matrix observation. An absent cell is distinct from a passing observation.</summary>
    public sealed class MatrixCell {
        internal MatrixCell(StatusKind? status, string? text, bool diagonal, string? marker = null) {
            Status = status;
            Text = text;
            Diagonal = diagonal;
            Marker = marker;
        }

        /// <summary>The supplied status, or null for no observation.</summary>
        public StatusKind? Status { get; }

        /// <summary>Optional short plain text shown after the status glyph.</summary>
        public string? Text { get; }

        /// <summary>Optional short text used in numbered grids, chosen by the data owner.</summary>
        public string? ShortText { get; set; }

        /// <summary>A caller-supplied role for evidence whose meaning is more specific than a health status.</summary>
        public ThemeToken? Tone { get; set; }

        /// <summary>An optional literal glyph replacing the status glyph without changing status, tone or counts.
        /// Use an ASCII-safe glyph when the same observation must render in every glyph tier.</summary>
        public string? Marker { get; }

        /// <summary>Whether this cell represents a self relationship instead of an observation.</summary>
        public bool Diagonal { get; }
    }
}
