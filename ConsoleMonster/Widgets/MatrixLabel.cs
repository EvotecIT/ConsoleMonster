using System;

namespace ConsoleMonster.Widgets {
    /// <summary>An axis label and optional band in a <see cref="Matrix"/>.</summary>
    public sealed class MatrixLabel {
        /// <summary>Creates a plain text axis label.</summary>
        /// <param name="text">The full label.</param>
        /// <param name="band">An optional contiguous band label.</param>
        public MatrixLabel(string text, string? band = null) {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Band = band;
        }

        /// <summary>The full label, treated literally.</summary>
        public string Text { get; }

        /// <summary>An optional shorter label for medium widths; null uses the full label.</summary>
        public string? ShortText { get; set; }

        /// <summary>The band label, treated literally; adjacent equal bands share a heading.</summary>
        public string? Band { get; }
    }
}
