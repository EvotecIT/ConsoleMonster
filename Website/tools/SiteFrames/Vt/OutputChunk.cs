namespace SiteFrames.Vt {
    /// <summary>Recorded UTF-8 terminal output and its source timestamp in milliseconds.</summary>
    internal readonly struct OutputChunk {
        public OutputChunk(long time, byte[] data, System.TimeSpan hold = default) { Time = time; Data = data; Hold = hold; }
        public long Time { get; }
        public byte[] Data { get; }
        public System.TimeSpan Hold { get; }
    }
}
