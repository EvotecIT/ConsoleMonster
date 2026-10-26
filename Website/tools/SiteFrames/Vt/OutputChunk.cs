namespace SiteFrames.Vt {
    /// <summary>Recorded UTF-8 terminal output and its source timestamp in milliseconds.</summary>
    internal readonly struct OutputChunk {
        public OutputChunk(long time, byte[] data) { Time = time; Data = data; }
        public long Time { get; }
        public byte[] Data { get; }
    }
}
