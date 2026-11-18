using System;
using System.IO;
using System.Text;

namespace ConsoleMonster.Output {

    /// <summary>
    /// Writes text to a stream, typically redirected standard output, in UTF-8 (without BOM) unless another encoding is
    /// given. Text is buffered until <see cref="Flush"/> so a frame or block reaches the device in one write.
    /// </summary>
    public sealed class StreamTerminalWriter : ITerminalWriter {
        private static readonly Encoding Utf8 = new UTF8Encoding(false);
        private readonly Stream _stream;
        private readonly Encoding _encoding;
        private readonly bool _ownsStream;
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly object _gate = new object();
        private bool _disposed;

        /// <summary>Creates a writer over a stream.</summary>
        /// <param name="stream">The stream.</param>
        /// <param name="ownsStream">Whether disposing the writer disposes the stream.</param>
        public StreamTerminalWriter(Stream stream, bool ownsStream = false) : this(stream, Utf8, ownsStream) { }

        /// <summary>Creates a writer over a stream that writes in <paramref name="encoding"/>, without its preamble.</summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="ownsStream">Whether disposing the writer disposes the stream.</param>
        public StreamTerminalWriter(Stream stream, Encoding encoding, bool ownsStream = false) {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _ownsStream = ownsStream;
        }

        /// <summary>
        /// Creates a writer over the process's real standard output (not <see cref="Console.Out"/>). On Windows it writes
        /// in the console code page, which is how a process that captures this one decodes it; CI logs and other
        /// platforms get UTF-8.
        /// </summary>
        /// <returns>The writer.</returns>
        public static StreamTerminalWriter ForStandardOutput() => ForStandardOutput(Terminal.Profile.IsContinuousIntegration);

        internal static StreamTerminalWriter ForStandardOutput(bool continuousIntegration)
            => new StreamTerminalWriter(Console.OpenStandardOutput(), StreamEncoding.ForStandardOutput(continuousIntegration), true);

        /// <inheritdoc />
        public bool IsConsole => false;

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
                if (_buffer.Length == 0) {
                    return;
                }
                byte[] bytes = _encoding.GetBytes(_buffer.ToString());
                _stream.Write(bytes, 0, bytes.Length);
                _buffer.Clear();
                _stream.Flush();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            lock (_gate) {
                if (_disposed) {
                    return;
                }
                try {
                    Flush();
                } finally {
                    _disposed = true;
                    if (_ownsStream) {
                        _stream.Dispose();
                    }
                }
            }
        }

        private void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(StreamTerminalWriter));
            }
        }
    }
}
