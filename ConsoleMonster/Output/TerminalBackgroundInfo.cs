namespace ConsoleMonster.Output {

    /// <summary>Describes the detected terminal background colour and its source.</summary>
    public sealed class TerminalBackgroundInfo {
        internal TerminalBackgroundInfo(bool isLight, bool hasColor, byte red, byte green, byte blue, double? luminance, string source, string decision) {
            IsLight = isLight;
            HasColor = hasColor;
            Red = red;
            Green = green;
            Blue = blue;
            Luminance = luminance;
            Source = source;
            Decision = decision;
        }

        /// <summary>Whether the background is classified as light.</summary>
        public bool IsLight { get; }
        /// <summary>Whether an RGB colour was available.</summary>
        public bool HasColor { get; }
        /// <summary>Red channel, meaningful when <see cref="HasColor"/> is true.</summary>
        public byte Red { get; }
        /// <summary>Green channel, meaningful when <see cref="HasColor"/> is true.</summary>
        public byte Green { get; }
        /// <summary>Blue channel, meaningful when <see cref="HasColor"/> is true.</summary>
        public byte Blue { get; }
        /// <summary>Relative luminance when an RGB colour was available.</summary>
        public double? Luminance { get; }
        /// <summary>Detection source: console color table, OSC 11, COLORFGBG, or unknown.</summary>
        public string Source { get; }
        /// <summary>Human-readable explanation of the detection result.</summary>
        public string Decision { get; }
    }
}
