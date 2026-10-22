using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Output;

namespace ConsoleMonster.Live.Tails {
    /// <summary>A synchronous, caller-polled reader of completed literal lines from a file that may be rotated.</summary>
    /// <remarks>
    /// Owns its file handle and uses ReadWrite/Delete sharing. No timers, threads or filesystem watchers are created.
    /// Calls, including disposal, must be serialized by the caller. UTF-8 is the default; UTF-8, UTF-16 and UTF-32 BOMs override
    /// the supplied encoding, including a BOM split between polls. Decoder state survives split writes.
    /// CR, LF and CRLF terminate lines; CR emits immediately and a following LF is consumed even across polls.
    /// Unterminated text is retained until a terminator arrives, and is discarded on reset or disposal.
    /// Rotation follows the path, not the renamed file: unread bytes in the old file are not drained.
    /// Identity changes, observed shrinkage and changes in the last 64 consumed bytes reset decoding and resume at byte zero.
    /// A truncate/regrow entirely between polls with identical checkpoint bytes is indistinguishable from an append.
    /// Concurrent rewrites are not atomic snapshots. Initialisation scans the opening length once, retaining only the
    /// requested completed lines plus any unfinished line; each later poll has both a byte cap and a fixed length boundary.
    /// File identity uses the operating system on Windows and on x64/arm64 Linux/macOS; other Unix ABIs are unsupported.
    /// </remarks>
    public sealed class FileLogTail : IDisposable {
        private const int BufferSize = 8192;
        private readonly string _path;
        private readonly Encoding _fallback;
        private readonly int _maxBytesPerRead;
        private readonly byte[] _bytes = new byte[BufferSize];
        private readonly byte[] _anchor = new byte[64];
        private readonly byte[] _preamble = new byte[4];
        private readonly StringBuilder _line = new StringBuilder();
        private FileStream? _stream;
        private FileTailIdentity _identity;
        private Decoder? _decoder;
        private char[] _characters = Array.Empty<char>();
        private IReadOnlyList<TailedLogEntry> _initial = Array.Empty<TailedLogEntry>();
        private long _offset;
        private int _anchorLength;
        private int _preambleLength;
        private bool _skipLf;
        private bool _disposed;

        /// <summary>Opens the file and captures its last completed lines at the current length.</summary>
        /// <param name="path">The file to follow; it must exist when this reader is constructed.</param>
        /// <param name="initialLineCount">Completed history lines to return on the first poll; defaults to 10. Zero skips completed history.</param>
        /// <param name="encoding">Encoding for files without a BOM, or null for UTF-8. The encoding is copied.</param>
        /// <param name="maxBytesPerRead">Maximum newly consumed bytes per poll; defaults to 65,536 and must be positive. Initial history is separate.</param>
        /// <remarks>
        /// An existing unfinished line is retained even with zero history, and returned once a later append terminates it.
        /// A missing path during subsequent polling returns no new entries; the next available file is followed.
        /// Access errors and other I/O failures propagate. No tail output is interpreted as markup.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A line count is negative or the byte cap is not positive.</exception>
        /// <exception cref="IOException">The file cannot be opened or read.</exception>
        /// <exception cref="PlatformNotSupportedException">The platform's file identity ABI is unsupported.</exception>
        public FileLogTail(string path, int initialLineCount = 10, Encoding? encoding = null, int maxBytesPerRead = 65536) {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }
            if (initialLineCount < 0) { throw new ArgumentOutOfRangeException(nameof(initialLineCount)); }
            if (maxBytesPerRead <= 0) { throw new ArgumentOutOfRangeException(nameof(maxBytesPerRead)); }
            _path = Path.GetFullPath(path);
            _fallback = (Encoding)(encoding ?? Encoding.UTF8).Clone();
            _maxBytesPerRead = maxBytesPerRead;
            try {
                FileStream stream = Open();
                _stream = stream;
                _identity = FileTailIdentity.Read(stream);
                Queue<string> history = new Queue<string>();
                ReadSnapshot(stream, stream.Length, text => {
                    if (initialLineCount == 0) { return; }
                    if (history.Count == initialLineCount) { history.Dequeue(); }
                    history.Enqueue(text);
                });
                List<TailedLogEntry> entries = new List<TailedLogEntry>(history.Count);
                foreach (string text in history) { entries.Add(LogLineParser.Parse(text)); }
                _initial = entries;
            } catch {
                _stream?.Dispose();
                _stream = null;
                throw;
            }
        }

        /// <summary>Returns initial history once, then completed lines from a bounded snapshot of newly available bytes.</summary>
        /// <returns>Literal parsed entries in file order, or an empty list when no completed lines are available.</returns>
        /// <remarks>
        /// The opening file length and configured byte cap bound each poll even when a producer keeps writing.
        /// A reset starts at the beginning of the new file, subject to that same cap. Missing paths are retried by the next call.
        /// A single very long unfinished line may span many polls and is retained in memory until completed or reset.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">This reader has been disposed.</exception>
        /// <exception cref="IOException">The file cannot be read or identified.</exception>
        public IReadOnlyList<TailedLogEntry> ReadAvailable() {
            if (_disposed) { throw new ObjectDisposedException(nameof(FileLogTail)); }
            List<TailedLogEntry> entries = new List<TailedLogEntry>(_initial);
            FileStream? candidate;
            try {
                candidate = Open();
            } catch (FileNotFoundException) {
                _initial = Array.Empty<TailedLogEntry>();
                return entries;
            } catch (DirectoryNotFoundException) {
                _initial = Array.Empty<TailedLogEntry>();
                return entries;
            }
            try {
                FileTailIdentity identity = FileTailIdentity.Read(candidate);
                long length = candidate.Length;
                if (!_identity.Equals(identity) || length < _offset || !CheckpointMatches(candidate)) { Reset(); }
                _stream?.Dispose();
                FileStream stream = candidate;
                _stream = stream;
                _identity = identity;
                candidate = null;
                stream.Position = _offset;
                long end = _offset + Math.Min(length - _offset, _maxBytesPerRead);
                ReadSnapshot(stream, end, text => entries.Add(LogLineParser.Parse(text)));
                _initial = Array.Empty<TailedLogEntry>();
                return entries;
            } finally {
                candidate?.Dispose();
            }
        }

        /// <summary>Closes the owned handle and discards any queued history or unfinished line. Repeated calls are harmless.</summary>
        public void Dispose() {
            if (_disposed) { return; }
            _disposed = true;
            _stream?.Dispose();
            _stream = null;
            _initial = Array.Empty<TailedLogEntry>();
            _line.Clear();
            _decoder = null;
        }

        private FileStream Open() => new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, BufferSize);

        private void Reset() {
            _offset = 0;
            _anchorLength = 0;
            _preambleLength = 0;
            _decoder = null;
            _skipLf = false;
            _line.Clear();
        }

        private bool CheckpointMatches(FileStream stream) {
            if (_anchorLength == 0) { return true; }
            stream.Position = _offset - _anchorLength;
            for (int i = 0; i < _anchorLength; i++) {
                if (stream.ReadByte() != _anchor[i]) { return false; }
            }
            return true;
        }

        private void ReadSnapshot(FileStream stream, long end, Action<string> emit) {
            while (_offset < end) {
                int count = stream.Read(_bytes, 0, (int)Math.Min(BufferSize, end - _offset));
                if (count == 0) { break; }
                _offset += count;
                RememberCheckpoint(count);
                Decode(count, emit);
            }
        }

        private void RememberCheckpoint(int count) {
            int retained = Math.Min(_anchorLength, Math.Max(0, _anchor.Length - count));
            if (retained > 0) { Array.Copy(_anchor, _anchorLength - retained, _anchor, 0, retained); }
            int added = Math.Min(count, _anchor.Length);
            Array.Copy(_bytes, count - added, _anchor, retained, added);
            _anchorLength = retained + added;
        }

        private void Decode(int count, Action<string> emit) {
            int start = 0;
            while (_decoder == null && start < count) {
                _preamble[_preambleLength++] = _bytes[start++];
                if (!LogEncodingPreamble.TrySelect(_preamble, _preambleLength, false, out Encoding? detected, out int bomLength)) { continue; }
                Encoding encoding = detected ?? _fallback;
                _decoder = encoding.GetDecoder();
                _characters = new char[encoding.GetMaxCharCount(BufferSize)];
                DecodeCharacters(_preamble, bomLength, _preambleLength - bomLength, emit);
            }
            if (_decoder != null && start < count) { DecodeCharacters(_bytes, start, count - start, emit); }
        }

        private void DecodeCharacters(byte[] bytes, int start, int count, Action<string> emit) {
            Decoder decoder = _decoder ?? throw new InvalidOperationException("The decoder has not been selected.");
            int characters = decoder.GetChars(bytes, start, count, _characters, 0, false);
            for (int i = 0; i < characters; i++) {
                char value = _characters[i];
                if (_skipLf) {
                    _skipLf = false;
                    if (value == '\n') { continue; }
                }
                if (value == '\r' || value == '\n') {
                    emit(_line.ToString());
                    _line.Clear();
                    _skipLf = value == '\r';
                } else {
                    _line.Append(value);
                }
            }
        }
    }
}
