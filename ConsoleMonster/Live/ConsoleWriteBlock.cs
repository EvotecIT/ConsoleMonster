using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.Live {
    /// <summary>A single one-shot write, prepared before admission so a stop either accepts all of it or none of it.</summary>
    internal sealed class ConsoleWriteBlock {
        private readonly IReadOnlyList<CellBuffer>? _blocks;
        private readonly IReadOnlyList<StyledLine>? _lines;
        private readonly bool _newLine;

        internal ConsoleWriteBlock(IEnumerable<CellBuffer> blocks) { _blocks = new List<CellBuffer>(blocks); }

        internal ConsoleWriteBlock(IEnumerable<StyledLine> lines, bool newLine) {
            _lines = new List<StyledLine>(lines);
            _newLine = newLine;
        }

        internal void Write(ITerminalOutput output) {
            if (_blocks != null) {
                foreach (CellBuffer block in _blocks) { output.WriteBlock(block); }
            } else if (_lines != null) {
                for (int i = 0; i < _lines.Count; i++) {
                    if (i < _lines.Count - 1 || _newLine) { output.WriteLine(_lines[i]); }
                    else { output.WriteInline(_lines[i]); }
                }
            }
        }
    }
}
