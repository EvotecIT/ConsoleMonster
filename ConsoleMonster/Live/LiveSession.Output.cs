using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        private LogRegion? _captureLog;
        private IWidget? _captureRoot;
        private int _captureHeight;
        private bool _outputFinished;

        // Dispatcher-only routing: stream output is progressive; live output preserves each complete write for scrollback.
        private void RouteConsoleWrite(ConsoleWriteBlock write) {
            if (_output != null && !_output.SupportsLiveFrames) {
                lock (_outputGate) { write.Write(_output); }
            } else {
                _unroutedLines.Enqueue(write);
            }
        }

        // Rejected one-shot writes still share the session's physical output lock until its final output is complete.
        // Before restoration, stream writes use that output and live writes wait for scrollback. Restored but not yet
        // finished output keeps late blocks for the final flush. Never invoke Terminal's normal writer under this lock:
        // it serializes writer callbacks, which can themselves request a session snapshot.
        internal bool TryWriteFallback(ConsoleWriteBlock write) {
            lock (_lifecycle) {
                if (!_renderExited && (_cleanupDeferred || _output?.SupportsLiveFrames == true)) {
                    // Live writes already wait for scrollback; user rendering can still hold _outputGate even before
                    // a stopping caller times out. Admit without waiting for it. Keeping the exit
                    // check and enqueue under _lifecycle makes this precede the render thread's final cleanup.
                    _unroutedLines.Enqueue(write);
                    return true;
                }
            }
            lock (_outputGate) {
                if (_outputFinished || _output == null) { return false; }
                if (_restored) { _unroutedLines.Enqueue(write); }
                else { RouteConsoleWrite(write); }
                return true;
            }
        }

        private void RouteUnclaimedLog(LiveEvent liveEvent) {
            StyledText text;
            bool literalFallback = false;
            try {
                text = liveEvent.IsMarkup ? _context!.Markup(liveEvent.Text!) : StyledText.FromPlain(liveEvent.Text);
            } catch (System.FormatException ex) {
                System.Threading.Interlocked.Increment(ref _applyErrors);
                Error(ex);
                literalFallback = true;
                text = StyledText.FromPlain(liveEvent.Text);
            }
            if (_output != null && _output.SupportsLiveFrames && _options.Mode == LiveMode.FullScreen) {
                if (_captureLog == null) {
                    _captureLog = new LogRegion { PreferredHeight = 6, ShowTime = false, Wrap = true };
                }
                if (literalFallback) {
                    _captureLog.Write(liveEvent.Text!, liveEvent.State ?? "info", System.TimeSpan.FromMilliseconds(liveEvent.Time));
                } else { _captureLog.Apply(liveEvent); }
            }
            RouteConsoleWrite(new ConsoleWriteBlock(text.Lines, true));
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
            Rendering.InlineOutput? inline = _output as Rendering.InlineOutput;
            inline?.BeginWriteBatch();
            try {
                int count = _unroutedLines.Count;
                for (int i = 0; i < count && _unroutedLines.TryDequeue(out ConsoleWriteBlock? write); i++) { write.Write(_output!); }
            } finally { inline?.EndWriteBatch(); }
        }
    }
}
