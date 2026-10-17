using System;

namespace ConsoleMonster.Layout {
    /// <summary>A size in terminal cells.</summary>
    public readonly struct Size : IEquatable<Size> {
        /// <summary>Creates a size; negative values become 0.</summary>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells.</param>
        public Size(int width, int height) {
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
        }

        /// <summary>Width in cells.</summary>
        public int Width { get; }

        /// <summary>Height in cells.</summary>
        public int Height { get; }

        /// <summary>The empty size.</summary>
        public static Size Empty => default;

        /// <inheritdoc />
        public bool Equals(Size other) => Width == other.Width && Height == other.Height;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Size other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (Width * 397) ^ Height;

        /// <inheritdoc />
        public override string ToString() => Width + "x" + Height;
    }
}
