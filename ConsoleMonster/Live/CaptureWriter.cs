using System;
using System.IO;
using System.Text;

namespace ConsoleMonster.Live {
    internal sealed class CaptureWriter : TextWriter {
        private readonly object _gate = new object();
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly Action<string> _write;
        private readonly TextWriter _fallback;
        private bool _completed;
        private bool _carriageReturn;

        internal CaptureWriter(Action<string> write, TextWriter fallback) {
            _write = write;
            _fallback = fallback;
        }

        public override Encoding Encoding => _fallback.Encoding;

        public override void Write(char value) {
            lock (_gate) {
                if (_completed) { _fallback.Write(value); return; }
                Append(value);
            }
        }

        public override void Write(string? value) {
            if (value == null) { return; }
            lock (_gate) {
                if (_completed) { _fallback.Write(value); return; }
                foreach (char c in value) { Append(c); }
            }
        }

        public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));

        public override void WriteLine(string? value) {
            lock (_gate) {
                if (_completed) { _fallback.WriteLine(value); return; }
                if (value != null) { foreach (char c in value) { Append(c); } }
                Append('\n');
            }
        }

        internal void Complete() {
            lock (_gate) {
                if (_completed) { return; }
                _completed = true;
                if (_buffer.Length > 0) { Emit(); }
            }
        }

        private void Append(char value) {
            if (value == '\n') {
                if (!_carriageReturn) { Emit(); }
                _carriageReturn = false;
            } else if (value == '\r') {
                Emit();
                _carriageReturn = true;
            } else {
                _carriageReturn = false;
                _buffer.Append(value);
            }
        }

        private void Emit() {
            string line = _buffer.ToString();
            _buffer.Clear();
            _write(line);
        }
    }
}
