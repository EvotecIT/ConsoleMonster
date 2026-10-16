using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// The interactive VT output. Print-once blocks are appended to scrollback; live frames are drawn on the alternate
    /// screen, diffed against the previous frame and written in one synchronized update (DEC mode 2026), so the screen never
    /// shows a half-drawn frame. The bottom-right cell is never written, which avoids an unwanted scroll on any terminal.
    /// </summary>
    public sealed class VtOutput : ITerminalOutput {
        private const string Esc = "\u001b[";
        private readonly ITerminalWriter _writer;
        private readonly bool _ownsWriter;
        private readonly StringBuilder _sb = new StringBuilder(16384);
        private CellBuffer? _previous;
        private bool _invalidated = true;
        private bool _live;

        /// <summary>Creates a VT output.</summary>
        /// <param name="writer">The sink.</param>
        /// <param name="profile">The profile (colour depth, size).</param>
        /// <param name="ownsWriter">Whether disposing this output disposes the writer.</param>
        public VtOutput(ITerminalWriter writer, TerminalProfile profile, bool ownsWriter = true) {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _ownsWriter = ownsWriter;
        }

        /// <inheritdoc />
        public TerminalProfile Profile { get; }

        /// <inheritdoc />
        public bool SupportsLiveFrames => true;

        // OSC 8 is emitted only where it is known to work; other terminals would print the sequence or ignore it unevenly.
        private bool Hyperlinks => Profile.Kind == TerminalKind.WindowsTerminal || Profile.Kind == TerminalKind.VisualStudioCode;

        /// <summary>Number of bytes (UTF-16 code units) written by the last frame; useful for benchmarks.</summary>
        public int LastFrameLength { get; private set; }

        /// <inheritdoc />
        public void WriteBlock(CellBuffer buffer) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            _sb.Clear();
            for (int y = 0; y < buffer.Height; y++) {
                AppendLine(buffer, y);
            }
            Emit();
        }

        /// <inheritdoc />
        public void WriteLine(StyledLine line) {
            if (line == null) {
                throw new ArgumentNullException(nameof(line));
            }
            _sb.Clear();
            ColorDepth depth = Profile.ColorDepth;
            foreach (TextSpan span in line) {
                SgrEncoder.Append(_sb, span.Style, depth);
                TextSanitizer.Append(_sb, span.Text);
            }
            if (depth != ColorDepth.None) {
                _sb.Append(SgrEncoder.Reset);
            }
            _sb.Append("\r\n");
            Emit();
        }

        /// <inheritdoc />
        public void BeginLive() {
            if (_live) {
                return;
            }
            _live = true;
            _invalidated = true;
            _writer.Write(Esc + "?1049h" + Esc + "?25l" + Esc + "0m" + Esc + "2J" + Esc + "H");
            _writer.Flush();
        }

        /// <inheritdoc />
        public void RenderFrame(CellBuffer frame) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            _sb.Clear();
            _sb.Append(Esc).Append("?2026h");
            bool full = _invalidated || _previous == null || _previous.Width != frame.Width || _previous.Height != frame.Height;
            Style? current = null;
            ColorDepth depth = Profile.ColorDepth;
            if (full) {
                _sb.Append(Esc).Append("0m").Append(Esc).Append("2J");
                for (int y = 0; y < frame.Height; y++) {
                    MoveTo(0, y);
                    current = null;
                    CellEncoder.AppendRun(_sb, frame, y, 0, RowLimit(frame, y), depth, ref current, true, Hyperlinks);
                }
            } else {
                List<DiffRun> runs = FrameDiff.Compute(_previous!, frame);
                foreach (DiffRun run in runs) {
                    int end = Math.Min(run.Start + run.Length, RowLimit(frame, run.Row));
                    if (end <= run.Start) {
                        continue;
                    }
                    MoveTo(run.Start, run.Row);
                    CellEncoder.AppendRun(_sb, frame, run.Row, run.Start, end, depth, ref current, true, Hyperlinks);
                }
            }
            if (depth != ColorDepth.None) {
                _sb.Append(SgrEncoder.Reset);
            }
            _sb.Append(Esc).Append("?2026l");
            if (_previous == null) {
                _previous = new CellBuffer(frame.Width, frame.Height);
            }
            _previous.CopyFrom(frame);
            _invalidated = false;
            LastFrameLength = _sb.Length;
            Emit();
        }

        /// <summary>Runs the process-wide terminal width probe through this output's console writer.</summary>
        /// <param name="timeout">The time budget.</param>
        /// <param name="profile">The measured profile.</param>
        /// <param name="decision">The probe result or skip reason.</param>
        /// <returns>True when the terminal was measured.</returns>
        internal bool TryProbeWidths(TimeSpan timeout, out TerminalWidthProfile profile, out string decision) {
            return TerminalWidthProbe.TryProbe(_writer, timeout, out profile, out decision);
        }

        /// <inheritdoc />
        public void Invalidate() {
            _invalidated = true;
        }

        /// <inheritdoc />
        public void EndLive() {
            if (!_live) {
                return;
            }
            _live = false;
            _previous = null;
            _writer.Write(Esc + "0m" + Esc + "?25h" + Esc + "?1049l");
            _writer.Flush();
        }

        /// <inheritdoc />
        public void Dispose() {
            EndLive();
            if (_ownsWriter) {
                _writer.Dispose();
            }
        }

        private static int RowLimit(CellBuffer frame, int y) {
            return y == frame.Height - 1 ? frame.Width - 1 : frame.Width;
        }

        private void AppendLine(CellBuffer buffer, int y) {
            ColorDepth depth = Profile.ColorDepth;
            int end = CellEncoder.ContentEnd(buffer, y, depth);
            Style? current = null;
            CellEncoder.AppendRun(_sb, buffer, y, 0, end, depth, ref current, true, Hyperlinks);
            if (depth != ColorDepth.None && end > 0) {
                _sb.Append(SgrEncoder.Reset);
            }
            _sb.Append("\r\n");
        }

        private void MoveTo(int x, int y) {
            _sb.Append(Esc).Append((y + 1).ToString(CultureInfo.InvariantCulture)).Append(';').Append((x + 1).ToString(CultureInfo.InvariantCulture)).Append('H');
        }

        private void Emit() {
            _writer.Write(_sb.ToString());
            _writer.Flush();
            _sb.Clear();
        }
    }
}
