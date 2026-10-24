namespace ConsoleMonster.Export {
    /// <summary>The natural pixel dimensions of an exported terminal window, including padding and chrome.</summary>
    public readonly struct ExportFrameSize {
        internal ExportFrameSize(double width, double height) { Width = width; Height = height; }
        /// <summary>Width in pixels.</summary>
        public double Width { get; }
        /// <summary>Height in pixels.</summary>
        public double Height { get; }
    }
}
