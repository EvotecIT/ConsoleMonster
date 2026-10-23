using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ConsoleMonster.Interop;
using ConsoleMonster.Output;

namespace ConsoleMonster.Input {
    /// <summary>
    /// Reads the Windows console input buffer with <c>ReadConsoleInputW</c>: every key with its modifiers (Ctrl and Alt
    /// combinations, Shift+Tab, function keys), Alt+numpad characters, AltGr characters, pastes (a burst of characters in one
    /// read), mouse button transitions, window resizes and focus changes. While it is open, Quick Edit, line input, echo
    /// and Ctrl+C processing are off, so mouse buttons and Ctrl+C arrive as events; the original mode is restored on disposal.
    /// </summary>
    public sealed partial class WindowsConsoleInput : IInputSource, IConsoleInputMode {
        private const int PasteThreshold = 3;
        private readonly IntPtr _handle;
        private readonly uint _originalMode;
        private readonly uint _rawMode;
        private readonly ConsoleInputRecord[] _buffer = new ConsoleInputRecord[256];
        private readonly Queue<InputEvent> _pending = new Queue<InputEvent>();
        private readonly List<InputEvent> _batch = new List<InputEvent>();
        private int _width;
        private int _height;
        private char _highSurrogate;
        private uint _mouseButtons;
        private bool _disposed;

        private WindowsConsoleInput(IntPtr handle, uint originalMode, uint rawMode) {
            _handle = handle;
            _originalMode = originalMode;
            _rawMode = rawMode;
            TerminalSize.TryGet(out _width, out _height);
        }

        /// <inheritdoc />
        public string Name => "windows console (ReadConsoleInputW)";

        /// <summary>Opens the console input when standard input is a console; returns null otherwise (redirected, no console).</summary>
        /// <returns>The source, or null.</returns>
        public static WindowsConsoleInput? TryOpen() {
            if (!WindowsConsole.IsWindows) {
                return null;
            }
            IntPtr handle = ConsoleInputNative.StandardInput;
            if (handle == IntPtr.Zero || handle == new IntPtr(-1) || !ConsoleInputNative.TryGetMode(handle, out uint mode)) {
                return null;
            }
            uint raw = (mode & ~(ConsoleInputNative.EnableProcessedInput | ConsoleInputNative.EnableLineInput | ConsoleInputNative.EnableEchoInput
                | ConsoleInputNative.EnableQuickEditMode | ConsoleInputNative.EnableVirtualTerminalInput))
                | ConsoleInputNative.EnableWindowInput | ConsoleInputNative.EnableMouseInput | ConsoleInputNative.EnableExtendedFlags;
            if (!ConsoleInputNative.TrySetMode(handle, raw)) {
                return null;
            }
            return new WindowsConsoleInput(handle, mode, raw);
        }

        /// <inheritdoc />
        public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(WindowsConsoleInput));
            }
            DateTime deadline = DateTime.UtcNow + (timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (_pending.Count > 0) {
                    return _pending.Dequeue();
                }
                CheckResize();
                if (_pending.Count > 0) {
                    return _pending.Dequeue();
                }
                int remaining = (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalMilliseconds);
                if (ConsoleInputNative.Wait(_handle, Math.Min(Math.Max(remaining, 0), 50))) {
                    // A signalled handle does not guarantee a record (pseudo consoles signal spuriously), and
                    // ReadConsoleInputW blocks until one arrives: read only what is queued.
                    if (ConsoleInputNative.PendingCount(_handle) > 0) {
                        int read = ConsoleInputNative.Read(_handle, _buffer);
                        if (read < 0) {
                            throw new InputClosedException("The console input could not be read.");
                        }
                        _batch.Clear();
                        Translate(_buffer, read, _batch, ref _highSurrogate, ref _mouseButtons);
                        foreach (InputEvent e in _batch) {
                            if (e.Kind == InputEventKind.Resize) {
                                CheckResize();
                            } else {
                                _pending.Enqueue(e);
                            }
                        }
                        if (_pending.Count > 0) {
                            continue;
                        }
                    } else if (remaining > 0) {
                        Thread.Sleep(Math.Min(remaining, 15));
                    }
                }
                if (remaining <= 0) {
                    return null;
                }
            }
        }
        /// <summary>
        /// Translates one independent batch of console input records. Key-up and modifier-only keys are skipped;
        /// three or more consecutive characters become a paste. Use the stateful overload for successive reads.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <param name="count">How many records are valid.</param>
        /// <param name="output">Receives the events.</param>
        /// <param name="highSurrogate">Carries a high surrogate between batches.</param>
        internal static void Translate(ConsoleInputRecord[] records, int count, List<InputEvent> output, ref char highSurrogate) {
            uint mouseButtons = 0;
            Translate(records, count, output, ref highSurrogate, ref mouseButtons);
        }

        /// <summary>Translates successive console reads, carrying surrogate and mouse button state between batches.</summary>
        /// <param name="records">The native records.</param>
        /// <param name="count">The number of valid records.</param>
        /// <param name="output">Receives normalized events in record order.</param>
        /// <param name="highSurrogate">Carries an unfinished surrogate pair.</param>
        /// <param name="mouseButtons">Carries the last observed supported mouse button state.</param>
        internal static void Translate(ConsoleInputRecord[] records, int count, List<InputEvent> output, ref char highSurrogate, ref uint mouseButtons) {
            List<InputEvent> keys = new List<InputEvent>(count);
            for (int i = 0; i < count; i++) {
                ConsoleInputRecord record = records[i];
                switch (record.EventType) {
                    case ConsoleInputNative.KeyEvent:
                        TranslateKey(record, keys, ref highSurrogate);
                        break;
                    case ConsoleInputNative.MouseEvent:
                        TranslateMouse(record, keys, ref mouseButtons);
                        break;
                    case ConsoleInputNative.WindowBufferSizeEvent:
                        keys.Add(InputEvent.Resized(record.SizeX, record.SizeY));
                        break;
                    case ConsoleInputNative.FocusEvent:
                        if (record.SetFocus == 0) mouseButtons = 0;
                        keys.Add(InputEvent.Focus(record.SetFocus != 0));
                        break;
                }
            }
            CoalescePastes(keys, output);
        }

        private static void TranslateKey(ConsoleInputRecord record, List<InputEvent> keys, ref char highSurrogate) {
            ushort vk = record.VirtualKeyCode;
            char c = (char)record.UnicodeChar;
            if (record.KeyDown == 0) {
                // Alt+numpad composition delivers the character on the Alt key release.
                if (vk == 0x12 && c != '\0') {
                    AddCharacter(c, KeyModifiers.None, keys, ref highSurrogate);
                }
                return;
            }
            if (IsModifierKey(vk)) {
                return;
            }
            uint state = record.ControlKeyState;
            KeyModifiers modifiers = KeyModifiers.None;
            bool ctrl = (state & (ConsoleInputNative.LeftCtrl | ConsoleInputNative.RightCtrl)) != 0;
            bool alt = (state & (ConsoleInputNative.LeftAlt | ConsoleInputNative.RightAlt)) != 0;
            if (ctrl) {
                modifiers |= KeyModifiers.Control;
            }
            if (alt) {
                modifiers |= KeyModifiers.Alt;
            }
            if ((state & ConsoleInputNative.ShiftPressed) != 0) {
                modifiers |= KeyModifiers.Shift;
            }
            int repeat = Math.Max(1, Math.Min((int)record.RepeatCount, 32));
            for (int r = 0; r < repeat; r++) {
                if (c >= ' ' && c != '\u007f') {
                    // AltGr is Ctrl+Alt on Windows: the character is what the user meant.
                    KeyModifiers charModifiers = ctrl && alt ? KeyModifiers.None : modifiers;
                    AddCharacter(c, charModifiers, keys, ref highSurrogate);
                    continue;
                }
                ConsoleKey key = (ConsoleKey)vk;
                switch (key) {
                    case ConsoleKey.Enter:
                    case ConsoleKey.Tab:
                    case ConsoleKey.Escape:
                    case ConsoleKey.Backspace:
                        keys.Add(InputEvent.Press(key, modifiers));
                        continue;
                }
                keys.Add(InputEvent.Press(key, modifiers));
            }
        }

        private static void AddCharacter(char c, KeyModifiers modifiers, List<InputEvent> keys, ref char highSurrogate) {
            if (char.IsHighSurrogate(c)) {
                highSurrogate = c;
                return;
            }
            if (char.IsLowSurrogate(c)) {
                if (highSurrogate != '\0') {
                    keys.Add(InputEvent.Paste(new string(new[] { highSurrogate, c })));
                }
                highSurrogate = '\0';
                return;
            }
            highSurrogate = '\0';
            keys.Add(InputEvent.Character(c, modifiers));
        }

        // A run of plain characters (with Enter or Tab inside it) read in one batch is a paste, not typing: nobody types three
        // keys within one read. A trailing Enter stays a key, so pasting a line and pressing Enter behaves like a shell.
        private static void CoalescePastes(List<InputEvent> keys, List<InputEvent> output) {
            int i = 0;
            while (i < keys.Count) {
                int end = i;
                int printable = 0;
                while (end < keys.Count && IsPasteable(keys[end])) {
                    if (keys[end].IsText) {
                        printable++;
                    }
                    end++;
                }
                int trimmed = end;
                while (trimmed > i && !keys[trimmed - 1].IsText) {
                    trimmed--;
                }
                if (printable >= PasteThreshold && trimmed - i >= PasteThreshold) {
                    StringBuilder text = new StringBuilder();
                    for (int k = i; k < trimmed; k++) {
                        text.Append(keys[k].Kind == InputEventKind.Paste ? keys[k].Text : keys[k].Char.ToString());
                    }
                    output.Add(InputEvent.Paste(text.ToString()));
                    for (int k = trimmed; k < end; k++) {
                        output.Add(keys[k]);
                    }
                    i = end;
                    continue;
                }
                if (end == i) {
                    output.Add(keys[i]);
                    i++;
                    continue;
                }
                for (int k = i; k < end; k++) {
                    output.Add(keys[k]);
                }
                i = end;
            }
        }

        private static bool IsPasteable(InputEvent e) {
            if (e.Kind == InputEventKind.Paste) {
                return true;
            }
            return e.IsText || (e.Modifiers == KeyModifiers.None && (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Tab));
        }

        private static bool IsModifierKey(ushort vk) {
            switch (vk) {
                case 0x10: // Shift
                case 0x11: // Ctrl
                case 0x12: // Alt
                case 0x14: // Caps Lock
                case 0x5B: // Left Windows
                case 0x5C: // Right Windows
                case 0x90: // Num Lock
                case 0x91: // Scroll Lock
                case 0xA0:
                case 0xA1:
                case 0xA2:
                case 0xA3:
                case 0xA4:
                case 0xA5:
                    return true;
            }
            return false;
        }

        private void CheckResize() {
            if (TerminalSize.TryGet(out int width, out int height) && width > 0 && height > 0 && (width != _width || height != _height)) {
                _width = width;
                _height = height;
                _pending.Enqueue(InputEvent.Resized(width, height));
            }
        }

        /// <summary>
        /// Gives the console back its normal input mode for a while (a live dashboard run, where Ctrl+C must reach the
        /// session as a signal); <see cref="Resume"/> switches back and drops keys typed meanwhile.
        /// </summary>
        public void Suspend() {
            ConsoleInputNative.TrySetMode(_handle, _originalMode);
        }

        /// <summary>Switches back to raw key input after <see cref="Suspend"/>, discarding keys typed meanwhile.</summary>
        public void Resume() {
            ConsoleInputNative.Flush(_handle);
            _pending.Clear();
            _mouseButtons = 0;
            ConsoleInputNative.TrySetMode(_handle, _rawMode);
        }

        /// <inheritdoc />
        public void Dispose() {
            if (_disposed) {
                return;
            }
            _disposed = true;
            ConsoleInputNative.TrySetMode(_handle, _originalMode);
        }
    }
}
