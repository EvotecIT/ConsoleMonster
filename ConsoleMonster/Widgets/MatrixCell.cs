using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A matrix observation. An absent cell is distinct from a passing observation.</summary>
    public sealed class MatrixCell {
        internal MatrixCell(StatusKind? status, string? text, bool diagonal) {
            Status = status;
            Text = text;
            Diagonal = diagonal;
        }

        /// <summary>The supplied status, or null for no observation.</summary>
        public StatusKind? Status { get; }

        /// <summary>Optional short plain text shown after the status glyph.</summary>
        public string? Text { get; }

        /// <summary>Whether this cell represents a self relationship instead of an observation.</summary>
        public bool Diagonal { get; }
    }
}
