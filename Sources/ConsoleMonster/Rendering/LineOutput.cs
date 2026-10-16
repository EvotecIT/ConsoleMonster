using System;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// The line-based output for <see cref="OutputMode.Stream"/> and <see cref="OutputMode.Plain"/>: append-only lines,
    /// never any cursor movement. Stream keeps colour when the profile allows it (CI logs render ANSI); Plain never emits
    /// escape sequences. Live frames are ignored: a live session narrates events as lines instead.
    /// </summary>
    public sealed class LineOutput : ITerminalOutput {
        private readonly ITerminalWriter _writer;
        private readonly bool _ownsWriter;
        private readonly StringBuilder _sb = new StringBuilder(4096);

        /// <summary>Creates a line output.</summary>
        /// <param name="writer">The sink.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="ownsWriter">Whether disposing this output disposes the writer.</param>
        public LineOutput(ITerminalWriter writer, TerminalProfile profile, bool ownsWriter = true) {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _ownsWriter = ownsWriter;
        }

        /// <inheritdoc />
        public TerminalProfile Profile { get; }

        /// <inheritdoc />
        public bool SupportsLiveFrames => false;

        private ColorDepth Depth => Profile.Mode == OutputMode.Plain ? ColorDepth.None : Profile.ColorDepth;

        /// <inheritdoc />
        public void WriteBlock(CellBuffer buffer) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            _sb.Clear();
            ColorDepth depth = Depth;
            for (int y = 0; y < buffer.Height; y++) {
                int end = CellEncoder.ContentEnd(buffer, y, depth);
                Style? current = null;
                CellEncoder.AppendRun(_sb, buffer, y, 0, end, depth, ref current);
                if (depth != ColorDepth.None && end > 0) {
                    _sb.Append(SgrEncoder.Reset);
                }
                _sb.Append(Environment.NewLine);
            }
            Emit();
        }

        /// <inheritdoc />
        public void WriteLine(StyledLine line) {
            if (line == null) {
                throw new ArgumentNullException(nameof(line));
            }
            _sb.Clear();
            ColorDepth depth = Depth;
            foreach (TextSpan span in line) {
                SgrEncoder.Append(_sb, span.Style, depth);
                TextSanitizer.Append(_sb, span.Text);
            }
            if (depth != ColorDepth.None && line.Count > 0) {
                _sb.Append(SgrEncoder.Reset);
            }
            _sb.Append(Environment.NewLine);
            Emit();
        }

        /// <inheritdoc />
        public void BeginLive() {
        }

        /// <inheritdoc />
        public void RenderFrame(CellBuffer frame) {
        }

        /// <inheritdoc />
        public void Invalidate() {
        }

        /// <inheritdoc />
        public void EndLive() {
            _writer.Flush();
        }

        /// <inheritdoc />
        public void Dispose() {
            _writer.Flush();
            if (_ownsWriter) {
                _writer.Dispose();
            }
        }

        private void Emit() {
            _writer.Write(_sb.ToString());
            _writer.Flush();
            _sb.Clear();
        }
    }
}
