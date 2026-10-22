using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleMonster.Live {
    internal sealed class CaptureWriter : TextWriter {
        // Threads inside LiveSession.SuspendCapture: their writes go to the original stream, not the log. A host that writes
        // a session's own output lines back through Console.Out (PowerShell's console host in redirected output) would
        // otherwise feed every line back into the log, where it is drawn and written again.
        [ThreadStatic]
        private static int t_passThrough;

        private readonly object _gate = new object();
        private readonly ThreadLocal<Fragment> _fragments = new ThreadLocal<Fragment>(() => new Fragment(), true);
        private readonly Action<string> _write;
        private readonly TextWriter _fallback;
        private bool _completed;

        internal CaptureWriter(Action<string> write, TextWriter fallback) {
            _write = write;
            _fallback = fallback;
        }

        internal static bool PassingThrough => t_passThrough > 0;

        internal static void EnterPassThrough() => t_passThrough++;

        internal static void ExitPassThrough() => t_passThrough = Math.Max(0, t_passThrough - 1);

        public override Encoding Encoding => _fallback.Encoding;

        public override void Write(char value) {
            lock (_gate) {
                if (_completed || PassingThrough) { _fallback.Write(value); return; }
                Append(_fragments.Value!, value);
            }
        }

        public override void Write(string? value) {
            if (value == null) { return; }
            lock (_gate) {
                if (_completed || PassingThrough) { _fallback.Write(value); return; }
                Fragment fragment = _fragments.Value!;
                foreach (char c in value) { Append(fragment, c); }
            }
        }

        public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));

        public override void WriteLine(string? value) {
            lock (_gate) {
                if (_completed || PassingThrough) { _fallback.WriteLine(value); return; }
                Fragment fragment = _fragments.Value!;
                if (value != null) { foreach (char c in value) { Append(fragment, c); } }
                Append(fragment, '\n');
            }
        }

        public override void Flush() {
            if (PassingThrough) {
                lock (_gate) { _fallback.Flush(); }
            }
        }

        internal void Complete() {
            lock (_gate) {
                if (_completed) { return; }
                _completed = true;
                // Track all values so a producer that has already exited still gets its final partial line flushed.
                foreach (Fragment fragment in _fragments.Values) {
                    if (fragment.Buffer.Length > 0) { Emit(fragment); }
                }
                _fragments.Dispose();
            }
        }

        private void Append(Fragment fragment, char value) {
            if (value == '\n') {
                if (!fragment.CarriageReturn) { Emit(fragment); }
                fragment.CarriageReturn = false;
            } else if (value == '\r') {
                Emit(fragment);
                fragment.CarriageReturn = true;
            } else {
                fragment.CarriageReturn = false;
                fragment.Buffer.Append(value);
            }
        }

        private void Emit(Fragment fragment) {
            string line = fragment.Buffer.ToString();
            fragment.Buffer.Clear();
            _write(line);
        }

        private sealed class Fragment {
            internal readonly StringBuilder Buffer = new StringBuilder();
            internal bool CarriageReturn;
        }
    }
}
