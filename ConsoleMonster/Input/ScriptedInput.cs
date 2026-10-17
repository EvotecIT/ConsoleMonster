using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace ConsoleMonster.Input {
    /// <summary>
    /// Input from a script, for tests, demos and recordings: <c>Down Down Space "ad.evotec" Enter</c>.
    /// <list type="bullet">
    /// <item>key names as <see cref="KeyChord.Parse"/> accepts them (<c>Ctrl+K</c>, <c>Shift+Tab</c>, <c>F2</c>, <c>?</c>);</item>
    /// <item><c>Down*3</c> repeats a key;</item>
    /// <item>quoted text (<c>"text"</c> or <c>'text'</c>) types each character;</item>
    /// <item><c>paste:"text"</c> pastes; <c>resize:80x24</c> resizes; <c>wait:250</c> pauses (milliseconds).</item>
    /// </list>
    /// Reading past the end throws <see cref="InputClosedException"/>, so a test whose script is too short fails instead of hanging.
    /// </summary>
    public sealed class ScriptedInput : IInputSource {
        private readonly Queue<Step> _steps = new Queue<Step>();
        private DateTime _waitUntil = DateTime.MinValue;

        /// <summary>Creates a scripted source.</summary>
        /// <param name="script">The script.</param>
        public ScriptedInput(string script) {
            foreach (Step step in Parse(script ?? throw new ArgumentNullException(nameof(script)))) {
                _steps.Enqueue(step);
            }
        }

        /// <summary>Creates a scripted source from events.</summary>
        /// <param name="events">The events.</param>
        public ScriptedInput(IEnumerable<InputEvent> events) {
            foreach (InputEvent e in events ?? throw new ArgumentNullException(nameof(events))) {
                _steps.Enqueue(new Step(e, 0));
            }
        }

        /// <inheritdoc />
        public string Name => "script";

        /// <summary>How many events remain.</summary>
        public int Remaining => _steps.Count;

        /// <summary>
        /// Whether pauses (<c>wait:</c>) really sleep. Off by default so tests run instantly; demos turn it on.
        /// </summary>
        public bool RealTime { get; set; }

        /// <summary>
        /// When true, a read that does not wait (zero timeout) returns null, so a reader that drains bursts draws a frame
        /// after every scripted event (golden tests of each step).
        /// </summary>
        public bool Paced { get; set; }

        /// <summary>When true, reading past the end returns null (a timeout) instead of throwing.</summary>
        public bool EndsQuietly { get; set; }

        /// <summary>Appends events to the script.</summary>
        /// <param name="script">More script.</param>
        public void Append(string script) {
            foreach (Step step in Parse(script)) {
                _steps.Enqueue(step);
            }
        }

        /// <inheritdoc />
        public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            if (Paced && timeout == TimeSpan.Zero) {
                return null;
            }
            while (true) {
                if (RealTime && DateTime.UtcNow < _waitUntil) {
                    TimeSpan left = _waitUntil - DateTime.UtcNow;
                    if (left > timeout) {
                        Sleep(timeout, cancellationToken);
                        return null;
                    }
                    Sleep(left, cancellationToken);
                }
                if (_steps.Count == 0) {
                    if (EndsQuietly) {
                        Sleep(timeout, cancellationToken);
                        return null;
                    }
                    throw new InputClosedException("The input script ended before the prompt was answered.");
                }
                Step step = _steps.Dequeue();
                if (step.Event == null) {
                    _waitUntil = DateTime.UtcNow + TimeSpan.FromMilliseconds(step.WaitMilliseconds);
                    if (!RealTime) {
                        continue;
                    }
                    continue;
                }
                return step.Event;
            }
        }

        private static void Sleep(TimeSpan span, CancellationToken token) {
            if (span <= TimeSpan.Zero) {
                return;
            }
            token.WaitHandle.WaitOne(span);
            token.ThrowIfCancellationRequested();
        }

        /// <summary>Parses a script into events (pauses are dropped).</summary>
        /// <param name="script">The script.</param>
        /// <returns>The events.</returns>
        public static IReadOnlyList<InputEvent> ParseEvents(string script) {
            List<InputEvent> events = new List<InputEvent>();
            foreach (Step step in Parse(script)) {
                if (step.Event != null) {
                    events.Add(step.Event);
                }
            }
            return events;
        }

        private static List<Step> Parse(string script) {
            List<Step> steps = new List<Step>();
            int i = 0;
            while (i < script.Length) {
                char c = script[i];
                if (char.IsWhiteSpace(c)) {
                    i++;
                    continue;
                }
                if (c == '"' || c == '\'') {
                    string text = ReadQuoted(script, ref i);
                    foreach (char t in text) {
                        steps.Add(new Step(t == '\n' ? InputEvent.Press(ConsoleKey.Enter) : InputEvent.Character(t), 0));
                    }
                    continue;
                }
                int start = i;
                while (i < script.Length && !char.IsWhiteSpace(script[i])) {
                    if (script[i] == ':' && i + 1 < script.Length && (script[i + 1] == '"' || script[i + 1] == '\'')) {
                        break;
                    }
                    i++;
                }
                string token = script.Substring(start, i - start);
                if (token.Equals("paste:", StringComparison.OrdinalIgnoreCase) || (token.Equals("paste", StringComparison.OrdinalIgnoreCase) && i < script.Length && script[i] == ':')) {
                    if (i < script.Length && script[i] == ':') {
                        i++;
                    }
                    steps.Add(new Step(InputEvent.Paste(ReadQuoted(script, ref i)), 0));
                    continue;
                }
                if (token.StartsWith("wait:", StringComparison.OrdinalIgnoreCase)) {
                    steps.Add(new Step(null, int.Parse(token.Substring(5), CultureInfo.InvariantCulture)));
                    continue;
                }
                if (token.StartsWith("resize:", StringComparison.OrdinalIgnoreCase)) {
                    string[] size = token.Substring(7).Split('x');
                    steps.Add(new Step(InputEvent.Resized(int.Parse(size[0], CultureInfo.InvariantCulture), int.Parse(size[1], CultureInfo.InvariantCulture)), 0));
                    continue;
                }
                int repeat = 1;
                int star = token.LastIndexOf('*');
                if (star > 0 && star < token.Length - 1 && int.TryParse(token.Substring(star + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)) {
                    repeat = count;
                    token = token.Substring(0, star);
                }
                KeyChord chord = KeyChord.Parse(token);
                InputEvent e = chord.IsCharacter ? InputEvent.Character(chord.Char) : chord.Key == ConsoleKey.Spacebar && chord.Modifiers == KeyModifiers.None
                    ? InputEvent.Character(' ')
                    : InputEvent.Press(chord.Key, chord.Modifiers);
                for (int r = 0; r < repeat; r++) {
                    steps.Add(new Step(e, 0));
                }
            }
            return steps;
        }

        private static string ReadQuoted(string script, ref int i) {
            if (i >= script.Length || (script[i] != '"' && script[i] != '\'')) {
                throw new FormatException("Expected quoted text at position " + i.ToString(CultureInfo.InvariantCulture) + ".");
            }
            char quote = script[i++];
            StringBuilder sb = new StringBuilder();
            while (i < script.Length && script[i] != quote) {
                if (script[i] == '\\' && i + 1 < script.Length) {
                    char next = script[i + 1];
                    sb.Append(next == 'n' ? '\n' : next == 't' ? '\t' : next);
                    i += 2;
                    continue;
                }
                sb.Append(script[i++]);
            }
            i++;
            return sb.ToString();
        }

        /// <inheritdoc />
        public void Dispose() {
        }

        private sealed class Step {
            public Step(InputEvent? e, int wait) {
                Event = e;
                WaitMilliseconds = wait;
            }

            public InputEvent? Event { get; }

            public int WaitMilliseconds { get; }
        }
    }
}
