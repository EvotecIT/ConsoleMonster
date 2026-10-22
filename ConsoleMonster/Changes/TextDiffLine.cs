namespace ConsoleMonster.Changes {
    /// <summary>One unchanged, removed or added line, with its original one-based line numbers.</summary>
    public sealed class TextDiffLine {
        internal TextDiffLine(ChangeKind kind, string text, int? referenceLineNumber, int? differenceLineNumber, bool hasLineEnding) {
            Kind = kind;
            Text = text;
            ReferenceLineNumber = referenceLineNumber;
            DifferenceLineNumber = differenceLineNumber;
            HasLineEnding = hasLineEnding;
        }

        /// <summary>Whether this line is unchanged, removed or added.</summary>
        public ChangeKind Kind { get; }

        /// <summary>The original line content, without its line ending.</summary>
        public string Text { get; }

        /// <summary>The original reference line number, or null for an added line.</summary>
        public int? ReferenceLineNumber { get; }

        /// <summary>The original difference line number, or null for a removed line.</summary>
        public int? DifferenceLineNumber { get; }

        /// <summary>Whether the source line has a line ending. CRLF, LF and CR are compared as equivalent endings.</summary>
        public bool HasLineEnding { get; }
    }
}
