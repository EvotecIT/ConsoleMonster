using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        private LogRegion? _captureLog;
        private IWidget? _captureRoot;
        private int _captureHeight;

        // Dispatcher-only routing: stream output is progressive; live output preserves each complete write for scrollback.
        private void RouteConsoleWrite(ConsoleWriteBlock write) {
            if (_output != null && !_output.SupportsLiveFrames) {
                lock (_outputGate) { write.Write(_output); }
            } else {
                _unroutedLines.Enqueue(write);
            }
        }

        // Rejected one-shot writes still share the session's physical output lock until its final output is complete.
        // Before restoration, stream writes use that output and live writes wait for scrollback; afterwards the normal
        // writer runs under the same lock. This is separate from dispatcher admission and does not count as a posted event.
        internal void WriteFallback(ConsoleWriteBlock write, Action normalWrite) {
            lock (_outputGate) {
                if (!_restored && _output != null) { RouteConsoleWrite(write); }
                else { normalWrite(); }
            }
        }

        private void RouteUnclaimedLog(LiveEvent liveEvent) {
            if (_output != null && _output.SupportsLiveFrames && _options.Mode == LiveMode.FullScreen) {
                if (_captureLog == null) {
                    _captureLog = new LogRegion { PreferredHeight = 6, ShowTime = false, Wrap = true };
                }
                _captureLog.Apply(liveEvent);
            }
            var lines = liveEvent.IsMarkup ? _context!.Markup(liveEvent.Text!).Lines : StyledText.FromPlain(liveEvent.Text).Lines;
            RouteConsoleWrite(new ConsoleWriteBlock(lines, true));
        }

        private IWidget RootForFrame(int height) {
            if (_captureLog == null) { return _root; }
            if (_captureRoot == null || _captureHeight != height) {
                _captureHeight = height;
                _captureRoot = new Rows().Add(_root, LayoutSize.Fraction(1, 1))
                    .Add(_captureLog, LayoutSize.Fixed(System.Math.Min(6, System.Math.Max(1, height / 3))));
            }
            return _captureRoot;
        }

        private void FlushUnrouted() {
            while (_unroutedLines.TryDequeue(out ConsoleWriteBlock? write)) { write.Write(_output!); }
        }
    }
}
