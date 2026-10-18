using System;
using System.IO;

namespace ConsoleMonster.Live {
    internal sealed class ConsoleCapture : IDisposable {
        private readonly TextWriter _originalOut;
        private readonly TextWriter _originalError;
        private readonly CaptureWriter _out;
        private readonly CaptureWriter _error;
        private bool _disposed;

        internal ConsoleCapture(Action<string, string> write) {
            _originalOut = Console.Out;
            _originalError = Console.Error;
            _out = new CaptureWriter(line => write(line, "out"), _originalOut);
            _error = new CaptureWriter(line => write(line, "err"), _originalError);
            Console.SetOut(_out);
            Console.SetError(_error);
        }

        public void Dispose() {
            lock (this) {
                if (_disposed) { return; }
                _disposed = true;
                // SetOut wraps writers in a synchronized adapter, so retain the originals explicitly.
                Console.SetOut(_originalOut);
                Console.SetError(_originalError);
                _out.Complete();
                _error.Complete();
            }
        }
    }
}
