using System;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Output;

namespace ConsoleMonster.Input {
    /// <summary>
    /// Reads Unix TTY input through the shared <see cref="VtInputDecoder"/>, including SGR mouse reports, with the exact
    /// terminal mode restored on disposal. Other platforms use <see cref="Console.ReadKey(bool)"/> and normalize its keys.
    /// Resizes are detected by polling the window size.
    /// </summary>
    public sealed class ConsoleKeyInput : IInputSource, IConsoleInputMode {
        private readonly UnixKeyInput? _unix;
        private int _width;
        private int _height;

        /// <summary>Creates the reader.</summary>
        public ConsoleKeyInput() {
            _unix = UnixKeyInput.TryOpen();
            if (_unix != null) { TerminalBackground.ProbeBeforeInput(); }
            ConsoleInput.MarkInputStarted();
            TerminalSize.TryGet(out _width, out _height);
        }

        /// <inheritdoc />
        public string Name => "console keys";

        /// <summary>Preserves Ctrl+C signals for live work while reading other keys on Unix.</summary>
        public void EnableCancelKeyProcessing() => _unix?.EnableCancelKeyProcessing();

        /// <summary>Enters the leased native mode without consuming or discarding initial keys.</summary>
        internal void Activate() => _unix?.Activate();

        void IConsoleInputMode.Suspend() => _unix?.Suspend();

        void IConsoleInputMode.Resume() => _unix?.Resume();

        /// <inheritdoc />
        public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
            TimeSpan wait = timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout;
            Stopwatch elapsed = Stopwatch.StartNew();
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (TerminalSize.TryGet(out int width, out int height) && width > 0 && (width != _width || height != _height)) {
                    _width = width;
                    _height = height;
                    return InputEvent.Resized(width, height);
                }
                if (_unix != null) { return _unix.Read(wait - elapsed.Elapsed, cancellationToken); }
                try {
                    if (Console.KeyAvailable) {
                        return FromKeyInfo(Console.ReadKey(true));
                    }
                } catch (InvalidOperationException ex) {
                    throw new InputClosedException("Keys cannot be read: " + ex.Message, ex);
                }
                if (elapsed.Elapsed >= wait) {
                    return null;
                }
                Thread.Sleep(15);
            }
        }

        /// <summary>Converts a .NET key record into an input event.</summary>
        /// <param name="info">The key record.</param>
        /// <returns>The event.</returns>
        public static InputEvent FromKeyInfo(ConsoleKeyInfo info) {
            KeyModifiers modifiers = KeyModifiers.None;
            if ((info.Modifiers & ConsoleModifiers.Control) != 0) {
                modifiers |= KeyModifiers.Control;
            }
            if ((info.Modifiers & ConsoleModifiers.Alt) != 0) {
                modifiers |= KeyModifiers.Alt;
            }
            if ((info.Modifiers & ConsoleModifiers.Shift) != 0) {
                modifiers |= KeyModifiers.Shift;
            }
            char c = info.KeyChar;
            if (c >= ' ' && c != '\u007f') {
                bool altGr = (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == (KeyModifiers.Control | KeyModifiers.Alt);
                return InputEvent.Character(c, altGr ? KeyModifiers.None : modifiers);
            }
            return InputEvent.Press(info.Key, modifiers);
        }

        /// <inheritdoc />
        public void Dispose() {
            _unix?.Dispose();
        }
    }
}
