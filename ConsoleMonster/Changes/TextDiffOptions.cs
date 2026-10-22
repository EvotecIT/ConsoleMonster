using System;

namespace ConsoleMonster.Changes {
    /// <summary>Controls text comparison and the amount of unchanged context shown by a text diff.</summary>
    public sealed class TextDiffOptions {
        private int _context = 3;

        /// <summary>Unchanged lines retained on each side of a change (default three). Must be nonnegative.</summary>
        public int Context {
            get => _context;
            set => _context = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Ignores Unicode whitespace within lines. Line boundaries and a missing final newline still matter.</summary>
        public bool IgnoreWhitespace { get; set; }

        /// <summary>The plain original-text label in the widget and unified header (default Before).</summary>
        public string ReferenceLabel { get; set; } = "Before";

        /// <summary>The plain new-text label in the widget and unified header (default After).</summary>
        public string DifferenceLabel { get; set; } = "After";
    }
}
