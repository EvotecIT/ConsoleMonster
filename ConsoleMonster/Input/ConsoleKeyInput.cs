using System;
using System.Threading;
using ConsoleMonster.Output;

namespace ConsoleMonster.Input {
    /// <summary>
    /// Reads keys through <see cref="Console.ReadKey(bool)"/>, which decodes the terminal's VT input on Linux and macOS.
    /// Used where the Windows console API is not available. Pastes arrive as single characters and focus changes are not
    /// reported; resizes are detected by polling the window size.
    /// </summary>
    public sealed class ConsoleKeyInput : IInputSource {
        private int _width;
        private int _height;

        /// <summary>Creates the reader.</summary>
        public ConsoleKeyInput() {
            TerminalSize.TryGet(out _width, out _height);
        }

        /// <inheritdoc />
        public string Name => "console keys (Console.ReadKey)";

        /// <inheritdoc />
        public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
            DateTime deadline = DateTime.UtcNow + (timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (TerminalSize.TryGet(out int width, out int height) && width > 0 && (width != _width || height != _height)) {
                    _width = width;
                    _height = height;
                    return InputEvent.Resized(width, height);
                }
                bool available;
                try {
                    available = Console.KeyAvailable;
                } catch (InvalidOperationException ex) {
                    throw new InputClosedException("Keys cannot be read: " + ex.Message, ex);
                }
                if (available) {
                    return FromKeyInfo(Console.ReadKey(true));
                }
                if (DateTime.UtcNow >= deadline) {
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
        }
    }
}
