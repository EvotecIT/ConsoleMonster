using System;
using ConsoleMonster.Output;

namespace ConsoleMonster.Sessions {
    /// <summary>
    /// Writes terminal output to its usual writer and the same text to an in-process recorder, so a recording holds
    /// exactly what the session drew: composed frames with their log panels, footers and dialogs.
    /// </summary>
    internal sealed class RecordingTeeWriter : ITerminalWriter {
        private readonly ITerminalWriter _inner;
        private readonly bool _ownsInner;
        private SessionRecorder? _recorder;

        internal RecordingTeeWriter(ITerminalWriter inner, bool ownsInner, SessionRecorder recorder) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _ownsInner = ownsInner;
            _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        }

        public bool IsConsole => _inner.IsConsole;

        public void Write(string text) {
            _inner.Write(text);
            SessionRecorder? recorder = _recorder;
            if (recorder == null) { return; }
            // The caller owns the recorder; once it is finished the session keeps drawing without it.
            if (recorder.HasExited) { _recorder = null; return; }
            try { recorder.CaptureOutput(text); }
            catch (ObjectDisposedException) { _recorder = null; }
        }

        public void Flush() => _inner.Flush();

        internal void Resize(int width, int height) {
            SessionRecorder? recorder = _recorder;
            if (recorder == null || recorder.HasExited) { return; }
            try { recorder.Resize(width, height); }
            catch (ObjectDisposedException) { _recorder = null; }
        }

        public void Dispose() {
            _recorder = null;
            if (_ownsInner) { _inner.Dispose(); }
        }
    }
}
