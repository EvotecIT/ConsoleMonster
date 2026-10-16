using System;

namespace ConsoleMonster.Canvas {
    /// <summary>A rectangle in buffer coordinates.</summary>
    public readonly struct Rect : IEquatable<Rect> {
        /// <summary>Creates a rectangle; negative sizes become 0.</summary>
        /// <param name="x">Left column.</param>
        /// <param name="y">Top row.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public Rect(int x, int y, int width, int height) {
            X = x;
            Y = y;
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
        }

        /// <summary>Left column.</summary>
        public int X { get; }

        /// <summary>Top row.</summary>
        public int Y { get; }

        /// <summary>Width.</summary>
        public int Width { get; }

        /// <summary>Height.</summary>
        public int Height { get; }

        /// <summary>Exclusive right edge.</summary>
        public int Right => X + Width;

        /// <summary>Exclusive bottom edge.</summary>
        public int Bottom => Y + Height;

        /// <summary>Whether the rectangle has no area.</summary>
        public bool IsEmpty => Width == 0 || Height == 0;

        /// <summary>Returns the intersection with another rectangle.</summary>
        /// <param name="other">The other rectangle.</param>
        /// <returns>The overlap (possibly empty).</returns>
        public Rect Intersect(Rect other) {
            int x = Math.Max(X, other.X);
            int y = Math.Max(Y, other.Y);
            int right = Math.Min(Right, other.Right);
            int bottom = Math.Min(Bottom, other.Bottom);
            return new Rect(x, y, right - x, bottom - y);
        }

        /// <summary>Returns the rectangle shrunk on each side.</summary>
        /// <param name="left">Left inset.</param>
        /// <param name="top">Top inset.</param>
        /// <param name="right">Right inset.</param>
        /// <param name="bottom">Bottom inset.</param>
        /// <returns>The inner rectangle.</returns>
        public Rect Inset(int left, int top, int right, int bottom) {
            return new Rect(X + left, Y + top, Width - left - right, Height - top - bottom);
        }

        /// <inheritdoc />
        public bool Equals(Rect other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Rect other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                return (((((X * 397) ^ Y) * 397) ^ Width) * 397) ^ Height;
            }
        }

        /// <inheritdoc />
        public override string ToString() => X + "," + Y + " " + Width + "x" + Height;
    }
}
