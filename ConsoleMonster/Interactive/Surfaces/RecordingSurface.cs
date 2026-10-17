using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>Keeps every presented frame (for golden tests of prompts, menus and forms driven by scripted keys).</summary>
    internal sealed class RecordingSurface : IPromptSurface {
        private readonly List<CellBuffer> _frames = new List<CellBuffer>();
        private readonly List<Caret?> _carets = new List<Caret?>();

        public RecordingSurface(int width, int height, bool inline) {
            Width = width;
            Height = height;
            IsInline = inline;
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public bool IsInline { get; }

        public IReadOnlyList<CellBuffer> Frames => _frames;

        public IReadOnlyList<Caret?> Carets => _carets;

        public IReadOnlyList<StyledLine>? Summary { get; private set; }

        public bool Completed { get; private set; }

        public CellBuffer? LastFrame => _frames.Count == 0 ? null : _frames[_frames.Count - 1];

        /// <summary>Changes the size, as a window resize would.</summary>
        public void Resize(int width, int height) {
            Width = width;
            Height = height;
        }

        public bool Refresh() => false;

        public void Present(CellBuffer frame, Caret? caret) {
            CellBuffer copy = new CellBuffer(frame.Width, frame.Height);
            copy.CopyFrom(frame);
            _frames.Add(copy);
            _carets.Add(caret);
        }

        public void Complete(IReadOnlyList<StyledLine>? summary) {
            Summary = summary;
            Completed = true;
        }

        public string LastText() => LastFrame?.ToPlainText() ?? string.Empty;

        public string SummaryText() {
            if (Summary == null) {
                return string.Empty;
            }
            List<string> lines = new List<string>();
            foreach (StyledLine line in Summary) {
                lines.Add(line.PlainText);
            }
            return string.Join("\n", lines);
        }

        public void Dispose() {
        }
    }
}
