using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {

    /// <summary>Buffers UTF-16 text and writes directly to the Windows stdout console, independent of its code page.</summary>
    public sealed class ConsoleHandleWriter : ITerminalWriter {
        private readonly IntPtr _handle;
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly object _gate = new object();
        private bool _disposed;

        /// <summary>Creates a writer for the process stdout console handle.</summary>
        /// <exception cref="PlatformNotSupportedException">The process is not running on Windows.</exception>
        /// <exception cref="IOException">Stdout is not a console handle.</exception>
        public ConsoleHandleWriter() {
            if (!WindowsConsole.IsWindows) {
                throw new PlatformNotSupportedException("WriteConsoleW requires Windows.");
            }
            _handle = WindowsConsole.StandardOutput;
            if (!WindowsConsole.TryGetMode(_handle, out uint _)) {
                throw new IOException("Standard output is not a Windows console handle.");
            }
        }

        /// <inheritdoc />
        public bool IsConsole => true;

        /// <inheritdoc />
        public void Write(string text) {
            lock (_gate) {
                ThrowIfDisposed();
                _buffer.Append(text);
            }
        }

        /// <inheritdoc />
        public void Flush() {
            lock (_gate) {
                ThrowIfDisposed();
                FlushCore();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            lock (_gate) {
                if (_disposed) {
                    return;
                }
                try {
                    FlushCore();
                } finally {
                    _disposed = true;
                }
            }
        }

        internal static int ChunkLength(string text, int offset) {
            int length = Math.Min(8192, text.Length - offset);
            if (length > 0 && offset + length < text.Length
                && char.IsHighSurrogate(text[offset + length - 1]) && char.IsLowSurrogate(text[offset + length])) {
                length--;
            }
            return length;
        }

        private void FlushCore() {
            string text = _buffer.ToString();
            int offset = 0;
            while (offset < text.Length) {
                int length = ChunkLength(text, offset);
                string chunk = text.Substring(offset, length);
                if (!WindowsConsole.TryWrite(_handle, chunk, out int written)) {
                    throw new IOException("WriteConsoleW failed.", new Win32Exception(Marshal.GetLastWin32Error()));
                }
                if (written <= 0 || written > length) {
                    throw new IOException("WriteConsoleW made no progress.");
                }
                offset += written;
                _buffer.Remove(0, written);
            }
        }

        private void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(ConsoleHandleWriter));
            }
        }
    }
}
