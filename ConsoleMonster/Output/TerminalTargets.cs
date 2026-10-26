using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Output {
    /// <summary>The process console: detected profile (size refreshed on every write), keyboard input. The default target.</summary>
    public sealed class ConsoleTarget : ITerminalTarget {
        private ConsoleTarget() {
        }

        /// <summary>The console target.</summary>
        public static ConsoleTarget Instance { get; } = new ConsoleTarget();

        /// <inheritdoc />
        public string Name => "console";

        /// <inheritdoc />
        public TerminalProfile Profile => Terminal.GetConsoleProfile();

        /// <inheritdoc />
        public ITheme? Theme => null;

        /// <inheritdoc />
        public ITerminalWriter? Writer => null;

        /// <inheritdoc />
        public IInputSource? Input => null;
    }

    /// <summary>
    /// Output to a <see cref="TextWriter"/> (a log, a StringWriter, a host's output stream) laid out for a chosen width,
    /// glyph tier and colour depth: append-only lines, with SGR colour codes only when the depth asks for them. No input.
    /// </summary>
    public sealed class WriterTarget : ITerminalTarget {
        private readonly TextTerminalWriter _writer;

        /// <summary>Creates a writer target.</summary>
        /// <param name="writer">The writer; the caller keeps ownership.</param>
        /// <param name="width">Width in cells; zero resolves the current target, falling back to 100.</param>
        /// <param name="glyphs">The glyph tier (default ASCII, safe in any log).</param>
        /// <param name="colors">The colour depth (default none: plain text).</param>
        /// <param name="culture">The culture for numbers, dates and built-in labels; null for the current culture.</param>
        public WriterTarget(TextWriter writer, int width = 0, GlyphTier glyphs = GlyphTier.Ascii, ColorDepth colors = ColorDepth.None, CultureInfo? culture = null)
            : this(writer, TerminalProfile.Fixed(Terminal.ResolveWidth(width), TerminalProfile.DefaultHeight, colors, glyphs, OutputMode.Stream).WithCulture(culture).WithLanguage(culture)) {
        }

        /// <summary>Creates a writer target with an explicit profile (its mode decides between line output and VT output).</summary>
        /// <param name="writer">The writer; the caller keeps ownership.</param>
        /// <param name="profile">The profile.</param>
        public WriterTarget(TextWriter writer, TerminalProfile profile) {
            _writer = new TextTerminalWriter(writer ?? throw new ArgumentNullException(nameof(writer)));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <inheritdoc />
        public string Name => "writer " + Profile.Width.ToString(CultureInfo.InvariantCulture) + " columns";

        /// <inheritdoc />
        public TerminalProfile Profile { get; }

        /// <summary>The theme (default the dark theme, so output does not depend on the machine).</summary>
        public ITheme? Theme { get; set; } = Styling.Theme.Dark;

        /// <inheritdoc />
        public ITerminalWriter? Writer => _writer;

        /// <inheritdoc />
        public IInputSource? Input => null;
    }

    /// <summary>
    /// An in-memory target for tests: output collects in <see cref="Text"/>, optional scripted keys answer prompts, and the
    /// screens that prompts, menus, forms and grids draw collect in <see cref="Screens"/>. Pair with
    /// <see cref="Testing.GoldenFile"/> through <see cref="MatchGolden"/>. Create one with
    /// <see cref="Testing.TestTerminal.Target"/>.
    /// </summary>
    public sealed class RecordingTarget : ITerminalTarget {
        private static readonly Regex Escapes = new Regex("\u001b\\[[0-9;?]*[ -/]*[@-~]|\u001b\\][^\u0007\u001b]*(\u0007|\u001b\\\\)", RegexOptions.CultureInvariant);
        private readonly StringTerminalWriter _writer = new StringTerminalWriter();
        private int _checkpointCount;

        /// <summary>Creates a recording target.</summary>
        /// <param name="profile">The profile (use <see cref="Testing.TestTerminal.Profile"/> for a fixed one).</param>
        /// <param name="keys">Scripted keys for prompts (see <see cref="Testing.TestTerminal.Keys"/>), or null for no input.</param>
        public RecordingTarget(TerminalProfile profile, string? keys = null) {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Input = keys == null ? null : new ScriptedInput(keys) { Paced = true };
            Screens = new ScreenCapture(Math.Max(10, profile.Width), Math.Max(3, profile.Height));
        }

        /// <inheritdoc />
        public string Name => "recording " + Profile.Width.ToString(CultureInfo.InvariantCulture) + "x" + Profile.Height.ToString(CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public TerminalProfile Profile { get; }

        /// <summary>The theme (default the dark theme).</summary>
        public ITheme? Theme { get; set; } = Styling.Theme.Dark;

        /// <inheritdoc />
        public ITerminalWriter? Writer => _writer;

        /// <inheritdoc />
        public IInputSource? Input { get; }

        /// <summary>The screens drawn by prompts, menus, forms, grids and selected live-session checkpoints.</summary>
        public ScreenCapture Screens { get; }

        /// <summary>
        /// Skips <see cref="Live.LiveSession.Delay"/> waits and captures a compact dashboard checkpoint in
        /// <see cref="Screens"/> instead. Default false; configure before starting the session.
        /// </summary>
        public bool SkipDelays { get; set; }

        /// <summary>
        /// Captures Delay checkpoints at the profile's full viewport size, including constrained layouts and captured
        /// console logs. Append-only outputs commit heartbeat summaries at logical checkpoints instead of capturing bitmap frames.
        /// Default false keeps compact block checkpoints. Viewport animation frames follow checkpoint order. Configure before starting the session.
        /// </summary>
        public bool CaptureViewport { get; set; }

        /// <summary>
        /// Zero-based live-session Delay calls to capture when <see cref="SkipDelays"/> is enabled; null captures all,
        /// an empty array captures none. Configure before starting the session. Checkpoint order assumes one producer.
        /// </summary>
        public int[]? Checkpoints { get; set; }

        /// <summary>Total valid Delay calls on sessions using this target, including checkpoints that were not selected.</summary>
        public int CheckpointCount => Volatile.Read(ref _checkpointCount);

        internal bool SelectCheckpoint() {
            int index = Interlocked.Increment(ref _checkpointCount) - 1;
            int[]? selected = Checkpoints;
            return selected == null || Array.IndexOf(selected, index) >= 0;
        }

        // Called by the live dispatcher after all earlier queued updates have been applied.
        internal void CaptureCheckpoint(CellBuffer frame) => Screens.Surface.Present(frame, null);

        /// <summary>Everything written so far, exactly as written (including colour codes when the profile has colour).</summary>
        public string Output => _writer.ToString();

        /// <summary>The output as plain text: escape sequences removed, line endings normalised to <c>\n</c>.</summary>
        public string Text => Escapes.Replace(_writer.ToString(), string.Empty).Replace("\r\n", "\n");

        /// <summary>Replays the captured output into cells, including colours and cursor movement, at this target's fixed size.
        /// Prompt and application frames captured directly in <see cref="Screens"/> remain separate.</summary>
        /// <returns>The printed rows, with their original cell styles.</returns>
        public Canvas.CellBuffer Transcript() {
            var screen = new Sessions.VtScreen(Profile.Width, Profile.Height);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Output);
            screen.Feed(bytes, bytes.Length);
            return screen.Transcript();
        }

        /// <summary>Forgets the output written so far.</summary>
        public void Clear() => _writer.Clear();

        /// <summary>Compares <see cref="Text"/> with a golden file (see <see cref="Testing.GoldenFile.Match"/>).</summary>
        /// <param name="path">The golden file path.</param>
        public void MatchGolden(string path) => Testing.GoldenFile.Match(path, Text);
    }

    /// <summary>Discards all output and has no input: prompts use their defaults. For quiet runs and benchmarks.</summary>
    public sealed class NullTarget : ITerminalTarget {
        private NullTarget() {
        }

        /// <summary>The null target.</summary>
        public static NullTarget Instance { get; } = new NullTarget();

        /// <inheritdoc />
        public string Name => "null";

        /// <inheritdoc />
        public TerminalProfile Profile { get; } = TerminalProfile.Fixed(80, 24, ColorDepth.None, GlyphTier.Ascii, OutputMode.Stream);

        /// <inheritdoc />
        public ITheme? Theme => Styling.Theme.Dark;

        /// <inheritdoc />
        public ITerminalWriter? Writer { get; } = new DiscardWriter();

        /// <inheritdoc />
        public IInputSource? Input => null;

        private sealed class DiscardWriter : ITerminalWriter {
            public bool IsConsole => false;

            public void Write(string text) {
            }

            public void Flush() {
            }

            public void Dispose() {
            }
        }
    }

    /// <summary>An <see cref="ITerminalWriter"/> over a <see cref="TextWriter"/> the caller owns.</summary>
    public sealed class TextTerminalWriter : ITerminalWriter {
        private readonly TextWriter _writer;
        private readonly object _gate = new object();

        /// <summary>Creates the writer.</summary>
        /// <param name="writer">The text writer; it is never disposed here.</param>
        public TextTerminalWriter(TextWriter writer) {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        /// <inheritdoc />
        public bool IsConsole => false;

        /// <inheritdoc />
        public void Write(string text) {
            lock (_gate) {
                _writer.Write(text);
            }
        }

        /// <inheritdoc />
        public void Flush() {
            lock (_gate) {
                _writer.Flush();
            }
        }

        /// <summary>Does nothing: the caller owns the text writer.</summary>
        public void Dispose() {
        }
    }
}
