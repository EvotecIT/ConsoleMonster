using System;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// The space offered to a widget during measurement. Print-once output offers an unbounded height;
    /// live output offers the screen.
    /// </summary>
    public readonly struct Constraints {
        /// <summary>Marker for an unbounded dimension.</summary>
        public const int Unbounded = int.MaxValue;

        /// <summary>Creates constraints.</summary>
        /// <param name="maxWidth">Maximum width in cells.</param>
        /// <param name="maxHeight">Maximum height in cells, or <see cref="Unbounded"/>.</param>
        public Constraints(int maxWidth, int maxHeight = Unbounded) {
            MaxWidth = Math.Max(0, maxWidth);
            MaxHeight = Math.Max(0, maxHeight);
        }

        /// <summary>Maximum width in cells.</summary>
        public int MaxWidth { get; }

        /// <summary>Maximum height in cells, or <see cref="Unbounded"/>.</summary>
        public int MaxHeight { get; }

        /// <summary>Whether the height is unbounded.</summary>
        public bool IsHeightUnbounded => MaxHeight == Unbounded;

        /// <summary>Returns constraints reduced by padding or borders.</summary>
        /// <param name="horizontal">Cells to remove from the width.</param>
        /// <param name="vertical">Cells to remove from the height (ignored when unbounded).</param>
        /// <returns>The reduced constraints.</returns>
        public Constraints Shrink(int horizontal, int vertical) {
            return new Constraints(MaxWidth - horizontal, IsHeightUnbounded ? Unbounded : MaxHeight - vertical);
        }

        /// <summary>Returns constraints with a different maximum width.</summary>
        /// <param name="maxWidth">The width.</param>
        /// <returns>New constraints.</returns>
        public Constraints Width(int maxWidth) => new Constraints(maxWidth, MaxHeight);

        /// <summary>Returns constraints with a different maximum height.</summary>
        /// <param name="maxHeight">The height.</param>
        /// <returns>New constraints.</returns>
        public Constraints Height(int maxHeight) => new Constraints(MaxWidth, maxHeight);

        /// <inheritdoc />
        public override string ToString() => MaxWidth + "x" + (IsHeightUnbounded ? "inf" : MaxHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
