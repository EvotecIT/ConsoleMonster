using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Output;

namespace ConsoleMonster.PowerShell {
    /// <summary>A terminal writer that sends complete lines to the host as information records when flushed (pipeline thread only).</summary>
    internal sealed class HostInformationWriter : ITerminalWriter {
        private readonly PSCmdlet _cmdlet;
        private readonly StringBuilder _buffer = new StringBuilder();

        public HostInformationWriter(PSCmdlet cmdlet) {
            _cmdlet = cmdlet;
        }

        public bool IsConsole => false;

        public void Write(string text) => _buffer.Append(text);

        public void Flush() {
            if (_buffer.Length == 0) {
                return;
            }
            string text = _buffer.ToString();
            _buffer.Clear();
            foreach (string line in HostQueueWriter.SplitLines(text, true)) {
                HostOutput.WriteHostLine(_cmdlet, line);
            }
        }

        public void Dispose() => Flush();
    }
}
