using System;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Headless {
    /// <summary>
    /// A terminal with no window: the full app, prompt, menu or dashboard runs exactly as it would in a real terminal (the
    /// same VT output, alternate screen and cell diffs), while a built-in terminal model keeps the screen. Scripted keys,
    /// clicks and resizes go in; the composed screen comes out as cells, for CI, golden tests, recordings, the browser host
    /// and the debug socket.
    /// </summary>
    /// <remarks>
    /// Pass it wherever a target is accepted (<see cref="Interactive.InteractiveOptions.Target"/>,
    /// <see cref="Live.LiveSessionOptions.Target"/>), or make it the ambient target with <see cref="Terminal.Use"/>.
    /// Setting <c>CONSOLEMONSTER_HEADLESS=120x40</c> makes one the process default, so an unmodified app such as a CLI can be
    /// driven through <see cref="DebugSocket"/>. All members are thread-safe.
    /// </remarks>
    public sealed class HeadlessTerminal : IConsoleDriver, IDisposable {
        private readonly object _gate = new object();
        private readonly HeadlessInput _input = new HeadlessInput();
        private readonly HeadlessWriter _writer;
        private readonly TerminalProfile _profile;
        private readonly VtScreen _screen;
        private CellBuffer _lastFrame;
        private long _frames;
        private SessionRecorder? _recorder;

        /// <summary>Creates a headless terminal.</summary>
        /// <param name="width">Width in cells (default 100).</param>
        /// <param name="height">Height in cells (default 30).</param>
        /// <param name="colors">The colour depth the app sees (default true colour, so recordings keep every colour).</param>
        /// <param name="glyphs">The glyph tier (default full Unicode).</param>
        /// <param name="culture">The culture for numbers, dates and built-in labels; null for the invariant culture.</param>
        public HeadlessTerminal(int width = 100, int height = 30, ColorDepth colors = ColorDepth.TrueColor, GlyphTier glyphs = GlyphTier.Full, CultureInfo? culture = null)
            : this(Fixed(width, height, colors, glyphs).WithCulture(culture ?? CultureInfo.InvariantCulture).WithLanguage(culture ?? CultureInfo.InvariantCulture)) {
        }

        private HeadlessTerminal(TerminalProfile profile) {
            _profile = profile;
            _screen = new VtScreen(profile.Width, profile.Height, keepScrollback: false);
            _lastFrame = new CellBuffer(profile.Width, profile.Height);
            _writer = new HeadlessWriter(this);
        }

        // For an app a person would otherwise run in their console (the PowerShell route, CONSOLEMONSTER_HEADLESS): number
        // formats and label language follow the same settings the console would use.
        internal static HeadlessTerminal LikeConsole(int width, int height) {
            TerminalProfile console = Terminal.Profile;
            TerminalProfile profile = Fixed(width, height, ColorDepth.TrueColor, GlyphTier.Full);
            return new HeadlessTerminal(profile.WithCulture(console.Culture).WithLanguage(console.Language));
        }

        private static TerminalProfile Fixed(int width, int height, ColorDepth colors, GlyphTier glyphs) {
            ValidateSize(width, height);
            return TerminalProfile.Fixed(width, height, colors, glyphs).WithHyperlinks(true);
        }

        /// <inheritdoc />
        public string Name { get { lock (_gate) { return "headless " + _screen.Width.ToString(CultureInfo.InvariantCulture) + "x" + _screen.Height.ToString(CultureInfo.InvariantCulture); } } }

        /// <summary>The profile at the current size: VT output, the chosen colour depth, glyph tier and culture, and hyperlinks.</summary>
        public TerminalProfile Profile { get { lock (_gate) { return _profile.WithSize(_screen.Width, _screen.Height); } } }

        /// <summary>The theme (default the dark theme, so the screen does not depend on the machine).</summary>
        public ITheme? Theme { get; set; } = Styling.Theme.Dark;

        /// <inheritdoc />
        public ITerminalWriter? Writer => _writer;

        /// <inheritdoc />
        public IInputSource? Input => _input;

        /// <summary>The current width in cells.</summary>
        public int Width { get { lock (_gate) { return _screen.Width; } } }

        /// <summary>The current height in cells.</summary>
        public int Height { get { lock (_gate) { return _screen.Height; } } }

        /// <summary>How many distinct screens have been drawn so far.</summary>
        public long FrameCount => Interlocked.Read(ref _frames);

        /// <summary>
        /// Raised after each completed screen that differs from the previous one, with a copy of it. It is raised on the
        /// thread that drew the screen (often a render thread): keep handlers short and never draw from them.
        /// </summary>
        public event Action<CellBuffer>? FrameRendered;

        /// <inheritdoc />
        public bool TryGetWindow(out int left, out int top, out int width, out int height) {
            lock (_gate) {
                left = 0; top = 0; width = _screen.Width; height = _screen.Height;
                return true;
            }
        }

        /// <summary>A copy of the visible screen as the user would see it: the app's alternate screen while it runs.</summary>
        /// <returns>The cells.</returns>
        public CellBuffer Snapshot() {
            lock (_gate) { return _screen.Snapshot(); }
        }

        /// <summary>
        /// Queues input in <see cref="ScriptedInput"/> syntax: key names (<c>Down Enter Ctrl+K F2</c>), repeats
        /// (<c>Down*3</c>), quoted text (<c>"text"</c>), <c>paste:"text"</c>, <c>resize:80x24</c> and pauses
        /// (<c>wait:250</c>, in real time, so recordings keep a reading pace). Steps are read in order; nothing is queued when
        /// the script is invalid, and at most 100000 steps wait at a time.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <exception cref="FormatException">The script is malformed.</exception>
        public void Send(string script) {
            if (script == null) { throw new ArgumentNullException(nameof(script)); }
            System.Collections.Generic.List<ScriptedInput.Step> steps = ScriptedInput.Parse(script);
            // Nothing is queued unless the whole script is valid.
            foreach (ScriptedInput.Step step in steps) {
                if (step.Event?.Kind == InputEventKind.Resize) { ValidateSize(step.Event.Width, step.Event.Height); }
            }
            _input.Enqueue(steps, (width, height) => ApplySize(width, height));
        }

        /// <summary>Queues one input event (a key, a character, a paste, or a mouse press or release from <see cref="InputEvent.Mouse"/>).</summary>
        /// <param name="input">The event. Resize with <see cref="Resize"/>, which also changes the screen.</param>
        public void Send(InputEvent input) {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            if (input.Kind == InputEventKind.Resize) { throw new ArgumentException("Use Resize to change the screen size.", nameof(input)); }
            _input.Enqueue(input);
        }

        /// <summary>Resizes the screen now, as dragging a window would, and tells the app with a resize event.</summary>
        /// <param name="width">The new width in cells (2 to 1000).</param>
        /// <param name="height">The new height in cells (2 to 1000).</param>
        public void Resize(int width, int height) {
            ValidateSize(width, height);
            ApplySize(width, height);
            _input.Enqueue(InputEvent.Resized(width, height));
        }

        /// <summary>
        /// Ends the input once the queued events are read: the next read reports that input has ended, so an app run by a
        /// script finishes instead of waiting for a person. No more input can be sent afterwards.
        /// </summary>
        public void CompleteInput() => _input.Complete();

        /// <summary>Waits until the visible screen contains the text (ordinal, case-sensitive).</summary>
        /// <param name="text">The text.</param>
        /// <param name="timeout">The longest wait.</param>
        /// <param name="cancellationToken">Cancels the wait.</param>
        /// <exception cref="TimeoutException">The text did not appear in time.</exception>
        public void WaitForText(string text, TimeSpan timeout, CancellationToken cancellationToken = default) {
            if (!TryWaitForText(text, timeout, cancellationToken)) {
                throw new TimeoutException("The headless screen did not show '" + text + "' within " + timeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture) + " s.");
            }
        }

        internal bool TryWaitForText(string text, TimeSpan timeout, CancellationToken cancellationToken) {
            if (string.IsNullOrEmpty(text)) { throw new ArgumentException("The text cannot be empty.", nameof(text)); }
            return TryWaitFor(frame => frame.ToPlainText().IndexOf(text, StringComparison.Ordinal) >= 0, timeout, cancellationToken);
        }

        // Waits until the last completed screen satisfies a condition, checked under the screen lock.
        internal bool TryWaitFor(Func<CellBuffer, bool> match, TimeSpan timeout, CancellationToken cancellationToken) {
            if (timeout < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(timeout)); }
            System.Diagnostics.Stopwatch waited = System.Diagnostics.Stopwatch.StartNew();
            lock (_gate) {
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (match(_lastFrame)) { return true; }
                    TimeSpan left = timeout - waited.Elapsed;
                    if (left <= TimeSpan.Zero) { return false; }
                    Monitor.Wait(_gate, left < TimeSpan.FromMilliseconds(50) ? left : TimeSpan.FromMilliseconds(50));
                }
            }
        }

        /// <summary>
        /// Records everything this terminal shows from now on to an asciicast v2 file (a <see cref="SessionRecorder"/> in
        /// process mode), including log panels, footers, dialogs and prompts. The recording starts with the current screen.
        /// Export it with <see cref="SessionRecording"/> or <c>Export-ConsoleRecording</c> to HTML, SVG, GIF or APNG.
        /// Dispose the recorder to finish the file; input is never recorded and secret patterns are redacted as usual.
        /// </summary>
        /// <param name="options">The file, title and redaction settings; size and in-process mode are set from this terminal.</param>
        /// <returns>The recorder.</returns>
        /// <exception cref="InvalidOperationException">A recording is already running.</exception>
        public SessionRecorder Record(SessionRecordingOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            lock (_gate) {
                if (_recorder != null && !_recorder.HasExited) { throw new InvalidOperationException("This headless terminal is already recording."); }
                options.InProcess = true;
                options.Width = _screen.Width;
                options.Height = _screen.Height;
                SessionRecorder recorder = SessionRecorder.Start(options);
                try {
                    CellBuffer current = _screen.Snapshot();
                    if (HasContent(current)) {
                        using (StringTerminalWriter seed = new StringTerminalWriter())
                        using (VtOutput output = new VtOutput(seed, _profile.WithSize(current.Width, current.Height), false)) {
                            output.RenderFrame(current);
                            recorder.CaptureOutput(seed.ToString());
                        }
                    }
                } catch { recorder.Dispose(); throw; }
                _recorder = recorder;
                return recorder;
            }
        }

        /// <summary>Ends the input (see <see cref="CompleteInput"/>). Recordings stay open until their recorder is disposed.</summary>
        public void Dispose() => _input.Complete();

        internal void Write(string text) {
            lock (_gate) {
                _screen.Feed(text);
                SessionRecorder? recorder = _recorder;
                if (recorder == null) { return; }
                if (recorder.HasExited) { _recorder = null; return; }
                try { recorder.CaptureOutput(text); }
                catch (ObjectDisposedException) { _recorder = null; }
            }
        }

        // A flush ends a frame: VT output flushes once per frame, and a synchronized update (DEC 2026) is complete only when
        // it ends, so a half-painted frame is never reported.
        internal void Flush() {
            CellBuffer frame;
            Action<CellBuffer>? handlers;
            lock (_gate) {
                if (_screen.SynchronizedOutput) { return; }
                frame = _screen.Snapshot();
                if (RecordingFrames.Same(frame, _lastFrame)) { return; }
                _lastFrame = frame;
                _frames++;
                handlers = FrameRendered;
                Monitor.PulseAll(_gate);
            }
            if (handlers != null) {
                CellBuffer copy = new CellBuffer(frame.Width, frame.Height);
                copy.CopyFrom(frame);
                handlers(copy);
            }
        }

        private void ApplySize(int width, int height) {
            lock (_gate) {
                if (width == _screen.Width && height == _screen.Height) { return; }
                _screen.Resize(width, height);
                SessionRecorder? recorder = _recorder;
                if (recorder != null && !recorder.HasExited) {
                    try { recorder.Resize(width, height); }
                    catch (ObjectDisposedException) { _recorder = null; }
                }
            }
        }

        private static void ValidateSize(int width, int height) {
            if (width < 2 || width > 1000) { throw new ArgumentOutOfRangeException(nameof(width), "Use 2 to 1000 columns."); }
            if (height < 2 || height > 1000) { throw new ArgumentOutOfRangeException(nameof(height), "Use 2 to 1000 rows."); }
            if ((long)width * height > 250000) { throw new ArgumentOutOfRangeException(nameof(width), "A screen holds at most 250000 cells."); }
        }

        private static bool HasContent(CellBuffer frame) {
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    Cell cell = frame[x, y];
                    if (!string.IsNullOrWhiteSpace(cell.Glyph) || !cell.Style.BackgroundColor.IsDefault) { return true; }
                }
            }
            return false;
        }

        private sealed class HeadlessWriter : ITerminalWriter {
            private readonly HeadlessTerminal _owner;
            internal HeadlessWriter(HeadlessTerminal owner) { _owner = owner; }
            public bool IsConsole => false;
            public void Write(string text) { if (text == null) { throw new ArgumentNullException(nameof(text)); } _owner.Write(text); }
            public void Flush() => _owner.Flush();
            // The terminal owns its writer: outputs that borrow it must not end the screen.
            public void Dispose() { }
        }
    }
}
