using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Interactive;
using ConsoleMonster.Text;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Signals completed presentations without reading a capture from its rendering thread's caller.</summary>
    internal sealed class FrameSignalSurface : IPromptSurface {
        private readonly IPromptSurface _inner;
        private readonly object _gate = new object();
        private string _text = string.Empty;

        internal FrameSignalSurface(ScreenCapture capture) { _inner = capture.Surface; }

        public int Width => _inner.Width;
        public int Height => _inner.Height;
        public bool IsInline => _inner.IsInline;
        public bool Refresh() => _inner.Refresh();

        public void Present(CellBuffer frame, Caret? caret) {
            lock (_gate) {
                _inner.Present(frame, caret);
                _text = frame.ToPlainText();
                Monitor.PulseAll(_gate);
            }
        }

        internal void WaitForFrame(params string[] text) {
            var deadline = Stopwatch.StartNew();
            lock (_gate) {
                while (!text.All(part => _text.Contains(part))) {
                    TimeSpan remaining = TimeSpan.FromSeconds(10) - deadline.Elapsed;
                    if (remaining <= TimeSpan.Zero) {
                        throw new TimeoutException("No completed frame shows '" + string.Join("', '", text) + "'. Last frame:\n" + _text);
                    }
                    Monitor.Wait(_gate, remaining);
                }
            }
        }

        public void Complete(IReadOnlyList<StyledLine>? summary) => _inner.Complete(summary);
        public void Dispose() => _inner.Dispose();
    }

    /// <summary>Counts actual VT frame writes; the transcript is read only after the live session stops.</summary>
    internal sealed class FrameCountingWriter : StringWriter {
        private int _frames;
        internal int Frames => Volatile.Read(ref _frames);

        public override void Write(string? value) {
            base.Write(value);
            if (value != null && value.Contains("\u001b[?2026h") && value.Contains("\u001b[?2026l")) {
                Interlocked.Increment(ref _frames);
            }
        }
    }
}
