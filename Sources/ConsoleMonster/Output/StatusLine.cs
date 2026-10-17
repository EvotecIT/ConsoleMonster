using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Output {
    /// <summary>
    /// A single updating status line on a real VT console (Write-ConsoleStatus in PowerShell): redrawn in place (carriage return, the
    /// line, clear to the end), with an optional spinner, clock and elapsed time refreshed about ten times a second. The
    /// refresh stops by itself as soon as anything else moves the cursor (other output, the prompt), so it never draws over
    /// later output; completing the status ends the line.
    /// </summary>
    public sealed class StatusLine {
        private static readonly object Gate = new object();
        private static StatusLine? _current;

        private readonly TerminalProfile _profile;
        private readonly DateTime _started;
        private RichText _content = RichText.Empty;
        private StatusKind? _status;
        private bool _spinner;
        private bool _clock;
        private bool _elapsed;
        private string _clockFormat = "HH:mm:ss";
        private Timer? _timer;
        private long _frame;
        private int _cursorLeft = -1;
        private int _cursorTop = -1;

        private StatusLine(TerminalProfile profile) {
            _profile = profile;
            _started = DateTime.Now;
        }

        /// <summary>Shows or updates the status line; with <paramref name="final"/> the line is completed.</summary>
        /// <param name="profile">The console profile.</param>
        /// <param name="content">The text after the marker.</param>
        /// <param name="status">A status marker instead of the spinner, or null.</param>
        /// <param name="spinner">Whether a spinner turns until the line completes.</param>
        /// <param name="clock">Whether the time of day leads the line.</param>
        /// <param name="elapsed">Whether the elapsed time follows the content.</param>
        /// <param name="clockFormat">The time format for the clock.</param>
        /// <param name="final">Whether the line completes (the next output starts below it).</param>
        public static void Show(TerminalProfile profile, RichText content, StatusKind? status, bool spinner, bool clock, bool elapsed, string clockFormat, bool final) {
            lock (Gate) {
                StatusLine line = _current != null && _current.CursorUnchanged() ? _current : new StatusLine(profile);
                _current?.StopTimer();
                line._content = content;
                line._status = status;
                line._spinner = spinner;
                line._clock = clock;
                line._elapsed = elapsed;
                line._clockFormat = string.IsNullOrEmpty(clockFormat) ? "HH:mm:ss" : clockFormat;
                line.Render(final);
                if (final) {
                    _current = null;
                    return;
                }
                _current = line;
                if (spinner || clock || elapsed) {
                    line._timer = new Timer(_ => line.Tick(), null, 100, 100);
                }
            }
        }

        /// <summary>Builds the status line text for a context: spinner or status marker, clock, elapsed time, then the content.</summary>
        /// <param name="context">The render context.</param>
        /// <param name="content">The text after the marker.</param>
        /// <param name="status">A status marker instead of the spinner, or null.</param>
        /// <param name="spinner">Whether to show the spinner.</param>
        /// <param name="clock">Whether the time of day leads the line.</param>
        /// <param name="elapsed">Whether the elapsed time follows the content.</param>
        /// <param name="clockFormat">The time format for the clock.</param>
        /// <param name="started">When the status started (for the elapsed time).</param>
        /// <param name="frame">The animation frame (spinner position).</param>
        /// <param name="final">Whether the line is complete (no spinner).</param>
        /// <returns>The line.</returns>
        public static StyledLine Compose(RenderContext context, RichText content, StatusKind? status, bool spinner, bool clock, bool elapsed, string clockFormat, DateTime started, long frame, bool final) {
            List<TextSpan> spans = new List<TextSpan>();
            Style muted = context.Style(ThemeToken.Muted);
            if (status.HasValue) {
                spans.Add(new TextSpan(context.Glyphs.StatusMarker(status.Value) + " ", context.Style(status.Value)));
            } else if (spinner && !final) {
                IReadOnlyList<string> frames = context.Glyphs.SpinnerFrames;
                if (frames.Count > 0) {
                    spans.Add(new TextSpan(frames[(int)(frame % frames.Count)] + " ", context.Style(ThemeToken.Running)));
                }
            }
            if (clock) {
                spans.Add(new TextSpan("[" + DateTime.Now.ToString(clockFormat, System.Globalization.CultureInfo.CurrentCulture) + "] ", muted));
            }
            if (elapsed) {
                spans.Add(new TextSpan("[" + Widgets.NumberFormat.Clock(DateTime.Now - started, true) + "] ", muted));
            }
            StyledText text = content.ToStyledText(context);
            for (int i = 0; i < text.Lines.Count; i++) {
                if (i > 0) {
                    spans.Add(new TextSpan(" ", Style.Plain));
                }
                spans.AddRange(text.Lines[i]);
            }
            return new StyledLine(spans);
        }

        private void Tick() {
            lock (Gate) {
                if (!ReferenceEquals(_current, this)) {
                    StopTimer();
                    return;
                }
                if (!CursorUnchanged()) {
                    // Something else wrote (output, the prompt): stop instead of drawing over it.
                    StopTimer();
                    _current = null;
                    return;
                }
                _frame++;
                Render(false);
            }
        }

        private void Render(bool final) {
            try {
                TerminalProfile profile = Terminal.GetCurrentProfile();
                RenderContext context = Terminal.CreateContext(profile);
                StyledLine line = Compose(context, _content, _status, _spinner, _clock, _elapsed, _clockFormat, _started, _frame, final);
                // One cell short of the window, so the line never wraps.
                line = TextWrapper.Truncate(line, Math.Max(1, profile.Width - 1), context.Ellipsis);
                using (ITerminalOutput output = Terminal.CreateOutput(profile)) {
                    if (output is VtOutput vt) {
                        vt.WriteInPlace(line, final);
                    } else if (final) {
                        output.WriteLine(line);
                    }
                }
                RememberCursor();
            } catch (Exception) {
                // A console that went away (closed window, redirected mid-run): stop refreshing.
                StopTimer();
            }
        }

        private void RememberCursor() {
            try {
                _cursorLeft = Console.CursorLeft;
                _cursorTop = Console.CursorTop;
            } catch (Exception) {
                _cursorLeft = -1;
                _cursorTop = -1;
            }
        }

        private bool CursorUnchanged() {
            if (_cursorLeft < 0) {
                return true;
            }
            try {
                return Console.CursorLeft == _cursorLeft && Console.CursorTop == _cursorTop;
            } catch (Exception) {
                return true;
            }
        }

        private void StopTimer() {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
