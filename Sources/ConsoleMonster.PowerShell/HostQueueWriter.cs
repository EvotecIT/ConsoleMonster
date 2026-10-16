using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Output;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// A thread-safe terminal writer for stream-mode dashboards outside a real console: the render thread queues complete
    /// lines and the cmdlet's pipeline thread drains them to the host from its wait loop.
    /// </summary>
    internal sealed class HostQueueWriter : ITerminalWriter {
        private readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
        private readonly StringBuilder _partial = new StringBuilder();
        private readonly object _gate = new object();

        public bool IsConsole => false;

        public void Write(string text) {
            lock (_gate) {
                _partial.Append(text);
                string buffered = _partial.ToString();
                int end = buffered.LastIndexOf('\n');
                if (end < 0) {
                    return;
                }
                _partial.Clear();
                _partial.Append(buffered.Substring(end + 1));
                foreach (string line in SplitLines(buffered.Substring(0, end + 1), false)) {
                    _lines.Enqueue(line);
                }
            }
        }

        public void Flush() {
        }

        public void Dispose() {
        }

        /// <summary>Writes the queued lines to the host; call on the cmdlet's pipeline thread.</summary>
        /// <param name="cmdlet">The cmdlet.</param>
        /// <param name="final">Also write an unterminated last line.</param>
        public void Drain(PSCmdlet cmdlet, bool final = false) {
            if (final) {
                lock (_gate) {
                    if (_partial.Length > 0) {
                        _lines.Enqueue(_partial.ToString().TrimEnd('\r'));
                        _partial.Clear();
                    }
                }
            }
            while (_lines.TryDequeue(out string? line)) {
                HostOutput.WriteHostLine(cmdlet, line);
            }
        }

        // Lines without their terminators; a final terminator does not add an empty line.
        internal static IEnumerable<string> SplitLines(string text, bool includePartial) {
            int start = 0;
            while (start < text.Length) {
                int end = text.IndexOf('\n', start);
                if (end < 0) {
                    if (includePartial) {
                        yield return text.Substring(start).TrimEnd('\r');
                    }
                    yield break;
                }
                yield return text.Substring(start, end - start).TrimEnd('\r');
                start = end + 1;
            }
        }
    }
}
