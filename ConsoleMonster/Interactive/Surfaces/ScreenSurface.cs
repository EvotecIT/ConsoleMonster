using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The alternate screen for menus and app shells: full-screen frames, diffed against the previous frame and written as
    /// one synchronized update (no flicker). The scrollback is untouched and comes back when the app ends.
    /// </summary>
    internal sealed class ScreenSurface : IPromptSurface {
        private readonly TerminalProfile _profile;
        private readonly ITerminalWriter _writer;
        private readonly bool _ownsWriter;
        private readonly VtOutput _output;
        private readonly Func<int[]?>? _sizeSource;
        private int _width;
        private int _height;
        private bool _ended;

        public ScreenSurface(TerminalProfile profile, ITerminalWriter? writer = null, Func<int[]?>? sizeSource = null) {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _ownsWriter = writer == null;
            _writer = writer ?? TerminalWriterFactory.Create(profile);
            _sizeSource = sizeSource;
            _output = new VtOutput(_writer, profile, false);
            ReadSize(out _width, out _height);
            _output.BeginLive();
        }

        public int Width => _width;

        public int Height => _height;

        public bool IsInline => false;

        /// <summary>The output, borrowed by live dashboards started from a menu (they must not end the alternate screen).</summary>
        public ITerminalOutput Output => _output;

        public TerminalProfile Profile => _profile;

        public bool Refresh() {
            ReadSize(out int width, out int height);
            if (width == _width && height == _height) {
                return false;
            }
            _width = width;
            _height = height;
            _output.Invalidate();
            return true;
        }

        /// <summary>Forces the next frame to be drawn completely (after a live dashboard drew on the screen).</summary>
        public void Invalidate() => _output.Invalidate();

        public void Present(CellBuffer frame, Caret? caret) {
            _writer.Write("\u001b[?25l");
            _output.RenderFrame(frame);
            if (caret.HasValue) {
                _writer.Write("\u001b[" + (caret.Value.Y + 1).ToString(CultureInfo.InvariantCulture) + ";" + (caret.Value.X + 1).ToString(CultureInfo.InvariantCulture) + "H\u001b[?25h");
            }
            _writer.Flush();
        }

        public void Complete(IReadOnlyList<StyledLine>? summary) {
            End();
            if (summary != null && summary.Count > 0) {
                VtOutput lines = new VtOutput(_writer, _profile, false);
                foreach (StyledLine line in summary) {
                    lines.WriteLine(line);
                }
            }
        }

        private void End() {
            if (_ended) {
                return;
            }
            _ended = true;
            _output.EndLive();
        }

        public void Dispose() {
            End();
            if (_ownsWriter) {
                _writer.Dispose();
            }
        }

        private void ReadSize(out int width, out int height) {
            if (_sizeSource != null) {
                int[]? size = _sizeSource();
                if (size != null && size.Length == 2 && size[0] > 0 && size[1] > 0) {
                    width = size[0];
                    height = size[1];
                    return;
                }
            } else if (TerminalSize.TryGet(out width, out height) && width > 0 && height > 0) {
                return;
            }
            width = _profile.Width;
            height = _profile.Height;
        }
    }
}
