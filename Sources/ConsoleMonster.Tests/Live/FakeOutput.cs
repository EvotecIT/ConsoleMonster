using System;
using System.Collections.Concurrent;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.Tests.Live {
    internal sealed class FakeOutput : ITerminalOutput {
        internal readonly ConcurrentQueue<string> Lines = new ConcurrentQueue<string>();
        internal readonly ConcurrentQueue<string> Calls = new ConcurrentQueue<string>();
        internal Action? Frame;
        internal Action? Ending;
        internal Action? Beginning;
        internal int Frames;
        internal int Invalidations;

        internal FakeOutput(bool live = true) { SupportsLiveFrames = live; }

        public TerminalProfile Profile { get; } = TerminalProfile.Fixed(20, 3);

        public bool SupportsLiveFrames { get; }

        public void WriteBlock(CellBuffer buffer) { Calls.Enqueue("block"); }

        public void WriteLine(StyledLine line) { Lines.Enqueue(line.PlainText); Calls.Enqueue("line"); }

        public void BeginLive() { Calls.Enqueue("begin"); Beginning?.Invoke(); }

        internal int LastFrameWidth;

        public void RenderFrame(CellBuffer frame) {
            Interlocked.Increment(ref Frames);
            LastFrameWidth = frame.Width;
            Calls.Enqueue("frame");
            Frame?.Invoke();
        }

        public void Invalidate() { Interlocked.Increment(ref Invalidations); }

        public void EndLive() { Calls.Enqueue("end"); Ending?.Invoke(); }

        public void Dispose() { Calls.Enqueue("dispose"); }
    }
}
