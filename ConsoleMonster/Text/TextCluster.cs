namespace ConsoleMonster.Text {
    internal readonly struct TextCluster {
        internal TextCluster(int start, int length, int width) {
            Start = start;
            Length = length;
            Width = width;
        }
        internal int Start { get; }
        internal int Length { get; }
        internal int Width { get; }
    }
}
