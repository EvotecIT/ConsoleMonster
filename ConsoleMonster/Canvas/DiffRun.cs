namespace ConsoleMonster.Canvas {
    /// <summary>A horizontal run of cells that changed between two frames.</summary>
    public readonly struct DiffRun {
        /// <summary>Creates a run.</summary>
        /// <param name="row">Row.</param>
        /// <param name="start">First column.</param>
        /// <param name="length">Number of columns.</param>
        public DiffRun(int row, int start, int length) {
            Row = row;
            Start = start;
            Length = length;
        }

        /// <summary>Row.</summary>
        public int Row { get; }

        /// <summary>First column.</summary>
        public int Start { get; }

        /// <summary>Number of columns.</summary>
        public int Length { get; }

        /// <inheritdoc />
        public override string ToString() => "row " + Row + " cols " + Start + ".." + (Start + Length - 1);
    }
}
