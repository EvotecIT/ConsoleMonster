using System.Text;

namespace ConsoleMonster.Output {

    /// <summary>
    /// An in-memory <see cref="ITerminalWriter"/>. Used by tests (VT byte-stream goldens) and by callers that
    /// want rendered output as a string, for example to embed it in another log.
    /// </summary>
    public sealed class StringTerminalWriter : ITerminalWriter {
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly object _gate = new object();

        /// <inheritdoc />
        public bool IsConsole => false;

        /// <summary>Number of <see cref="Flush"/> calls; a live renderer flushes once per frame.</summary>
        public int FlushCount { get; private set; }

        /// <inheritdoc />
        public void Write(string text) {
            lock (_gate) {
                _builder.Append(text);
            }
        }

        /// <inheritdoc />
        public void Flush() {
            lock (_gate) {
                FlushCount++;
            }
        }

        /// <summary>Returns everything written so far.</summary>
        /// <returns>The accumulated text.</returns>
        public override string ToString() {
            lock (_gate) {
                return _builder.ToString();
            }
        }

        /// <summary>Clears the accumulated text.</summary>
        public void Clear() {
            lock (_gate) {
                _builder.Clear();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
        }
    }
}
